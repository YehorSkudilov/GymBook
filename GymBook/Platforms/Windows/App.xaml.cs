using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GymBook.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            // TEMP diagnostics: dump unhandled exceptions to %LOCALAPPDATA%\GymBook-crash.txt.
            UnhandledException += (_, e) => DumpCrash("WinUI", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => DumpCrash("AppDomain", e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, e) => DumpCrash("Task", e.Exception);
        }

        static void DumpCrash(string source, Exception? ex)
        {
            var text = $"[{DateTime.Now:O}] {source}\n{ex}\n\n";
            System.Diagnostics.Debug.WriteLine(text);
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GymBook-crash.txt");
                File.AppendAllText(path, text);
            }
            catch
            {
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
