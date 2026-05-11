using System.Net;
using System.Net.Sockets;
using System.Text;

namespace N1MM_DXK_GW;

public sealed class UdpListener : IDisposable
{
   private readonly int port;
   private readonly Action<string> onMessage;
   private UdpClient? client;
   private CancellationTokenSource? cts;
   private Task? receiveTask;

   public UdpListener(int port, Action<string> onMessage)
   {
      this.port = port;
      this.onMessage = onMessage;
   }

   public void Start()
   {
      if (client != null)
      {
         throw new InvalidOperationException("UdpListener already started.");
      }

      var udp = new UdpClient();
      // SO_REUSEADDR must be set BEFORE Bind on Windows for port sharing
      // to take effect. Allows other apps on this machine to also bind
      // the same port (e.g. another N1MM consumer running side-by-side).
      udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
      client = udp;

      cts = new CancellationTokenSource();
      receiveTask = Task.Run(() => ReceiveLoopAsync(cts.Token));
   }

   private async Task ReceiveLoopAsync(CancellationToken token)
   {
      while (!token.IsCancellationRequested)
      {
         try
         {
            var result = await client!.ReceiveAsync(token).ConfigureAwait(false);

            // KB Q260018: Winsock UDP fires a spurious 1-byte wakeup when
            // a prior send received an ICMP Port Unreachable. Discard.
            if (result.Buffer.Length <= 1)
            {
               continue;
            }

            onMessage(Encoding.UTF8.GetString(result.Buffer));
         }
         catch (OperationCanceledException)
         {
            break;
         }
         catch (ObjectDisposedException)
         {
            break;
         }
         catch (SocketException ex)
         {
            System.Diagnostics.Debug.WriteLine($"UdpListener socket error: {ex.SocketErrorCode} {ex.Message}");
         }
      }
   }

   public void Stop()
   {
      cts?.Cancel();
      client?.Close();

      try
      {
         receiveTask?.Wait(TimeSpan.FromSeconds(1));
      }
      catch (AggregateException)
      {
         // Receive task surfaces cancellation as AggregateException; ignore.
      }

      client?.Dispose();
      cts?.Dispose();
      client = null;
      cts = null;
      receiveTask = null;
   }

   public void Dispose() => Stop();
}
