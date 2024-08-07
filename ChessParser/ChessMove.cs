using ChessNotationParser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessNotationParser
{
    public class ChessMove
    {
        public ChessPiece.PieceColor PieceColor = ChessPiece.PieceColor.White;
        public ChessNotationParser.AST.Parse MoveInfo = new();

        public ChessMove(ChessPiece.PieceColor pieceColor, Parse moveInfo)
        {
            PieceColor = pieceColor;
            MoveInfo = moveInfo;
        }

        public ChessMove() { }
    }
}
