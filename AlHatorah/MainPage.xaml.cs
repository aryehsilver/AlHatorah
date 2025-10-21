using AlHatorah.Platforms.Android;
using Android.Widget;
using Timer = System.Timers.Timer;

namespace AlHatorah;

public partial class MainPage : ContentPage, IDisposable {
  private Timer _saveTimer;

  public MainPage() {
    InitializeComponent();
    SetSource();
    SetupSaveTimer();
  }

  private void SetSource() {
    webView.Source = Preferences.Default.Get($"AH{nameof(App.StartUrl)}", App.StartUrl);
    App.SetUrl = false;
  }

  private void SetupSaveTimer() {
    _saveTimer = new(5000) { AutoReset = true, Enabled = true };
    _saveTimer.Elapsed += (sender, e) => {
      MainThread.BeginInvokeOnMainThread(async () => {
        string result = await webView.EvaluateJavaScriptAsync(@$"function getLoc() {{
          console.log('About to return location');
          return document.location.href;
        }}
        getLoc();");
        await Console.Out.WriteLineAsync($"About to save {result} as the App.StartUrl");
        App.StartUrl = result;
        Preferences.Default.Set($"AH{nameof(App.StartUrl)}", result);
      });
    };
    _saveTimer.Start();
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
        Preferences.Default.Set($"AH{nameof(App.StartUrl)}", args.Url);
      }
    }
  }

  private async void OnNavigated(object sender, WebNavigatedEventArgs e) =>
    await webView.EvaluateJavaScriptAsync(@$"
      document.querySelector('div:has(iframe[src*=youtube])').style.display='none';
      document.getElementsByClassName(""topbar-container"")[0].style.background = ""linear-gradient(#531b1b, #350101)"";
    ");

  private async void OnFloatingButtonClicked(object sender, EventArgs e) {
    if (PopupMenu.IsVisible) {
      await HidePopupMenu();
    } else {
      await ShowPopupMenu();
    }
  }

  private async Task ShowPopupMenu() {
    //PopupOverlay.IsVisible = true;
    PopupMenu.IsVisible = true;

    // Animate the popup menu
    PopupMenu.Scale = 0.1;
    PopupMenu.Opacity = 0;

    await Task.WhenAll(
      PopupMenu.ScaleTo(1, 200, Easing.CubicOut),
      PopupMenu.FadeTo(1, 200)
    );
  }

  private async void OnOverlayTapped(object sender, EventArgs e) =>
    await HidePopupMenu();

  private async Task HidePopupMenu() {
    // Animate popup menu disappearing
    await Task.WhenAll(
      PopupMenu.ScaleTo(0.1, 150, Easing.CubicIn),
      PopupMenu.FadeTo(0, 150)
    );

    //PopupOverlay.IsVisible = false;
    PopupMenu.IsVisible = false;
  }

  private async void OnRefreshClicked(object sender, EventArgs e) {
    await HidePopupMenu();
    webView.Reload();
  }

  private async void OnBackClicked(object sender, EventArgs e) {
    await HidePopupMenu();

    if (webView.CanGoBack) {
      webView.GoBack();
      if (!webView.CanGoBack) {
        BackButton.IsEnabled = false;
      }
    }
  }

  private async void OnForwardClicked(object sender, EventArgs e) {
    await HidePopupMenu();

    if (webView.CanGoForward) {
      webView.GoForward();
      if (!webView.CanGoForward) {
        ForwardButton.IsEnabled = false;
      }
    }
  }

  public void Dispose() =>
    _saveTimer?.Dispose();
}
