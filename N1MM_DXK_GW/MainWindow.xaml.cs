using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

// Three assemblies define overlapping names here: WPF, WPF-UI, and WinForms
// (still referenced for NDde's hidden window and Screen). Pin the ones this
// file means rather than qualifying every use, so the choice is stated once.
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using TextBlock = System.Windows.Controls.TextBlock;

namespace N1MM_DXK_GW;

/// <summary>
/// The gateway's only window. Behaviour is a direct port of the WinForms
/// MainForm; the non-UI classes it drives are unchanged, so anything that was
/// verified against live N1MM and DXKeeper still holds.
/// </summary>
public partial class MainWindow : FluentWindow
{
   private const int OperationLogCap = 1000;
   private const int DequeueIntervalMs = 100;

   private readonly Logger logger = new();
   private readonly MessageDispatcher dispatcher = new();
   private readonly DxKeeperTcpClient tcpClient = new();
   private readonly FailedQsoStore failedQsos;
   private readonly QsoSendQueue sendQueue;
   private readonly DdeChannel dxkDde = new("DXKeeper", "DDEServer");
   private readonly DdeChannel dxvDde = new("DXView", "DDEServer");
   private readonly DdeChannel pfDde = new("Pathfinder", "DDEServer");
   private readonly DispatcherTimer dequeueTimer = new()
   {
      Interval = TimeSpan.FromMilliseconds(DequeueIntervalMs),
   };

   private readonly HashSet<string> reportedUnhandledTypes = new(StringComparer.OrdinalIgnoreCase);

   private Settings settings = new();
   private UdpListener? udpListener;

   // Suppresses the setting-changed handlers while loaded values are pushed
   // into the controls, so startup does not immediately re-save them.
   private bool loadingSettings;

   public MainWindow()
   {
      InitializeComponent();

      // Follow the operator's Windows light/dark setting, and repaint if they
      // change it while the gateway is running. A contest station is often run
      // in a dark shack; forcing either theme would be wrong for someone.
      SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, updateAccents: true);

      failedQsos = new FailedQsoStore(logger);
      failedQsos.RecordSaved += OnFailedQsoSaved;

      // Result callback runs on the send worker thread — see OnSendResult.
      sendQueue = new QsoSendQueue(tcpClient, OnSendResult);

      dequeueTimer.Tick += (_, _) => dispatcher.Drain();
      dispatcher.ContactInfoReceived += OnContactInfo;
      dispatcher.ContactReplaceReceived += OnContactReplace;
      dispatcher.LookupInfoReceived += OnLookupInfo;
      dispatcher.ContactDeleteReceived += OnContactDelete;
      dispatcher.InvalidMessageReceived += OnInvalidMessage;
      dispatcher.UnhandledMessageReceived += OnUnhandledMessage;
      dispatcher.DispatchFailed += OnDispatchFailed;

      dxkDde.Connected += () => OnDdeStatusChanged(dxkDde, DxkDot, DxkStatus);
      dxkDde.Disconnected += () => OnDdeStatusChanged(dxkDde, DxkDot, DxkStatus);
      dxvDde.Connected += () => OnDdeStatusChanged(dxvDde, DxvDot, DxvStatus);
      dxvDde.Disconnected += () => OnDdeStatusChanged(dxvDde, DxvDot, DxvStatus);
      pfDde.Connected += () => OnDdeStatusChanged(pfDde, PfDot, PfStatus);
      pfDde.Disconnected += () => OnDdeStatusChanged(pfDde, PfDot, PfStatus);

      Loaded += MainWindow_Loaded;
      Closing += MainWindow_Closing;
      Closed += MainWindow_Closed;
      Activated += (_, _) => RefreshFailedQsoStatus();
   }

   private void MainWindow_Loaded(object sender, RoutedEventArgs e)
   {
      settings = Settings.Load();
      logger.DebugEnabled = settings.DebugLogging;
      logger.LogWritten += OnLogWritten;

      ApplySettingsToUi();
      RefreshDxKeeperPortDisplay();
      // Normally hides itself — this run's file cannot exist yet.
      RefreshFailedQsoStatus();
      SetDdeDot(DxkDot, DxkStatus, connected: false);
      SetDdeDot(DxvDot, DxvStatus, connected: false);
      SetDdeDot(PfDot, PfStatus, connected: false);
      RestoreWindowPosition();
      SetTitleWithVersion();
      StartListenerOnConfiguredPort();
      dequeueTimer.Start();

      dxkDde.Start();
      dxvDde.Start();
      pfDde.Start();
   }

   private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
   {
      SaveWindowPosition();
   }

   private void MainWindow_Closed(object? sender, EventArgs e)
   {
      dequeueTimer.Stop();
      udpListener?.Dispose();
      udpListener = null;
      // Dispose before the DDE channels: it flushes any QSO it could not
      // deliver through OnSendResult, which needs the logger and file store.
      sendQueue.Dispose();
      dxkDde.Dispose();
      dxvDde.Dispose();
      pfDde.Dispose();
   }

   // ---------------------------------------------------------------- window

   private void SetTitleWithVersion()
   {
      var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
      if (v != null)
      {
         Title = $"{Strings.AppTitle} {v.Major}.{v.Minor}.{v.Build}";
      }
   }

   private void RestoreWindowPosition()
   {
      if (settings.WindowWidth == Settings.WindowPositionUnset ||
          settings.WindowHeight == Settings.WindowPositionUnset ||
          settings.WindowLeft == Settings.WindowPositionUnset ||
          settings.WindowTop == Settings.WindowPositionUnset)
      {
         return;
      }

      // Reject implausibly small sizes and offscreen rectangles — a monitor
      // disconnect can otherwise strand the window where it cannot be seen.
      if (settings.WindowWidth < MinWidth || settings.WindowHeight < MinHeight)
      {
         return;
      }

      var saved = new System.Drawing.Rectangle(
         settings.WindowLeft, settings.WindowTop, settings.WindowWidth, settings.WindowHeight);
      if (!System.Windows.Forms.Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(saved)))
      {
         return;
      }

      WindowStartupLocation = WindowStartupLocation.Manual;
      Left = settings.WindowLeft;
      Top = settings.WindowTop;
      Width = settings.WindowWidth;
      Height = settings.WindowHeight;

      // Don't restore Minimized — a window that launches hidden in the taskbar
      // is a poor start. Treat Minimized-at-close as "use Normal".
      if ((WindowState)settings.WindowState == WindowState.Maximized)
      {
         WindowState = WindowState.Maximized;
      }
   }

   private void SaveWindowPosition()
   {
      if (WindowState == WindowState.Minimized)
      {
         // Don't overwrite saved bounds when closed from the taskbar.
         settings.WindowState = (int)WindowState.Normal;
         settings.Save();
         return;
      }

      // RestoreBounds carries the pre-maximise rectangle, so un-maximising on
      // the next launch returns to the size the operator actually chose.
      var bounds = WindowState == WindowState.Maximized ? RestoreBounds
                                                        : new Rect(Left, Top, Width, Height);
      settings.WindowState = (int)(WindowState == WindowState.Maximized
         ? WindowState.Maximized
         : WindowState.Normal);

      settings.WindowLeft = (int)bounds.Left;
      settings.WindowTop = (int)bounds.Top;
      settings.WindowWidth = (int)bounds.Width;
      settings.WindowHeight = (int)bounds.Height;
      settings.Save();
   }

   // -------------------------------------------------------------- settings

   private void ApplySettingsToUi()
   {
      loadingSettings = true;
      try
      {
         UdpPortBox.Text = settings.UdpPort.ToString(CultureInfo.InvariantCulture);
         MulticastBox.Text = settings.MulticastGroup;
         DxkLookupCheck.IsChecked = settings.DxkLookup;
         CallbookCheck.IsChecked = settings.DxkCallbook;
         EqslCheck.IsChecked = settings.DxkEqslUpload;
         LotwCheck.IsChecked = settings.DxkLotwUpload;
         ClubLogCheck.IsChecked = settings.DxkClubLogUpload;
         VerboseLogCheck.IsChecked = settings.VerboseLogging;
         DebugLogCheck.IsChecked = settings.DebugLogging;
      }
      finally
      {
         loadingSettings = false;
      }
   }

   private void RefreshDxKeeperPortDisplay()
   {
      var info = DxKeeperTcpClient.GetDxKeeperBasePortInfo();
      var suffix = info.FromRegistry
         ? string.Empty
         : $" — assumed default; check DXKeeper: {Strings.DxKeeperConfigPath}";
      DxkPortValue.Text = $"{info.BasePort} (using TCP port {info.ServicePort}){suffix}";

      // The menu path is substituted rather than translated: DXKeeper's own
      // interface is English-only, so a translated path names a menu that does
      // not exist.
      var tip = string.Format(CultureInfo.CurrentCulture,
                              Strings.DxKeeperPortTip, Strings.DxKeeperConfigPath);
      DxkPortLabel.ToolTip = tip;
      DxkPortValue.ToolTip = tip;
   }

   private void SettingCheckChanged(object sender, RoutedEventArgs e)
   {
      if (loadingSettings)
      {
         return;
      }
      settings.DxkLookup = DxkLookupCheck.IsChecked == true;
      settings.DxkCallbook = CallbookCheck.IsChecked == true;
      settings.DxkEqslUpload = EqslCheck.IsChecked == true;
      settings.DxkLotwUpload = LotwCheck.IsChecked == true;
      settings.DxkClubLogUpload = ClubLogCheck.IsChecked == true;
      settings.VerboseLogging = VerboseLogCheck.IsChecked == true;
      settings.Save();
   }

   private void DebugLogCheck_Changed(object sender, RoutedEventArgs e)
   {
      if (loadingSettings)
      {
         return;
      }
      settings.DebugLogging = DebugLogCheck.IsChecked == true;
      logger.DebugEnabled = settings.DebugLogging;
      settings.Save();
      logger.Log($"Debug logging {(settings.DebugLogging ? "enabled" : "disabled")} by user");
   }

   // ------------------------------------------------------------- UDP port

   private void UdpPortBox_KeyDown(object sender, KeyEventArgs e)
   {
      if (e.Key != Key.Enter)
      {
         return;
      }
      // Commit on Enter as well as on losing focus. WPF does not ding like
      // WinForms did, but Enter still has to mean something.
      e.Handled = true;
      if (IsUdpPortTextValid())
      {
         ApplyUdpPortChange();
      }
   }

   private void UdpPortBox_LostFocus(object sender, RoutedEventArgs e)
   {
      if (IsUdpPortTextValid())
      {
         ApplyUdpPortChange();
      }
   }

   /// <summary>
   /// True if the box holds a usable port. On bad input it tells the operator,
   /// restores the last good value and selects it, so the field is never left
   /// showing something the gateway is not actually using.
   /// </summary>
   private bool IsUdpPortTextValid()
   {
      if (int.TryParse(UdpPortBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
          && n >= 1 && n <= 65535)
      {
         return true;
      }
      MessageBox.Show(this, "UDP port must be an integer between 1 and 65535.",
         Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
      UdpPortBox.Text = settings.UdpPort.ToString(CultureInfo.InvariantCulture);
      UdpPortBox.SelectAll();
      return false;
   }

   /// <summary>
   /// Rebinds the listener to the port now in the box. Safe to call more than
   /// once for the same value — Enter commits immediately and the focus change
   /// that follows re-enters here with nothing to do.
   /// </summary>
   private void ApplyUdpPortChange()
   {
      var newPort = int.Parse(UdpPortBox.Text, CultureInfo.InvariantCulture);
      if (newPort == settings.UdpPort)
      {
         return;
      }

      var oldPort = settings.UdpPort;
      udpListener?.Dispose();
      udpListener = null;

      settings.UdpPort = newPort;
      settings.Save();
      AppendLog($"UDP port changed to {newPort}, rebinding...");
      StartListenerOnConfiguredPort();

      if (udpListener == null)
      {
         // Bind failed — revert to the previous, known-working port.
         settings.UdpPort = oldPort;
         settings.Save();
         UdpPortBox.Text = oldPort.ToString(CultureInfo.InvariantCulture);
         AppendLog($"Reverted UDP port to {oldPort}.");
         StartListenerOnConfiguredPort();
      }
   }

   // ------------------------------------------------------------- multicast

   private void MulticastBox_KeyDown(object sender, KeyEventArgs e)
   {
      if (e.Key != Key.Enter)
      {
         return;
      }
      e.Handled = true;
      ApplyMulticastChange();
   }

   private void MulticastBox_LostFocus(object sender, RoutedEventArgs e) => ApplyMulticastChange();

   /// <summary>
   /// Applies an edited multicast group. Joining happens at bind time, so this
   /// rebinds the listener — the same path a port change takes.
   /// </summary>
   private void ApplyMulticastChange()
   {
      var entered = MulticastBox.Text.Trim();
      if (string.Equals(entered, settings.MulticastGroup, StringComparison.OrdinalIgnoreCase))
      {
         return;
      }

      // Reject bad input before tearing down a working listener.
      if (entered.Length > 0)
      {
         ParseMulticastGroup(entered, out var error);
         if (error != null)
         {
            MessageBox.Show(this,
               $"{error}.\n\nLeave this blank unless the sending program is configured to send to a multicast group.",
               Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            MulticastBox.Text = settings.MulticastGroup;
            MulticastBox.SelectAll();
            return;
         }
      }

      var previous = settings.MulticastGroup;
      udpListener?.Dispose();
      udpListener = null;

      settings.MulticastGroup = entered;
      settings.Save();
      AppendLog(entered.Length > 0
         ? $"Multicast group set to {entered}, rebinding..."
         : "Multicast disabled, rebinding...");
      StartListenerOnConfiguredPort();

      if (udpListener == null)
      {
         settings.MulticastGroup = previous;
         settings.Save();
         MulticastBox.Text = previous;
         AppendLog($"Reverted multicast group to '{previous}'.");
         StartListenerOnConfiguredPort();
      }
   }

   /// <summary>
   /// Parses the configured group. Returns null for "no multicast", and sets
   /// <paramref name="error"/> when the operator typed something that is not a
   /// usable group — the two must stay distinguishable so a typo is reported
   /// rather than silently treated as "disabled".
   /// </summary>
   private static IPAddress? ParseMulticastGroup(string configured, out string? error)
   {
      error = null;
      var text = configured?.Trim() ?? string.Empty;
      if (text.Length == 0)
      {
         return null;
      }
      if (!IPAddress.TryParse(text, out var address))
      {
         error = $"'{text}' is not a valid IP address";
         return null;
      }
      if (!UdpListener.IsIPv4Multicast(address))
      {
         error = $"'{text}' is not an IPv4 multicast address (224.0.0.0 - 239.255.255.255)";
         return null;
      }
      return address;
   }

   // -------------------------------------------------------------- listener

   private void StartListenerOnConfiguredPort()
   {
      var group = ParseMulticastGroup(settings.MulticastGroup, out var groupError);
      if (groupError != null)
      {
         // Don't silently fall back to no multicast — the operator configured a
         // group and would otherwise see a healthy listener receiving nothing.
         AppendLog($"ERROR: {groupError} — starting WITHOUT multicast.");
         logger.Log($"Multicast group rejected: {groupError}");
      }

      AppendLog($"UDP listener starting on port {settings.UdpPort}...");
      logger.DebugLog($"Binding UDP port {settings.UdpPort}"
                      + (group != null ? $", joining multicast group {group}" : string.Empty));

      var listener = new UdpListener(settings.UdpPort, OnUdpDatagram, group);
      try
      {
         listener.Start();
         udpListener = listener;

         if (listener.JoinedGroup != null)
         {
            AppendLog($"UDP listener bound to port {settings.UdpPort}, joined multicast group {listener.JoinedGroup}.");
            logger.Log($"Joined multicast group {listener.JoinedGroup} on port {settings.UdpPort}");
         }
         else
         {
            AppendLog($"UDP listener bound to port {settings.UdpPort}.");
         }
      }
      catch (Exception ex)
      {
         listener.Dispose();
         var msg = $"Failed to bind UDP port {settings.UdpPort}: {ex.Message}";
         AppendLog("ERROR: " + msg);
         logger.Log(msg);
         MessageBox.Show(this, msg, Strings.AppTitle,
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
   }

   private void OnUdpDatagram(string xml)
   {
      // Fires on UdpListener's background receive thread. Logger is
      // thread-safe, and DebugLog is cheap when debug is off.
      logger.DebugLog($"Received UDP on port {settings.UdpPort}: {xml}");
      dispatcher.Enqueue(xml);
   }

   // ------------------------------------------------------------- messages

   private void OnContactInfo(XElement root)
   {
      AdifBuilder.Result adif;
      try
      {
         adif = AdifBuilder.Build(root);
      }
      catch (Exception ex)
      {
         AppendLog($"ERROR building ADIF: {ex.Message}");
         logger.Log($"AdifBuilder threw: {ex}");
         return;
      }

      var queued = sendQueue.PendingCount;
      var backlog = queued > 0 ? $" ({queued} ahead of it)" : string.Empty;
      AppendLog($"contactinfo: {adif.Summary} -> sending to DXKeeper{backlog}...");
      logger.DebugLog($"ADIF: {adif.AdifRecord}");

      // Queued, not sent directly: QsoSendQueue serialises delivery so the
      // gateway paces itself to DXKeeper instead of overrunning it.
      sendQueue.EnqueueLog(adif, BuildLogOptions());
   }

   /// <summary>
   /// Per-QSO externallog flags, from the operator's checkboxes. Shared by the
   /// new-QSO path and the re-log half of a replace, so an edited QSO is
   /// uploaded and enriched exactly as the original was.
   /// </summary>
   private DxKeeperTcpClient.ExternalLogOptions BuildLogOptions() =>
      new()
      {
         UploadEqsl = settings.DxkEqslUpload,
         UploadLotw = settings.DxkLotwUpload,
         UploadClubLog = settings.DxkClubLogUpload,
         QueryCallbook = settings.DxkCallbook,
         DeduceMissing = true,
         UpdateEqslMembership = true,
         UpdateLotwMembership = true,
         CheckOverrides = true,
      };

   /// <summary>
   /// A QSO edited in N1MM. DXKeeper has no replace command, so this becomes
   /// delete-then-relog, queued as one inseparable operation.
   /// </summary>
   private void OnContactReplace(XElement root)
   {
      var key = AdifBuilder.BuildDeleteRecord(root, useOldIdentity: true);
      if (!key.IsValid)
      {
         // Without a usable pre-edit identity we cannot delete the right QSO,
         // and logging the edited copy alone would duplicate it.
         AppendLog($"contactreplace for {key.Call}: no usable <oldcall>/<oldtimestamp> — edit NOT applied to DXKeeper");
         logger.Log($"contactreplace ignored, cannot build delete key: {root}");
         return;
      }

      AdifBuilder.Result adif;
      try
      {
         adif = AdifBuilder.Build(root);
      }
      catch (Exception ex)
      {
         AppendLog($"ERROR building ADIF for contactreplace: {ex.Message}");
         logger.Log($"AdifBuilder threw on contactreplace: {ex}");
         return;
      }

      AppendLog($"contactreplace: {key.Summary} -> {adif.Summary} (delete then re-log)...");
      logger.DebugLog($"replace delete key: {key.AdifRecord}");
      logger.DebugLog($"replace new ADIF: {adif.AdifRecord}");

      sendQueue.EnqueueReplace(key, adif, BuildLogOptions());
   }

   /// <summary>
   /// A QSO deleted in N1MM. contactdelete carries far fewer fields than
   /// contactinfo — no mode, no frequency, no RST — but that does not matter:
   /// DXKeeper identifies a QSO by CALL + QSO_DATE + TIME_ON, all present.
   /// </summary>
   private void OnContactDelete(XElement root)
   {
      var key = AdifBuilder.BuildDeleteRecord(root, useOldIdentity: false);
      if (!key.IsValid)
      {
         AppendLog($"contactdelete for {key.Call}: missing call or timestamp — nothing sent to DXKeeper");
         logger.Log($"contactdelete ignored, cannot build delete key: {root}");
         return;
      }

      AppendLog($"contactdelete: {key.Summary} -> deleting from DXKeeper...");
      logger.DebugLog($"delete key: {key.AdifRecord}");
      sendQueue.EnqueueDelete(key);
   }

   private void OnLookupInfo(XElement root)
   {
      var call = XmlHelpers.GetValue(root, "call");
      if (string.IsNullOrWhiteSpace(call))
      {
         AppendLog("lookupinfo: missing <call>, ignoring");
         return;
      }

      var routed = new List<string>();

      // VB6 logic: if DXKLookup is checked AND DXKeeper is connected, ask only
      // DXKeeper. Otherwise fall back to DXView and Pathfinder when connected.
      // DXKeeper's "check" is preferred because it can mark the worked/needed
      // state in DXKeeper's own UI.
      if (settings.DxkLookup && dxkDde.IsConnected)
      {
         var cmd = DxLabWire.ServerPrefix(DxLabWire.LogServer) + "check" + call;
         if (dxkDde.Execute(cmd))
         {
            routed.Add("DXKeeper");
         }
      }
      else
      {
         if (dxvDde.IsConnected)
         {
            var cmd = DxLabWire.ServerPrefix(DxLabWire.DXViewServer) + "lookup" + call;
            if (dxvDde.Execute(cmd))
            {
               routed.Add("DXView");
            }
         }
         if (pfDde.IsConnected)
         {
            var fields = DxLabWire.EncodeField("callsign", call);
            var cmd = DxLabWire.ServerPrefix(DxLabWire.QSLInfoServer) + "getqslinfo" + fields;
            if (pfDde.Execute(cmd))
            {
               routed.Add("Pathfinder");
            }
         }
      }

      AppendLog(routed.Count > 0
         ? $"lookupinfo: {call} -> {string.Join(", ", routed)}"
         : $"lookupinfo: {call} (no DDE recipient available)");
   }

   private void OnUnhandledMessage(string messageType)
   {
      // A known N1MM message we don't act on — not an error, so nothing goes to
      // the operation log. Noted once per type per session at debug level:
      // N1MM broadcasts RadioInfo several times a second, and logging each one
      // would drown the entries that matter.
      if (reportedUnhandledTypes.Add(messageType))
      {
         logger.DebugLog($"Ignoring <{messageType}> messages — known N1MM type this gateway does not handle (noted once per session)");
      }
   }

   private void OnDispatchFailed(string xml, Exception fault)
   {
      // A defect, not bad input. Say so plainly and keep the full stack in the
      // error log — that is the evidence needed to fix it.
      AppendLog($"INTERNAL ERROR handling a message ({fault.GetType().Name}: {fault.Message}) — see ErrorLog.txt. The gateway is still running.");
      logger.Log($"Unhandled exception while dispatching a UDP message: {fault}");
      logger.Log($"   message body: {xml}");
   }

   private void OnInvalidMessage(string xml, string reason)
   {
      AppendLog($"INVALID: {reason}");
      logger.Log($"Invalid UDP message: {reason} -- body: {xml}");
   }

   // ------------------------------------------------------------ send results

   /// <summary>
   /// Called on the QsoSendQueue worker thread — including during shutdown,
   /// after the window may already be gone. Everything that must not be lost
   /// (the failed-QSO file, ErrorLog.txt) is written here directly; only the
   /// on-screen log is marshalled, and best-effort.
   /// </summary>
   private void OnSendResult(QsoSendQueue.OperationResult op)
   {
      var result = op.Send;
      var portTag = result.Port.HasValue ? $" (TCP {result.Port.Value})" : string.Empty;

      // Log both wire frames at debug level. A replace emits two, and the
      // delete is logged first because that is the order they went out in.
      if (op.DeleteSend?.WireFrame != null)
      {
         logger.DebugLog(
            $"Sent to DXKeeper{portTag} [replace 1/2, delete -> {op.DeleteSend.Outcome}]: {op.DeleteSend.WireFrame}");
      }
      if (result.WireFrame != null)
      {
         var stage = op.Kind == QsoSendQueue.OpKind.Replace ? " [replace 2/2, re-log]" : string.Empty;
         logger.DebugLog($"Sent to DXKeeper{portTag}{stage}: {result.WireFrame}");
      }

      if (result.Outcome == DxKeeperTcpClient.SendOutcome.Sent)
      {
         var verb = op.Kind switch
         {
            QsoSendQueue.OpKind.Delete => "deleted from DXKeeper:",
            QsoSendQueue.OpKind.Replace => string.Empty,
            _ => "logged QSO with",
         };
         PostToOperationLog($"{verb} {op.Summary}{portTag}".TrimStart());
         logger.DebugLog(
            $"DXKeeper closed the connection for {op.Call}; response: {result.Response ?? "(none)"}");
         return;
      }

      // Failed, Unconfirmed and Busy all mean the same to the operator:
      // DXKeeper did not confirm this. Never retry — DXKeeper does not detect
      // duplicates, so retrying something it had processed would duplicate it.
      var reason = DescribeFailure(result);

      if (op.DeletedButNotRelogged)
      {
         // The one case where DXKeeper is left worse off than before we
         // started: the original is gone and the replacement never arrived.
         var kept = op.PreserveAdif != null &&
                    failedQsos.Save(op.PreserveAdif, $"replace failed after delete succeeded — {reason} (QSO with {op.Call})");
         PostToOperationLog(kept
            ? $"*** {op.Call}: DXKeeper DELETED the original but did not log the edit{portTag} — the edited QSO is in {failedQsos.FileName}, import it. Reason: {reason}"
            : $"*** {op.Call}: DXKeeper DELETED the original, the edit was not logged{portTag}, AND it could not be saved to {failedQsos.FileName}. Reason: {reason}");
         logger.Log($"REPLACE LEFT DXKEEPER WITHOUT THE QSO — {op.Call}{portTag}: delete succeeded, externallog did not ({reason})");
         return;
      }

      var savedNote = string.Empty;
      if (op.PreserveAdif != null)
      {
         var saved = failedQsos.Save(op.PreserveAdif, $"{reason} ({op.Summary})");
         savedNote = saved
            ? $" — saved to {failedQsos.FileName}"
            : $" — AND it could not be saved to {failedQsos.FileName}";
      }

      PostToOperationLog($"DXKeeper did not confirm {op.Summary}{portTag}{savedNote}: {reason}");
      logger.Log($"{op.Kind} not confirmed for {op.Call}{portTag}: {reason}");
   }

   private static string DescribeFailure(DxKeeperTcpClient.SendResult result) =>
      result.Outcome switch
      {
         DxKeeperTcpClient.SendOutcome.Busy => "another send was already in flight",
         DxKeeperTcpClient.SendOutcome.Unconfirmed =>
            result.ErrorMessage ?? "DXKeeper never closed the connection",
         _ => result.ErrorMessage ?? "send failed",
      };

   // ------------------------------------------------------------------- DDE

   private void OnDdeStatusChanged(DdeChannel channel, System.Windows.Shapes.Ellipse dot, TextBlock status)
   {
      // Fires off the UI thread — marshal before touching controls.
      RunOnUi(() =>
      {
         var connected = channel.IsConnected;
         SetDdeDot(dot, status, connected);
         AppendVerboseLog($"{channel.Service} DDE: {(connected ? "connected" : "disconnected")}");
         logger.DebugLog($"{channel.Service}|{channel.Topic} DDE state: {(connected ? "connected" : "disconnected")}");
      });
   }

   private static void SetDdeDot(System.Windows.Shapes.Ellipse dot, TextBlock status, bool connected)
   {
      dot.Fill = connected
         ? new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43))
         : new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C));
      status.Text = connected ? Strings.Connected : Strings.Disconnected;
      status.Opacity = connected ? 1.0 : 0.7;
   }

   // ------------------------------------------------------------ failed QSOs

   private void OnFailedQsoSaved() => RunOnUi(RefreshFailedQsoStatus);

   /// <summary>
   /// Status line for QSOs this session could not deliver: a count plus links
   /// to the file and its folder, hidden entirely when nothing has failed.
   ///
   /// The count is read from the file every time rather than tracked in memory.
   /// Opening the file or folder does NOT clear it, because nothing we can
   /// observe tells us the operator actually imported the records. It falls to
   /// zero only when the file is gone — the one signal we can trust.
   /// </summary>
   private void RefreshFailedQsoStatus()
   {
      var count = failedQsos.RecordCount();
      var visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;

      FailedQsoText.Visibility = visibility;
      FailedQsoFileLink.Visibility = visibility;
      FailedQsoFolderLink.Visibility = visibility;

      if (count > 0)
      {
         FailedQsoText.Text = count == 1
            ? Strings.FailedQsoCountOne
            : string.Format(CultureInfo.CurrentCulture, Strings.FailedQsoCount, count);
         FailedQsoText.ToolTip = string.Format(CultureInfo.CurrentCulture,
                                               Strings.FailedQsoTip, failedQsos.FilePath);
      }
   }

   private void FailedQsoFileLink_Click(object sender, RoutedEventArgs e)
   {
      if (!failedQsos.Exists)
      {
         MessageBox.Show(this,
            $"No stranded QSOs this session.\n\n{failedQsos.FilePath} does not exist.",
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
         RefreshFailedQsoStatus();
         return;
      }
      try
      {
         System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
         {
            FileName = failedQsos.FilePath,
            UseShellExecute = true,
         });
      }
      catch (Exception ex)
      {
         // .adi has no default handler on many machines — say so usefully
         // rather than reporting a bare Win32 error.
         MessageBox.Show(this,
            $"Could not open {failedQsos.FileName}:\n\n{ex.Message}\n\nUse the folder link and import it into DXKeeper from there.",
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
      }
   }

   private void FailedQsoFolderLink_Click(object sender, RoutedEventArgs e)
   {
      try
      {
         // /select, highlights the file in Explorer so it can be dragged
         // straight into DXKeeper's import.
         var argument = failedQsos.Exists
            ? $"/select,\"{failedQsos.FilePath}\""
            : $"\"{System.IO.Path.GetDirectoryName(failedQsos.FilePath)}\"";
         System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
         {
            FileName = "explorer.exe",
            Arguments = argument,
            UseShellExecute = true,
         });
      }
      catch (Exception ex)
      {
         MessageBox.Show(this, $"Could not open the folder:\n\n{ex.Message}",
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
      }
   }

   // -------------------------------------------------------------- log & UI

   private void OnLogWritten() => RunOnUi(() => ErrorLogLink.Visibility = Visibility.Visible);

   private void ErrorLogLink_Click(object sender, RoutedEventArgs e) => OpenErrorLog();

   private void ShowErrorLogButton_Click(object sender, RoutedEventArgs e) => OpenErrorLog();

   private void OpenErrorLog()
   {
      if (!System.IO.File.Exists(logger.LogPath))
      {
         MessageBox.Show(this, $"No error log yet at:\n{logger.LogPath}",
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
         return;
      }
      try
      {
         System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
         {
            FileName = logger.LogPath,
            UseShellExecute = true,
         });
      }
      catch (Exception ex)
      {
         MessageBox.Show(this, $"Could not open ErrorLog.txt:\n\n{ex.Message}",
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
      }
   }

   private void HelpButton_Click(object sender, RoutedEventArgs e)
   {
      const string helpUrl = "https://github.com/ny4i/N1MM-DXK-Gateway-CSharp#readme";
      try
      {
         System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
         {
            FileName = helpUrl,
            UseShellExecute = true,
         });
      }
      catch (Exception ex)
      {
         MessageBox.Show(this, $"Could not open help page:\n\n{ex.Message}",
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
      }
   }

   /// <summary>
   /// Low-priority status events (e.g. DDE connect/disconnect). Suppressed
   /// unless "Verbose logging" is on, so the operation log stays focused on QSO
   /// traffic. Independent of "Log debugging information", which controls the
   /// volume of ErrorLog.txt.
   /// </summary>
   private void AppendVerboseLog(string line)
   {
      if (settings.VerboseLogging)
      {
         AppendLog(line);
      }
   }

   private void AppendLog(string line)
   {
      var stamped = $"{DateTime.Now:HH:mm:ss}  {line}";
      OperationLogList.Items.Add(stamped);
      while (OperationLogList.Items.Count > OperationLogCap)
      {
         OperationLogList.Items.RemoveAt(0);
      }
      OperationLogList.ScrollIntoView(OperationLogList.Items[^1]);
   }

   /// <summary>
   /// Append to the on-screen log from any thread. Gives up silently if the
   /// window is gone — the durable record is already in ErrorLog.txt.
   /// </summary>
   private void PostToOperationLog(string line) => RunOnUi(() => AppendLog(line));

   /// <summary>
   /// Marshals to the UI thread, tolerating a window that is closing or gone.
   /// Callbacks arrive from the UDP receive thread, the DDE channels and the
   /// send-queue worker, including during shutdown.
   /// </summary>
   private void RunOnUi(Action action)
   {
      try
      {
         if (Dispatcher.CheckAccess())
         {
            action();
         }
         else
         {
            Dispatcher.BeginInvoke(action);
         }
      }
      catch (TaskCanceledException) { }
      catch (InvalidOperationException) { }
   }
}
