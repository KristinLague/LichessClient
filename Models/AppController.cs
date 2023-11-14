using System;

namespace LichessClient.Models;

public class AppController
{
    private static readonly Lazy<AppController> s_Lazy = new(() => new AppController());
    public static AppController Instance => s_Lazy.Value;
    
    private readonly AuthorizationProcess authorizationProcess;
    public AppStates CurrenAppState { get; private set; }
    public event EventHandler<AppStates> OnAppStateChanged;
    
    
    private AppController()
    {
        authorizationProcess = new AuthorizationProcess();
        CurrenAppState = AuthorizationProcess.HasToken() ? AppStates.Home : AppStates.Authorization;
        OnAppStateChanged?.Invoke(this, CurrenAppState);
        authorizationProcess.OnAuthCompleted += OnAuthCompleted;
    }

    private void OnAuthCompleted(object sender, AuthEventArgs e)
    {
        CurrenAppState = e.IsSuccessful ? AppStates.Home : AppStates.Authorization;
        OnAppStateChanged?.Invoke(this, CurrenAppState);
    }

    public void StartAuthentication()
    {
        authorizationProcess.StartAuthentication();
    }
}

public enum AppStates
{
    Authorization,
    Home
}