using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
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
        int maxColumns = games.Length > 1 ? 3 : 1; // Use 1 column if only one game
        int requiredRows = (int)Math.Ceiling((double)games.Length / maxColumns);

        // Clear existing definitions and chessboards
        ChessBoardsContainer.RowDefinitions.Clear();
        ChessBoardsContainer.ColumnDefinitions.Clear();
        ChessBoardsContainer.Children.Clear();

        // Dynamically add rows and columns to the grid
        for (int i = 0; i < requiredRows; i++)
        {
            ChessBoardsContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
        for (int i = 0; i < maxColumns; i++)
        {
            ChessBoardsContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        // Calculate the maximum size that a chessboard can be, based on container size and number of rows/columns
        // Wait for the container to be properly sized - this might need to be adjusted based on your layout logic
        Size containerSize = ChessBoardsContainer.Bounds.Size;
        double maxBoardWidth = containerSize.Width / maxColumns;
        double maxBoardHeight = containerSize.Height / requiredRows;
        double boardSize = Math.Min(maxBoardWidth, maxBoardHeight);

        // Add chessboards to the grid
        for (int i = 0; i < games.Length; i++)
        {
            var chessBoard = new ChessBoardPreview();
            Console.WriteLine(games[i].fen);
            chessBoard.SetAsPreviewBoard(games[i].fullId, games[i].fen, games[i].color == "white");
            chessBoard.SetOpponentName(games[i].opponent.username);
            chessBoard.Width = boardSize;
            chessBoard.Height = boardSize;

            // Center the chessboard in its grid cell
            chessBoard.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            chessBoard.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;

            int row = i / maxColumns;
            int col = i % maxColumns;
            Grid.SetRow(chessBoard, row);
            Grid.SetColumn(chessBoard, col);
            ChessBoardsContainer.Children.Add(chessBoard);
        }
    }

    private void On_10_0_Click(object sender, RoutedEventArgs e)
    {
        // If a seek is already in progress, cancel it
        if (seekCancellationTokenSource != null)
        {
            Console.WriteLine("Cancelling the ongoing game seek...");
            seekCancellationTokenSource.Cancel();
            seekCancellationTokenSource = null;
            Button100.Content = "10+0";
            return;
        }

        Button100.Content = "Cancel";
        seekCancellationTokenSource = new CancellationTokenSource();
        LichessAPIUtils.SeekGameAsync(true, 10, 0, seekCancellationTokenSource.Token, success =>
        {
            if (success)
            {
                Console.WriteLine("Game seeking successful");
                Button100.Content = "10+0";
                LichessAPIUtils.TryGetGames(activeGames =>
                 {
                 AddChessBoards(activeGames.nowPlaying);
                 });
            }
            else
            {
                Console.WriteLine("Game seeking failed or was cancelled");
                Button100.Content = "10+0";
                // Additional logic on failure
            }

            // After completion or cancellation, reset the CancellationTokenSource
            seekCancellationTokenSource = null;
        });
    }



}