using Avalonia.Controls;
using Avalonia.Interactivity;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class Game : UserControl
{
    private GameClock gameClock;
    private GameFull gameFull;
    public Game()
    {
        gameFull = AppController.Instance.lastGameFull;
        InitializeComponent();
        ActiveBoard.OnGameStarted(gameFull);
        
        PlayerName.Text = LichessAPIUtils.Username;
        OpponentName.Text =  IsPlayingWhite ? gameFull.black.name : gameFull.white.name;
        
        LichessAPIUtils.OnBoardUpdated += OnBoardUpdated;
        gameClock = new GameClock();
        gameClock.OnTimePlayer += OnTimePlayer;
        gameClock.OnTimeOpponent += OnTimeOpponent;
        gameClock.SyncWithServerTime(gameFull.state.wtime,gameFull.state.btime,IsPlayingWhite,IsPlayerTurn(gameFull.state.moves) );
        LichessAPIUtils.OnGameOver += OnGameOver;
        LichessAPIUtils.OnDrawOffered += OnDrawOffered;
    }

    private void OnDrawOffered(GameState state)
    {
        if (state.wdraw && IsPlayingWhite || state.bdraw && !IsPlayingWhite)
            return;
        
        DrawOfferPopup.IsOpen = true;
    }

    private void OnGameOver(GameState obj)
    {
        gameClock.OnTimePlayer -= OnTimePlayer;
        gameClock.OnTimeOpponent -= OnTimeOpponent;
    }

    private void OnBoardUpdated(GameState state)
    {
        gameClock.SyncWithServerTime(state.wtime,state.btime,IsPlayingWhite, IsPlayerTurn(state.moves));
    }

    private void OnRequestDraw(object sender, RoutedEventArgs e)
    {
        LichessAPIUtils.HandleDrawOfferAsync(gameFull.id,true);
        DrawButton.IsEnabled = false;
    }
    
    private void OnResign(object sender, RoutedEventArgs e)
    {
        ResignPopup.IsOpen = true;
    }
    
    private void CancelResign_Click(object sender, RoutedEventArgs e)
    {
        ResignPopup.IsOpen = false;
    }
    
    private void YesResign_Click(object sender, RoutedEventArgs e)
    {
        ResignPopup.IsOpen = false;
        LichessAPIUtils.ResignGame(gameFull.id);
    }
    
    private void AcceptDraw_Click(object sender, RoutedEventArgs e)
    {
        LichessAPIUtils.HandleDrawOfferAsync(gameFull.id,true);
        DrawOfferPopup.IsOpen = false; // Close the popup
    }
    
    private void DeclineDraw_Click(object sender, RoutedEventArgs e)
    {
        LichessAPIUtils.HandleDrawOfferAsync(gameFull.id,false);
        DrawOfferPopup.IsOpen = false; 
    }

    private bool IsPlayingWhite => gameFull.white.name == LichessAPIUtils.Username;
    
    private bool IsPlayerTurn(string moves)
    {
        if (moves == "")
        {
            return IsPlayingWhite;
        }
        
        string[] totalMoves = moves.Split(' ');
        return (IsPlayingWhite && totalMoves.Length % 2 == 0) ||
               (!IsPlayingWhite && totalMoves.Length % 2 == 1);
    }
    
    private void OnTimeOpponent(string time)
    {
        OpponentClock.Text = time;
    }

    private void OnTimePlayer(string time)
    {
       PlayerClock.Text = time;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        gameClock.OnTimePlayer -= OnTimePlayer;
        gameClock.OnTimeOpponent -= OnTimeOpponent;
        gameClock.Dispose();
        base.OnUnloaded(e);
    }
}