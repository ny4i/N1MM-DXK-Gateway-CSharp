using System.Globalization;
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
/// the gateway never opens overlapping TCP sessions to DXKeeper.
/// </summary>
public sealed class DxKeeperTcpClient
{
   private const string DxkRegistryPath =
      @"Software\VB and VBA Program Settings\DXKeeper\TCPServer";
   private const int DxkDefaultBasePort = 52000;
   private const int DxkPortOffset = 1;
   private const string DxkHost = "127.0.0.1";
   private const string ExternalLogCommand = "externallog";

   private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
   private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(10);
   private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(2);

   private int sendInProgress; // 0 = idle, 1 = in flight

   public enum SendOutcome
   {
      Sent,
      Busy,
      Failed,
   }

   public sealed class SendResult
   {
      public SendOutcome Outcome { get; init; }
      public string? Response { get; init; }
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

   public async Task<SendResult> ExternalLogAsync(
      string adifRecord,
      ExternalLogOptions options,
      CancellationToken cancel = default)
   {
      if (Interlocked.CompareExchange(ref sendInProgress, 1, 0) != 0)
      {
         return new SendResult { Outcome = SendOutcome.Busy };
      }

      try
      {
         var parameters = BuildExternalLogParameters(adifRecord, options);
         var frame = BuildFrame(ExternalLogCommand, parameters);
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
               ErrorMessage = $"Cannot connect to DXKeeper at {DxkHost}:{port} — {ex.Message}. Is DXKeeper running with TCP enabled?",
               WireFrame = frame,
               Port = port,
            };
         }

         var stream = client.GetStream();
         stream.WriteTimeout = (int)SendTimeout.TotalMilliseconds;

         var payload = Encoding.ASCII.GetBytes(frame);
         await stream.WriteAsync(payload, cancel).ConfigureAwait(false);
         await stream.FlushAsync(cancel).ConfigureAwait(false);

         // Graceful half-close: send FIN so DXKeeper knows our request is
         // complete. Without this, DXKeeper's accept loop has no in-band
         // signal that we're done writing — and there are documented VB6
         // failures where DXKeeper was left in a non-listening state by
         // clients that didn't terminate cleanly.
         try
         {
            client.Client.Shutdown(SocketShutdown.Send);
         }
         catch (SocketException)
         {
            // Peer already closed — half-close not applicable; proceed to read.
         }

         // Read until the peer closes (Read returns 0) or our timeout
         // elapses. This both surfaces any response DXKeeper emits and
         // completes the four-way close handshake before Dispose.
         var response = await ReadUntilEofAsync(stream, cancel).ConfigureAwait(false);

         return new SendResult
         {
            Outcome = SendOutcome.Sent,
            Response = response,
            WireFrame = frame,
            Port = port,
         };
      }
      catch (Exception ex)
      {
         return new SendResult { Outcome = SendOutcome.Failed, ErrorMessage = ex.Message };
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

   private static async Task<string?> ReadUntilEofAsync(NetworkStream stream, CancellationToken cancel)
   {
      // Loops until the peer half-closes (Read returns 0) or our timeout
      // elapses. Receiving EOF is the explicit signal that DXKeeper has
      // finished — we wait for it (up to ReadTimeout) before disposing so
      // the four-way FIN/ACK handshake completes cleanly.
      using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
      readCts.CancelAfter(ReadTimeout);

      var sb = new StringBuilder();
      var buffer = new byte[1024];
      try
      {
         while (true)
         {
            var read = await stream.ReadAsync(buffer, readCts.Token).ConfigureAwait(false);
            if (read == 0)
            {
               break; // clean EOF — peer closed its send side
            }
            sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
         }
      }
      catch (OperationCanceledException)
      {
         // Timed out waiting for peer's FIN. Not fatal — Dispose will RST
         // and DXKeeper has already received our half-close FIN, so it
         // won't be stranded waiting for input.
      }
      catch (Exception)
      {
         // Any other I/O error — give up and let Dispose finish the teardown.
      }
      return sb.Length > 0 ? sb.ToString() : null;
   }
}
