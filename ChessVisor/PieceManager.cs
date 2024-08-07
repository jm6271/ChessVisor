using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ChessNotationParser;
using System.Drawing;

namespace ChessVisor
{
    class PieceManager
    {
        public List<ChessPiece> WhitePieces = [];
        public List<ChessPiece> BlackPieces = [];

        private int FindPieceIndex(ChessPiece.Square location, ChessPiece.PieceColor color)
        {
            List<ChessPiece> lst;

            if (color == ChessPiece.PieceColor.White)
                lst = WhitePieces;
            else
                lst = BlackPieces;

            for (int i = 0; i < lst.Count; i++)
            {
                if (lst[i].Position == location) return i;
            }

            return -1;
        }

        public void PlayGame(List<ChessMove> moves)
        {
            // create a storyboard with all the moves in it
            Storyboard board = new();
            int AnimationBeginTime = 0;

            // loop through moves and create the animation for each one
            foreach (var move in moves)
            {
                // find out what kind of move this is
                if (move.MoveInfo.move.type == ChessNotationParser.AST.AST.NODE_TYPE.StandardMove)
                {
                    var standardMove = (ChessNotationParser.AST.StandardMove)move.MoveInfo.move;

                    // First we have to find out which square the piece to move is
                    // already on

                    ChessPiece.Square CurrentSquare;
                    ChessPiece.PieceType CurrentPieceType;
                    bool Capture;
                    

                    if (standardMove.capture == null) Capture = false;
                    else Capture = true;

                    if (standardMove.piece != null)
                    {
                        switch (standardMove.piece.PieceType)
                        {
                            case ChessNotationParser.AST.Piece.Pieces.King:
                                CurrentPieceType = ChessPiece.PieceType.King;
                                break;
                            case ChessNotationParser.AST.Piece.Pieces.Queen:
                                CurrentPieceType = ChessPiece.PieceType.Queen;
                                break;
                            case ChessNotationParser.AST.Piece.Pieces.Rook:
                                CurrentPieceType = ChessPiece.PieceType.Rook;
                                break;
                            case ChessNotationParser.AST.Piece.Pieces.Bishop:
                                CurrentPieceType = ChessPiece.PieceType.Bishop;
                                break;
                            case ChessNotationParser.AST.Piece.Pieces.Knight:
                                CurrentPieceType = ChessPiece.PieceType.Knight;
                                break;
                            default:
                                throw new Exception("Unrecognized piece type");
                        }
                    }
                    else
                    {
                        CurrentPieceType = ChessPiece.PieceType.Pawn;
                    }

                    if (move.PieceColor == ChessPiece.PieceColor.White)
                    {
                        CurrentSquare = GetCurrentSquareWhite(new ChessPiece.Square(standardMove.square), CurrentPieceType, Capture);
                    }
                    else
                    {
                        CurrentSquare = GetCurrentSquareBlack(new ChessPiece.Square(standardMove.square), CurrentPieceType, Capture);
                    }

                    // create the animation object and add it to the storyboard
                    var animations = CreateMoveAnimation(new ChessPiece.Square(standardMove.square), CurrentSquare);
                    animations[0].BeginTime = TimeSpan.FromSeconds(AnimationBeginTime);
                    animations[1].BeginTime = TimeSpan.FromSeconds(AnimationBeginTime);
                    AnimationBeginTime++;

                    int index = FindPieceIndex(CurrentSquare, move.PieceColor);
                    if (index == -1) throw new Exception("Error: Invalid move.");

                    Wpf.Ui.Controls.Image img;

                    if (move.PieceColor == ChessPiece.PieceColor.White)
                    {
                        WhitePieces[index].Position = new ChessPiece.Square(standardMove.square);
                        var mainWindow = Application.Current.MainWindow;
                        img = (Wpf.Ui.Controls.Image)mainWindow.FindName(WhitePieces[index].ImageControlName);
                    }
                    else
                    {
                        BlackPieces[index].Position = new ChessPiece.Square(standardMove.square);
                        var mainWindow = Application.Current.MainWindow;
                        img = (Wpf.Ui.Controls.Image)mainWindow.FindName(BlackPieces[index].ImageControlName);
                    }

                    TranslateTransform translateTransform = new();
                    img.RenderTransform = translateTransform;

                    Storyboard.SetTarget(animations[0], img);
                    Storyboard.SetTargetProperty(animations[0], new PropertyPath("RenderTransform.Y"));
                    Storyboard.SetTarget(animations[1], img);
                    Storyboard.SetTargetProperty(animations[1], new PropertyPath("RenderTransform.X"));
                    board.Children.Add(animations[0]);
                    board.Children.Add(animations[1]);
                }

                
            }

            board.Begin();
        }

        public DoubleAnimation[] CreateMoveAnimation(ChessPiece.Square Destination, ChessPiece.Square CurrentLocation)
        {
            DoubleAnimation animY = new(0, GetYFromRank(Destination.Rank) - GetYFromRank(CurrentLocation.Rank), TimeSpan.FromSeconds(1));
            DoubleAnimation animX = new(0, GetXFromFile(Destination.File) - GetXFromFile(CurrentLocation.File), TimeSpan.FromSeconds(1));

            DoubleAnimation[] r = [animY, animX];
            return r;
        }

        public void Move(ChessPiece.PieceColor color, ChessPiece.PieceType pieceType, ChessPiece.Square Destination)
        {
            
        }

        private int GetXFromFile(char file)
        {
            int r = file switch
            {
                'A' => 0,
                'B' => 75,
                'C' => 75 * 2,
                'D' => 75 * 3,
                'E' => 75 * 4,
                'F' => 75 * 5,
                'G' => 75 * 6,
                'H' => 75 * 7,
                _ => 0,
            };
            return r;
        }

        private int GetYFromRank(short Rank)
        {
            int r =  Rank switch
            {
                8 => 0,
                7 => 75,
                6 => 75 * 2,
                5 => 75 * 3,
                4 => 75 * 4,
                3 => 75 * 5,
                2 => 75 * 6,
                1 => 75 * 7,
                _ => 0
            };
            return r;
        }

        public PieceManager()
        {

        }

        private ChessPiece.Square GetCurrentSquareWhite(ChessPiece.Square destination, ChessPiece.PieceType type, bool capture = false, short? DepartureRank = null, char? DepartureFile = null)
        {
            // make a list of all the possible squares the piece could be on
            List<ChessPiece.Square> squareList = [];

            switch (type)
            {
                case ChessPiece.PieceType.Pawn:
                    if (capture)
                    {
                        // the file of the departure square should be specified
                        if (DepartureFile != null)
                        {
                            ChessPiece.Square departure = new();
                            departure.File = (char)DepartureFile;
                            departure.Rank = (short)(destination.Rank - 1);
                            squareList.Add(departure);
                        }
                        else
                        {
                            throw new Exception("Invalid Syntax: Pawn captures should specify the departure square");
                        }
                    }
                    else
                    {
                        // if rank is equal to 4, then the piece could be on rank 2 or 3. The file will remain the same.
                        // else, the rank will be one square less than the destination rank.
                        if (destination.Rank == 4)
                        {
                            ChessPiece.Square departure2 = new();
                            departure2.Rank = 2;
                            departure2.File = destination.File;

                            ChessPiece.Square departure3 = new();
                            departure3.Rank = 3;
                            departure3.File = destination.File;

                            squareList.Add(departure2);
                            squareList.Add(departure3);
                        }
                        else
                        {
                            ChessPiece.Square departure = new();
                            departure.File = destination.File;
                            departure.Rank = (short)(destination.Rank - 1);
                            squareList.Add(departure);
                        }
                    }
                    break;
                case ChessPiece.PieceType.Knight:
                    break;
                case ChessPiece.PieceType.Bishop:
                    break;
                case ChessPiece.PieceType.Rook:
                    break;
                case ChessPiece.PieceType.Queen:
                    break;
                case ChessPiece.PieceType.King:
                    break;
                default:
                    break;
            }

            // loop through the list of possible squares to find out which one is correct
            foreach (var square in squareList) 
            { 
                if (FindPieceIndex(square, ChessPiece.PieceColor.White) != -1)
                    return square;
            }

            throw new Exception("Could not find piece to move");
        }

        private ChessPiece.Square GetCurrentSquareBlack(ChessPiece.Square destination, ChessPiece.PieceType type, bool capture = false, short? DepartureRank = null, char? DepartureFile = null)
        {
            // make a list of all the possible squares the piece could be on
            List<ChessPiece.Square> squareList = [];

            switch (type)
            {
                case ChessPiece.PieceType.Pawn:
                    if (capture)
                    {
                        // the file of the departure square should be specified
                        if (DepartureFile != null)
                        {
                            ChessPiece.Square departure = new();
                            departure.File = (char)DepartureFile;
                            departure.Rank = (short)(destination.Rank + 1);
                            squareList.Add(departure);
                        }
                        else
                        {
                            throw new Exception("Invalid Syntax: Pawn captures should specify the departure square");
                        }
                    }
                    else
                    {
                        // if rank is equal to 5, then the piece could be on rank 6 or 7. The file will remain the same.
                        // else, the rank will be one square more than the destination rank.
                        if (destination.Rank == 5)
                        {
                            ChessPiece.Square departure6 = new();
                            departure6.Rank = 6;
                            departure6.File = destination.File;

                            ChessPiece.Square departure7 = new();
                            departure7.Rank = 7;
                            departure7.File = destination.File;

                            squareList.Add(departure6);
                            squareList.Add(departure7);
                        }
                        else
                        {
                            ChessPiece.Square departure = new();
                            departure.File = destination.File;
                            departure.Rank = (short)(destination.Rank + 1);
                            squareList.Add(departure);
                        }
                    }
                    break;
                case ChessPiece.PieceType.Knight:
                    break;
                case ChessPiece.PieceType.Bishop:
                    break;
                case ChessPiece.PieceType.Rook:
                    break;
                case ChessPiece.PieceType.Queen:
                    break;
                case ChessPiece.PieceType.King:
                    break;
                default:
                    break;
            }

            // loop through the list of possible squares to find out which one is correct
            foreach (var square in squareList)
            {
                if (FindPieceIndex(square, ChessPiece.PieceColor.Black) != -1)
                    return square;
            }

            throw new Exception("Could not find piece to move");
        }
    }
}
