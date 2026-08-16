// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using Wpf.Ui.Controls;

using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace N1MM_DXK_GW;

/// <summary>
/// The first-run notice, and the licence viewer behind the footer link.
///
/// This is NOT a licence agreement, and is deliberately not shaped like one.
/// The GPL is a licence to copy, modify and distribute; GPLv3 section 9 states
/// that a user need not accept it in order to run the program, so a dialog
/// that blocked startup until someone "agreed" would assert a restriction the
/// licence does not make. The very common Accept/Decline page in installers is
/// mostly an artefact of installer toolkits shipping a generic EULA page.
///
/// What genuinely belongs in front of the operator is the WARRANTY DISCLAIMER.
/// Disclaimers of implied warranty are expected to be conspicuous, and for
/// this program in particular the stake is real: it is the only thing carrying
/// a contest log into DXKeeper. So the window says so plainly, offers the full
/// text, and asks only to be acknowledged.
/// </summary>
public partial class NoticeWindow : FluentWindow
{
   private const string LicenceFileName = "COPYING";
   private const string LicenceUrl = "https://www.gnu.org/licenses/gpl-3.0.html";

   public NoticeWindow()
   {
      InitializeComponent();
   }

   /// <summary>
   /// Where COPYING lives: beside the executable, which is where the installer
   /// puts it. AppContext.BaseDirectory rather than the current directory,
   /// because a shortcut can start the gateway anywhere.
   /// </summary>
   public static string LicencePath =>
      Path.Combine(AppContext.BaseDirectory, LicenceFileName);

   /// <summary>
   /// Opens the licence for reading, from wherever it is invoked.
   ///
   /// A missing COPYING is a broken install, not a reason to say nothing: the
   /// fallback states the licence and the warranty position in the message
   /// itself and points at the canonical text online, so the operator is never
   /// left without the information the file was supposed to carry.
   /// </summary>
   public static void ShowLicence(Window? owner)
   {
      var path = LicencePath;
      if (!File.Exists(path))
      {
         MessageBox.Show(owner, Localize(Strings.DlgLicenceMissing, path),
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
         return;
      }

      try
      {
         Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
      }
      catch (Exception ex)
      {
         // COPYING has no extension, so a machine with no handler for it will
         // refuse. Say where it is rather than reporting a bare Win32 error.
         MessageBox.Show(owner,
            Localize(Strings.DlgCouldNotOpenLicence, $"{ex.Message}\n\n{path}\n{LicenceUrl}"),
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
      }
   }

   private static string Localize(string format, params object?[] args)
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

   private void ViewLicence_Click(object sender, RoutedEventArgs e) => ShowLicence(this);

   private void Understood_Click(object sender, RoutedEventArgs e)
   {
      DialogResult = true;
      Close();
   }
}
