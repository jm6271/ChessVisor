using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessNotationParser
{
    public class ChessMove
    {
        public ChessPiece.PieceColor PieceColor;
        public ChessPiece.PieceType Type;
        public ChessPiece.Square CurrentLocation;
        public ChessPiece.Square Destination;
    }
}
