using System;
using Avalonia;
using Avalonia.Controls;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class Home : UserControl
{
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
            chessBoard.SetAsPreviewBoard(games[i].fen, games[i].color == "white");
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


}