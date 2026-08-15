using System.Globalization;
using System.Xml.Linq;

namespace N1MM_DXK_GW;

public partial class MainForm : Form
{
   private const int OperationLogCap = 1000;
   private const int DequeueIntervalMs = 100;

   private readonly ToolTip toolTip;
   private readonly Logger logger = new();
   private readonly MessageDispatcher dispatcher = new();
   private readonly DxKeeperTcpClient tcpClient = new();
   private readonly FailedQsoStore failedQsos;
   private readonly QsoSendQueue sendQueue;
   private readonly DdeChannel dxkDde = new("DXKeeper", "DDEServer");
   private readonly DdeChannel dxvDde = new("DXView", "DDEServer");
   private readonly DdeChannel pfDde = new("Pathfinder", "DDEServer");
   private readonly System.Windows.Forms.Timer dequeueTimer = new() { Interval = DequeueIntervalMs };
   private Settings settings = new();
   private UdpListener? udpListener;

   public MainForm()
   {
      InitializeComponent();
      toolTip = new ToolTip(components!);
      AttachToolTips();

      failedQsos = new FailedQsoStore(logger);
      // Result callback runs on the send worker thread — see OnSendResult.
      sendQueue = new QsoSendQueue(tcpClient, OnSendResult);

      showErrorLogButton.Click += (_, _) => OpenErrorLog();
      errorLogLink.LinkClicked += (_, _) => OpenErrorLog();
      helpButton.Click += HelpButton_Click;

      udpPortTextBox.Validating += UdpPortTextBox_Validating;
      udpPortTextBox.Validated += UdpPortTextBox_Validated;
      udpPortTextBox.KeyDown += UdpPortTextBox_KeyDown;

      dxkLookupCheck.CheckedChanged += SettingCheckChanged;
      callbookCheck.CheckedChanged += SettingCheckChanged;
      eqslCheck.CheckedChanged += SettingCheckChanged;
      lotwCheck.CheckedChanged += SettingCheckChanged;
      clubLogCheck.CheckedChanged += SettingCheckChanged;
      verboseLoggingCheck.CheckedChanged += SettingCheckChanged;
      logDebugInfoCheck.CheckedChanged += LogDebugInfoCheck_CheckedChanged;

      dequeueTimer.Tick += (_, _) => dispatcher.Drain();
      dispatcher.ContactInfoReceived += OnContactInfo;
      dispatcher.LookupInfoReceived += OnLookupInfo;
      dispatcher.ContactDeleteReceived += OnContactDelete;
      dispatcher.InvalidMessageReceived += OnInvalidMessage;
      dispatcher.UnhandledMessageReceived += OnUnhandledMessage;
      dispatcher.DispatchFailed += OnDispatchFailed;

      dxkDde.Connected += () => OnDdeStatusChanged(dxkDde, dxkDot, dxkStatusLabel);
      dxkDde.Disconnected += () => OnDdeStatusChanged(dxkDde, dxkDot, dxkStatusLabel);
      dxvDde.Connected += () => OnDdeStatusChanged(dxvDde, dxvDot, dxvStatusLabel);
      dxvDde.Disconnected += () => OnDdeStatusChanged(dxvDde, dxvDot, dxvStatusLabel);
      pfDde.Connected += () => OnDdeStatusChanged(pfDde, pfDot, pfStatusLabel);
      pfDde.Disconnected += () => OnDdeStatusChanged(pfDde, pfDot, pfStatusLabel);
   }

   protected override void OnLoad(EventArgs e)
   {
      base.OnLoad(e);

      settings = Settings.Load();
      logger.DebugEnabled = settings.DebugLogging;
      logger.LogWritten += OnLogWritten;

      ApplySettingsToUi();
      RefreshDxKeeperPortDisplay();
      RestoreWindowPosition();
      SetTitleWithVersion();
      StartListenerOnConfiguredPort();
      dequeueTimer.Start();

      dxkDde.Start();
      dxvDde.Start();
      pfDde.Start();
   }

   protected override void OnFormClosing(FormClosingEventArgs e)
   {
      SaveWindowPosition();
      base.OnFormClosing(e);
   }

   protected override void OnFormClosed(FormClosedEventArgs e)
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
      base.OnFormClosed(e);
   }

   private void RefreshDxKeeperPortDisplay()
   {
      var info = DxKeeperTcpClient.GetDxKeeperBasePortInfo();
      var suffix = info.FromRegistry ? string.Empty : " — default, DXKeeper not detected in registry";
      dxkPortValue.Text = $"{info.BasePort} (using TCP port {info.ServicePort}){suffix}";
   }

   private void SetTitleWithVersion()
   {
      var asm = System.Reflection.Assembly.GetExecutingAssembly();
      var v = asm.GetName().Version;
      if (v != null)
      {
         Text = $"N1MM-DXKeeper Gateway {v.Major}.{v.Minor}.{v.Build} (C# port)";
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

      var saved = new Rectangle(settings.WindowLeft, settings.WindowTop,
                                 settings.WindowWidth, settings.WindowHeight);

      // Reject implausibly small sizes and offscreen rectangles (monitor
      // disconnect can otherwise strand the window where the user can't see it).
      if (saved.Width < MinimumSize.Width || saved.Height < MinimumSize.Height)
      {
         return;
      }
      if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(saved)))
      {
         return;
      }

      StartPosition = FormStartPosition.Manual;
      Bounds = saved;

      var saved_state = (FormWindowState)settings.WindowState;
      if (saved_state == FormWindowState.Maximized)
      {
         WindowState = FormWindowState.Maximized;
      }
      // Don't restore Minimized — that would be a poor UX (window launches
      // hidden in the taskbar). Treat Minimized-at-close as "use Normal".
   }

   private void SaveWindowPosition()
   {
      // When maximized, capture the underlying Normal-state bounds via
      // RestoreBounds so the next launch can return to that size when
      // un-maximized. When minimized, ignore — we don't want to persist
      // an "off-screen" position.
      Rectangle bounds;
      if (WindowState == FormWindowState.Minimized)
      {
         // Don't overwrite saved bounds when the user closes from the taskbar.
         settings.WindowState = (int)FormWindowState.Normal;
         settings.Save();
         return;
      }
      else if (WindowState == FormWindowState.Maximized)
      {
         bounds = RestoreBounds;
         settings.WindowState = (int)FormWindowState.Maximized;
      }
      else
      {
         bounds = Bounds;
         settings.WindowState = (int)FormWindowState.Normal;
      }

      settings.WindowLeft = bounds.Left;
      settings.WindowTop = bounds.Top;
      settings.WindowWidth = bounds.Width;
      settings.WindowHeight = bounds.Height;
      settings.Save();
   }

   private void ApplySettingsToUi()
   {
      // Detach change handlers before pushing loaded values into controls so
      // we don't immediately re-save the same values back to the registry.
      var checks = new[] { dxkLookupCheck, callbookCheck, eqslCheck, lotwCheck, clubLogCheck, verboseLoggingCheck };
      foreach (var cb in checks)
      {
         cb.CheckedChanged -= SettingCheckChanged;
      }
      logDebugInfoCheck.CheckedChanged -= LogDebugInfoCheck_CheckedChanged;

      udpPortTextBox.Text = settings.UdpPort.ToString(CultureInfo.InvariantCulture);
      dxkLookupCheck.Checked = settings.DxkLookup;
      callbookCheck.Checked = settings.DxkCallbook;
      eqslCheck.Checked = settings.DxkEqslUpload;
      lotwCheck.Checked = settings.DxkLotwUpload;
      clubLogCheck.Checked = settings.DxkClubLogUpload;
      verboseLoggingCheck.Checked = settings.VerboseLogging;
      logDebugInfoCheck.Checked = settings.DebugLogging;

      foreach (var cb in checks)
      {
         cb.CheckedChanged += SettingCheckChanged;
      }
      logDebugInfoCheck.CheckedChanged += LogDebugInfoCheck_CheckedChanged;
   }

   private void StartListenerOnConfiguredPort()
   {
      AppendLog($"UDP listener starting on port {settings.UdpPort}...");
      logger.DebugLog($"Binding UDP port {settings.UdpPort}");

      var listener = new UdpListener(settings.UdpPort, OnUdpDatagram);
      try
      {
         listener.Start();
         udpListener = listener;
         AppendLog($"UDP listener bound to port {settings.UdpPort}.");
      }
      catch (Exception ex)
      {
         listener.Dispose();
         var msg = $"Failed to bind UDP port {settings.UdpPort}: {ex.Message}";
         AppendLog("ERROR: " + msg);
         logger.Log(msg);
         MessageBox.Show(this, msg, "N1MM-DXKeeper Gateway",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
   }

   private void OnUdpDatagram(string xml)
   {
      // Fires on UdpListener's background receive thread. Logger is
      // thread-safe (it locks internally). DebugLog is cheap when debug
      // is off (early-return on the flag), so always-call is fine.
      logger.DebugLog($"Received UDP on port {settings.UdpPort}: {xml}");
      dispatcher.Enqueue(xml);
   }

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

      var options = new DxKeeperTcpClient.ExternalLogOptions
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

      // Queued, not sent directly: QsoSendQueue serialises delivery so the
      // gateway paces itself to DXKeeper instead of overrunning it.
      sendQueue.Enqueue(adif, options);
   }

   /// <summary>
   /// Called on the QsoSendQueue worker thread — including during shutdown,
   /// after the form's handle may already be gone. Everything that must not be
   /// lost (the failed-QSO file, ErrorLog.txt) is written here directly; only
   /// the on-screen operation log is marshalled, and best-effort.
   /// </summary>
   private void OnSendResult(AdifBuilder.Result adif, DxKeeperTcpClient.SendResult result)
   {
      var portTag = result.Port.HasValue ? $" (TCP {result.Port.Value})" : string.Empty;

      // Always log the wire frame at debug level — pairs with the ADIF and
      // UDP-receive debug lines to give a full round-trip trace.
      if (result.WireFrame != null)
      {
         logger.DebugLog($"Sent to DXKeeper{portTag}: {result.WireFrame}");
      }

      string uiLine;
      switch (result.Outcome)
      {
         case DxKeeperTcpClient.SendOutcome.Sent:
            uiLine = $"logged QSO with {adif.Summary}{portTag}";
            logger.DebugLog(
               $"DXKeeper closed the connection for {adif.Call}; response: {result.Response ?? "(none)"}");
            break;

         default:
            // Failed, Unconfirmed, and Busy all mean the same thing to the
            // operator: DXKeeper did not confirm this QSO. Preserve it and
            // never retry — DXKeeper does not detect duplicates, so a retry
            // of a QSO it had in fact processed would duplicate the record.
            var reason = DescribeFailure(result);
            var saved = failedQsos.Save(adif.AdifRecord, $"{reason} (QSO with {adif.Call})");
            uiLine = saved
               ? $"DXKeeper did not confirm QSO with {adif.Call}{portTag} — saved to {FailedQsoStore.FileName}: {reason}"
               : $"LOST QSO with {adif.Call}{portTag} — {reason}, and it could not be saved to {FailedQsoStore.FileName}";
            logger.Log($"externallog not confirmed for {adif.Call}{portTag}: {reason}");
            break;
      }

      PostToOperationLog(uiLine);
   }

   private static string DescribeFailure(DxKeeperTcpClient.SendResult result) =>
      result.Outcome switch
      {
         DxKeeperTcpClient.SendOutcome.Busy =>
            "another send was already in flight",
         DxKeeperTcpClient.SendOutcome.Unconfirmed =>
            result.ErrorMessage ?? "DXKeeper never closed the connection",
         _ => result.ErrorMessage ?? "send failed",
      };

   /// <summary>
   /// Append a line to the on-screen log from any thread. Silently gives up if
   /// the form is gone — the durable record is already in ErrorLog.txt.
   /// </summary>
   private void PostToOperationLog(string line)
   {
      if (IsDisposed || !IsHandleCreated)
      {
         return;
      }
      try
      {
         if (InvokeRequired)
         {
            BeginInvoke(() => AppendLog(line));
         }
         else
         {
            AppendLog(line);
         }
      }
      catch (ObjectDisposedException) { }
      catch (InvalidOperationException) { }
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

      // VB6 logic: if DXKLookup is checked AND DXKeeper is connected, only
      // ask DXKeeper. Otherwise broadcast to DXView and Pathfinder (when each
      // is connected). DXKeeper's "check" is preferred because it can mark
      // the worked/needed state in DXKeeper's UI directly.
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

      if (routed.Count > 0)
      {
         AppendLog($"lookupinfo: {call} -> {string.Join(", ", routed)}");
      }
      else
      {
         AppendLog($"lookupinfo: {call} (no DDE recipient available)");
      }
   }

   private void OnDdeStatusChanged(DdeChannel channel, Label dot, Label statusLabel)
   {
      // Fires off the UI thread — marshal before touching controls.
      if (IsDisposed || !IsHandleCreated)
      {
         return;
      }
      try
      {
         BeginInvoke(() =>
         {
            var connected = channel.IsConnected;
            dot.ForeColor = connected ? Color.LimeGreen : Color.IndianRed;
            statusLabel.Text = connected ? "connected" : "disconnected";
            statusLabel.ForeColor = connected ? SystemColors.ControlText : SystemColors.GrayText;
            AppendVerboseLog($"{channel.Service} DDE: {(connected ? "connected" : "disconnected")}");
            logger.DebugLog($"{channel.Service}|{channel.Topic} DDE state: {(connected ? "connected" : "disconnected")}");
         });
      }
      catch (ObjectDisposedException) { }
      catch (InvalidOperationException) { }
   }

   private void OnContactDelete(XElement root)
   {
      var call = XmlHelpers.GetValue(root, "call");
      var ts = XmlHelpers.GetValue(root, "timestamp");
      // VB6 captures contactdelete but never forwards to DXKeeper; we match that.
      AppendLog($"contactdelete: {call} at {ts}  [not forwarded — matches VB6]");
      logger.DebugLog($"contactdelete: {call} {ts}");
   }

   private readonly HashSet<string> reportedUnhandledTypes =
      new(StringComparer.OrdinalIgnoreCase);

   private void OnUnhandledMessage(string messageType)
   {
      // A known N1MM message we don't act on — not an error, so nothing goes
      // to the operation log. Note it once per type per session at debug level
      // so a diagnostic run shows what is arriving; N1MM broadcasts RadioInfo
      // several times a second, so logging every one would flood ErrorLog.txt
      // and drown the entries that matter.
      if (reportedUnhandledTypes.Add(messageType))
      {
         logger.DebugLog($"Ignoring <{messageType}> messages — known N1MM type this gateway does not handle (noted once per session)");
      }
   }

   private void OnDispatchFailed(string xml, Exception fault)
   {
      // A defect, not bad input. Say so plainly and keep the full stack in the
      // error log — this is the evidence needed to fix it.
      AppendLog($"INTERNAL ERROR handling a message ({fault.GetType().Name}: {fault.Message}) — see ErrorLog.txt. The gateway is still running.");
      logger.Log($"Unhandled exception while dispatching a UDP message: {fault}");
      logger.Log($"   message body: {xml}");
   }

   private void OnInvalidMessage(string xml, string reason)
   {
      AppendLog($"INVALID: {reason}");
      // Log the full body so the user can see exactly what arrived. Single
      // line keeps each ErrorLog entry on one timestamped row — fine even
      // for long XML payloads. If this ever becomes too voluminous, move it
      // to logger.DebugLog so it's gated by the "Log debugging information"
      // checkbox.
      logger.Log($"Invalid UDP message: {reason} -- body: {xml}");
   }

   private void OnLogWritten()
   {
      if (IsDisposed || !IsHandleCreated)
      {
         return;
      }
      try
      {
         BeginInvoke(() =>
         {
            if (!errorLogLink.Visible)
            {
               errorLogLink.Visible = true;
            }
         });
      }
      catch (ObjectDisposedException) { }
      catch (InvalidOperationException) { }
   }

   private void AppendVerboseLog(string line)
   {
      // Low-priority status events (e.g. DDE connect/disconnect). Suppressed
      // unless "Verbose logging" is enabled — keeps the operation log focused
      // on QSO traffic by default. Independent of "Log debugging information"
      // which controls ErrorLog.txt file volume.
      if (settings.VerboseLogging)
      {
         AppendLog(line);
      }
   }

   private void AppendLog(string line)
   {
      var stamped = $"{DateTime.Now:HH:mm:ss}  {line}";
      var items = operationLogListBox.Items;
      items.Add(stamped);
      while (items.Count > OperationLogCap)
      {
         items.RemoveAt(0);
      }
      operationLogListBox.TopIndex = items.Count - 1;
   }

   private void UdpPortTextBox_KeyDown(object? sender, KeyEventArgs e)
   {
      if (e.KeyCode != Keys.Enter)
      {
         return;
      }

      // A single-line TextBox on a form with no AcceptButton leaves Enter
      // unhandled, and WinForms answers that with the system ding. Suppressing
      // the keystroke silences it; committing here makes Enter do what the
      // operator plainly means by it, rather than only Tab working.
      e.SuppressKeyPress = true;
      e.Handled = true;

      if (IsUdpPortTextValid())
      {
         ApplyUdpPortChange();
      }
   }

   private void UdpPortTextBox_Validating(object? sender, System.ComponentModel.CancelEventArgs e)
   {
      e.Cancel = !IsUdpPortTextValid();
   }

   /// <summary>
   /// True if the textbox holds a usable port. On bad input it tells the user,
   /// restores the last good value and selects it, so the field is never left
   /// holding something the gateway is not actually using.
   /// </summary>
   private bool IsUdpPortTextValid()
   {
      if (int.TryParse(udpPortTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
          && n >= 1 && n <= 65535)
      {
         return true;
      }
      MessageBox.Show(this, "UDP port must be an integer between 1 and 65535.",
         "N1MM-DXKeeper Gateway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      udpPortTextBox.Text = settings.UdpPort.ToString(CultureInfo.InvariantCulture);
      udpPortTextBox.SelectAll();
      return false;
   }

   private void UdpPortTextBox_Validated(object? sender, EventArgs e)
   {
      ApplyUdpPortChange();
   }

   /// <summary>
   /// Rebinds the listener to the port now in the textbox. Safe to call more
   /// than once for the same value — Enter commits immediately, and the Tab
   /// that follows re-enters here with nothing left to do.
   /// </summary>
   private void ApplyUdpPortChange()
   {
      var newPort = int.Parse(udpPortTextBox.Text, CultureInfo.InvariantCulture);
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
         // Bind failed — revert to the previous (known-working) port.
         settings.UdpPort = oldPort;
         settings.Save();
         udpPortTextBox.Text = oldPort.ToString(CultureInfo.InvariantCulture);
         AppendLog($"Reverted UDP port to {oldPort}.");
         StartListenerOnConfiguredPort();
      }
   }

   private void SettingCheckChanged(object? sender, EventArgs e)
   {
      settings.DxkLookup = dxkLookupCheck.Checked;
      settings.DxkCallbook = callbookCheck.Checked;
      settings.DxkEqslUpload = eqslCheck.Checked;
      settings.DxkLotwUpload = lotwCheck.Checked;
      settings.DxkClubLogUpload = clubLogCheck.Checked;
      settings.VerboseLogging = verboseLoggingCheck.Checked;
      settings.Save();
   }

   private void LogDebugInfoCheck_CheckedChanged(object? sender, EventArgs e)
   {
      settings.DebugLogging = logDebugInfoCheck.Checked;
      logger.DebugEnabled = settings.DebugLogging;
      settings.Save();
      logger.Log($"Debug logging {(settings.DebugLogging ? "enabled" : "disabled")} by user");
   }

   private void OpenErrorLog()
   {
      if (!File.Exists(logger.LogPath))
      {
         MessageBox.Show(this, $"No error log yet at:\n{logger.LogPath}", "N1MM-DXKeeper Gateway",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
         return;
      }
      try
      {
         System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
         {
            FileName = logger.LogPath,
            UseShellExecute = true
         });
      }
      catch (Exception ex)
      {
         MessageBox.Show(this, $"Could not open ErrorLog.txt:\n\n{ex.Message}",
            "N1MM-DXKeeper Gateway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
   }

   private void AttachToolTips()
   {
      foreach (var cb in new[] { dxkLookupCheck, callbookCheck, eqslCheck, lotwCheck, clubLogCheck, logDebugInfoCheck, verboseLoggingCheck })
      {
         if (cb.Tag is string tip)
         {
            toolTip.SetToolTip(cb, tip);
         }
      }
      toolTip.SetToolTip(udpPortTextBox, "UDP port that N1MM Logger+ broadcasts QSO XML to (default 12060)");
      toolTip.SetToolTip(dxkPortLabel,
         @"Read-only. DXKeeper's TCP service base port from HKCU\Software\VB and VBA Program Settings\DXKeeper\TCPServer\ServiceBasePort. The gateway sends to base + 1.");
      toolTip.SetToolTip(dxkPortValue,
         @"Read-only. DXKeeper's TCP service base port from HKCU\Software\VB and VBA Program Settings\DXKeeper\TCPServer\ServiceBasePort. The gateway sends to base + 1.");
      toolTip.SetToolTip(showErrorLogButton, "Open ErrorLog.txt in the default text editor");
      toolTip.SetToolTip(helpButton, "Open online documentation");
      toolTip.SetToolTip(errorLogLink, "Errors have been logged - click to open ErrorLog.txt");
   }

   private void HelpButton_Click(object? sender, EventArgs e)
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
            "N1MM-DXKeeper Gateway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
   }
}
