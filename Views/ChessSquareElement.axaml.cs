using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace LichessClient.Views;

public partial class ChessSquareElement : UserControl
{
    public enum Color
    {
        light,
        dark
    }

    public enum Piece
    {
        pawn,
        knight,
        bishop,
        rook,
        queen,
        king,
        none
    }
    
    private Color currentColor;
    public Color CurrentColor
    {
        get => currentColor;
        set
        {
            currentColor = value;
            SetSquareColor(currentColor);
        }
    }

    private Color pieceColor;
    public Color PieceColor
    {
        get => pieceColor;
        set
        {
            pieceColor = value;
            SetPieceImage(currentPiece);
        }
    }
    
    public Action OnClick { get; set; }

    public string Coordinate { get; set; }
    
    private Piece currentPiece;
    public Piece CurrentPiece
    {
        get => currentPiece;
        set
        {
            currentPiece = value;
            SetPieceImage(currentPiece);
        }
    }
    public ChessSquareElement()
    {
        InitializeComponent();
    }
    
    public void SetSquareColor(Color color)
    {
        SquareBorder.Background = new SolidColorBrush(color == Color.light ? new Avalonia.Media.Color(255,0,0,0) : new Avalonia.Media.Color(255,255,255,255));
    }

    public void SetPieceImage(Piece piece)
    {
        var imageName = piece switch
        {
            Piece.pawn => (pieceColor == Color.light ? "w_" : "b_") + "pawn_2x.png",
            Piece.rook => (pieceColor == Color.light ? "w_" : "b_") + "rook_2x.png",
            Piece.bishop => (pieceColor == Color.light ? "w_" : "b_") + "bishop_2x.png",
            Piece.knight => (pieceColor == Color.light ? "w_" : "b_") + "knight_2x.png",
            Piece.king => (pieceColor == Color.light ? "w_" : "b_") + "king_2x.png",
            Piece.queen => (pieceColor == Color.light ? "w_" : "b_") + "queen_2x.png",
            _ => null,
        };

        if (imageName != null)
        {
            PieceImageControl.Source = new Bitmap($"Images/ChessPieces/{imageName}");
        }
        
        PieceImageControl.IsVisible = imageName != null;
    }
    private void SquareBorder_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        OnClick?.Invoke();
    }
    
    public void SetLastMove(bool lastMove)
    {
        if (lastMove)
        {
            SquareBorder.Background = new SolidColorBrush(currentColor == Color.light ? new Avalonia.Media.Color(255,110,244,0) : new Avalonia.Media.Color(255,31,90,0));
        }
        else
        {
            SetSquareColor(currentColor);
        }
    }
    
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            SquareBorder.Background = new SolidColorBrush(new Avalonia.Media.Color(255,150,90,214));
        }
        else
        {
            SetSquareColor(currentColor);
        }
    }

    public void SetCheck(bool check)
    {
         if (check)
         {
             SquareBorder.Background = new SolidColorBrush(new Avalonia.Media.Color(255,255,0,0));
         }
         else
         {
             SetSquareColor(currentColor);
         }
    }
    
    public void SetMarker(bool marker)
    {
        if (marker)
        {
            SquareBorder.Background = new SolidColorBrush(new Avalonia.Media.Color(255,150,90,139));
        }
        else
        {
            SetSquareColor(currentColor);
        }
    }
}