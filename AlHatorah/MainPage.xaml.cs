using AlHatorah.Platforms.Android;
using Android.Widget;

namespace AlHatorah;

public partial class MainPage : ContentPage {
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

  private async void SaveUrl_Clicked(object sender, EventArgs e) {
    string result = await webView.EvaluateJavaScriptAsync(@$"function getLoc() {{
      console.log('About to return location');
      return document.location.href;
    }}
    getLoc();");
    await Console.Out.WriteLineAsync($"About to save {result} as the App.StartUrl");
    App.StartUrl = result;
    Preferences.Default.Set($"AH{nameof(App.StartUrl)}", result);
    Toast.MakeText(MainActivity.Instance, $"{result} saved", ToastLength.Long).Show();
  }

  private async void OnNavigated(object sender, WebNavigatedEventArgs e) {
    if (Preferences.Get($"AH{nameof(App.DarkMode)}", false)) {
      App.DarkMode = true;
      await ToggleDarkMode(App.DarkMode);
    }
  }

  private async void DarkMode_Clicked(object sender, EventArgs e) {
    bool darkMode = !App.DarkMode;
    await ToggleDarkMode(darkMode);
  }

  private async Task ToggleDarkMode(bool darkMode) {
    Preferences.Default.Set($"AH{nameof(App.DarkMode)}", darkMode);
    if (darkMode) {
      await webView.EvaluateJavaScriptAsync(@$"
        document.body.style.backgroundColor = ""#3c3c3c"";
        document.getElementsByClassName(""topbar-container"")[0].style.background = ""linear-gradient(#531b1b, #350101)"";
        const style = document.createElement(""style"");
        style.appendChild(document.createTextNode(""body:not(.copying-to-clipboard) .verse, .options-place {{ background: #242424; color: #cdcdcd; }} .options-place-list li {{ background: #3c3c3c; border-color: #757575 !important; }} .mg-dlg .close {{ color: #ffffff; }} .options-heading {{ color: #b90000; }} .options-place-list li.options-place-selected, .home-section {{ background: #282828; color: #d77070; }} .options-place-chapter:before {{ background: #282828; }} .content:before {{ color: #b7b7b7 !important; }} .sidebar.active {{ background-color: #484848; }} .sidebar a:not(.close) {{ background: #999999; }} .sidebar a:not(.close).active {{ background: #282828; }}"")); document.getElementsByTagName(""body"")[0].appendChild(style);");
      SetButtonsColours(true);
    } else {
      SetButtonsColours(false);
      webView.Reload();
    }
  }

  private void SetButtonsColours(bool dark) {
    darkButton.Text = dark ? "☀️" : "🌙";
    darkButton.Background = Color.FromArgb(dark ? "#282828" : "#fff");
    darkButton.BorderColor = Color.FromArgb(dark ? "#757575" : "#b5b5b5");
    saveButton.Background = Color.FromArgb(dark ? "#282828" : "#fff");
    saveButton.BorderColor = Color.FromArgb(dark ? "#757575" : "#b5b5b5");
    saveButton.TextColor = Color.FromArgb(dark ? "#fff" : "#000");
  }
}
