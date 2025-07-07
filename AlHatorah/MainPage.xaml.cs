using AlHatorah.Platforms.Android;
using Android.Widget;
using Button = Microsoft.Maui.Controls.Button;

namespace AlHatorah;

public partial class MainPage : ContentPage {
  private const string _sun = "\uf185";
  private const string _moon = "\uf186";

  public MainPage() {
    InitializeComponent();
    SetSource();
  }

  private void SetSource() {
    webView.Source = Preferences.Default.Get($"AH{nameof(App.StartUrl)}", App.StartUrl);
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
        Preferences.Default.Set($"AH{nameof(App.StartUrl)}", args.Url);
      }
    }
  }

  private async void OnNavigated(object sender, WebNavigatedEventArgs e) {
    await webView.EvaluateJavaScriptAsync(@$"document.querySelector('div:has(iframe[src*=youtube])').style.display='none';");
    if (App.DarkMode || Preferences.Get($"AH{nameof(App.DarkMode)}", false)) {
      App.DarkMode = true;
      await ToggleDarkMode(App.DarkMode);
    }
  }

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

  private async void OnSaveClicked(object sender, EventArgs e) {
    string result = await webView.EvaluateJavaScriptAsync(@$"function getLoc() {{
      console.log('About to return location');
      return document.location.href;
    }}
    getLoc();");
    await Console.Out.WriteLineAsync($"About to save {result} as the App.StartUrl");
    App.StartUrl = result;
    Preferences.Default.Set($"AH{nameof(App.StartUrl)}", result);
    Toast.MakeText(MainActivity.Instance, $"{result} saved", ToastLength.Long).Show();
    await HidePopupMenu();
  }

  private async void OnRefreshClicked(object sender, EventArgs e) {
    await HidePopupMenu();
    webView.Reload();
  }

  private async void OnDarkModeClicked(object sender, EventArgs e) {
    App.DarkMode = !App.DarkMode;
    await ToggleDarkMode(App.DarkMode);
    await HidePopupMenu();
  }

  private async Task ToggleDarkMode(bool darkMode) {
    FloatingButton.BackgroundColor = Color.FromArgb(darkMode ? "#3C3C3C" : "#FFF");
    FloatingButton.TextColor = Color.FromArgb(darkMode ? "#FFF" : "#000");

    PopupMenu.BackgroundColor = Color.FromArgb(darkMode ? "#3C3C3C" : "#fff");
    PopupMenu.Stroke = Color.FromArgb(darkMode ? "#757575" : "#b5b5b5");

    DarkModeButton.Text = darkMode ? _sun : _moon;
    DarkModeButton.TextColor = Color.FromArgb(darkMode ? "#fff" : "#000");

    Grid grid = (Grid)PopupMenu.Content;
    for (int i = 0; i < grid.Children.Count; i++) {
      if (grid.Children[i] is Button btn && btn != DarkModeButton) {
        btn.TextColor = Color.FromArgb(darkMode ? "#fff" : "#000");
      }
    }

    Preferences.Default.Set($"AH{nameof(App.DarkMode)}", darkMode);
    if (darkMode) {
      await webView.EvaluateJavaScriptAsync(@$"
        document.body.style.backgroundColor = ""#3c3c3c"";
        document.getElementsByClassName(""topbar-container"")[0].style.background = ""linear-gradient(#531b1b, #350101)"";
        const style = document.createElement(""style"");
        style.appendChild(document.createTextNode(""body:not(.copying-to-clipboard) .verse, .options-place {{ background: #242424; color: #cdcdcd; }} .options-place-list li {{ background: #3c3c3c; border-color: #757575 !important; }} .mg-dlg .close {{ color: #ffffff; }} .options-heading {{ color: #b90000; }} .options-place-list li.options-place-selected, .home-section {{ background: #282828; color: #d77070; }} .options-place-chapter:before {{ background: #282828; }} .content:before {{ color: #b7b7b7 !important; }} .sidebar.active {{ background-color: #484848; }} .sidebar a:not(.close) {{ background: #999999; }} .sidebar a:not(.close).active {{ background: #282828; }} .white-btn {{ background: linear-gradient(#000000, #000000 25%, #2d2d2d); color: #fff; border: 1px solid #000000; border-bottom-color: #000000; }}"")); document.getElementsByTagName(""body"")[0].appendChild(style);");
    } else {
      webView.Reload();
    }
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
}
