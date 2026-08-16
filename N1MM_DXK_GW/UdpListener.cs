// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace N1MM_DXK_GW;

public sealed class UdpListener : IDisposable
{
   private readonly int port;
   private readonly Action<string> onMessage;
   private readonly IPAddress? multicastGroup;
   private UdpClient? client;
   private CancellationTokenSource? cts;
   private Task? receiveTask;

   /// <summary>Group actually joined, for the caller to report. Null if none.</summary>
   public IPAddress? JoinedGroup { get; private set; }

   /// <param name="multicastGroup">
   /// Optional IPv4 multicast group to join after binding. Null or a
   /// non-multicast address means unicast and broadcast only.
   /// </param>
   public UdpListener(int port, Action<string> onMessage, IPAddress? multicastGroup = null)
   {
      this.port = port;
      this.onMessage = onMessage;
      this.multicastGroup = multicastGroup;
   }

   /// <summary>
   /// True for the IPv4 multicast range 224.0.0.0 - 239.255.255.255.
   /// Used to validate operator input before we try to join, since joining a
   /// non-multicast address throws.
   /// </summary>
   public static bool IsIPv4Multicast(IPAddress address)
   {
      if (address.AddressFamily != AddressFamily.InterNetwork)
      {
         return false;
      }
      var first = address.GetAddressBytes()[0];
      return first >= 224 && first <= 239;
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

      JoinMulticastIfRequested(udp);

      cts = new CancellationTokenSource();
      receiveTask = Task.Run(() => ReceiveLoopAsync(cts.Token));
   }

   /// <summary>
   /// Joins the configured group, after Bind — the socket must be bound first
   /// or the join fails.
   ///
   /// The join is additive: the socket keeps receiving unicast and broadcast
   /// on this port as well, so enabling multicast never costs the operator the
   /// traffic they were already getting.
   ///
   /// ONE INTERFACE, DELIBERATELY. This joins on the interface the routing
   /// table selects, not on every interface. Joining on all of them would be
   /// more forgiving on a multi-homed machine, but if the sender's datagrams
   /// then arrived on two interfaces the gateway would receive each QSO twice
   /// and log it twice — and DXKeeper does not detect duplicates. A silent
   /// duplicate is far worse than receiving nothing: nothing is obvious within
   /// seconds, whereas duplicates are found later, by hand, in the log.
   ///
   /// The failure mode this accepts: on a machine whose default route is not
   /// the radio LAN, the join succeeds but no traffic arrives. That is why the
   /// joined group is reported to the operator rather than joined silently.
   /// A per-interface setting is the fix if it ever bites.
   /// </summary>
   private void JoinMulticastIfRequested(UdpClient udp)
   {
      if (multicastGroup == null)
      {
         return;
      }

      if (!IsIPv4Multicast(multicastGroup))
      {
         // Caller should have validated; refuse rather than throw from Start.
         throw new ArgumentException(
            $"{multicastGroup} is not an IPv4 multicast address (224.0.0.0 - 239.255.255.255).");
      }

      udp.JoinMulticastGroup(multicastGroup);
      JoinedGroup = multicastGroup;
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
      // Closing the socket drops any multicast membership with it; clearing
      // this keeps the reported state honest after a restart on a new port.
      JoinedGroup = null;
   }

   public void Dispose() => Stop();
}