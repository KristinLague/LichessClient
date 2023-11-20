using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LichessClient.Models;
using LichessClient.Models.ChessEngine;

namespace LichessClient.Views;

public partial class Home : UserControl
{
    private CancellationTokenSource m_SeekCancellationToken;
    public Home()
    {
        InitializeComponent();
        LichessAPIUtils.Instance.TryGetProfile(profile =>
        {
            SetUserName(profile.username);
            Console.WriteLine(profile.username);
        });
        
        LichessAPIUtils.Instance.TryGetGames(activeGames =>
        {
            AddChessBoards(activeGames.nowPlaying);
        });

    }
    
    private void SetUserName(string userName)
    {
        UserNameTextBlock.Text = userName;
    }
    
    private void AddChessBoards(ActiveGame[] games)
    {
        ChessBoardsContainer.Children.Clear();

        if (games.Length == 0)
        {
            var noGamesText = new TextBlock
            {
                Text = "No active games",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            ChessBoardsContainer.Children.Add(noGamesText);
            return;
        }
        
        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        foreach (var game in games)
        {
            var chessBoard = new ChessBoardPreview();
            chessBoard.SetAsPreviewBoard(game.fullId, game.fen, game.color == "white");
            chessBoard.SetOpponentName(game.opponent.username);
            panel.Children.Add(chessBoard);
        }

        ChessBoardsContainer.Children.Add(panel);
    }
    
    private void ToggleButtonStates(bool enable, Button sender)
    {
        Button100.IsEnabled = enable;
        Button105.IsEnabled = enable;
        Button1510.IsEnabled = enable;
        Button300.IsEnabled = enable;
        Button3020.IsEnabled = enable;

        if (!enable)
        {
            (sender).IsEnabled = true;
        }
    }

    private void SeekViaButton(Button button, int time, int increment, string buttonText)
    {
        if (m_SeekCancellationToken != null)
        {
            m_SeekCancellationToken.Cancel();
            m_SeekCancellationToken = null;
            button.Content = buttonText;
            return;
        }

        button.Content = "Cancel";
        ToggleButtonStates(false, button);
        m_SeekCancellationToken = new CancellationTokenSource();
        LichessAPIUtils.Instance.SeekGameAsync(true, time, increment, m_SeekCancellationToken.Token, success =>
        {
            if (success)
            {
                button.Content = buttonText;
                LichessAPIUtils.Instance.TryGetGames(activeGames =>
                {
                    AddChessBoards(activeGames.nowPlaying);
                });
                ToggleButtonStates(true,button);
            }
            else
            {
                button.Content = buttonText;
                ToggleButtonStates(true,button);
            }

            
            m_SeekCancellationToken = null;
        });
    }


    private void On_10_0_Click(object sender, RoutedEventArgs e)
    {
        SeekViaButton(Button100,10,0,"10+0");
    }
    
    private void On_10_5_Click(object sender, RoutedEventArgs e)
    {
        SeekViaButton(Button105,10,5,"10+5");
    }
    
    private void On_15_10_Click(object sender, RoutedEventArgs e)
    {
        SeekViaButton(Button1510,15,10,"15+10");
    }
    
    private void On_30_0_Click(object sender, RoutedEventArgs e)
    {
        SeekViaButton(Button300,30,0,"30+0");
    }
    
    private void On_30_20_Click(object sender, RoutedEventArgs e)
    {
        SeekViaButton(Button3020,30,20,"30+20");
    }
}