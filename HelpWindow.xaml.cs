using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace BoseSoundTouchBridge;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        Viewer.Document = HelpContent.Build(Hyperlink_RequestNavigate);
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.ToString(), UseShellExecute = true });
        }
        catch { }
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
