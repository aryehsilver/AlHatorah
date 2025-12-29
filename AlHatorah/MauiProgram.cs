using Android.Content;
using Android.Webkit;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace AlHatorah;

public static class MauiProgram {
  public static MauiApp CreateMauiApp() {
    MauiAppBuilder builder = MauiApp.CreateBuilder();
    builder
      .UseMauiApp<App>()
      .ConfigureFonts(fonts => {
        fonts.AddFont("Font Awesome 6 Free-Solid-900.otf", "FontAwesome");
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
      });

    // Enable pinch-to-zoom and ensure JS/DOM storage on Android WebView
    WebViewHandler.Mapper.AppendToMapping("EnableZoom", (handler, view) => {
      try {
        if (handler.PlatformView is Android.Webkit.WebView native) {
          WebSettings settings = native.Settings;
          settings.JavaScriptEnabled = true;
          settings.DomStorageEnabled = true;
          settings.BuiltInZoomControls = true;
          settings.DisplayZoomControls = false; // hide on-screen zoom controls
          settings.SetSupportZoom(true);
          settings.UseWideViewPort = true;
          settings.LoadWithOverviewMode = true;
        }
      } catch { }
    });

    // Add Android-only long-press link context menu (open, copy, share)
    WebViewHandler.Mapper.AppendToMapping("LongPressMenu", (handler, view) => {
      try {
        if (handler.PlatformView is Android.Webkit.WebView native) {
          // Ensure long clicks are enabled
          native.LongClickable = true;
          // Use the handler's context (Activity) if available, otherwise fall back to application context
          Context ctx = handler.MauiContext?.Context ?? Android.App.Application.Context;
          native.SetOnLongClickListener(new WebViewLongClickListener(ctx));
        }
      } catch { }
    });

#if DEBUG
    builder.Logging.AddDebug();
#endif

    return builder.Build();
  }
}
