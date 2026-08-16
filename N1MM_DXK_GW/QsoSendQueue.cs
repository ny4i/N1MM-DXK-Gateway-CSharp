// SPDX-License-Identifier: GPL-3.0-or-later

using System.Threading.Channels;

namespace N1MM_DXK_GW;

/// <summary>
/// Serialises DXKeeper operations: one command in flight at a time, each
/// awaiting DXKeeper's acknowledgement before the next begins.
///
/// This restores a property the VB6 gateway got for free. There, HandleData
/// called TCPClientModule.SendCommand synchronously, so the message queue
/// paced itself to DXKeeper. A direct port to fire-and-forget async loses
/// that: the dispatcher drains its queue in a tight loop, several sends start
/// concurrently, and DxKeeperTcpClient's single-in-flight guard rejects all
/// but the first — turning a burst into discarded QSOs. Since DXKeeper takes
/// seconds per QSO (callbook and award work behind an internal DDE queue),
/// that is the normal case during a contest run, not an edge case.
///
/// A Replace is ONE operation, not two queued commands: DXKeeper has no
/// atomic replace, so an edit becomes delete-then-relog, and the relog is
/// sent only if the delete was acknowledged. The order is forced — the QSO
/// identity is CALL + QSO_DATE + TIME_ON, which an edit to (say) the name or
/// mode leaves untouched, so relog-first would leave two identical records
/// and the delete would remove an arbitrary one, or both. Keeping the pair
/// inseparable in the queue also stops another QSO's command interleaving
/// between them.
///
/// The delete-succeeded / relog-failed window cannot be closed against a
/// DXKeeper with no atomic replace. It is reported loudly and the new record
/// preserved, never swallowed.
///
/// ORDERING RELIES ON DXKEEPER'S FIFO QUEUE. An acknowledgement means
/// DXKeeper accepted and ENQUEUED the command, not that it executed it.
/// Measured from DXKeeper's own log during a replace: it enqueued the delete
/// at 05:23:58.353 and the re-log at 05:23:58.378, but only finished the
/// delete at 05:24:05.573 and parsed the re-log at 05:24:05.809 — seven
/// seconds later, and in the order sent. So waiting for the delete's
/// acknowledgement does not mean the QSO is gone yet; the re-log lands
/// behind it in DXKeeper's internal DDE queue and is applied after. Were
/// DXKeeper ever to drain that queue concurrently, delete and re-log could
/// interleave and this design would need revisiting.
///
/// The queue is unbounded. Bounding it would mean choosing which QSO to throw
/// away, and memory is not the constraint here.
///
/// Results are reported on the worker thread, never the UI thread. The
/// callback must be safe to invoke while the form is closing.
/// </summary>
public sealed class QsoSendQueue : IDisposable
{
   /// <summary>
   /// How long Dispose waits for in-flight and queued work before giving up.
   /// Kept short so closing the app stays responsive; anything still pending
   /// is handed back through the result callback as undelivered, so the
   /// no-silent-loss invariant holds either way.
   /// </summary>
   private static readonly TimeSpan ShutdownGrace = TimeSpan.FromSeconds(5);

   public enum OpKind
   {
      Log,
      Delete,
      Replace,
   }

   public sealed class OperationResult
   {
      public OpKind Kind { get; init; }
      public string Call { get; init; } = string.Empty;

      /// <summary>Human-readable description of what was attempted.</summary>
      public string Summary { get; init; } = string.Empty;

      /// <summary>Outcome of the command that decided this operation.</summary>
      public DxKeeperTcpClient.SendResult Send { get; init; } = new();

      /// <summary>
      /// For a Replace, the outcome of the delete half. A Replace puts two
      /// commands on the wire and only the second decides the result, so
      /// without this the delete is invisible in the log and "did the delete
      /// actually go out?" is unanswerable after the fact. Null for other
      /// kinds, and for a Replace abandoned before the delete was attempted.
      /// </summary>
      public DxKeeperTcpClient.SendResult? DeleteSend { get; init; }

      /// <summary>
      /// ADIF to preserve in FailedQSOs.adi when the operation did not
      /// succeed. Null when preserving would do harm — notably a Replace
      /// whose delete failed, where DXKeeper still holds the original and
      /// importing the edited copy would duplicate the QSO.
      /// </summary>
      public string? PreserveAdif { get; init; }

      /// <summary>
      /// The dangerous case: the delete was acknowledged but the relog was
      /// not, so DXKeeper has lost the QSO entirely. Always reported at the
      /// top of the operator's attention, never merely logged.
      /// </summary>
      public bool DeletedButNotRelogged { get; init; }
   }

   private sealed class Op
   {
      public OpKind Kind { get; init; }
      public AdifBuilder.DeleteKey? Delete { get; init; }
      public AdifBuilder.Result? Log { get; init; }
      public DxKeeperTcpClient.ExternalLogOptions? Options { get; init; }
   }

   private readonly Channel<Op> channel =
      Channel.CreateUnbounded<Op>(new UnboundedChannelOptions
      {
         SingleReader = true,
         SingleWriter = false,
      });

   private readonly DxKeeperTcpClient client;
   private readonly Action<OperationResult> onResult;
   private readonly CancellationTokenSource cts = new();
   private readonly Task worker;
   private int pending;
   private bool disposed;

   public QsoSendQueue(DxKeeperTcpClient client, Action<OperationResult> onResult)
   {
      this.client = client;
      this.onResult = onResult;
      worker = Task.Run(RunAsync);
   }

   /// <summary>
   /// Operations accepted but not yet reported on, including the one in
   /// flight.
   ///
   /// Counted here rather than via ChannelReader.Count: SingleReader = true
   /// selects SingleConsumerUnboundedChannel, whose reader does not implement
   /// Count and throws NotSupportedException. Reading it from the UI thread
   /// crashed the gateway on the first contactinfo.
   /// </summary>
   public int PendingCount => Volatile.Read(ref pending);

   public void EnqueueLog(AdifBuilder.Result adif, DxKeeperTcpClient.ExternalLogOptions options) =>
      Enqueue(new Op { Kind = OpKind.Log, Log = adif, Options = options });

   public void EnqueueDelete(AdifBuilder.DeleteKey key) =>
      Enqueue(new Op { Kind = OpKind.Delete, Delete = key });

   public void EnqueueReplace(
      AdifBuilder.DeleteKey key,
      AdifBuilder.Result adif,
      DxKeeperTcpClient.ExternalLogOptions options) =>
      Enqueue(new Op { Kind = OpKind.Replace, Delete = key, Log = adif, Options = options });

   private void Enqueue(Op op)
   {
      // Increment before the write so the count can never read low; the
      // rejection path below undoes it if the write is refused.
      Interlocked.Increment(ref pending);

      if (!channel.Writer.TryWrite(op))
      {
         // Only reachable once the writer is completed (i.e. during shutdown).
         // Report rather than drop — the caller preserves it.
         Report(op, Abandoned("Gateway is shutting down; nothing was sent."));
      }
   }

   private async Task RunAsync()
   {
      try
      {
         await foreach (var op in channel.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
         {
            OperationResult result;
            try
            {
               result = await ExecuteAsync(op).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
               // Belt and braces: one bad operation must never kill the worker
               // and strand every QSO behind it.
               result = Describe(op, new DxKeeperTcpClient.SendResult
               {
                  Outcome = DxKeeperTcpClient.SendOutcome.Failed,
                  Failure = DxKeeperTcpClient.SendFailure.Exception,
                  ErrorMessage = ex.Message,
               });
            }

            Interlocked.Decrement(ref pending);
            Deliver(result);
         }
      }
      catch (OperationCanceledException)
      {
         // Shutdown grace expired mid-send. Dispose flushes whatever is left.
      }
   }

   private async Task<OperationResult> ExecuteAsync(Op op)
   {
      if (op.Kind == OpKind.Log)
      {
         var sent = await client.ExternalLogAsync(op.Log!.AdifRecord, op.Options!, cts.Token)
                                .ConfigureAwait(false);
         return Describe(op, sent);
      }

      if (op.Kind == OpKind.Delete)
      {
         var deleted = await client.DeleteQsoAsync(op.Delete!.AdifRecord, cts.Token)
                                   .ConfigureAwait(false);
         return Describe(op, deleted);
      }

      // Replace: delete first, and relog only on an acknowledged delete.
      var deleteResult = await client.DeleteQsoAsync(op.Delete!.AdifRecord, cts.Token)
                                     .ConfigureAwait(false);

      if (deleteResult.Outcome != DxKeeperTcpClient.SendOutcome.Sent)
      {
         // The original is still in DXKeeper. Do NOT preserve the edited copy:
         // importing it later would sit alongside the original as a duplicate,
         // and DXKeeper does not detect duplicates.
         return new OperationResult
         {
            Kind = OpKind.Replace,
            Call = op.Log!.Call,
            Summary = $"edit of {op.Delete.Summary} — delete stage failed, DXKeeper still holds the original",
            Send = deleteResult,
            DeleteSend = deleteResult,
            PreserveAdif = null,
         };
      }

      var relogResult = await client.ExternalLogAsync(op.Log!.AdifRecord, op.Options!, cts.Token)
                                    .ConfigureAwait(false);

      if (relogResult.Outcome != DxKeeperTcpClient.SendOutcome.Sent)
      {
         return new OperationResult
         {
            Kind = OpKind.Replace,
            Call = op.Log.Call,
            Summary = $"edit of {op.Delete.Summary} — original DELETED but the edited QSO was not logged",
            Send = relogResult,
            DeleteSend = deleteResult,
            PreserveAdif = op.Log.AdifRecord,
            DeletedButNotRelogged = true,
         };
      }

      return new OperationResult
      {
         Kind = OpKind.Replace,
         Call = op.Log.Call,
         Summary = $"replaced {op.Delete.Summary} with {op.Log.Summary}",
         Send = relogResult,
         DeleteSend = deleteResult,
      };
   }

   private static OperationResult Describe(Op op, DxKeeperTcpClient.SendResult send) =>
      op.Kind switch
      {
         OpKind.Log => new OperationResult
         {
            Kind = OpKind.Log,
            Call = op.Log!.Call,
            Summary = op.Log.Summary,
            Send = send,
            PreserveAdif = op.Log.AdifRecord,
         },
         OpKind.Delete => new OperationResult
         {
            Kind = OpKind.Delete,
            Call = op.Delete!.Call,
            Summary = $"delete of {op.Delete.Summary}",
            Send = send,
            // Nothing to preserve: the operator asked for this QSO to go away.
            PreserveAdif = null,
         },
         _ => new OperationResult
         {
            Kind = OpKind.Replace,
            Call = op.Log?.Call ?? op.Delete?.Call ?? string.Empty,
            Summary = $"edit of {op.Delete?.Summary}",
            Send = send,
            PreserveAdif = null,
         },
      };

   private static DxKeeperTcpClient.SendResult Abandoned(string why) =>
      new()
      {
         Outcome = DxKeeperTcpClient.SendOutcome.Failed,
         Failure = DxKeeperTcpClient.SendFailure.ShuttingDown,
         ErrorMessage = why,
      };

   private void Report(Op op, DxKeeperTcpClient.SendResult send)
   {
      Interlocked.Decrement(ref pending);
      Deliver(Describe(op, send));
   }

   private void Deliver(OperationResult result)
   {
      try
      {
         onResult(result);
      }
      catch
      {
         // A throwing subscriber must not take down the send worker.
      }
   }

   public void Dispose()
   {
      if (disposed)
      {
         return;
      }
      disposed = true;

      // Stop accepting new work, then give the worker a bounded window to
      // finish the operation it is on and drain the rest.
      channel.Writer.TryComplete();

      if (!worker.Wait(ShutdownGrace))
      {
         cts.Cancel();
         // Don't wait again — the in-flight command is abandoned deliberately
         // and its record is flushed below.
      }

      // Anything the worker never reached is reported as undelivered so the
      // caller can preserve it. Nothing is discarded silently.
      while (channel.Reader.TryRead(out var op))
      {
         Report(op, Abandoned("Gateway closed before this operation could be sent to DXKeeper."));
      }

      cts.Dispose();
   }
}