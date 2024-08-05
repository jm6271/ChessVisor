using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessNotationParser
{
    public class ChessPiece
    {
        public struct Square
        {
            public char File = 'A';
            public short Rank = 1;

            public Square(char file, short rank)
            {
                File = file;
                Rank = rank;
            }

            public static bool operator ==(Square left, Square right)
            {
                if (left.File == right.File)
                {
                    if (left.Rank == right.Rank)
                        return true;
                }
                return false;
            }

            public static bool operator !=(Square left, Square right)
            {
                return !(left == right);
            }
        }

        public Square Position = new();

        public enum PieceType
        {
            Pawn,
            Knight,
            Bishop,
            Rook,
            Queen,
            King
        }

        public enum PieceColor
        {
            White,
            Black
        }

        public PieceColor Color = PieceColor.White;
        public PieceType Type = PieceType.King;

        public string ImageControlName = "";
    }
}
