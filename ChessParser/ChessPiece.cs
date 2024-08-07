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

            public Square()
            { }

            public Square(ChessNotationParser.AST.Square square)
            {
                switch (square.file.file)
                {
                    case AST.File.Files.A:
                        File = 'A';
                        break;
                    case AST.File.Files.B:
                        File = 'B';
                        break;
                    case AST.File.Files.C:
                        File = 'C';
                        break;
                    case AST.File.Files.D:
                        File = 'D';
                        break;
                    case AST.File.Files.E:
                        File = 'E';
                        break;
                    case AST.File.Files.F:
                        File = 'F';
                        break;
                    case AST.File.Files.G:
                        File = 'G';
                        break;
                    case AST.File.Files.H:
                        File = 'H';
                        break;
                    default:
                        break;
                }

                switch (square.rank.rank)
                {
                    case AST.Rank.Ranks.R1:
                        Rank = 1;
                        break;
                    case AST.Rank.Ranks.R2:
                        Rank = 2;
                        break;
                    case AST.Rank.Ranks.R3:
                        Rank = 3;
                        break;
                    case AST.Rank.Ranks.R4:
                        Rank = 4;
                        break;
                    case AST.Rank.Ranks.R5:
                        Rank = 5;
                        break;
                    case AST.Rank.Ranks.R6:
                        Rank = 6;
                        break;
                    case AST.Rank.Ranks.R7:
                        Rank = 7;
                        break;
                    case AST.Rank.Ranks.R8:
                        Rank = 8;
                        break;
                    default:
                        break;
                }
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
