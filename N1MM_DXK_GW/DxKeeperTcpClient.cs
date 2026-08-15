using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;

namespace N1MM_DXK_GW;

/// <summary>
/// Sends "externallog" commands to DXKeeper over TCP. DXKeeper exposes its
/// TCP server on (ServiceBasePort + 1) where ServiceBasePort is read from
/// DXKeeper's own registry key. Default DXKeeper TCP service port = 52001.
///
/// Wire format (matches the VB6 TCPClientModule and the DXKeeper protocol
/// shared across the DXLab Suite):
///     &lt;command:11&gt;externallog&lt;parameters:N&gt;&lt;ExternalLogADIF:M&gt;...
/// Each parameter is encoded with EncodeField: &lt;name:len&gt;value
/// Boolean parameters are encoded as "Y" (true) or "" (false).
///
/// Single in-flight send only: a second call while one is pending returns
/// SendResult.Busy. Matches the VB6 SendCommandInProgressFlag semantics so
/// the gateway never opens overlapping TCP sessions to DXKeeper. Callers
/// should serialise upstream (see <see cref="QsoSendQueue"/>) so Busy is a
/// backstop rather than a routine outcome.
///
/// DELIVERY SEMANTICS — do not "optimise" these away. Established by
/// measurement against DXKeeper on 2026-08-15, not by reading documentation:
///
///  * DXKeeper acknowledges externallog by CLOSING THE CONNECTION and by
///    nothing else. It sends no reply body.
///  * A successful write proves only that the local TCP stack accepted the
///    bytes. Windows completes the three-way handshake into the listen
///    backlog, so a connection DXKeeper has not yet accepted still reports
///    Connected and still accepts a write. Closing at that point destroys
///    the command with no error at either end — measured at 5 of 20 and
///    then 12 of 20 QSOs silently lost.
///  * DXKeeper can be seconds behind: it queues each TCP command onto an
///    internal DDE queue and drains it while doing callbook and award work
///    per QSO. Lags of 4.5 s appear in its own log.
///
/// Therefore a QSO counts as delivered only when we observe the peer close
/// (a zero-length read). If that never arrives within PeerCloseTimeout the
/// outcome is Unconfirmed, which the caller must treat as NOT delivered.
/// </summary>
public sealed class DxKeeperTcpClient
{
   private const string DxkRegistryPath =
      @"Software\VB and VBA Program Settings\DXKeeper\TCPServer";
   private const int DxkDefaultBasePort = 52000;
   private const int DxkPortOffset = 1;
   private const string DxkHost = "127.0.0.1";
   private const string ExternalLogCommand = "externallog";
   private const string DeleteQsoCommand = "deleteqso";

   private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
   private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(10);

   // How long to wait for DXKeeper's close, which is the only acknowledgement
   // it gives. Must comfortably exceed DXKeeper's observed internal queue lag
   // (4.5 s measured); the VB6 client waited 10 s. A shorter wait turns a busy
   // DXKeeper into a stream of Unconfirmed QSOs.
   private static readonly TimeSpan PeerCloseTimeout = TimeSpan.FromSeconds(10);

   // Surfaced so the UI can quote the same numbers in a translated sentence
   // rather than restating them and drifting out of step with the timeouts.
   public static int ConnectTimeoutSeconds => (int)ConnectTimeout.TotalSeconds;
   public static int PeerCloseTimeoutSeconds => (int)PeerCloseTimeout.TotalSeconds;

   private int sendInProgress; // 0 = idle, 1 = in flight

   public enum SendOutcome
   {
      /// <summary>DXKeeper closed the connection — the QSO is logged.</summary>
      Sent,

      /// <summary>Another send was in flight; nothing was transmitted.</summary>
      Busy,

      /// <summary>Connect or write failed; the QSO was not delivered.</summary>
      Failed,

      /// <summary>
      /// Bytes were written but DXKeeper never closed the connection within
      /// PeerCloseTimeout. Treat as NOT delivered: the command may still be
      /// sitting in an unaccepted backlog and will be destroyed on close.
      /// </summary>
      Unconfirmed,
   }

   /// <summary>
   /// What went wrong, as a value rather than as prose.
   ///
   /// This exists because <see cref="SendResult.ErrorMessage"/> has two
   /// audiences that want different things. It is written verbatim to
   /// ErrorLog.txt, which is the file an operator pastes into a support thread
   /// and which must therefore stay English; but the operator also needs to
   /// read the reason on screen, in their own language. Localising the one
   /// string would have translated the support artefact.
   ///
   /// So the transport says what happened and the UI decides how to say it:
   /// ErrorMessage stays English for the log, and MainWindow maps this enum to
   /// a translated sentence for the operation log. Adding a failure mode means
   /// adding a case here and a string in Strings.resx, which is exactly the
   /// pair that should move together.
   /// </summary>
   public enum SendFailure
   {
      None,
      ConnectTimeout,
      ConnectRefused,
      PeerCloseTimeout,

      /// <summary>Never handed to the transport — the queue was closing.</summary>
      ShuttingDown,

      /// <summary>Anything else; ErrorMessage carries the detail.</summary>
      Exception,
   }

   public sealed class SendResult
   {
      public SendOutcome Outcome { get; init; }
      public SendFailure Failure { get; init; }
      public string? Response { get; init; }

      /// <summary>
      /// English, always. Goes to ErrorLog.txt verbatim. Use
      /// <see cref="Failure"/> to decide what to show the operator.
      /// </summary>
      public string? ErrorMessage { get; init; }
      // The exact bytes (as a string) we wrote to the socket — surfaced so
      // the UI's debug log can show the user the on-wire frame for diagnosis.
      public string? WireFrame { get; init; }
      public int? Port { get; init; }
   }

   public sealed class ExternalLogOptions
   {
      public bool UploadEqsl { get; init; }
      public bool UploadLotw { get; init; }
      public bool UploadClubLog { get; init; }
      public bool DeduceMissing { get; init; } = true;
      public bool QueryCallbook { get; init; }
      public bool UpdateEqslMembership { get; init; } = true;
      public bool UpdateLotwMembership { get; init; } = true;
      public bool CheckOverrides { get; init; } = true;
   }

   /// <summary>Logs a QSO. The ADIF is wrapped in an ExternalLogADIF field.</summary>
   public Task<SendResult> ExternalLogAsync(
      string adifRecord,
      ExternalLogOptions options,
      CancellationToken cancel = default) =>
      SendCommandAsync(ExternalLogCommand, BuildExternalLogParameters(adifRecord, options), cancel);

   /// <summary>
   /// Deletes a QSO. DXKeeper identifies it by CALL + QSO_DATE + TIME_ON and
   /// ignores any other field, so <paramref name="deleteAdif"/> carries just
   /// those three plus &lt;EOR&gt;.
   ///
   /// Note the asymmetry with externallog: deleteqso's parameters are the raw
   /// ADIF fields, NOT wrapped in &lt;ExternalLogADIF:N&gt;. Wrapping them
   /// makes DXKeeper match nothing. (Confirmed against TR4W's
   /// BuildDXKeeperDeleteMessage, where the wrapping lines are commented out.)
   /// </summary>
   public Task<SendResult> DeleteQsoAsync(
      string deleteAdif,
      CancellationToken cancel = default) =>
      SendCommandAsync(DeleteQsoCommand, deleteAdif, cancel);

   private async Task<SendResult> SendCommandAsync(
      string command,
      string parameters,
      CancellationToken cancel = default)
   {
      if (Interlocked.CompareExchange(ref sendInProgress, 1, 0) != 0)
      {
         return new SendResult { Outcome = SendOutcome.Busy };
      }

      try
      {
         var frame = BuildFrame(command, parameters);
         var port = GetDxKeeperServicePort();

         using var client = new TcpClient();
         using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
         connectCts.CancelAfter(ConnectTimeout);

         try
         {
            await client.ConnectAsync(DxkHost, port, connectCts.Token).ConfigureAwait(false);
         }
         catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
         {
            return new SendResult
            {
               Outcome = SendOutcome.Failed,
               Failure = SendFailure.ConnectTimeout,
               ErrorMessage = $"Connection to DXKeeper at {DxkHost}:{port} timed out after {ConnectTimeout.TotalSeconds:0}s.",
               WireFrame = frame,
               Port = port,
            };
         }
         catch (SocketException ex)
         {
            return new SendResult
            {
               Outcome = SendOutcome.Failed,
               Failure = SendFailure.ConnectRefused,
               // Name the panel the operator can check, not the registry key
               // this port was read from — that heading also reports whether
               // the service is actually listening.
               ErrorMessage = $"Cannot connect to DXKeeper at {DxkHost}:{port} — {ex.Message}. "
                              + "In DXKeeper, check Config > Defaults tab > Network Service shows \"Listening\".",
               WireFrame = frame,
               Port = port,
            };
         }

         var stream = client.GetStream();
         stream.WriteTimeout = (int)SendTimeout.TotalMilliseconds;

         var payload = Encoding.ASCII.GetBytes(frame);
         await stream.WriteAsync(payload, cancel).ConfigureAwait(false);
         await stream.FlushAsync(cancel).ConfigureAwait(false);

         // No half-close here. The command frame is self-delimiting (both
         // <command:N> and <parameters:N> carry explicit lengths), so DXKeeper
         // does not need a FIN to know the request is complete, and the VB6
         // client — the implementation these semantics were measured against —
         // has no way to half-close a Winsock control and delivers reliably
         // without one. Sending FIN to a connection still sitting in the
         // listen backlog is untested behaviour we have no reason to risk.
         //
         // Wait for DXKeeper to close. That close IS the acknowledgement.
         var peer = await WaitForPeerCloseAsync(stream, cancel).ConfigureAwait(false);

         if (!peer.SawClose)
         {
            return new SendResult
            {
               Outcome = SendOutcome.Unconfirmed,
               Failure = SendFailure.PeerCloseTimeout,
               Response = peer.Text,
               ErrorMessage =
                  $"DXKeeper did not close the connection within {PeerCloseTimeout.TotalSeconds:0}s. " +
                  "The QSO may never have been accepted — treating it as undelivered.",
               WireFrame = frame,
               Port = port,
            };
         }

         return new SendResult
         {
            Outcome = SendOutcome.Sent,
            Response = peer.Text,
            WireFrame = frame,
            Port = port,
         };
      }
      catch (Exception ex)
      {
         return new SendResult
         {
            Outcome = SendOutcome.Failed,
            Failure = SendFailure.Exception,
            ErrorMessage = ex.Message,
         };
      }
      finally
      {
         Interlocked.Exchange(ref sendInProgress, 0);
      }
   }

   public int GetDxKeeperServicePort() => GetDxKeeperBasePort() + DxkPortOffset;

   public static int GetDxKeeperBasePort() => GetDxKeeperBasePortInfo().BasePort;

   public readonly record struct BasePortInfo(int BasePort, int ServicePort, bool FromRegistry);

   public static BasePortInfo GetDxKeeperBasePortInfo()
   {
      // VB6 stores port as a REG_SZ string. Read defensively and fall back
      // to the documented default 52000 if missing or malformed.
      try
      {
         using var key = Registry.CurrentUser.OpenSubKey(DxkRegistryPath, writable: false);
         if (key?.GetValue("ServiceBasePort") is string s &&
             int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
         {
            return new BasePortInfo(port, port + DxkPortOffset, FromRegistry: true);
         }
      }
      catch
      {
         // Treat any registry access failure as "use default".
      }
      return new BasePortInfo(DxkDefaultBasePort, DxkDefaultBasePort + DxkPortOffset, FromRegistry: false);
   }

   private static string BuildFrame(string command, string parameters)
   {
      // Length prefixes use character count, matching VB6 Len(). Safe for
      // the ASCII-only payloads we emit; any future Unicode field values
      // would need explicit byte-length encoding to stay protocol-correct.
      return $"<command:{command.Length}>{command}<parameters:{parameters.Length}>{parameters}";
   }

   private static string BuildExternalLogParameters(string adif, ExternalLogOptions o)
   {
      var sb = new StringBuilder();
      sb.Append(DxLabWire.EncodeField("ExternalLogADIF", adif));
      sb.Append(DxLabWire.EncodeField("UploadeQSL", BoolY(o.UploadEqsl)));
      sb.Append(DxLabWire.EncodeField("UploadLoTW", BoolY(o.UploadLotw)));
      sb.Append(DxLabWire.EncodeField("DeduceMissing", BoolY(o.DeduceMissing)));
      sb.Append(DxLabWire.EncodeField("QueryCallbook", BoolY(o.QueryCallbook)));
      sb.Append(DxLabWire.EncodeField("UpdateeQSL", BoolY(o.UpdateEqslMembership)));
      sb.Append(DxLabWire.EncodeField("UpdateLoTW", BoolY(o.UpdateLotwMembership)));
      sb.Append(DxLabWire.EncodeField("CheckOverrides", BoolY(o.CheckOverrides)));
      sb.Append(DxLabWire.EncodeField("UploadClubLog", BoolY(o.UploadClubLog)));
      return sb.ToString();
   }

   private static string BoolY(bool value) => value ? "Y" : string.Empty;

   private readonly record struct PeerCloseResult(bool SawClose, string? Text);

   private static async Task<PeerCloseResult> WaitForPeerCloseAsync(
      NetworkStream stream, CancellationToken cancel)
   {
      // Reads until the peer closes (Read returns 0) or PeerCloseTimeout
      // elapses. The zero-length read is the delivery signal; any bytes
      // received on the way are captured only for the debug log, since
      // externallog has no reply body.
      using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
      readCts.CancelAfter(PeerCloseTimeout);

      var sb = new StringBuilder();
      var buffer = new byte[1024];
      try
      {
         while (true)
         {
            var read = await stream.ReadAsync(buffer, readCts.Token).ConfigureAwait(false);
            if (read == 0)
            {
               // Clean EOF — DXKeeper accepted, processed, and closed.
               return new PeerCloseResult(true, sb.Length > 0 ? sb.ToString() : null);
            }
            sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
         }
      }
      catch (OperationCanceledException)
      {
         // Timed out. NOT benign: the connection may still be unaccepted in
         // DXKeeper's listen backlog, in which case our Dispose destroys the
         // command. Report it as unconfirmed so the caller preserves the QSO.
      }
      catch (IOException)
      {
         // Peer reset, or the connection died mid-wait. Either way we never
         // observed a clean close, so we cannot claim delivery.
      }
      catch (ObjectDisposedException)
      {
         // Socket torn down underneath us — same conclusion.
      }
      return new PeerCloseResult(false, sb.Length > 0 ? sb.ToString() : null);
   }
}
