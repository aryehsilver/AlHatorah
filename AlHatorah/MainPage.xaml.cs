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

  private async void OnNavigated(object sender, WebNavigatedEventArgs e) =>
    // TODO: Make this a floating button that saves to preferences and load accordingly
    await webView.EvaluateJavaScriptAsync(@$"
      document.body.style.backgroundColor = ""#3c3c3c"";
      var elements = document.getElementsByClassName(""verse"");
      for (var i = 0; i < elements.length; i++) {{
        elements[i].style.background = ""#242424"";
        elements[i].style.color = ""#cdcdcd"";
      }}
      document.getElementsByClassName(""topbar-container"")[0].style.background = ""linear-gradient(#531b1b, #350101)"";
      const style = document.createElement(""style"");
      style.appendChild(document.createTextNode("".content:before {{ color: #b7b7b7 !important; }} .sidebar.active {{ background-color: #484848; }} .sidebar a:not(.close) {{ background: #999999; }} .sidebar a:not(.close).active {{ background: #282828; }}""));
      document.getElementsByTagName(""body"")[0].appendChild(style);
    ");
}
