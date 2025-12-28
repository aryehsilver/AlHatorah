using AlHatorah.Platforms.Android;
using Android.Widget;
using System.Collections.Specialized;
using Timer = System.Timers.Timer;

namespace AlHatorah;

public partial class MainPage : ContentPage, IDisposable {
  private Timer _saveTimer;
  private string _originalUserAgent;

  public MainPage() {
    InitializeComponent();
    SetSource();

    // Load persisted desktop mode preference
    App.DesktopMode = Preferences.Default.Get($"AH{nameof(App.DesktopMode)}", App.DesktopMode);

    SetupSaveTimer();

    // Update desktop/mobile icon state immediately (safe if handler not ready)
    ApplyDesktopMode();
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
    // Intercept JS -> native bridge messages using a custom app scheme
    if (!string.IsNullOrEmpty(args.Url) && args.Url.StartsWith("app://refresh", StringComparison.OrdinalIgnoreCase)) {
      try {
        // parse query ?enabled=1 or 0
        Uri uri = new(args.Url);
        NameValueCollection query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        string enabled = query.Get("enabled");
        bool isEnabled = enabled is "1" or "true";
        // Only update UI on main thread
        MainThread.BeginInvokeOnMainThread(() => {
          try {
            refreshView.IsEnabled = isEnabled;
          } catch { }
        });
      } catch { }

      args.Cancel = true;
      return;
    }

    await Console.Out.WriteLineAsync(args.Url);

    if (!args.Url.Contains("alhatorah.org")) {
      try {
        await Browser.OpenAsync(args.Url, BrowserLaunchMode.SystemPreferred);
      } catch {
        Toast.MakeText(Android.App.Application.Context, "Failed to launch application.", ToastLength.Long)?.Show();
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
    // Inject JS to detect touches inside scrollable elements (modals) and notify the app
    try {
      await webView.EvaluateJavaScriptAsync(@"(function(){
        if(window.__mauiRefreshBridgeInstalled) return;
        window.__mauiRefreshBridgeInstalled = true;

        function send(enabled){
          try{
            // use replace to avoid polluting history
            window.location.replace('app://refresh?enabled=' + (enabled ? '1' : '0'));
          }catch(e){}
        }

        function isScrollable(el){
          if(!el) return false;
          try{
            var style = window.getComputedStyle(el);
            if((style.overflowY === 'auto' || style.overflowY === 'scroll') && el.scrollHeight > el.clientHeight) return true;
            return el.scrollHeight > el.clientHeight;
          }catch(e){return false;}
        }

        var insideScrollable = false;

        document.addEventListener('touchstart', function(e){
          var t = e.target;
          var found = null;
          while(t && t !== document.body){
            if(isScrollable(t)){ found = t; break; }
            t = t.parentElement;
          }
          insideScrollable = !!found;
          if(insideScrollable){ send(false); }
          else { send(window.pageYOffset===0); }
        }, {passive:true});

        document.addEventListener('touchmove', function(e){ if(insideScrollable){ send(false); } }, {passive:true});
        document.addEventListener('touchend', function(e){ insideScrollable = false; send(window.pageYOffset===0); }, {passive:true});
        window.addEventListener('scroll', function(){ send(window.pageYOffset===0); }, {passive:true});
      })();");
    } catch { }

    await webView.EvaluateJavaScriptAsync(@$"
      document.querySelector('div:has(iframe[src*=youtube])').style.display='none';
      document.getElementsByClassName(""topbar-container"")[0].style.background = ""linear-gradient(#531b1b, #350101)"";
    ");

    // Ensure viewport allows zoom (some sites disable pinch-to-zoom via meta tag)
    try {
      await webView.EvaluateJavaScriptAsync(@"(function(){
        try{
          var m = document.querySelector('meta[name=viewport]');
          if(m){
            m.setAttribute('content','width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=1');
          }else{
            var meta = document.createElement('meta');
            meta.name = 'viewport';
            meta.content = 'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=1';
            document.head.appendChild(meta);
          }
        }catch(e){}
      })();");
    } catch { }

    // Stop RefreshView refreshing state if pull-to-refresh was used
    try {
      refreshView.IsRefreshing = false;
    } catch { /* ignore if not present */ }

    // Apply desktop mode if enabled (ensures UA applied after handler initialization)
    ApplyDesktopMode();
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
      PopupMenu.ScaleToAsync(1, 200, Easing.CubicOut),
      PopupMenu.FadeToAsync(1, 200)
    );
  }

  private async void OnOverlayTapped(object sender, EventArgs e) =>
    await HidePopupMenu();

  private async Task HidePopupMenu() {
    // Animate popup menu disappearing
    await Task.WhenAll(
      PopupMenu.ScaleToAsync(0.1, 150, Easing.CubicIn),
      PopupMenu.FadeToAsync(0, 150)
    );

    //PopupOverlay.IsVisible = false;
    PopupMenu.IsVisible = false;
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

  private void OnRefreshRequested(object sender, EventArgs e) {
    try {
      webView.Reload();
    } catch { }

    try {
      refreshView.IsRefreshing = false;
    } catch { }
  }

  private void ApplyDesktopMode() {
    try {
      // Update native Android WebView user agent when possible
      if (webView.Handler?.PlatformView is Android.Webkit.WebView native) {
        try {
          // Capture original UA once
          if (string.IsNullOrEmpty(_originalUserAgent)) {
            _originalUserAgent = native.Settings.UserAgentString;
          }

          string desiredUA = App.DesktopMode
            ? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36"
            : _originalUserAgent ?? native.Settings.UserAgentString;

          string currentUA = native.Settings.UserAgentString ?? "";

          // Only update and reload if the UA actually changes to avoid reload loops
          if (!string.Equals(currentUA, desiredUA, StringComparison.Ordinal)) {
            native.Settings.UserAgentString = desiredUA;

            // Apply zoom-out behavior on the UI thread and reload afterwards
            MainThread.BeginInvokeOnMainThread(() => {
              try {
                // Ensure wide viewport and overview mode
                native.Settings.UseWideViewPort = true;
                native.Settings.LoadWithOverviewMode = true;

                // Try to force initial scale to minimum
                try { native.SetInitialScale(1); } catch { }

                // Repeatedly call ZoomOut while it's available to get as far out as possible
                try {
                  while (native.ZoomOut()) { /* noop until no more zoom out */ }
                } catch { }

                // Finally reload to apply UA and scale
                try { webView.Reload(); } catch { }
              } catch { }
            });
          }
        } catch { }
      }
    } catch { }

    try {
      // Show mobile icon when in desktop mode, and desktop icon when in mobile mode
      DesktopButton.Text = App.DesktopMode ? "\uf3cf" : "\uf108";
    } catch { }
  }

  private async void OnDesktopModeClicked(object sender, EventArgs e) {
    // Toggle flag and persist
    App.DesktopMode = !App.DesktopMode;
    Preferences.Default.Set($"AH{nameof(App.DesktopMode)}", App.DesktopMode);

    ApplyDesktopMode();

    await HidePopupMenu();
  }

  public void Dispose() =>
    _saveTimer?.Dispose();
}
