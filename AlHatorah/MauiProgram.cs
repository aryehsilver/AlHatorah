using Microsoft.Extensions.Logging;

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

#if DEBUG
    builder.Logging.AddDebug();
#endif

    return builder.Build();
  }
}
