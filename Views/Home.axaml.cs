using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class Home : UserControl
{
    private CancellationTokenSource seekCancellationTokenSource;
    public Home()
    {
        InitializeComponent();
        LichessAPIUtils.TryGetProfile(profile =>
        {
            SetUserName(profile.username);
            Console.WriteLine(profile.username);
        });
        
        LichessAPIUtils.TryGetGames(activeGames =>
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

        // Using StackPanel for single column, switch to WrapPanel for multiple columns
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
        // If a seek is already in progress, cancel it
        if (seekCancellationTokenSource != null)
        {
            Console.WriteLine("Cancelling the ongoing game seek...");
            seekCancellationTokenSource.Cancel();
            seekCancellationTokenSource = null;
            button.Content = buttonText;
            return;
        }

        button.Content = "Cancel";
        ToggleButtonStates(false, button);
        seekCancellationTokenSource = new CancellationTokenSource();
        LichessAPIUtils.SeekGameAsync(true, time, increment, seekCancellationTokenSource.Token, success =>
        {
            if (success)
            {
                Console.WriteLine("Game seeking successful");
                button.Content = buttonText;
                LichessAPIUtils.TryGetGames(activeGames =>
                {
                    AddChessBoards(activeGames.nowPlaying);
                });
                ToggleButtonStates(true,button);
            }
            else
            {
                Console.WriteLine("Game seeking failed or was cancelled");
                button.Content = buttonText;
                ToggleButtonStates(true,button);
            }

            
            seekCancellationTokenSource = null;
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