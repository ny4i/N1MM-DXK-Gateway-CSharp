// SPDX-License-Identifier: GPL-3.0-or-later

using System.Windows;

namespace N1MM_DXK_GW;

/// <summary>
/// Application entry point. WPF generates Main from App.xaml, so the
/// single-instance guard that used to live in Program.cs lives here.
/// </summary>
public partial class App : Application
{
   // VB6 used App.PrevInstance to enforce a single instance per Windows
   // session. A named Mutex with no namespace prefix is the per-session local
   // namespace, matching that: two users on the same machine can each run
   // their own copy, but one user cannot run two.
   //
   // This matters more than it looks. N1MM broadcasts, and broadcast datagrams
   // are delivered to EVERY socket bound to the port, so a second gateway
   // would receive the same QSOs and log every one of them to DXKeeper twice.
   // The UDP socket sets SO_REUSEADDR and so no longer refuses the second
   // bind; this mutex is the only thing preventing that.
   private const string SingleInstanceMutexName = "N1MM-DXKeeper-Gateway-SingleInstance";

   private Mutex? singleInstanceMutex;

   protected override void OnStartup(StartupEventArgs e)
   {
      // Before anything reads a string. XAML resolves its x:Static references
      // at load time, so the culture has to be in place before the first
      // window is constructed — and before the dialog below, which is the one
      // piece of UI a second instance ever sees.
      Localization.Apply(Settings.Load().Language);

      singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
      if (!createdNew)
      {
         MessageBox.Show(
            Strings.DlgAlreadyRunning,
            Strings.AppTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Information);

         // Shutdown() alone would still let OnStartup finish and the main
         // window be created, so return immediately afterwards.
         Shutdown();
         return;
      }

      base.OnStartup(e);

      // Created here rather than via StartupUri so the duplicate-instance path
      // above can return without a window ever being constructed.
      new MainWindow().Show();
   }

   protected override void OnExit(ExitEventArgs e)
   {
      singleInstanceMutex?.Dispose();
      singleInstanceMutex = null;
      base.OnExit(e);
   }
}