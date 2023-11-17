using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chess.Core;
using LichessClient.Models;
using GameState = LichessClient.Models.GameState;

namespace LichessClient.Views;

public partial class ChessBoard : UserControl
{
    private List<ChessSquareElement> boardElements = new List<ChessSquareElement>();
    public Dictionary<string,ChessSquareElement> squares = new Dictionary<string, ChessSquareElement>();
    private bool isPlayingWhite;
    private string fenString = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private string currentGameId;
    private bool isPlayerTurn;
    private ChessSquareElement lastCheck;
    private ChessSquareElement selectedSquare;
    private ChessSquareElement[] lastLegalMoves;
    private ChessSquareElement lastStartMove;
    private ChessSquareElement lastEndMove;
    
    public ChessBoard()
    {
        InitializeComponent();
        PopulateBoard();
        SetupBoard(isPlayingWhite);
        RefreshBoard("");
    }

    public void OnGameStarted(GameFull gameFull)
    {
        currentGameId = gameFull.id;
        //tab.label = gameFull.white.name + " vs " + gameFull.black.name;
        isPlayingWhite = gameFull.white.name == LichessAPIUtils.Username;
        SetupBoard(isPlayingWhite);
        fenString = gameFull.initialFen;
        //SetupPlayers(gameFull);
        isPlayerTurn = IsPlayerTurn(gameFull.state.moves);
        //opponentClockElement.AddToClassList(isPlayerTurn ? "background__primary" : "background__accent");
        //opponentClockElement.RemoveFromClassList(isPlayerTurn ? "background__accent" : "background__primary");
        //playerClockElement.AddToClassList(isPlayerTurn ? "background__accent" : "background__primary");
        //playerClockElement.RemoveFromClassList(isPlayerTurn ? "background__primary" : "background__accent");
        
        RefreshBoard(gameFull.state.moves);
        
        if (InCheck(fenString))
        {
            if (isPlayerTurn)
            {
                var pos = GetKingPosition(fenString, isPlayingWhite);
                squares[pos].SetCheck(true);
                lastCheck = squares[pos];
            }
            else
            {
                var pos = GetKingPosition(fenString, !isPlayingWhite);
                squares[pos].SetCheck(true);
                lastCheck = squares[pos];
            }
        }
        
        MarkLastMove(gameFull.state.moves);
        
        LichessAPIUtils.OnBoardUpdated += OnBoardUpdated;
        LichessAPIUtils.OnGameOver += OnGameOver;
    }

    private void OnGameOver(GameState obj)
    {
        //throw new NotImplementedException();
        Console.WriteLine("On Game Over");
    }
    
    private void OnSquareSelected(ChessSquareElement square)
    {
        Console.WriteLine(square.Coordinate);
        if (!isPlayerTurn)
        {
            Console.WriteLine("not player turn ");
            //Reset selection
            Reset(false);
            return;
        }

        if (selectedSquare != null)
        {
            if (selectedSquare == square)
            {
                Console.WriteLine("same square");
                //Reset
                Reset(false);
                return;
            }

            if (lastLegalMoves != null)
            {

                if (lastLegalMoves.Contains(square))
                {
                    if(isPlayingWhite && square.Coordinate.Contains("8") || !isPlayingWhite && square.Coordinate.Contains("1"))
                    {
                        if (selectedSquare.CurrentPiece == ChessSquareElement.Piece.pawn)
                        {
                            Console.WriteLine("promotion");
                            //TODO: Show promotion dialog for now force to queen
                            LichessAPIUtils.MakeMove(currentGameId, selectedSquare.Coordinate + square.Coordinate + "Q");
                            return;
                        }
                    }
                    
                    //TODO: Make move
                    LichessAPIUtils.MakeMove(currentGameId, selectedSquare.Coordinate + square.Coordinate);
                }
                else
                {
                    if (square.PieceColor == ChessSquareElement.Color.light && isPlayingWhite ||
                        square.PieceColor == ChessSquareElement.Color.dark && !isPlayingWhite)
                    {
                        //Reset
                        Reset(false);
                        
                        selectedSquare = square;
                        selectedSquare.SetSelected(true);
                    
                        var legalMoves = GetLegalTargetMoveSquares(selectedSquare.Coordinate, fenString);
                        Console.WriteLine(legalMoves.Length + " legal moves " + selectedSquare.Coordinate);
                        lastLegalMoves = new ChessSquareElement[legalMoves.Length];
                        for (int i = 0; i < legalMoves.Length; i++)
                        {
                            lastLegalMoves[i] = squares[legalMoves[i]];
                            lastLegalMoves[i].SetMarker(true);
                        }
                    }
                    else
                    {
                        Console.WriteLine("not legal move");
                        //Reset
                        Reset(false);
                    }
                }
            }
            else
            {
                Console.WriteLine("last legal moves null");
            }
        }
        else
        {
            if (square.PieceColor == ChessSquareElement.Color.light && isPlayingWhite ||
                square.PieceColor == ChessSquareElement.Color.dark && !isPlayingWhite)
            {
                selectedSquare = square;
                selectedSquare.SetSelected(true);
                    
                var legalMoves = GetLegalTargetMoveSquares(selectedSquare.Coordinate, fenString);
                Console.WriteLine(fenString + " " + legalMoves.Length + " legal moves " + selectedSquare.Coordinate);
                lastLegalMoves = new ChessSquareElement[legalMoves.Length];
                for (int i = 0; i < legalMoves.Length; i++)
                {
                    lastLegalMoves[i] = squares[legalMoves[i]];
                    lastLegalMoves[i].SetMarker(true);
                }
            }
        }
    }
    
    private void Reset(bool force)
    {
        if (selectedSquare != null)
        {
            selectedSquare.SetSelected(false);
            selectedSquare = null;
        }

        if (lastLegalMoves != null)
        {
            foreach (var move in lastLegalMoves)
            {
                move.SetMarker(false);
            }
            lastLegalMoves = null;
        }

        if (force)
        {
            if (lastCheck != null)
            {
                lastCheck.SetCheck(false);
                lastCheck = null;
            }
        
            if(lastStartMove != null)
            {
                lastStartMove.SetLastMove(false);
                lastStartMove = null;
            }
        
            if(lastEndMove != null)
            {
                lastEndMove.SetLastMove(false);
                lastEndMove = null;
            }
        }
    }
    
    public string[] GetLegalTargetMoveSquares(string startSquareName, string fen)
    {
        Board board = new Board();
        board.LoadPosition(fen);
        MoveGenerator moveGenerator = new MoveGenerator();
        var moves = moveGenerator.GenerateMoves(board);

        int startSquareIndex = BoardHelper.SquareIndexFromName(startSquareName);
        List<string> targetSquareNames = new List<string>();
        
        Console.WriteLine("Total legal moves " + moves.Length);
        Console.WriteLine("Filter: " + startSquareName + " " + startSquareIndex);
        foreach (Move move in moves)
        {
            if (move.StartSquare == startSquareIndex)
            {
                targetSquareNames.Add(BoardHelper.SquareNameFromIndex(move.TargetSquare));
            }
        }
        Console.WriteLine("Num filtered: " + targetSquareNames.Count);
        
        return targetSquareNames.ToArray();
    }

    private void OnBoardUpdated(GameState update)
    {
        RefreshBoard(update.moves);
        isPlayerTurn = IsPlayerTurn(update.moves);
        
        //opponentClockElement.AddToClassList(isPlayerTurn ? "background__primary" : "background__accent");
        //opponentClockElement.RemoveFromClassList(isPlayerTurn ? "background__accent" : "background__primary");
        //playerClockElement.AddToClassList(isPlayerTurn ? "background__accent" : "background__primary");
        //playerClockElement.RemoveFromClassList(isPlayerTurn ? "background__primary" : "background__accent");
        
        //GameClock.Instance.SyncWithServerTime(update.wtime,update.btime,isPlayingWhite, isPlayerTurn);

        Reset(true);
        if (InCheck(fenString))
        {
            if (isPlayerTurn)
            {
                var pos = GetKingPosition(fenString, isPlayingWhite);
                squares[pos].SetCheck(true);
                lastCheck = squares[pos];
            }
            else
            {
                var pos = GetKingPosition(fenString, !isPlayingWhite);
                squares[pos].SetCheck(true);
                lastCheck = squares[pos];
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
        Console.WriteLine(totalMoves.Length + "lenght");
        if (totalMoves.Length > 0)
        {
            var lastMove = totalMoves[totalMoves.Length - 1];
            
            if (lastStartMove != null)
            {
                Console.WriteLine("what");
                lastStartMove.SetLastMove(false);
            }

            if(lastEndMove != null)
                lastEndMove.SetLastMove(false);
                
            var from = lastMove.Substring(0, 2);
            var to = lastMove.Substring(2, 2);
            squares[from].SetLastMove(true);
            lastStartMove = squares[from];
            squares[to].SetLastMove(true);
            lastEndMove = squares[to];
        }
    }

    private bool IsPlayerTurn(string moves)
    {
        if (moves == "")
        {
            return isPlayingWhite;
        }
        
        string[] totalMoves = moves.Split(' ');
        return (isPlayingWhite && totalMoves.Length % 2 == 0) ||
               (!isPlayingWhite && totalMoves.Length % 2 == 1);
    }
    
    bool InCheck(string fen)
    {
        Board board = new();
        board.LoadPosition(fen);
        return board.IsInCheck();
    }
    
    public string GetKingPosition(string fen, bool isWhite)
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

    public void SetupGame(string fen, bool isWhite)
    {
        isPlayingWhite = isWhite;
        fenString = fen;
        PopulateBoard();
        SetupBoard(isPlayingWhite);
        SetupBoardFromFEN(fenString);
    }

    private void RefreshBoard(string moves)
    {
        if (moves == "")
        {
            fenString = GetFENFromMoves(Array.Empty<string>());
            SetupBoardFromFEN(fenString);
        }
        else
        {
            string[] individualMoves = moves.Split(' ');
            fenString = GetFENFromMoves(individualMoves);
            SetupBoardFromFEN(fenString);
        }
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
                chessSquare.OnClick = () => OnSquareSelected(chessSquare);
                
                ChessBoardGrid.Children.Add(chessSquare);
                Grid.SetRow(chessSquare, row);
                Grid.SetColumn(chessSquare, col);
                boardElements.Add(chessSquare);
            }
        }
    }
}