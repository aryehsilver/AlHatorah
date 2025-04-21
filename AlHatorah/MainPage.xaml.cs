using AlHatorah.Platforms.Android;
using Android.Widget;

namespace AlHatorah;

public partial class MainPage : ContentPage {
  public MainPage() {
    InitializeComponent();
    SetSource();
  }

  private void SetSource() {
    webView.Source = Preferences.Default.Get(nameof(App.StartUrl), App.StartUrl);
    App.SetUrl = false;
  }

  protected override void OnAppearing() {
    base.OnAppearing();
    SetSource();
  }

  protected override bool OnBackButtonPressed() {
    if (webView.CanGoBack) {
      webView.GoBack();
      return true;
    } else {
      return base.OnBackButtonPressed();
    }
  }

  private async void OnNavigating(object sender, WebNavigatingEventArgs args) {
    await Console.Out.WriteLineAsync(args.Url);

    if (!args.Url.Contains("alhatorah.org")) {
      try {
        await Browser.OpenAsync(args.Url, BrowserLaunchMode.SystemPreferred);
      } catch {
        Toast.MakeText(MainActivity.Instance, "Failed to launch application.", ToastLength.Long).Show();
      }
      args.Cancel = true;
    } else {
      if (!string.IsNullOrWhiteSpace(App.StartUrl) && App.SetUrl) {
        args.Cancel = true;
        SetSource();
      } else {
        Preferences.Default.Set(nameof(App.StartUrl), args.Url);
      }
    }
  }
}
