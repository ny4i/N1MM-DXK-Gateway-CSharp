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

      showErrorLogButton.Click += (_, _) => OpenErrorLog();
      errorLogLink.LinkClicked += (_, _) => OpenErrorLog();
      helpButton.Click += HelpButton_Click;

      udpPortTextBox.Validating += UdpPortTextBox_Validating;
      udpPortTextBox.Validated += UdpPortTextBox_Validated;

      dxkLookupCheck.CheckedChanged += SettingCheckChanged;
      callbookCheck.CheckedChanged += SettingCheckChanged;
      eqslCheck.CheckedChanged += SettingCheckChanged;
      lotwCheck.CheckedChanged += SettingCheckChanged;
      clubLogCheck.CheckedChanged += SettingCheckChanged;
      logDebugInfoCheck.CheckedChanged += LogDebugInfoCheck_CheckedChanged;

      dequeueTimer.Tick += (_, _) => dispatcher.Drain();
      dispatcher.ContactInfoReceived += OnContactInfo;
      dispatcher.LookupInfoReceived += OnLookupInfo;
      dispatcher.ContactDeleteReceived += OnContactDelete;
      dispatcher.InvalidMessageReceived += OnInvalidMessage;

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
      dxkDde.Dispose();
      dxvDde.Dispose();
      pfDde.Dispose();
      base.OnFormClosed(e);
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
      var checks = new[] { dxkLookupCheck, callbookCheck, eqslCheck, lotwCheck, clubLogCheck };
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

      var listener = new UdpListener(settings.UdpPort, dispatcher.Enqueue);
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

      AppendLog($"contactinfo: {adif.Summary} -> sending to DXKeeper...");
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

      _ = SendAdifAsync(adif, options);
   }

   private async Task SendAdifAsync(AdifBuilder.Result adif, DxKeeperTcpClient.ExternalLogOptions options)
   {
      var result = await tcpClient.ExternalLogAsync(adif.AdifRecord, options).ConfigureAwait(false);

      try
      {
         if (IsDisposed || !IsHandleCreated)
         {
            return;
         }
         BeginInvoke(() => ReportSendResult(adif, result));
      }
      catch (ObjectDisposedException) { }
      catch (InvalidOperationException) { }
   }

   private void ReportSendResult(AdifBuilder.Result adif, DxKeeperTcpClient.SendResult result)
   {
      switch (result.Outcome)
      {
         case DxKeeperTcpClient.SendOutcome.Sent:
            AppendLog($"logged QSO with {adif.Summary}");
            logger.DebugLog($"TCP send completed for {adif.Call}; response: {result.Response ?? "(none)"}");
            break;
         case DxKeeperTcpClient.SendOutcome.Busy:
            AppendLog($"DXKeeper send busy — discarded QSO with {adif.Call}");
            logger.Log($"TCP send busy when attempting to log {adif.Call}");
            break;
         case DxKeeperTcpClient.SendOutcome.Failed:
            AppendLog($"FAILED to log QSO with {adif.Call}: {result.ErrorMessage}");
            logger.Log($"TCP send failed for {adif.Call}: {result.ErrorMessage}");
            break;
      }
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
            AppendLog($"{channel.Service} DDE: {(connected ? "connected" : "disconnected")}");
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

   private void OnInvalidMessage(string xml, string reason)
   {
      AppendLog($"INVALID: {reason}");
      logger.Log($"Invalid UDP message: {reason}");
      logger.DebugLog($"Invalid message body (truncated 500): {Preview(xml, 500)}");
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

   private static string Preview(string text, int maxLength)
   {
      var compact = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
      return compact.Length <= maxLength ? compact : compact[..maxLength] + "...";
   }

   private void UdpPortTextBox_Validating(object? sender, System.ComponentModel.CancelEventArgs e)
   {
      if (int.TryParse(udpPortTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
          && n >= 1 && n <= 65535)
      {
         return;
      }
      MessageBox.Show(this, "UDP port must be an integer between 1 and 65535.",
         "N1MM-DXKeeper Gateway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      udpPortTextBox.Text = settings.UdpPort.ToString(CultureInfo.InvariantCulture);
      udpPortTextBox.SelectAll();
      e.Cancel = true;
   }

   private void UdpPortTextBox_Validated(object? sender, EventArgs e)
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
      foreach (var cb in new[] { dxkLookupCheck, callbookCheck, eqslCheck, lotwCheck, clubLogCheck, logDebugInfoCheck })
      {
         if (cb.Tag is string tip)
         {
            toolTip.SetToolTip(cb, tip);
         }
      }
      toolTip.SetToolTip(udpPortTextBox, "UDP port that N1MM Logger+ broadcasts QSO XML to (default 12060)");
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
