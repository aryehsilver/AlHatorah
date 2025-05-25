namespace AlHatorah;

public partial class App : Application {
  public static string StartUrl = "https://alhatorah.org";
  public static bool SetUrl = false;
  public static bool DarkMode = false;

  public App() =>
    InitializeComponent();

  protected override Window CreateWindow(IActivationState activationState) =>
    new(new MainPage());
}
