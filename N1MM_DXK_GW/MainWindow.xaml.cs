using System.Globalization;
using System.Net;
using System.Resources;
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

   /// <summary>
   /// Formats a localized string. CurrentCulture, not InvariantCulture: these
   /// are sentences shown to a person, so counts and numbers should follow the
   /// operator's Windows regional settings. Wire values never come through
   /// here — those pin InvariantCulture at the point they are built.
   ///
   /// The catch is not defensive padding. Translations are produced outside
   /// this repository, and a translator or a machine-translation pass can
   /// easily damage a placeholder — "{0}" becoming "{ 0 }", or an index that
   /// was never in the English appearing in the translation. string.Format
   /// throws FormatException on both. Several callers run on the send-queue
   /// worker or inside AppendLog, so an unhandled one would take the gateway
   /// down mid-contest, in a language whoever is debugging it may not read.
   ///
   /// A damaged translation must degrade to something readable and keep the
   /// gateway running. Showing the raw template is ugly and unmistakably
   /// wrong, which is the right failure: visible, harmless, and diagnosable
   /// from the line written to ErrorLog.txt.
   /// </summary>
   private string L(string format, params object?[] args)
   {
      try
      {
         return string.Format(CultureInfo.CurrentCulture, format, args);
      }
      catch (FormatException ex)
      {
         logger.Log($"Damaged translation, showing the raw template instead. " +
                    $"Culture {CultureInfo.CurrentUICulture.Name}, {ex.Message}, template: {format}");
         return format;
      }
   }

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
      SizeChanged += (_, _) => UpdateSettingsHeightCap();
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
         DxkLookupToggle.IsChecked = settings.DxkLookup;
         CallbookToggle.IsChecked = settings.DxkCallbook;
         EqslToggle.IsChecked = settings.DxkEqslUpload;
         LotwToggle.IsChecked = settings.DxkLotwUpload;
         ClubLogToggle.IsChecked = settings.DxkClubLogUpload;
         VerboseLogToggle.IsChecked = settings.VerboseLogging;
         DebugLogToggle.IsChecked = settings.DebugLogging;
         PopulateLanguages();
      }
      finally
      {
         loadingSettings = false;
      }
   }

   /// <summary>
   /// Fills the language list from the translations actually shipped, with
   /// "Follow Windows" first and selected by default.
   ///
   /// Each entry shows the language in its own language (NativeName), because
   /// the operator looking for it cannot necessarily read the current one —
   /// which is the whole situation the setting exists to fix.
   /// </summary>
   private void PopulateLanguages()
   {
      LanguageCombo.Items.Clear();
      LanguageCombo.Items.Add(new LanguageChoice(string.Empty, Strings.LanguageFollowWindows));

      foreach (var culture in Localization.AvailableTranslations())
      {
         LanguageCombo.Items.Add(new LanguageChoice(culture.Name, culture.NativeName));
      }

      // Falls back to "Follow Windows" if the saved culture is no longer
      // shipped — the same outcome Localization.Apply produces, so the box
      // never claims a language the window is not actually drawn in.
      var match = LanguageCombo.Items.Cast<LanguageChoice>().FirstOrDefault(
         c => string.Equals(c.Culture, settings.Language, StringComparison.OrdinalIgnoreCase));
      LanguageCombo.SelectedItem = match ?? LanguageCombo.Items[0];
   }

   /// <summary>One entry of the language list. ToString is what the ComboBox
   /// displays, which keeps the markup free of a DisplayMemberPath.</summary>
   private sealed record LanguageChoice(string Culture, string Display)
   {
      public override string ToString() => Display;
   }

   private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
   {
      if (loadingSettings || LanguageCombo.SelectedItem is not LanguageChoice choice)
      {
         return;
      }
      if (string.Equals(choice.Culture, settings.Language, StringComparison.OrdinalIgnoreCase))
      {
         return;
      }

      settings.Language = choice.Culture;
      settings.Save();

      // Deliberately not re-applied live. The window's text comes from 45
      // x:Static references resolved when the XAML was loaded, so nothing
      // visible would change; saying "restart" is honest, and rebuilding the
      // visual tree to fake it would be a lot of machinery for a setting
      // touched once.
      //
      // It has to be a dialog rather than only a log line. Choosing a language
      // is a deliberate act that produces no visible change whatsoever, and an
      // operator who sees nothing happen reasonably concludes the setting is
      // broken. The log line stays as the record of the change.
      var message = L(RestartNoticeIn(choice.Culture), choice.Display);
      AppendLog(message, LogEntry.Level.Warning);
      MessageBox.Show(this, message, Strings.AppTitle,
         MessageBoxButton.OK, MessageBoxImage.Information);
   }

   /// <summary>
   /// The restart notice, resolved in the language the operator just picked
   /// rather than the one still on screen.
   ///
   /// That is deliberate: it is the first and only text they see in the new
   /// language, so it doubles as proof the translation actually loaded — which
   /// matters here, because a missing satellite fails silently by falling back
   /// to English rather than by raising anything. Reading it back in the wrong
   /// language would hide exactly that.
   ///
   /// Falls back to the neutral string if the culture has no satellite, which
   /// is also what the operator will get after the restart.
   /// </summary>
   private static string RestartNoticeIn(string cultureName)
   {
      try
      {
         // Blank means "follow Windows", so preview the Windows display
         // language — that is what will be in force on the next start.
         var culture = string.IsNullOrEmpty(cultureName)
            ? CultureInfo.InstalledUICulture
            : CultureInfo.GetCultureInfo(cultureName);

         return Strings.ResourceManager.GetString(nameof(Strings.LanguageRestartNote), culture)
                ?? Strings.LanguageRestartNote;
      }
      catch (CultureNotFoundException)
      {
         return Strings.LanguageRestartNote;
      }
      catch (MissingManifestResourceException)
      {
         return Strings.LanguageRestartNote;
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

   private void SettingToggleChanged(object sender, RoutedEventArgs e)
   {
      if (loadingSettings)
      {
         return;
      }
      settings.DxkLookup = DxkLookupToggle.IsChecked == true;
      settings.DxkCallbook = CallbookToggle.IsChecked == true;
      settings.DxkEqslUpload = EqslToggle.IsChecked == true;
      settings.DxkLotwUpload = LotwToggle.IsChecked == true;
      settings.DxkClubLogUpload = ClubLogToggle.IsChecked == true;
      settings.VerboseLogging = VerboseLogToggle.IsChecked == true;
      settings.Save();
   }

   private void DebugLogToggle_Changed(object sender, RoutedEventArgs e)
   {
      if (loadingSettings)
      {
         return;
      }
      settings.DebugLogging = DebugLogToggle.IsChecked == true;
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
      MessageBox.Show(this, Strings.DlgUdpPortInvalid,
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
         AppendLog(L(Strings.AlertUdpPortReverted, oldPort), LogEntry.Level.Warning);
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
            MessageBox.Show(this, L(Strings.DlgMulticastInvalid, error),
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
         AppendLog(L(Strings.AlertMulticastReverted, previous), LogEntry.Level.Warning);
         StartListenerOnConfiguredPort();
      }
   }

   /// <summary>
   /// Parses the configured group. Returns null for "no multicast", and sets
   /// <paramref name="error"/> when the operator typed something that is not a
   /// usable group — the two must stay distinguishable so a typo is reported
   /// rather than silently treated as "disabled".
   /// </summary>
   private IPAddress? ParseMulticastGroup(string configured, out string? error)
   {
      error = null;
      var text = configured?.Trim() ?? string.Empty;
      if (text.Length == 0)
      {
         return null;
      }
      if (!IPAddress.TryParse(text, out var address))
      {
         error = L(Strings.ErrNotAnIpAddress, text);
         return null;
      }
      if (!UdpListener.IsIPv4Multicast(address))
      {
         error = L(Strings.ErrNotMulticast, text);
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
         AppendLog(L(Strings.AlertMulticastRejected, groupError), LogEntry.Level.Error);
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
         var msg = L(Strings.MsgBindFailed, settings.UdpPort, ex.Message);
         AppendLog(msg, LogEntry.Level.Error);
         // English for the file, translated for the screen — ErrorLog.txt is
         // the artefact that gets pasted into a support thread.
         logger.Log($"Failed to bind UDP port {settings.UdpPort}: {ex.Message}");
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
         AppendLog(L(Strings.AlertReplaceNoKey, key.Call), LogEntry.Level.Warning);
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
         AppendLog(L(Strings.AlertDeleteNoKey, key.Call), LogEntry.Level.Warning);
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
      AppendLog(L(Strings.AlertInternalError, fault.GetType().Name, fault.Message),
                LogEntry.Level.Error);
      logger.Log($"Unhandled exception while dispatching a UDP message: {fault}");
      logger.Log($"   message body: {xml}");
   }

   private void OnInvalidMessage(string xml, string reason)
   {
      // The reason itself stays English: it describes malformed wire content
      // (a parse error, an unrecognized root element) and is written to
      // ErrorLog.txt unchanged.
      AppendLog(L(Strings.AlertInvalidMessage, reason), LogEntry.Level.Warning);
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
      // Two renderings of the same failure: `reason` for the operator's screen,
      // in their language; `englishReason` for ErrorLog.txt and the recovery
      // file, which stay English so they remain useful in a support thread.
      var reason = DescribeFailure(result);
      var englishReason = DescribeFailureEnglish(result);

      if (op.DeletedButNotRelogged)
      {
         // The one case where DXKeeper is left worse off than before we
         // started: the original is gone and the replacement never arrived.
         var kept = op.PreserveAdif != null &&
                    failedQsos.Save(op.PreserveAdif, $"replace failed after delete succeeded — {englishReason} (QSO with {op.Call})");
         PostToOperationLog(
            L(kept ? Strings.AlertReplaceLostQso : Strings.AlertReplaceLostQsoNotSaved,
              op.Call, portTag, failedQsos.FileName, reason),
            LogEntry.Level.Error);
         logger.Log($"REPLACE LEFT DXKEEPER WITHOUT THE QSO — {op.Call}{portTag}: delete succeeded, externallog did not ({englishReason})");
         return;
      }

      var savedNote = string.Empty;
      if (op.PreserveAdif != null)
      {
         var saved = failedQsos.Save(op.PreserveAdif, $"{englishReason} ({op.Summary})");
         savedNote = L(saved ? Strings.AlertSavedTo : Strings.AlertNotSavedTo,
                       failedQsos.FileName);
      }

      PostToOperationLog(
         L(Strings.AlertNotConfirmed, op.Summary, portTag, savedNote, reason),
         LogEntry.Level.Warning);
      logger.Log($"{op.Kind} not confirmed for {op.Call}{portTag}: {englishReason}");
   }

   /// <summary>
   /// Why DXKeeper did not confirm, in the operator's language.
   ///
   /// Driven by <see cref="DxKeeperTcpClient.SendFailure"/> rather than by the
   /// result's ErrorMessage, because that message is the English text bound for
   /// ErrorLog.txt. The one case that still passes prose through is Exception,
   /// where the detail comes from the operating system and is already in the
   /// system language.
   /// </summary>
   private string DescribeFailure(DxKeeperTcpClient.SendResult result)
   {
      if (result.Outcome == DxKeeperTcpClient.SendOutcome.Busy)
      {
         return Strings.FailBusy;
      }

      return result.Failure switch
      {
         DxKeeperTcpClient.SendFailure.ConnectTimeout =>
            L(Strings.FailConnectTimeout, DxKeeperTcpClient.ConnectTimeoutSeconds),
         DxKeeperTcpClient.SendFailure.ConnectRefused =>
            L(Strings.FailConnectRefused,
              result.Port ?? DxKeeperTcpClient.GetDxKeeperBasePortInfo().ServicePort,
              Strings.DxKeeperConfigNetworkService),
         DxKeeperTcpClient.SendFailure.PeerCloseTimeout =>
            L(Strings.FailNoPeerClose, DxKeeperTcpClient.PeerCloseTimeoutSeconds),
         DxKeeperTcpClient.SendFailure.ShuttingDown => Strings.FailShuttingDown,
         _ => result.ErrorMessage ?? Strings.FailSendFailed,
      };
   }

   /// <summary>
   /// The same failure in English, for ErrorLog.txt and the recovery file. A
   /// translated support artefact helps nobody.
   /// </summary>
   private static string DescribeFailureEnglish(DxKeeperTcpClient.SendResult result) =>
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
      // Disconnected is the resting state for DXView and Pathfinder on most
      // stations — dim it rather than shouting, and let the dot carry the
      // signal. Colour alone never conveys the state: the word is always there.
      status.Opacity = connected ? 1.0 : 0.65;
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
      if (count == 0)
      {
         FailedQsoBar.IsOpen = false;
         return;
      }

      FailedQsoBar.Message = count == 1
         ? string.Format(CultureInfo.CurrentCulture, Strings.FailedQsoBarMessageOne, failedQsos.FileName)
         : string.Format(CultureInfo.CurrentCulture, Strings.FailedQsoBarMessage, count, failedQsos.FileName);
      FailedQsoBar.ToolTip = string.Format(CultureInfo.CurrentCulture,
                                           Strings.FailedQsoTip, failedQsos.FilePath);
      // Not closable: dismissing it would hide a standing condition that has
      // not been dealt with. It closes itself when the file is gone.
      FailedQsoBar.IsOpen = true;
   }

   private void FailedQsoFileLink_Click(object sender, RoutedEventArgs e)
   {
      if (!failedQsos.Exists)
      {
         MessageBox.Show(this, L(Strings.DlgNoFailedQsos, failedQsos.FilePath),
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
            L(Strings.DlgCouldNotOpenFile, failedQsos.FileName, ex.Message),
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
         MessageBox.Show(this, L(Strings.DlgCouldNotOpenFolder, ex.Message),
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
         MessageBox.Show(this, L(Strings.DlgNoErrorLog, logger.LogPath),
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
         MessageBox.Show(this, L(Strings.DlgCouldNotOpenErrorLog, ex.Message),
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
         MessageBox.Show(this, L(Strings.DlgCouldNotOpenHelp, ex.Message),
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

   /// <summary>
   /// Appends a line whose severity is derived from its text. Correct only for
   /// the routine English traffic lines — see the overload below.
   /// </summary>
   private void AppendLog(string line) => AppendLog(line, LogEntry.Classify(line));

   /// <summary>
   /// Appends a line with an explicitly stated severity.
   ///
   /// Every translated line must use this overload. LogEntry.Classify decides
   /// severity by looking for English phrases ("ERROR", "***", "did not
   /// confirm"), so a translated failure line would classify as Normal and
   /// lose its colour — silently, and precisely in the languages whose readers
   /// most need the failure to stand out. Stating severity at the call site
   /// removes the text from the decision entirely.
   /// </summary>
   private void AppendLog(string line, LogEntry.Level severity)
   {
      var entry = new LogEntry
      {
         Text = $"{DateTime.Now:HH:mm:ss}  {line}",
         Severity = severity,
      };

      OperationLogList.Items.Add(entry);
      while (OperationLogList.Items.Count > OperationLogCap)
      {
         OperationLogList.Items.RemoveAt(0);
      }
      OperationLogList.ScrollIntoView(OperationLogList.Items[^1]);
   }

   /// <summary>
   /// Space the settings region must leave for everything below it: the
   /// connection status card, the log at its minimum, and the footer.
   /// </summary>
   private const double ReservedBelowSettings = 340;

   /// <summary>
   /// Caps how tall the settings region may grow, as a function of the current
   /// window height, so expanding a group can never squeeze the log away.
   ///
   /// This replaces an earlier attempt to resize the window itself on expand
   /// and collapse. That raced the expander's animation — the measurement ran
   /// while the content was still moving, so collapsing a group grew the
   /// window instead of shrinking it. Sizing off the window rather than off
   /// mid-animation content has no such race, and it adapts on its own when
   /// the operator resizes: a taller window simply shows more settings.
   ///
   /// Past the cap the settings region scrolls, with a visible scrollbar. That
   /// is honest; the earlier failure was not that it scrolled but that it
   /// clipped mid-row with no indication it had done so.
   /// </summary>
   /// <summary>
   /// Height reserved for the settings region regardless of which group is
   /// open: three collapsed headers plus the tallest group's content.
   ///
   /// This is what makes the panes below hold still. Because the accordion
   /// keeps at most one group open, the region never needs more than this, and
   /// pinning it as a minimum means opening or closing a group changes nothing
   /// below it. The cost is some empty space when every group is collapsed,
   /// which is a fair trade for a window that does not jump while the operator
   /// is reading the log.
   /// </summary>
   private const double SettingsReservedHeight = 300;

   private void UpdateSettingsHeightCap()
   {
      var cap = Math.Max(120, ActualHeight - ReservedBelowSettings);
      SettingsScroll.MaxHeight = cap;

      // Never reserve more than the cap allows, or a short window would give
      // the settings region space the log needs.
      SettingsScroll.MinHeight = Math.Min(SettingsReservedHeight, cap);
   }

   /// <summary>
   /// Guards against the accordion's own Collapse calls re-entering this
   /// handler and fighting the expansion that triggered them.
   /// </summary>
   private bool adjustingSections;

   /// <summary>
   /// One group open at a time, and scroll it into view.
   ///
   /// Accordion behaviour is what stops the window feeling like it jumps.
   /// With several groups open at once, opening another shifted the connection
   /// status, the log and the footer down by a whole card. With at most one
   /// open, the settings region only varies by the difference between one
   /// group's content and another's — and the MinHeight below absorbs even
   /// that, so nothing underneath moves at all.
   /// </summary>
   private void SettingsSection_ExpandChanged(object sender, RoutedEventArgs e)
   {
      if (adjustingSections || sender is not Wpf.Ui.Controls.CardExpander opened)
      {
         return;
      }

      // IsExpanded="True" in XAML raises Expanded DURING InitializeComponent,
      // before the later named fields have been assigned - so the accordion
      // loop below would dereference nulls and take the process down on
      // startup. Nothing needs adjusting before the tree is built anyway.
      if (NetworkSection is null || ServicesSection is null || DiagnosticsSection is null)
      {
         return;
      }

      if (opened.IsExpanded)
      {
         adjustingSections = true;
         try
         {
            foreach (var other in new[] { NetworkSection, ServicesSection, DiagnosticsSection })
            {
               if (!ReferenceEquals(other, opened))
               {
                  other.IsExpanded = false;
               }
            }
         }
         finally
         {
            adjustingSections = false;
         }
      }

      // After the expander's own layout pass, or the bounds are pre-expansion.
      Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => opened.BringIntoView()));
   }

   private void CopyLogButton_Click(object sender, RoutedEventArgs e)
   {
      if (OperationLogList.Items.Count == 0)
      {
         return;
      }
      var text = string.Join(Environment.NewLine,
         OperationLogList.Items.Cast<LogEntry>().Select(entry => entry.Text));
      try
      {
         System.Windows.Clipboard.SetText(text);
         AppendLog(string.Format(CultureInfo.CurrentCulture,
                                 Strings.LogCopied, OperationLogList.Items.Count));
      }
      catch (Exception ex)
      {
         // The clipboard can be locked by another process; not worth a dialog.
         logger.Log($"Could not copy the operation log to the clipboard: {ex.Message}");
      }
   }

   /// <summary>
   /// Clears the on-screen log only. ErrorLog.txt and the failed-QSO file are
   /// untouched — this is a view, and clearing a view must never destroy the
   /// durable record behind it.
   /// </summary>
   private void ClearLogButton_Click(object sender, RoutedEventArgs e)
   {
      OperationLogList.Items.Clear();
   }

   /// <summary>
   /// Append to the on-screen log from any thread. Gives up silently if the
   /// window is gone — the durable record is already in ErrorLog.txt.
   /// </summary>
   private void PostToOperationLog(string line) => RunOnUi(() => AppendLog(line));

   /// <summary>As <see cref="PostToOperationLog(string)"/>, with the severity
   /// stated rather than derived — required for translated lines.</summary>
   private void PostToOperationLog(string line, LogEntry.Level severity) =>
      RunOnUi(() => AppendLog(line, severity));

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
