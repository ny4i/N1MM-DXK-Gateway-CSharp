using System.Threading.Channels;

namespace N1MM_DXK_GW;

/// <summary>
/// Serialises QSO delivery to DXKeeper: one send at a time, each awaiting
/// DXKeeper's acknowledgement before the next begins.
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
/// The queue is unbounded. Bounding it would mean choosing which QSO to throw
/// away, and memory is not the constraint here — a pending QSO is a few
/// hundred bytes and DXKeeper drains in seconds.
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

   private readonly record struct Item(
      AdifBuilder.Result Adif,
      DxKeeperTcpClient.ExternalLogOptions Options);

   private readonly Channel<Item> channel =
      Channel.CreateUnbounded<Item>(new UnboundedChannelOptions
      {
         SingleReader = true,
         SingleWriter = false,
      });

   private readonly DxKeeperTcpClient client;
   private readonly Action<AdifBuilder.Result, DxKeeperTcpClient.SendResult> onResult;
   private readonly CancellationTokenSource cts = new();
   private readonly Task worker;
   private int pending;
   private bool disposed;

   public QsoSendQueue(
      DxKeeperTcpClient client,
      Action<AdifBuilder.Result, DxKeeperTcpClient.SendResult> onResult)
   {
      this.client = client;
      this.onResult = onResult;
      worker = Task.Run(RunAsync);
   }

   /// <summary>
   /// QSOs accepted but not yet reported on, including the one in flight.
   ///
   /// Counted here rather than via ChannelReader.Count: SingleReader = true
   /// selects SingleConsumerUnboundedChannel, whose reader does not implement
   /// Count and throws NotSupportedException. Reading it from the UI thread
   /// crashed the gateway on the first contactinfo. An explicit counter also
   /// keeps this independent of which channel implementation the options
   /// happen to select.
   /// </summary>
   public int PendingCount => Volatile.Read(ref pending);

   public void Enqueue(AdifBuilder.Result adif, DxKeeperTcpClient.ExternalLogOptions options)
   {
      // Increment before the write so the count can never read low; the
      // catch-all below undoes it if the write is refused.
      Interlocked.Increment(ref pending);

      if (!channel.Writer.TryWrite(new Item(adif, options)))
      {
         // Only reachable once the writer is completed (i.e. during shutdown).
         // Report rather than drop — the caller persists it.
         Report(adif, new DxKeeperTcpClient.SendResult
         {
            Outcome = DxKeeperTcpClient.SendOutcome.Failed,
            ErrorMessage = "Gateway is shutting down; QSO was not sent.",
         });
      }
   }

   private async Task RunAsync()
   {
      try
      {
         await foreach (var item in channel.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
         {
            DxKeeperTcpClient.SendResult result;
            try
            {
               result = await client
                  .ExternalLogAsync(item.Adif.AdifRecord, item.Options, cts.Token)
                  .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
               // ExternalLogAsync already catches broadly; this is belt and
               // braces so one bad send can never kill the worker and strand
               // every QSO behind it.
               result = new DxKeeperTcpClient.SendResult
               {
                  Outcome = DxKeeperTcpClient.SendOutcome.Failed,
                  ErrorMessage = ex.Message,
               };
            }

            Report(item.Adif, result);
         }
      }
      catch (OperationCanceledException)
      {
         // Shutdown grace expired mid-send. Dispose flushes whatever is left.
      }
   }

   /// <summary>
   /// The single exit point for a queued QSO. Every item is reported exactly
   /// once — delivered, failed, refused at enqueue, or flushed at shutdown —
   /// so decrementing here keeps PendingCount in step with the queue.
   /// </summary>
   private void Report(AdifBuilder.Result adif, DxKeeperTcpClient.SendResult result)
   {
      Interlocked.Decrement(ref pending);
      try
      {
         onResult(adif, result);
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
      // finish the QSO it is on and drain the rest.
      channel.Writer.TryComplete();

      if (!worker.Wait(ShutdownGrace))
      {
         cts.Cancel();
         // Don't wait again — the in-flight send is abandoned deliberately and
         // its QSO is flushed below.
      }

      // Anything the worker never got to is reported as undelivered so the
      // caller can persist it. Nothing is discarded silently.
      while (channel.Reader.TryRead(out var item))
      {
         Report(item.Adif, new DxKeeperTcpClient.SendResult
         {
            Outcome = DxKeeperTcpClient.SendOutcome.Failed,
            ErrorMessage = "Gateway closed before this QSO could be sent to DXKeeper.",
         });
      }

      cts.Dispose();
   }
}
