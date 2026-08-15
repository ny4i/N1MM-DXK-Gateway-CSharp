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

      // Deliberately NO SO_REUSEADDR here, and do not "restore" it.
      //
      // SO_REUSEADDR does not give two programs a shared copy of each
      // datagram. Windows delivers a given unicast datagram to exactly one
      // bound socket, so a second listener on this port does not duplicate
      // the stream — it steals an arbitrary share of it, and the QSOs that
      // land in the other process are simply never logged, with no error
      // anywhere. That failure is invisible until someone audits the log
      // against the contest, which is the worst possible way to lose a QSO.
      //
      // The exclusive bind is also the behaviour we want on startup: a
      // second copy of the gateway then fails loudly with "Address already
      // in use" instead of silently splitting traffic with the first.
      //
      // If several programs genuinely need N1MM's data, solve it at the
      // sender — N1MM Logger+ accepts multiple UDP destinations, so each
      // program gets its own copy on its own port — or add real multicast
      // (IP_ADD_MEMBERSHIP), which does duplicate to every member.
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
