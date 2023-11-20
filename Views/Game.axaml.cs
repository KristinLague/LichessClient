using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class Game : UserControl
{
    private GameClock m_GameClock;
    private GameFull m_GameFull;
    public Game()
    {
        m_GameFull = AppController.Instance.lastGameFull;
        InitializeComponent();
        ActiveBoard.OnGameStarted(m_GameFull);
        
        PlayerName.Text = LichessAPIUtils.Username;
        OpponentName.Text =  IsPlayingWhite ? m_GameFull.black.name : m_GameFull.white.name;

        LichessAPIUtils.OnBoardUpdated += OnBoardUpdated;
        
        m_GameClock = new GameClock();
        m_GameClock.OnTimePlayer += OnTimePlayer;
        m_GameClock.OnTimeOpponent += OnTimeOpponent;
        m_GameClock.SyncWithServerTime(m_GameFull.state.wtime,m_GameFull.state.btime,IsPlayingWhite,IsPlayerTurn(m_GameFull.state.moves) );
        
        LichessAPIUtils.OnGameOver += OnGameOver;
        LichessAPIUtils.OnDrawOffered += OnDrawOffered;
    }

    private void OnDrawOffered(GameState state)
    {
        if (state.wdraw && IsPlayingWhite || state.bdraw && !IsPlayingWhite)
            return;
        
        DrawOfferPopup.IsOpen = true;
    }

    private void OnGameOver(GameState state)
    {
        m_GameClock.OnTimePlayer -= OnTimePlayer;
        m_GameClock.OnTimeOpponent -= OnTimeOpponent;
        GameOverText.Text = $"{state.winner} wins!";
        GameOverPopup.IsOpen = true;
    }

    private void OnBoardUpdated(GameState state)
    {
        if (m_GameClock == null)
        {
            return;
        }
        
        m_GameClock.SyncWithServerTime(state.wtime,state.btime,IsPlayingWhite, IsPlayerTurn(state.moves));
    }

    private void RequestDraw_Click(object sender, RoutedEventArgs e)
    {
        LichessAPIUtils.HandleDrawOfferAsync(m_GameFull.id,true);
        DrawButton.IsEnabled = false;
    }
    
    private void Resign_Click(object sender, RoutedEventArgs e)
    {
        ResignPopup.IsOpen = true;
    }
    
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        AppController.Instance.ReturnToHome();
        
    }
    
    private void CancelResign_Click(object sender, RoutedEventArgs e)
    {
        ResignPopup.IsOpen = false;
    }
    
    private void YesResign_Click(object sender, RoutedEventArgs e)
    {
        ResignPopup.IsOpen = false;
        LichessAPIUtils.ResignGame(m_GameFull.id);
    }
    
    private void AcceptDraw_Click(object sender, RoutedEventArgs e)
    {
        LichessAPIUtils.HandleDrawOfferAsync(m_GameFull.id,true);
        DrawOfferPopup.IsOpen = false; // Close the popup
    }
    
    private void DeclineDraw_Click(object sender, RoutedEventArgs e)
    {
        LichessAPIUtils.HandleDrawOfferAsync(m_GameFull.id,false);
        DrawOfferPopup.IsOpen = false; 
    }

    private bool IsPlayingWhite => m_GameFull.white.name == LichessAPIUtils.Username;
    
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
        Console.WriteLine("Disposal of Game");
        m_GameClock.OnTimePlayer -= OnTimePlayer;
        m_GameClock.OnTimeOpponent -= OnTimeOpponent;
        m_GameClock.Dispose();
        base.OnUnloaded(e);
    }
}