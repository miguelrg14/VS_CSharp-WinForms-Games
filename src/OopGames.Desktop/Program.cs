namespace OopGames.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Applies the high DPI, visual styles and text rendering settings from the .csproj.
        ApplicationConfiguration.Initialize();
        Application.Run(new LauncherForm());
    }
}
