using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LichessClient.Views;

public partial class ChessBoard : UserControl
{
    public ChessBoard()
    {
        InitializeComponent();
        PopulateBoard();
    }
    
    private void PopulateBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var chessSquare = new ChessSquareElement();

                // Set square color based on position
                var isLightSquare = (row + col) % 2 == 0;
                chessSquare.SetSquareColor(isLightSquare ? ChessSquareElement.Color.light : ChessSquareElement.Color.dark);
                chessSquare.SetPieceImage(ChessSquareElement.Piece.pawn);
                // Add the chess square to the grid
                ChessBoardGrid.Children.Add(chessSquare);
                Grid.SetRow(chessSquare, row);
                Grid.SetColumn(chessSquare, col);
            }
        }
    }
}