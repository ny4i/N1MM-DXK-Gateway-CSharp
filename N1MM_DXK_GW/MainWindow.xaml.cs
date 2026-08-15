using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace N1MM_DXK_GW;

public partial class MainWindow : FluentWindow
{
   public MainWindow()
   {
      InitializeComponent();

      // Follow the operator's Windows light/dark setting, and repaint if they
      // change it while the gateway is running. A contest station is often run
      // in a dark shack; forcing either theme would be the wrong call for
      // someone, and Windows already knows their preference.
      SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, updateAccents: true);
   }
}
