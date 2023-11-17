using System;
using System.Threading;
using System.Threading.Tasks;
using LichessClient.Views;

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
    public AppStates lastAppState;
    public GameFull lastGameFull { get; private set; }
    
    private AppController()
    {
        LichessAPIUtils.InitializeClient();
        authorizationProcess = new AuthorizationProcess();
        OnAuthCompleted(this, new AuthEventArgs(AuthorizationProcess.HasToken()));
        authorizationProcess.OnAuthCompleted += OnAuthCompleted;
    }

    private void OnAuthCompleted(object sender, AuthEventArgs e)
    {
        SetAppState(e.IsSuccessful ? AppStates.Home : AppStates.Authorization);
        LichessAPIUtils.OnGameOver += OnGameEnded;
        
        //Subscribe to EventStream
        Task.Run(() => LichessAPIUtils.EventStreamAsync(eventStreamCancellationTokenSource.Token), eventStreamCancellationTokenSource.Token);
    }

    private void OnGameEnded(GameState game)
    {
        HasActiveGame = false;
        gameCancellationTokenSource?.Cancel();
        LichessAPIUtils.OnGameStarted -= OnGameStarted;
    }
    
    private void SetAppState(AppStates newState)
    {
        CurrenAppState = newState;
        OnAppStateChanged?.Invoke(this, CurrenAppState);
    }

    public void ReturnToHome()
    {
        if (CurrenAppState == AppStates.Game)
        {
            if (lastGameFull != null)
            {
                OnGameEnded(null);
            }
        }
        
        SetAppState(AppStates.Home);
    }

    public void PlayGame(string id)
    {
        HasActiveGame = true;
        gameCancellationTokenSource?.Cancel();
        gameCancellationTokenSource = new CancellationTokenSource();
        LichessAPIUtils.OnGameStarted += OnGameStarted;
        
        Task.Run(() => LichessAPIUtils.RequestStreamAsync(id, gameCancellationTokenSource.Token),
            gameCancellationTokenSource.Token);
    }

    private void OnGameStarted(GameFull game)
    {
        Console.WriteLine("Game started");
        lastGameFull = game;
        SetAppState(AppStates.Game);
    }


    public void StartAuthentication()
    {
        authorizationProcess.StartAuthentication();
    }

    public void Dispose()
    {
        eventStreamCancellationTokenSource.Cancel(true);
        gameCancellationTokenSource?.Cancel(true);
    }
}

public enum AppStates
{
    Authorization,
    Home,
    Game
}