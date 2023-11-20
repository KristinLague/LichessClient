using Avalonia;
using System;
using Avalonia.ReactiveUI;
using LichessClient;
using LichessClient.Models;

class Program
{
    private static AppController s_AppController;
    
    [STAThread]
    public static void Main(string[] args)
    {
        s_AppController = AppController.Instance;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().UseReactiveUI();
    
    
}
