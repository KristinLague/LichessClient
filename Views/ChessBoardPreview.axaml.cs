using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chess.Core;

namespace LichessClient.Views;

public partial class ChessBoardPreview : UserControl
{
    private List<ChessSquareElement> boardElements = new List<ChessSquareElement>();
    public Dictionary<string,ChessSquareElement> squares = new Dictionary<string, ChessSquareElement>();
    private bool isPlayingWhite;
    private string fenString = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public ChessBoardPreview()
    {
        InitializeComponent();
    }

    public void SetAsPreviewBoard(string fen, bool isWhite)
    {
        isPlayingWhite = isWhite;
        fenString = fen;
        PopulateBoard();
        SetupBoard(isPlayingWhite);
        SetupBoardFromFEN(fenString);
    }
    public void SetOpponentName(string opponentName)
    {
        OpponentNameTextBlock.Text = opponentName;
    }


    private void SetupBoardFromFEN(string fen)
    {
        Console.WriteLine("setup " + fen);
        ClearBoard();
        string boardFen = fen.Split(' ')[0];
        string[] rows = boardFen.Split('/');

        for (int rank = 8, i = 0; rank >= 1; rank--, i++)
        {
            int file = 0;

            foreach (char character in rows[i])
            {
                if (Char.IsDigit(character))
                {
                    int emptySpaces = Int32.Parse(character.ToString());
                    file += emptySpaces;
                }
                else
                {
                    string coordinate = $"{(char)('a' + file)}{rank}";
                    if (squares.ContainsKey(coordinate))
                    {
                        ChessSquareElement square = squares[coordinate];
                        LogPieceInfo(square, character);
                    }
                    else
                    {
                        Console.WriteLine("The coordinate " + coordinate + " does not exist in the squares dictionary.");
                    }

                    file++;
                }
            }
        }
    }

    string GetFENFromMoves(string[] moves)
    {
        Board board = new Board();
        board.LoadStartPosition();
        foreach (string moveString in moves)
        {
            board.MakeMove(MoveUtility.MoveFromName(moveString, board));
        }

        return FenUtility.CurrentFen(board);
    }
    
    private void ClearBoardVisualElements()
    {
        squares.Clear();
        squares = new Dictionary<string, ChessSquareElement>();
    }

    public void SetupBoard(bool white)
    {
        ClearBoardVisualElements();
        isPlayingWhite = white;
        if (isPlayingWhite)
        {
            SetupBoardForWhite();
        }
        else
        {
            SetupBoardForBlack();
        }
        SetupSquareColorByCoordinate();
        SetupBoardFromFEN(fenString);
    }

    private void SetupBoardForWhite()
    {
        Console.WriteLine("SETUP BOARD FOR WHITE");
        int i = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                string coordinate = $"{(char)(x + 97)}{8 - y}";
                ChessSquareElement square = boardElements[i];
                square.Coordinate = coordinate;
                squares.Add(coordinate, square);
                i++;
            }
        }
    }

    private void SetupBoardForBlack()
    {
        Console.WriteLine("SETUP BOARD FOR BLACK");
        int i = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                string coordinate = $"{(char)(104 - x)}{1 + y}";
                ChessSquareElement square = boardElements[i];
                square.Coordinate = coordinate;
                squares.Add(coordinate, square);
                i++;
            }
        }
    }

    private void SetupSquareColorByCoordinate()
    {
        foreach (var kvp in squares)
        {
            int x = kvp.Key[0] - 'a'; // this will give a value from 0 to 7
            int y = kvp.Key[1] - '1'; // this will give a value from 0 to 7

            if((x + y) % 2 == 0)
            {
                kvp.Value.CurrentColor = ChessSquareElement.Color.dark;
            }
            else
            {
                kvp.Value.CurrentColor = ChessSquareElement.Color.light;
            }
        }
    }



    
    private void LogPieceInfo(ChessSquareElement element,char fenChar)
    {
        if (Char.IsLetter(fenChar))
        {
            element.PieceColor = Char.IsLower(fenChar) ? ChessSquareElement.Color.dark : ChessSquareElement.Color.light;
            char pieceType = Char.ToLower(fenChar);
            
            switch (pieceType)
            {
                case 'p':
                    element.CurrentPiece = ChessSquareElement.Piece.pawn;
                    break;
                case 'r':
                    element.CurrentPiece = ChessSquareElement.Piece.rook;
                    break;
                case 'n':
                    element.CurrentPiece = ChessSquareElement.Piece.knight;
                    break;
                case 'b':
                    element.CurrentPiece = ChessSquareElement.Piece.bishop;
                    break;
                case 'q':
                    element.CurrentPiece = ChessSquareElement.Piece.queen;
                    break;
                case 'k': 
                    element.CurrentPiece = ChessSquareElement.Piece.king;
                    break;
                default:
                    element.CurrentPiece = ChessSquareElement.Piece.none;
                    break;
            }
        }
    }
    
    private void ClearBoard()
    {
        foreach (var kvp in squares)
        {
            kvp.Value.CurrentPiece = ChessSquareElement.Piece.none;
        }
    }
    
    private void PopulateBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var chessSquare = new ChessSquareElement();
                
                var isLightSquare = (row + col) % 2 == 0;
                chessSquare.SetSquareColor(isLightSquare ? ChessSquareElement.Color.light : ChessSquareElement.Color.dark);
                chessSquare.SetPieceImage(ChessSquareElement.Piece.pawn);
                
                ChessBoardGrid.Children.Add(chessSquare);
                Grid.SetRow(chessSquare, row);
                Grid.SetColumn(chessSquare, col);
                boardElements.Add(chessSquare);
            }
        }
    }
}