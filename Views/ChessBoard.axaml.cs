using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Chess.Core;
using LichessClient.Models;
using LichessClient.Models.ChessEngine;
using GameState = LichessClient.Models.ChessEngine.GameState;

namespace LichessClient.Views;

public partial class ChessBoard : UserControl
{
    private List<ChessSquareElement> m_BoardElements = new List<ChessSquareElement>();
    private Dictionary<string,ChessSquareElement> m_Squares = new Dictionary<string, ChessSquareElement>();
    
    private string m_FenString = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private string m_CurrentGameId;
    
    private bool m_IsPlayingWhite;
    private bool m_IsPlayerTurn;
    
    private ChessSquareElement m_LastCheck;
    private ChessSquareElement m_SelectedSquare;
    private ChessSquareElement[] m_LastLegalMove;
    private ChessSquareElement m_LastStartMove;
    private ChessSquareElement m_LastEndMove;
    
    public ChessBoard()
    {
        InitializeComponent();
        PopulateBoard();
        SetupBoard(m_IsPlayingWhite);
        RefreshBoard("");
    }

    public void OnGameStarted(GameFull gameFull)
    {
        m_CurrentGameId = gameFull.id;
        m_IsPlayingWhite = gameFull.white.name == LichessAPIUtils.Instance.Username;
        SetupBoard(m_IsPlayingWhite);
        m_FenString = gameFull.initialFen;
        m_IsPlayerTurn = IsPlayerTurn(gameFull.state.moves);
        
        RefreshBoard(gameFull.state.moves);
        
        if (InCheck(m_FenString))
        {
            if (m_IsPlayerTurn)
            {
                var pos = GetKingPosition(m_FenString, m_IsPlayingWhite);
                m_Squares[pos].SetCheck(true);
                m_LastCheck = m_Squares[pos];
            }
            else
            {
                var pos = GetKingPosition(m_FenString, !m_IsPlayingWhite);
                m_Squares[pos].SetCheck(true);
                m_LastCheck = m_Squares[pos];
            }
        }
        
        MarkLastMove(gameFull.state.moves);
        
        LichessAPIUtils.OnBoardUpdated += OnBoardUpdated;
    }
    
    private void OnSquareSelected(ChessSquareElement square)
    {
        Console.WriteLine(square.Coordinate);
        if (!m_IsPlayerTurn)
        {
            Reset(false);
            return;
        }

        if (m_SelectedSquare != null)
        {
            if (m_SelectedSquare == square)
            {
                Reset(false);
                return;
            }

            if (m_LastLegalMove != null)
            {

                if (m_LastLegalMove.Contains(square))
                {
                    if(m_IsPlayingWhite && square.Coordinate.Contains("8") || !m_IsPlayingWhite && square.Coordinate.Contains("1"))
                    {
                        if (m_SelectedSquare.CurrentPiece == ChessSquareElement.Piece.pawn)
                        {
                            Console.WriteLine("promotion");
                            //TODO: Show promotion dialog for now force to queen
                            LichessAPIUtils.Instance.MakeMove(m_CurrentGameId, m_SelectedSquare.Coordinate + square.Coordinate + "Q");
                            return;
                        }
                    }
                    
                    LichessAPIUtils.Instance.MakeMove(m_CurrentGameId, m_SelectedSquare.Coordinate + square.Coordinate);
                }
                else
                {
                    if (square.PieceColor == ChessSquareElement.Color.light && m_IsPlayingWhite ||
                        square.PieceColor == ChessSquareElement.Color.dark && !m_IsPlayingWhite)
                    {
              
                        Reset(false);
                        
                        m_SelectedSquare = square;
                        m_SelectedSquare.SetSelected(true);
                    
                        var legalMoves = GetLegalTargetMoveSquares(m_SelectedSquare.Coordinate, m_FenString);
                        m_LastLegalMove = new ChessSquareElement[legalMoves.Length];
                        for (int i = 0; i < legalMoves.Length; i++)
                        {
                            m_LastLegalMove[i] = m_Squares[legalMoves[i]];
                            m_LastLegalMove[i].SetMarker(true);
                        }
                    }
                    else
                    {
                        Reset(false);
                    }
                }
            }
        }
        else
        {
            if (square.PieceColor == ChessSquareElement.Color.light && m_IsPlayingWhite ||
                square.PieceColor == ChessSquareElement.Color.dark && !m_IsPlayingWhite)
            {
                m_SelectedSquare = square;
                m_SelectedSquare.SetSelected(true);
                    
                var legalMoves = GetLegalTargetMoveSquares(m_SelectedSquare.Coordinate, m_FenString);
                m_LastLegalMove = new ChessSquareElement[legalMoves.Length];
                for (int i = 0; i < legalMoves.Length; i++)
                {
                    m_LastLegalMove[i] = m_Squares[legalMoves[i]];
                    m_LastLegalMove[i].SetMarker(true);
                }
            }
        }
    }
    
    private void Reset(bool force)
    {
        if (m_SelectedSquare != null)
        {
            m_SelectedSquare.SetSelected(false);
            m_SelectedSquare = null;
        }

        if (m_LastLegalMove != null)
        {
            foreach (var move in m_LastLegalMove)
            {
                move.SetMarker(false);
            }
            m_LastLegalMove = null;
        }

        if (force)
        {
            if (m_LastCheck != null)
            {
                m_LastCheck.SetCheck(false);
                m_LastCheck = null;
            }
        
            if(m_LastStartMove != null)
            {
                m_LastStartMove.SetLastMove(false);
                m_LastStartMove = null;
            }
        
            if(m_LastEndMove != null)
            {
                m_LastEndMove.SetLastMove(false);
                m_LastEndMove = null;
            }
        }
    }

    private string[] GetLegalTargetMoveSquares(string startSquareName, string fen)
    {
        Board board = new Board();
        board.LoadPosition(fen);
        MoveGenerator moveGenerator = new MoveGenerator();
        var moves = moveGenerator.GenerateMoves(board);

        int startSquareIndex = BoardHelper.SquareIndexFromName(startSquareName);
        List<string> targetSquareNames = new List<string>();
        
        foreach (Move move in moves)
        {
            if (move.StartSquare == startSquareIndex)
            {
                targetSquareNames.Add(BoardHelper.SquareNameFromIndex(move.TargetSquare));
            }
        }
        
        return targetSquareNames.ToArray();
    }

    private void OnBoardUpdated(GameState update)
    {
        RefreshBoard(update.moves);
        m_IsPlayerTurn = IsPlayerTurn(update.moves);

        Reset(true);
        if (InCheck(m_FenString))
        {
            if (m_IsPlayerTurn)
            {
                var pos = GetKingPosition(m_FenString, m_IsPlayingWhite);
                m_Squares[pos].SetCheck(true);
                m_LastCheck = m_Squares[pos];
            }
            else
            {
                var pos = GetKingPosition(m_FenString, !m_IsPlayingWhite);
                m_Squares[pos].SetCheck(true);
                m_LastCheck = m_Squares[pos];
            }
        }

        MarkLastMove(update.moves);
    }
    
    private void MarkLastMove(string moves)
    {
        if (moves == "")
        {
            return;
        }
        
        var totalMoves = moves.Split(' ');
        if (totalMoves.Length > 0)
        {
            var lastMove = totalMoves[totalMoves.Length - 1];
            
            if (m_LastStartMove != null)
            {
                m_LastStartMove.SetLastMove(false);
            }

            if(m_LastEndMove != null)
                m_LastEndMove.SetLastMove(false);
                
            var from = lastMove.Substring(0, 2);
            var to = lastMove.Substring(2, 2);
            m_Squares[from].SetLastMove(true);
            m_LastStartMove = m_Squares[from];
            m_Squares[to].SetLastMove(true);
            m_LastEndMove = m_Squares[to];
        }
    }

    private bool IsPlayerTurn(string moves)
    {
        if (moves == "")
        {
            return m_IsPlayingWhite;
        }
        
        string[] totalMoves = moves.Split(' ');
        return (m_IsPlayingWhite && totalMoves.Length % 2 == 0) ||
               (!m_IsPlayingWhite && totalMoves.Length % 2 == 1);
    }
    
    bool InCheck(string fen)
    {
        Board board = new();
        board.LoadPosition(fen);
        return board.IsInCheck();
    }

    private string GetKingPosition(string fen, bool isWhite)
    {
        return FindKingPosition(fen, isWhite ? 'K' : 'k');
    }
    
    private string FindKingPosition(string fen, char king)
    {
        string[] parts = fen.Split(' ');
        string[] ranks = parts[0].Split('/');

        for (int i = 0; i < ranks.Length; i++)
        {
            int file = 0;
            foreach (char c in ranks[i])
            {
                if (c >= '1' && c <= '8')
                {
                    file += (int)char.GetNumericValue(c);
                }
                else
                {
                    if (c == king)
                    {
                        return "" + (char)('a' + file) + (8 - i);
                    }
                    file++;
                }
            }
        }

        return null;
    }

    private void RefreshBoard(string moves)
    {
        if (moves == "")
        {
            m_FenString = GetFENFromMoves(Array.Empty<string>());
            SetupBoardFromFEN(m_FenString);
        }
        else
        {
            string[] individualMoves = moves.Split(' ');
            m_FenString = GetFENFromMoves(individualMoves);
            SetupBoardFromFEN(m_FenString);
        }
    }

    private void SetupBoardFromFEN(string fen)
    {
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
                    if (m_Squares.ContainsKey(coordinate))
                    {
                        ChessSquareElement square = m_Squares[coordinate];
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
        m_Squares.Clear();
        m_Squares = new Dictionary<string, ChessSquareElement>();
    }

    private void SetupBoard(bool white)
    {
        ClearBoardVisualElements();
        m_IsPlayingWhite = white;
        if (m_IsPlayingWhite)
        {
            SetupBoardForWhite();
        }
        else
        {
            SetupBoardForBlack();
        }
        SetupSquareColorByCoordinate();
        SetupBoardFromFEN(m_FenString);
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
                ChessSquareElement square = m_BoardElements[i];
                square.Coordinate = coordinate;
                m_Squares.Add(coordinate, square);
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
                ChessSquareElement square = m_BoardElements[i];
                square.Coordinate = coordinate;
                m_Squares.Add(coordinate, square);
                i++;
            }
        }
    }

    private void SetupSquareColorByCoordinate()
    {
        foreach (var kvp in m_Squares)
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
        foreach (var kvp in m_Squares)
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
                chessSquare.OnClick = () => OnSquareSelected(chessSquare);
                
                ChessBoardGrid.Children.Add(chessSquare);
                Grid.SetRow(chessSquare, row);
                Grid.SetColumn(chessSquare, col);
                m_BoardElements.Add(chessSquare);
            }
        }
    }
}