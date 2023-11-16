using System;
using System.Threading;
using System.Threading.Tasks;

namespace LichessClient.Models;

public class AppController
{
    private static readonly Lazy<AppController> s_Lazy = new(() => new AppController());
    public static AppController Instance => s_Lazy.Value;
    
    private readonly AuthorizationProcess authorizationProcess;
    public AppStates CurrenAppState { get; private set; }
    public event EventHandler<AppStates> OnAppStateChanged;
    
    private readonly CancellationTokenSource eventStreamCancellationTokenSource = new();
    private CancellationTokenSource gameCancellationTokenSource;
    
    public bool HasActiveGame { get; private set; }
    
    
    
    private AppController()
    {
        authorizationProcess = new AuthorizationProcess();
        OnAuthCompleted(this, new AuthEventArgs(AuthorizationProcess.HasToken()));
        authorizationProcess.OnAuthCompleted += OnAuthCompleted;
    }

    private void OnAuthCompleted(object sender, AuthEventArgs e)
    {
        CurrenAppState = e.IsSuccessful ? AppStates.Home : AppStates.Authorization;
        OnAppStateChanged?.Invoke(this, CurrenAppState);
        LichessAPIUtils.OnGameStarted += OnGameStarted;
        LichessAPIUtils.OnGameEnded += OnGameEnded;
        Task.Run(() => LichessAPIUtils.EventStreamAsync(eventStreamCancellationTokenSource.Token), eventStreamCancellationTokenSource.Token);
    }

    private void OnGameEnded(Game game)
    {
        HasActiveGame = false;
        gameCancellationTokenSource?.Cancel();
    }

    private void OnGameStarted(Game game)
    {
        HasActiveGame = true;
        gameCancellationTokenSource?.Cancel();
        gameCancellationTokenSource = new CancellationTokenSource();
        
        Task.Run(() => LichessAPIUtils.RequestStreamAsync(game.fullId, gameCancellationTokenSource.Token), gameCancellationTokenSource.Token);
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