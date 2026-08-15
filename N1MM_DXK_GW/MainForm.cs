using System.Globalization;
using System.Net;
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

      ApplyApplicationIcon();

      failedQsos = new FailedQsoStore(logger);
      failedQsos.RecordSaved += OnFailedQsoSaved;

      failedQsoFileLink.LinkClicked += (_, _) => OpenFailedQsoFile();
      failedQsoFolderLink.LinkClicked += (_, _) => OpenFailedQsoFolder();
      // Result callback runs on the send worker thread — see OnSendResult.
      sendQueue = new QsoSendQueue(tcpClient, OnSendResult);

      showErrorLogButton.Click += (_, _) => OpenErrorLog();
      errorLogLink.LinkClicked += (_, _) => OpenErrorLog();
      helpButton.Click += HelpButton_Click;

      udpPortTextBox.Validating += UdpPortTextBox_Validating;
      udpPortTextBox.Validated += UdpPortTextBox_Validated;
      udpPortTextBox.KeyDown += UdpPortTextBox_KeyDown;

      multicastTextBox.Validated += (_, _) => ApplyMulticastChange();
      multicastTextBox.KeyDown += MulticastTextBox_KeyDown;

      dxkLookupCheck.CheckedChanged += SettingCheckChanged;
      callbookCheck.CheckedChanged += SettingCheckChanged;
      eqslCheck.CheckedChanged += SettingCheckChanged;
      lotwCheck.CheckedChanged += SettingCheckChanged;
      clubLogCheck.CheckedChanged += SettingCheckChanged;
      verboseLoggingCheck.CheckedChanged += SettingCheckChanged;
      logDebugInfoCheck.CheckedChanged += LogDebugInfoCheck_CheckedChanged;

      dequeueTimer.Tick += (_, _) => dispatcher.Drain();
      dispatcher.ContactInfoReceived += OnContactInfo;
      dispatcher.ContactReplaceReceived += OnContactReplace;
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
      // Normally hides itself - this run's file cannot exist yet.
      RefreshFailedQsoStatus();
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

   /// <summary>
   /// Sets the window icon from the embedded copy of the original VB6 icon.
   /// ApplicationIcon in the .csproj covers the .exe, but WinForms draws the
   /// title bar and Alt-Tab entry from Form.Icon, which it does not inherit.
   ///
   /// A missing or unreadable icon is cosmetic, so it must never prevent the
   /// gateway from starting — the form simply keeps the WinForms default.
   /// </summary>
   private void ApplyApplicationIcon()
   {
      const string resourceName = "N1MM_DXK_GW.N1MM_DXK_GW.ico";
      try
      {
         using var stream = typeof(MainForm).Assembly.GetManifestResourceStream(resourceName);
         if (stream != null)
         {
            Icon = new Icon(stream);
         }
      }
      catch (Exception ex)
      {
         System.Diagnostics.Debug.WriteLine($"Could not load {resourceName}: {ex.Message}");
      }
   }

   private void RefreshDxKeeperPortDisplay()
   {
      var info = DxKeeperTcpClient.GetDxKeeperBasePortInfo();
      var suffix = info.FromRegistry
         ? string.Empty
         : " — assumed default; check DXKeeper: Config > Defaults > Network Service";
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
      multicastTextBox.Text = settings.MulticastGroup;
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
      var group = ParseMulticastGroup(settings.MulticastGroup, out var groupError);
      if (groupError != null)
      {
         // Don't silently fall back to no multicast — the operator configured
         // a group and would otherwise see a healthy-looking listener that
         // receives nothing.
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
         MessageBox.Show(this, msg, "N1MM-DXKeeper Gateway",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
   }

   /// <summary>
   /// Parses the configured group. Returns null for "no multicast", and sets
   /// <paramref name="error"/> when the operator typed something that is not a
   /// usable group — the two cases must stay distinguishable so a typo is
   /// reported rather than treated as "disabled".
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

      var options = BuildLogOptions();

      // Queued, not sent directly: QsoSendQueue serialises delivery so the
      // gateway paces itself to DXKeeper instead of overrunning it.
      sendQueue.EnqueueLog(adif, options);
   }

   /// <summary>
   /// Per-QSO externallog flags, from the user's checkboxes. Shared by the
   /// new-QSO path and the re-log half of a replace so an edited QSO is
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
         // and logging the edited copy alone would duplicate it. Refuse and say so.
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
   /// Called on the QsoSendQueue worker thread — including during shutdown,
   /// after the form's handle may already be gone. Everything that must not be
   /// lost (the failed-QSO file, ErrorLog.txt) is written here directly; only
   /// the on-screen operation log is marshalled, and best-effort.
   /// </summary>
   private void OnSendResult(QsoSendQueue.OperationResult op)
   {
      var result = op.Send;
      var portTag = result.Port.HasValue ? $" (TCP {result.Port.Value})" : string.Empty;

      // Always log the wire frames at debug level — pairs with the ADIF and
      // UDP-receive debug lines to give a full round-trip trace. A replace
      // emits two, and the delete is logged first because that is the order
      // they went out in.
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

      // Failed, Unconfirmed and Busy all mean the same thing to the operator:
      // DXKeeper did not confirm this. Never retry — DXKeeper does not detect
      // duplicates, so retrying something it had in fact processed would
      // duplicate the record.
      var reason = DescribeFailure(result);

      if (op.DeletedButNotRelogged)
      {
         // The one case where DXKeeper is left worse off than before we
         // started: the original is gone and the replacement never arrived.
         // Say so first and in plain words.
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

   /// <summary>
   /// A QSO deleted in N1MM. The VB6 gateway parsed these and never forwarded
   /// them, so a deletion in N1MM left the QSO in DXKeeper; this now sends the
   /// deleteqso command.
   ///
   /// contactdelete carries far fewer fields than contactinfo — no mode, no
   /// frequency, no RST — but that does not matter: DXKeeper identifies a QSO
   /// by CALL + QSO_DATE + TIME_ON, all of which are present.
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

   /// <summary>
   /// Fires on the send-queue worker thread when a QSO is stranded. Marshals
   /// to refresh the banner so the operator sees it immediately rather than at
   /// the next restart.
   /// </summary>
   private void OnFailedQsoSaved()
   {
      if (IsDisposed || !IsHandleCreated)
      {
         return;
      }
      try
      {
         BeginInvoke(RefreshFailedQsoStatus);
      }
      catch (ObjectDisposedException) { }
      catch (InvalidOperationException) { }
   }

   /// <summary>
   /// Status line for QSOs this session could not deliver: a count plus links
   /// to the file and its folder. Hidden entirely when nothing has failed.
   ///
   /// The count is read from the file every time rather than tracked in
   /// memory. That matters: opening the file or the folder does NOT clear it,
   /// because nothing we can observe tells us the operator actually imported
   /// the records into DXKeeper. The count falls to zero only when the file
   /// itself is gone — which is exactly what the operator is told to do once
   /// the import is done, and is the only signal we can trust.
   /// </summary>
   private void RefreshFailedQsoStatus()
   {
      var count = failedQsos.RecordCount();
      var show = count > 0;

      failedQsoLabel.Visible = show;
      failedQsoFileLink.Visible = show;
      failedQsoFolderLink.Visible = show;

      if (show)
      {
         failedQsoLabel.Text =
            $"{count} QSO{(count == 1 ? string.Empty : "s")} not delivered — open";
         toolTip.SetToolTip(failedQsoLabel,
            $"Import {failedQsos.FilePath} into DXKeeper, then delete it. "
            + "This count clears when the file is gone.");
      }
   }

   /// <summary>
   /// Re-checks the file when the window regains focus, so deleting it after
   /// an import clears the count without needing a restart.
   /// </summary>
   protected override void OnActivated(EventArgs e)
   {
      base.OnActivated(e);
      if (IsHandleCreated && !IsDisposed)
      {
         RefreshFailedQsoStatus();
      }
   }

   private void OpenFailedQsoFile()
   {
      if (!failedQsos.Exists)
      {
         MessageBox.Show(this, $"No stranded QSOs this session.\n\n{failedQsos.FilePath} does not exist.",
            "N1MM-DXKeeper Gateway", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            $"Could not open {failedQsos.FileName}:\n\n{ex.Message}\n\nUse Open Folder and import it into DXKeeper from there.",
            "N1MM-DXKeeper Gateway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
   }

   private void OpenFailedQsoFolder()
   {
      try
      {
         // /select, highlights the file in Explorer so it can be dragged
         // straight into DXKeeper's import.
         var argument = failedQsos.Exists
            ? $"/select,\"{failedQsos.FilePath}\""
            : $"\"{Path.GetDirectoryName(failedQsos.FilePath)}\"";
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
            "N1MM-DXKeeper Gateway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
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

   private void MulticastTextBox_KeyDown(object? sender, KeyEventArgs e)
   {
      if (e.KeyCode != Keys.Enter)
      {
         return;
      }
      // Same reason as the port field: a single-line TextBox on a form with no
      // AcceptButton answers an unhandled Enter with the system ding.
      e.SuppressKeyPress = true;
      e.Handled = true;
      ApplyMulticastChange();
   }

   /// <summary>
   /// Applies an edited multicast group. Joining a group is done at bind time,
   /// so this rebinds the listener — the same path a port change takes.
   /// </summary>
   private void ApplyMulticastChange()
   {
      var entered = multicastTextBox.Text.Trim();
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
               "N1MM-DXKeeper Gateway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            multicastTextBox.Text = settings.MulticastGroup;
            multicastTextBox.SelectAll();
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
         // Rebind failed — go back to the setting that was working.
         settings.MulticastGroup = previous;
         settings.Save();
         multicastTextBox.Text = previous;
         AppendLog($"Reverted multicast group to '{previous}'.");
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
      const string multicastTip =
         "Optional. Leave blank unless the sending program is configured to send to a multicast group "
         + "(224.0.0.0 - 239.255.255.255). Unicast and broadcast are received either way. "
         + "The group is joined on the interface the routing table selects.";
      toolTip.SetToolTip(multicastTextBox, multicastTip);
      toolTip.SetToolTip(multicastLabel, multicastTip);
      // Point the operator at the place they can actually change this, not at
      // the registry key we happen to read it from.
      const string dxkPortTip =
         "Read-only here. Set it in DXKeeper: Config > Defaults tab > Network Service > Base Port. "
         + "The gateway sends to Base Port + 1, which is the port DXKeeper shows in that panel's heading.";
      toolTip.SetToolTip(dxkPortLabel, dxkPortTip);
      toolTip.SetToolTip(dxkPortValue, dxkPortTip);
      toolTip.SetToolTip(failedQsoFileLink,
         "Open the failed-QSO ADIF file in the default text editor");
      toolTip.SetToolTip(failedQsoFolderLink,
         "Show the file in Explorer so it can be imported into DXKeeper");
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
