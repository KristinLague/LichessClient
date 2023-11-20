using System;
using System.Threading;
using System.Threading.Tasks;
using LichessClient.Models.ChessEngine;

namespace LichessClient.Models;

public class AppController
{
    public static AppController Instance => s_Lazy.Value;
    
    public AppStates CurrenAppState { get; private set; }
    public event EventHandler<AppStates> OnAppStateChanged;
    
    public GameFull LastGameFull { get; private set; }
    
    private readonly AuthorizationProcess m_AuthorizationProcess;
    
    private CancellationTokenSource m_EventStreamCancellationTokenSource;
    private CancellationTokenSource m_GameCancellationTokenSource;
    private static readonly Lazy<AppController> s_Lazy = new(() => new AppController());
    
    private AppController()
    {
        m_AuthorizationProcess = new AuthorizationProcess();
        OnAuthCompleted(this, new AuthEventArgs(AuthorizationProcess.HasToken()));
        m_AuthorizationProcess.OnAuthCompleted += OnAuthCompleted;
    }

    private void OnAuthCompleted(object sender, AuthEventArgs e)
    {
        SetAppState(e.IsSuccessful ? AppStates.Home : AppStates.Authorization);
        LichessAPIUtils.OnGameOver += OnGameEnded;
        
        m_EventStreamCancellationTokenSource?.Cancel();
        m_EventStreamCancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => LichessAPIUtils.Instance.EventStreamAsync(m_EventStreamCancellationTokenSource.Token), m_EventStreamCancellationTokenSource.Token);
    }

    private void OnGameEnded(GameState game)
    {
        m_GameCancellationTokenSource?.Cancel();
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
            if (LastGameFull != null)
            {
                OnGameEnded(null);
            }
        }
        
        SetAppState(AppStates.Home);
    }

    public void PlayGame(string id)
    {
        m_GameCancellationTokenSource?.Cancel();
        m_GameCancellationTokenSource = new CancellationTokenSource();
        LichessAPIUtils.OnGameStarted += OnGameStarted;
        
        Task.Run(() => LichessAPIUtils.Instance.GameStreamAsync(id, m_GameCancellationTokenSource.Token),
            m_GameCancellationTokenSource.Token);
    }

    private void OnGameStarted(GameFull game)
    {
        LastGameFull = game;
        SetAppState(AppStates.Game);
    }

    public void StartAuthentication()
    {
        m_AuthorizationProcess.StartAuthentication();
    }

    public void Dispose()
    {
        m_EventStreamCancellationTokenSource?.Cancel(true);
        m_GameCancellationTokenSource?.Cancel(true);
    }
}

public enum AppStates
{
    Authorization,
    Home,
    Game
}