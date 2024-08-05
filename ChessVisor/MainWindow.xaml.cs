using System.Windows;
using ChessNotationParser;

namespace ChessVisor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        struct MoveNotation
        {
            public string WhiteNotation = "";
            public string BlackNotation = "";

            public MoveNotation()
            {
            }
        }

        PieceManager manager;

        public MainWindow()
        {
            InitializeComponent();

            manager = new PieceManager();
            PopulateWhitePieces();
            PopulateBlackPieces();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        
        }

        private void bttnRun_Click(object sender, RoutedEventArgs e)
        {
            // get the text and parse it

            string notation = editor.Text;

            // split text into moves and parse them one at a time
            List<string> lines = [.. notation.Split(Environment.NewLine)];

            var Moves = new List<MoveNotation>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Contains(' '))
                {
                    var moves = trimmed.Split(' ');
                    MoveNotation m = new();
                    m.WhiteNotation = moves[0];
                    m.BlackNotation = moves[1];
                    Moves.Add(m);
                }
                else
                {
                    MoveNotation m = new();
                    m.WhiteNotation = trimmed;
                    Moves.Add(m);
                }
            }

            
        }

        private void MovePiece(ChessMove move)
        {
            manager.Move(move);

        }

        private void PopulateWhitePieces()
        {
            manager.WhitePieces.Clear();

            ChessPiece piece = new();

            // A-pawn
            ChessPiece PawnA = new()
            {
                Position = new('A', 2),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "WhitePawnA"
            };
            manager.WhitePieces.Add(PawnA);

            // B-pawn
            ChessPiece PawnB = new()
            {
                Position = new('B', 2),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "WhitePawnB"
            };
            manager.WhitePieces.Add(PawnB);

            // C-pawn
            ChessPiece PawnC = new()
            {
                Position = new('C', 2),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "WhitePawnC"  
            };
            manager.WhitePieces.Add(PawnC);

            // D-pawn
            ChessPiece PawnD = new()
            {
                Position = new('D', 2),
                Color= ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "WhitePawnD"
            };
            manager.WhitePieces.Add(PawnD);

            // E-pawn
            ChessPiece PawnE = new()
            {
                Position = new('E', 2),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "WhitePawnE"
            };
            manager.WhitePieces.Add(PawnE);

            // F-pawn
            ChessPiece PawnF = new()
            {
                Position = new('F', 2),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "WhitePawnF"
            };
            manager.WhitePieces.Add(PawnF);

            // G-pawn
            ChessPiece PawnG = new()
            {
                Position = new('G', 2),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "WhitePawnG"
            };
            manager.WhitePieces.Add(PawnG);

            // H-pawn
            ChessPiece PawnH = new()
            {
                Position = new('H', 2),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "WhitePawnH"
            };
            manager.WhitePieces.Add(PawnH);


            // White rooks
            ChessPiece RookA = new()
            {
                Position = new('A', 1),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Rook,
                ImageControlName = "WhiteQRook"
            };
            manager.WhitePieces.Add(RookA);

            ChessPiece RookH = new()
            {
                Position = new('H', 1),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Rook,
                ImageControlName = "WhiteKRook"
            };
            manager.WhitePieces.Add(RookH);

            // White knights
            ChessPiece KnightB = new()
            {
                Position = new('B', 1),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Knight,
                ImageControlName = "WhiteQKnight"
            };
            manager.WhitePieces.Add(KnightB);

            ChessPiece KnightG = new()
            {
                Position = new('G', 1),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Knight,
                ImageControlName = "WhiteKKnight"
            };
            manager.WhitePieces.Add(KnightG);

            // White bishops
            ChessPiece BishopC = new()
            {
                Position = new('C', 1),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Bishop,
                ImageControlName = "WhiteQBishop"
            };
            manager.WhitePieces.Add(BishopC);

            ChessPiece BishopF = new()
            {
                Position = new('F', 1),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Bishop,
                ImageControlName = "WhiteKBishop"
            };
            manager.WhitePieces.Add(BishopF);

            // White queen
            ChessPiece Queen = new()
            {
                Position = new('D', 1),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.Queen,
                ImageControlName = "WhiteQueen"
            };
            manager.WhitePieces.Add(Queen);

            // White king
            ChessPiece King = new()
            {
                Position = new('E', 1),
                Color = ChessPiece.PieceColor.White,
                Type = ChessPiece.PieceType.King,
                ImageControlName = "WhiteKing"
            };
            manager.WhitePieces.Add(King);
        }

        private void PopulateBlackPieces()
        {
            manager.BlackPieces.Clear();

            // Black pawns
            ChessPiece PawnA = new()
            {
                Position = new('A', 7),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "BlackPawnA"
            };
            manager.BlackPieces.Add(PawnA);

            // B-pawn
            ChessPiece PawnB = new()
            {
                Position = new('B', 7),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "BlackPawnB"
            };
            manager.BlackPieces.Add(PawnB);

            // C-pawn
            ChessPiece PawnC = new()
            {
                Position = new('C', 7),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "BlackPawnC"
            };
            manager.BlackPieces.Add(PawnC);

            // D-pawn
            ChessPiece PawnD = new()
            {
                Position = new('D', 7),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "BlackPawnD"
            };
            manager.BlackPieces.Add(PawnD);

            // E-pawn
            ChessPiece PawnE = new()
            {
                Position = new('E', 7),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "BlackPawnE"
            };
            manager.BlackPieces.Add(PawnE);

            // F-pawn
            ChessPiece PawnF = new()
            {
                Position = new('F', 7),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "BlackPawnF"
            };
            manager.BlackPieces.Add(PawnF);

            // G-pawn
            ChessPiece PawnG = new()
            {
                Position = new('G', 7),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "BlackPawnG"
            };
            manager.BlackPieces.Add(PawnG);

            // H-pawn
            ChessPiece PawnH = new()
            {
                Position = new('H', 7),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Pawn,
                ImageControlName = "BlackPawnH"
            };
            manager.BlackPieces.Add(PawnH);

            // Black rooks
            ChessPiece RookA = new()
            {
                Position = new('A', 8),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Rook,
                ImageControlName = "BlackQRook"
            };
            manager.BlackPieces.Add(RookA);

            ChessPiece RookH = new()
            {
                Position = new('H', 8),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Rook,
                ImageControlName = "BlackKRook"
            };
            manager.BlackPieces.Add(RookH);

            // Black knights
            ChessPiece KnightB = new()
            {
                Position = new('B', 8),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Knight,
                ImageControlName = "BlackQKnight"
            };
            manager.BlackPieces.Add(KnightB);

            ChessPiece KnightG = new()
            {
                Position = new('G', 8),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Knight,
                ImageControlName = "BlackKKnight"
            };
            manager.BlackPieces.Add(KnightG);

            // Black bishops
            ChessPiece BishopC = new()
            {
                Position = new('C', 8),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Bishop,
                ImageControlName = "BlackBishopC"
            };
            manager.BlackPieces.Add(BishopC);

            ChessPiece BishopF = new()
            {
                Position = new('F', 8),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Bishop,
                ImageControlName = "BlackKBishop"
            };
            manager.BlackPieces.Add(BishopF);

            // Black queen
            ChessPiece Queen = new()
            {
                Position = new('D', 8),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.Queen,
                ImageControlName = "BlackQueen"
            };
            manager.BlackPieces.Add(Queen);

            // Black king
            ChessPiece King = new()
            {
                Position = new('E', 8),
                Color = ChessPiece.PieceColor.Black,
                Type = ChessPiece.PieceType.King,
                ImageControlName = "BlackKing"
            };
            manager.BlackPieces.Add(King);
        }
    }
}
