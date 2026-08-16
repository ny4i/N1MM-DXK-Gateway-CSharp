// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace N1MM_DXK_GW;

/// <summary>
/// Version, copyright, licence and — the reason it exists at all — credit for
/// whoever translated the language currently on screen.
///
/// Attribution is per-language and lives in the resources themselves
/// (<c>TranslatedBy</c>), so a satellite carries its own credit and the tooling
/// never overwrites it. That is the point: people who translate a program for
/// nothing should be able to see their name in it.
/// </summary>
public partial class AboutWindow : FluentWindow
{
   /// <summary>
   /// The one instance, if it is open. ShowDialog blocks mouse and keyboard
   /// input to the owner, but that is not the same as preventing a second
   /// window: UI Automation raises Click directly on the control and bypasses
   /// the input block entirely, so a second About can be stacked on the first.
   /// Accessibility tools and scripted automation both go that route.
   ///
   /// Guarding here rather than at the call site keeps the rule with the
   /// window it applies to, and means any future caller gets it for free.
   /// </summary>
   private static AboutWindow? open;

   /// <summary>
   /// Shows the About window, or brings the existing one forward.
   /// </summary>
   public static void ShowSingle(Window owner)
   {
      if (open != null)
      {
         open.Activate();
         return;
      }

      open = new AboutWindow { Owner = owner };
      try
      {
         open.ShowDialog();
      }
      finally
      {
         // In a finally: if ShowDialog throws, a stale reference would lock
         // the operator out of the About window for the rest of the session.
         open = null;
      }
   }

   public AboutWindow()
   {
      InitializeComponent();

      AppIcon.Source = LoadIcon();
      VersionText.Text = Format(Strings.AboutVersion, VersionString);
      CopyrightText.Text = Copyright;

      // No credit line in English. The strings were not translated into
      // English, they were written in it, so "Translation into English by..."
      // states something that did not happen. Every other language is running
      // on somebody's work and says whose.
      var credit = Strings.TranslatedBy;
      if (IsEnglish || string.IsNullOrWhiteSpace(credit))
      {
         TranslationCredit.Visibility = Visibility.Collapsed;
      }
      else
      {
         TranslationCredit.Text = Format(Strings.AboutTranslationBy, LanguageName, credit);
      }
   }

   /// <summary>Major.Minor.Build. The revision is never set and would only add
   /// a zero for a tester to mistype.</summary>
   private static string VersionString
   {
      get
      {
         var v = Assembly.GetExecutingAssembly().GetName().Version;
         return v == null
            ? "?"
            : $"{v.Major}.{v.Minor}.{v.Build}";
      }
   }

   private static string Copyright =>
      Assembly.GetExecutingAssembly()
              .GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
      ?? "Copyright (C) 2026 Tom Schaefer, NY4I";

   /// <summary>
   /// The language in its own words. Uses the neutral culture, so an operator
   /// on en-GB or pt-BR sees "English" or "português" rather than the regional
   /// variant — the translation is of a language, not of a country.
   /// </summary>
   /// <summary>
   /// True when the window is running on the neutral English resources, in
   /// whatever region. Keyed on the two-letter code so en-GB and en-AU are
   /// covered without listing them.
   /// </summary>
   private static bool IsEnglish =>
      string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "en",
                    StringComparison.OrdinalIgnoreCase);

   private static string LanguageName
   {
      get
      {
         var culture = CultureInfo.CurrentUICulture;
         if (!culture.IsNeutralCulture && culture.Parent != CultureInfo.InvariantCulture)
         {
            culture = culture.Parent;
         }
         return culture.NativeName;
      }
   }

   private static BitmapImage? LoadIcon()
   {
      try
      {
         var assembly = Assembly.GetExecutingAssembly();
         using var stream = assembly.GetManifestResourceStream(
            assembly.GetName().Name + ".N1MM_DXK_GW.ico");
         if (stream == null)
         {
            return null;
         }
         var image = new BitmapImage();
         image.BeginInit();
         image.StreamSource = stream;
         image.CacheOption = BitmapCacheOption.OnLoad;
         image.EndInit();
         image.Freeze();
         return image;
      }
      catch (Exception)
      {
         // An About window that cannot show a picture is still useful; one
         // that throws on the way up is not.
         return null;
      }
   }

   private static string Format(string format, params object?[] args)
   {
      try
      {
         return string.Format(CultureInfo.CurrentCulture, format, args);
      }
      catch (FormatException)
      {
         return format;
      }
   }

   private void ViewLicence_Click(object sender, RoutedEventArgs e) =>
      NoticeWindow.ShowLicence(this);

   /// <summary>
   /// Puts the build and environment on the clipboard, in English.
   ///
   /// This is support material, not UI: it goes into a bug report read by
   /// whoever maintains the gateway, so translating it would defeat the
   /// purpose in exactly the languages where help is hardest to give.
   /// </summary>
   private void CopyProfile_Click(object sender, RoutedEventArgs e)
   {
      var port = DxKeeperTcpClient.GetDxKeeperBasePortInfo();
      var text = new StringBuilder()
         .AppendLine($"N1MM-DXKeeper Gateway {VersionString}")
         .AppendLine($"OS            : {Environment.OSVersion.VersionString}")
         .AppendLine($".NET          : {Environment.Version}")
         .AppendLine($"UI culture    : {CultureInfo.CurrentUICulture.Name} "
                     + $"(translation by {Strings.TranslatedBy})")
         .AppendLine($"Format culture: {CultureInfo.CurrentCulture.Name}")
         .AppendLine($"DXKeeper port : base {port.BasePort}, TCP {port.ServicePort}"
                     + (port.FromRegistry ? string.Empty : " (assumed default)"))
         .AppendLine($"Installed at  : {AppContext.BaseDirectory}")
         .ToString();

      try
      {
         System.Windows.Clipboard.SetText(text);
         MessageBox.Show(this, Strings.AboutProfileCopied, Strings.AppTitle,
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception)
      {
         // The clipboard can be locked by another process. Not worth a scary
         // dialog when the same details are visible in the window already.
      }
   }

   private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
