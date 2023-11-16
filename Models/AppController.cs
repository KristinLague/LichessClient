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
        SetAppState(e.IsSuccessful ? AppStates.Home : AppStates.Authorization);
        LichessAPIUtils.OnGameStarted += OnGameStarted;
        LichessAPIUtils.OnGameEnded += OnGameEnded;
        
        //Subscribe to EventStream
        Task.Run(() => LichessAPIUtils.EventStreamAsync(eventStreamCancellationTokenSource.Token), eventStreamCancellationTokenSource.Token);
    }

    private void OnGameEnded(Game game)
    {
        HasActiveGame = false;
        gameCancellationTokenSource?.Cancel();
    }
    
    private void SetAppState(AppStates newState)
    {
        CurrenAppState = newState;
        OnAppStateChanged?.Invoke(this, CurrenAppState);
    }

    public void PlayGame(string id)
    {
        HasActiveGame = true;
        gameCancellationTokenSource?.Cancel();
        gameCancellationTokenSource = new CancellationTokenSource();
        
        SetAppState(AppStates.Game);
        
        Task.Run(() => LichessAPIUtils.RequestStreamAsync(id, gameCancellationTokenSource.Token),
            gameCancellationTokenSource.Token);
    }

    private void OnGameStarted(Game game)
    {
        
    }

    public void StartAuthentication()
    {
        authorizationProcess.StartAuthentication();
    }
}

public enum AppStates
{
    Authorization,
    Home,
    Game
}