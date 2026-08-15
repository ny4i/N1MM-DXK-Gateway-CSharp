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

      // SO_REUSEADDR, set before Bind because Windows only honours it then.
      //
      // Whether this shares the stream or splits it depends entirely on how
      // the datagram is addressed, which is why it is easy to get wrong:
      //
      //   unicast   - delivered to exactly ONE bound socket. A second
      //               listener would steal an arbitrary share of the stream
      //               and those QSOs would never be logged, silently.
      //   broadcast - delivered to EVERY socket bound to the port.
      //   multicast - delivered to every socket bound to the port that has
      //               also joined the group.
      //
      // Measured on this network (two sockets, one port, SO_REUSEADDR set):
      // a subnet broadcast reached 2 of 2; a unicast reached 1 of 2. And a
      // capture of N1MM Logger+ shows it sending to 192.168.x.255 — subnet
      // broadcast, not unicast. TR4W's UDP BROADCAST ADDRESS is likewise a
      // user-set destination, commonly a broadcast address.
      //
      // So for the traffic this gateway actually receives, SO_REUSEADDR lets
      // it coexist with other consumers on the same port, each getting a full
      // copy. Without it, the gateway takes the port exclusively and every
      // other consumer on this machine fails to bind.
      //
      // What it costs: the exclusive bind used to be a backstop against a
      // second copy of the gateway running and double-logging every QSO.
      // That job now rests entirely on the single-instance mutex in
      // Program.cs. The mutex is per-session, so two different Windows
      // sessions on one machine could still both run a gateway and both log
      // the same broadcast QSOs to DXKeeper.
      //
      // NOTE: receiving a MULTICAST stream needs more than this — the socket
      // must also join the group (IP_ADD_MEMBERSHIP / JoinMulticastGroup).
      // That is not done here because there is no configured group address.
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
