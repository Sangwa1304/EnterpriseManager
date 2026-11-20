using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace EnterpriseManager.Client.Avalonia;

[Activity(
    Label = "EnterpriseManager",
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation |
                           ConfigChanges.ScreenSize |
                           ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity<App>
{
}
