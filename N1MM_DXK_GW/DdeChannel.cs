// SPDX-License-Identifier: GPL-3.0-or-later

using NDde;
using NDde.Client;

namespace N1MM_DXK_GW;

/// <summary>
/// One DDE client connection with automatic reconnect.
///
/// DXLab tools (DXKeeper, DXView, Pathfinder) each expose a DDE server.
/// LinkTopic in VB6 is "Service|Topic" — here split into separate ctor args.
/// Reconnect uses a thread-pool timer so the channel doesn't depend on a
/// WinForms message pump (NDde itself runs DDE on its own internal thread).
///
/// Threading:
///   - Connect() / Execute() may be called from any thread; NDde marshals
///     internally to its DDE worker.
///   - Connected/Disconnected events may fire on NDde's internal thread or
///     on the reconnect timer's pool thread. UI subscribers MUST marshal via
///     Control.BeginInvoke before touching controls.
/// </summary>
public sealed class DdeChannel : IDisposable
{
   private readonly string service;
   private readonly string topic;
   private readonly TimeSpan reconnectInterval;
   private readonly object stateLock = new();

   private DdeClient? client;
   private System.Threading.Timer? reconnectTimer;
   private bool disposed;
   private bool connected;

   public string Service => service;
   public string Topic => topic;
   public bool IsConnected
   {
      get { lock (stateLock) { return connected; } }
   }

   public event Action? Connected;
   public event Action? Disconnected;

   public DdeChannel(string service, string topic, TimeSpan? reconnectInterval = null)
   {
      this.service = service;
      this.topic = topic;
      this.reconnectInterval = reconnectInterval ?? TimeSpan.FromSeconds(5);
   }

   public void Start()
   {
      TryConnect();
   }

   public bool Execute(string command, int timeoutMs = 5000)
   {
      DdeClient? snapshot;
      lock (stateLock)
      {
         if (!connected || client == null)
         {
            return false;
         }
         snapshot = client;
      }

      try
      {
         snapshot.Execute(command, timeoutMs);
         return true;
      }
      catch (DdeException)
      {
         HandleConnectionLoss();
         return false;
      }
      catch (ObjectDisposedException)
      {
         HandleConnectionLoss();
         return false;
      }
   }

   private void TryConnect()
   {
      lock (stateLock)
      {
         if (disposed || connected)
         {
            return;
         }

         // Tear down any half-initialized prior client.
         CleanupClientLocked();

         try
         {
            var newClient = new DdeClient(service, topic);
            newClient.Disconnected += OnClientDisconnected;
            newClient.Connect();

            client = newClient;
            connected = true;
            StopReconnectTimerLocked();
         }
         catch (DdeException)
         {
            CleanupClientLocked();
            ScheduleReconnectLocked();
         }
         catch (InvalidOperationException)
         {
            // NDde occasionally surfaces internal STA issues this way; treat
            // identically to a DDE failure and let the reconnect timer retry.
            CleanupClientLocked();
            ScheduleReconnectLocked();
         }
      }

      // Fire events outside the lock to avoid holding it across user code.
      if (IsConnected)
      {
         Connected?.Invoke();
      }
      else
      {
         Disconnected?.Invoke();
      }
   }

   private void OnClientDisconnected(object? sender, DdeDisconnectedEventArgs e)
   {
      HandleConnectionLoss();
   }

   private void HandleConnectionLoss()
   {
      bool wasConnected;
      lock (stateLock)
      {
         wasConnected = connected;
         connected = false;
         CleanupClientLocked();
         if (!disposed)
         {
            ScheduleReconnectLocked();
         }
      }

      if (wasConnected)
      {
         Disconnected?.Invoke();
      }
   }

   private void ScheduleReconnectLocked()
   {
      if (disposed)
      {
         return;
      }
      reconnectTimer ??= new System.Threading.Timer(_ => TryConnect());
      reconnectTimer.Change(reconnectInterval, Timeout.InfiniteTimeSpan);
   }

   private void StopReconnectTimerLocked()
   {
      reconnectTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
   }

   private void CleanupClientLocked()
   {
      if (client != null)
      {
         try { client.Disconnected -= OnClientDisconnected; } catch { }
         try { client.Disconnect(); } catch { }
         try { client.Dispose(); } catch { }
         client = null;
      }
   }

   public void Dispose()
   {
      lock (stateLock)
      {
         if (disposed)
         {
            return;
         }
         disposed = true;
         StopReconnectTimerLocked();
         reconnectTimer?.Dispose();
         reconnectTimer = null;
         CleanupClientLocked();
         connected = false;
      }
   }
}