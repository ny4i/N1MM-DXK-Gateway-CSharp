// SPDX-License-Identifier: GPL-3.0-or-later

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace N1MM_DXK_GW;

/// <summary>
/// The gateway's notification-area presence: what the operator can see and do
/// while the window is hidden.
///
/// Built on WinForms' NotifyIcon rather than a WPF tray package. WPF has no
/// tray support of its own, but this project already references WinForms for
/// NDde's hidden window, so NotifyIcon costs nothing — no new package, no new
/// assembly, no new build graph. A dedicated tray library would buy prettier
/// popups this program does not need.
///
/// Threading: NotifyIcon needs a message pump, so every member here must be
/// touched from the UI thread. Callers marshal (see MainWindow.RunOnUi).
/// </summary>
public sealed class TrayIcon : IDisposable
{
   /// <summary>
   /// Windows truncates a notification-area tooltip past this, and .NET
   /// throws rather than truncating for us. Measured on .NET 8: 127 accepted,
   /// 128 rejected. Translations run longer than the English, so everything
   /// built here is clamped rather than assumed to fit.
   /// </summary>
   private const int MaxTooltipLength = 127;

   [DllImport("user32.dll", SetLastError = true)]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static extern bool DestroyIcon(IntPtr handle);

   public event Action? ShowRequested;
   public event Action? QuitRequested;
   public event Action? OpenFailedFileRequested;
   public event Action? OpenErrorLogRequested;

   private readonly NotifyIcon notifyIcon;
   private readonly Icon normalIcon;
   private readonly Icon alertIcon;

   private readonly ToolStripMenuItem receivedItem = Info();
   private readonly ToolStripMenuItem loggedItem = Info();
   private readonly ToolStripMenuItem queuedItem = Info();
   private readonly ToolStripMenuItem deliveredItem = Info();
   private readonly ToolStripMenuItem lastQsoItem = Info();
   private readonly ToolStripMenuItem openFailedItem = new(Strings.TrayOpenFailedFile);
   private readonly ToolStripMenuItem errorLogItem = new(Strings.DisplayErrorLog);

   private bool showingAlertIcon;
   private string lastTooltip = string.Empty;

   public TrayIcon()
   {
      normalIcon = LoadAppIcon();
      alertIcon = WithAlertBadge(normalIcon);

      var showItem = new ToolStripMenuItem(Strings.TrayShow);
      showItem.Click += (_, _) => ShowRequested?.Invoke();
      // Bold: it is what a double-click does, and the entry an operator who
      // cannot find the window is looking for.
      showItem.Font = new Font(showItem.Font, FontStyle.Bold);

      openFailedItem.Click += (_, _) => OpenFailedFileRequested?.Invoke();
      errorLogItem.Click += (_, _) => OpenErrorLogRequested?.Invoke();

      var quitItem = new ToolStripMenuItem(Strings.TrayQuit);
      quitItem.Click += (_, _) => QuitRequested?.Invoke();

      var menu = new ContextMenuStrip();
      menu.Items.AddRange(new ToolStripItem[]
      {
         showItem,
         new ToolStripSeparator(),
         receivedItem, loggedItem, queuedItem, deliveredItem, lastQsoItem,
         new ToolStripSeparator(),
         openFailedItem, errorLogItem,
         new ToolStripSeparator(),
         quitItem,
      });

      notifyIcon = new NotifyIcon
      {
         Icon = normalIcon,
         Text = Strings.AppTitle,
         ContextMenuStrip = menu,
         Visible = false,
      };
      notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke();
      // A balloon is only ever raised to say something needs attention, so
      // clicking it should go straight to where that can be dealt with.
      notifyIcon.BalloonTipClicked += (_, _) => ShowRequested?.Invoke();
   }

   public bool Visible
   {
      get => notifyIcon.Visible;
      set => notifyIcon.Visible = value;
   }

   /// <summary>
   /// Live counts, shown in the menu and summarised in the tooltip. The icon
   /// gains a red badge while anything is undelivered: that is the one state
   /// the operator must not be able to miss from across the shack, and the
   /// window's own warning bar is invisible while it is hidden.
   /// </summary>
   public void UpdateStatus(long received, long logged, int queued, int notDelivered,
                            string? lastQso, bool errorLogWritten)
   {
      receivedItem.Text = Format(Strings.TrayReceived, received);
      loggedItem.Text = Format(Strings.TrayLogged, logged);
      queuedItem.Text = Format(Strings.TrayQueued, queued);
      deliveredItem.Text = notDelivered > 0
         ? Format(Strings.TrayNotDelivered, notDelivered)
         : Strings.TrayAllDelivered;
      lastQsoItem.Text = string.IsNullOrEmpty(lastQso)
         ? Strings.TrayNoQsoYet
         : Format(Strings.TrayLastQso, lastQso);

      openFailedItem.Visible = notDelivered > 0;
      errorLogItem.Visible = errorLogWritten;

      var wantAlert = notDelivered > 0;
      if (wantAlert != showingAlertIcon)
      {
         notifyIcon.Icon = wantAlert ? alertIcon : normalIcon;
         showingAlertIcon = wantAlert;
      }

      // Only write when it changed. Assigning Text repaints the tooltip and
      // this runs once a second for as long as the window stays hidden.
      var tooltip = Clamp($"{Strings.AppTitle}\n" +
                          Format(Strings.TrayTooltip, received, logged, notDelivered));
      if (tooltip != lastTooltip)
      {
         notifyIcon.Text = tooltip;
         lastTooltip = tooltip;
      }
   }

   public void Notify(string title, string message, bool warning)
   {
      if (!notifyIcon.Visible)
      {
         return;
      }
      notifyIcon.BalloonTipTitle = title;
      notifyIcon.BalloonTipText = message;
      notifyIcon.BalloonTipIcon = warning ? ToolTipIcon.Warning : ToolTipIcon.Info;
      // Windows decides how long it actually stays up; the timeout argument
      // has been advisory since Vista.
      notifyIcon.ShowBalloonTip(10000);
   }

   public void Dispose()
   {
      notifyIcon.Visible = false;
      notifyIcon.ContextMenuStrip?.Dispose();
      notifyIcon.Dispose();
      normalIcon.Dispose();
      alertIcon.Dispose();
   }

   // ------------------------------------------------------------- internals

   private static ToolStripMenuItem Info() => new() { Enabled = false };

   private static string Format(string format, params object?[] args)
   {
      try
      {
         return string.Format(CultureInfo.CurrentCulture, format, args);
      }
      catch (FormatException)
      {
         // Same reasoning as MainWindow.L(): translations come from outside
         // this repository, and a damaged placeholder must not take the
         // gateway down from a once-a-second status refresh.
         return format;
      }
   }

   private static string Clamp(string text) =>
      text.Length <= MaxTooltipLength
         ? text
         : text[..(MaxTooltipLength - 1)] + "…";

   /// <summary>
   /// The application icon, read from the copy embedded in this assembly so
   /// the tray never depends on a file sitting next to the executable.
   /// </summary>
   private static Icon LoadAppIcon()
   {
      var assembly = Assembly.GetExecutingAssembly();
      var name = assembly.GetName().Name + ".N1MM_DXK_GW.ico";
      using var stream = assembly.GetManifestResourceStream(name);
      if (stream != null)
      {
         return new Icon(stream, SystemIcons.Application.Size);
      }
      // Never leave the tray without an icon: an invisible entry is worse
      // than a generic one, because there is nothing left to click.
      return (Icon)SystemIcons.Application.Clone();
   }

   /// <summary>
   /// The same icon with a red dot in the corner, composed at startup rather
   /// than shipped as a second asset — one icon to keep in step, and no build
   /// step to forget.
   /// </summary>
   private static Icon WithAlertBadge(Icon source)
   {
      using var bitmap = source.ToBitmap();
      using (var g = Graphics.FromImage(bitmap))
      {
         g.SmoothingMode = SmoothingMode.AntiAlias;
         var d = Math.Max(6, bitmap.Width / 2);
         var x = bitmap.Width - d;
         var y = bitmap.Height - d;

         // A pale ring first, so the dot reads against a dark icon as well as
         // a light one — the shack may be running either Windows theme.
         using var ring = new SolidBrush(Color.FromArgb(0xF3, 0xF3, 0xF3));
         using var dot = new SolidBrush(Color.FromArgb(0xC4, 0x2B, 0x1C));
         g.FillEllipse(ring, x - 1, y - 1, d + 2, d + 2);
         g.FillEllipse(dot, x, y, d, d);
      }

      var handle = bitmap.GetHicon();
      try
      {
         // Clone so the returned Icon owns its own copy: the handle below is
         // destroyed immediately, and Icon.FromHandle does not take ownership.
         // Without this the tray icon leaks a GDI handle per construction.
         return (Icon)Icon.FromHandle(handle).Clone();
      }
      finally
      {
         DestroyIcon(handle);
      }
   }
}