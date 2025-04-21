using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace AlHatorah.Platforms.Android;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter([Intent.ActionView],
  Categories = [
    Intent.ActionView,
    Intent.CategoryDefault,
    Intent.CategoryBrowsable
  ],
  DataSchemes = ["http", "https"],
  DataHost = "alhatorah.org",
  AutoVerify = true
  )
]
public class MainActivity : MauiAppCompatActivity {
  public static MainActivity Instance { get; private set; }

  protected override void OnCreate(Bundle savedInstanceState) {
    DoIntent(Intent);
    base.OnCreate(savedInstanceState);
    Instance = this;
  }

  protected override void OnNewIntent(Intent intent) {
    base.OnNewIntent(intent);
    DoIntent(intent);
  }

  private static void DoIntent(Intent intent) {
    if (Intent.ActionView == intent.Action && !string.IsNullOrWhiteSpace(intent.DataString)) {
      //handle intent routing
      App.StartUrl = intent.DataString;
      App.SetUrl = true;
      System.Diagnostics.Debug.WriteLine($"Intent received: {intent.Data}");
      System.Diagnostics.Debug.WriteLine($"Intent received: {intent.DataString}");
    }
  }
}
