using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ChessNotationParser;

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

        public void Move(ChessMove move)
        {
            Move(move.PieceColor, move.Destination, move.CurrentLocation);
        }

        public void Move(ChessPiece.PieceColor color, ChessPiece.Square Destination, ChessPiece.Square CurrentLocation)
        {
            // find the chess piece and update its location
            int index = FindPieceIndex(CurrentLocation, color);
            if (index == -1) throw new Exception("Error: Invalid move.");

            Wpf.Ui.Controls.Image img;

            if (color == ChessPiece.PieceColor.White)
            {
                WhitePieces[index].Position = Destination;
                var mainWindow = Application.Current.MainWindow;
                img = (Wpf.Ui.Controls.Image)mainWindow.FindName(WhitePieces[index].ImageControlName);
            }
            else
            {
                BlackPieces[index].Position = Destination;
                var mainWindow = Application.Current.MainWindow;
                img = (Wpf.Ui.Controls.Image)mainWindow.FindName(BlackPieces[index].ImageControlName);
            }

            // get the canvas control the image is on
            Canvas canvas = (Canvas)VisualTreeHelper.GetParent(img);

            TranslateTransform trans = new();
            img.RenderTransform = trans;

            DoubleAnimation animY = new(GetYFromRank(CurrentLocation.Rank), GetYFromRank(Destination.Rank) - GetYFromRank(CurrentLocation.Rank), TimeSpan.FromSeconds(1));
            DoubleAnimation animX = new(GetXFromFile(CurrentLocation.File), GetXFromFile(Destination.File) - GetXFromFile(CurrentLocation.File), TimeSpan.FromSeconds(1));

            trans.BeginAnimation(TranslateTransform.YProperty, animY);
            trans.BeginAnimation(TranslateTransform.XProperty, animX);
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


    }
}
