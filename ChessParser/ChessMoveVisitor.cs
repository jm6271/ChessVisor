using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime;
using ChessNotationParser.AST;

namespace ChessNotationParser
{
    public class ChessMoveVisitor : ChessBaseVisitor<AST.AST>
    {
        public override AST.AST VisitParse([NotNull] ChessParser.ParseContext context)
        {
            // Get the move
            AST.Parse root = new();

            root.move = (AST.Move)Visit(context.move());
            return root;
        }

        public override AST.AST VisitMove([NotNull] ChessParser.MoveContext context)
        {
            AST.Move move;

            if (context.castling() != null) 
            {
                move = (AST.CastleMove)Visit(context.castling());
            }
            else if (context.enPassant() != null)
            {
                move = (AST.EnPassantMove)Visit(context.enPassant());
            }
            else
            {
                move = (AST.StandardMove)Visit(context.standardMove());
            }

            return move;
        }

        public override AST.AST VisitStandardMove([NotNull] ChessParser.StandardMoveContext context)
        {
            AST.StandardMove move = new();

            if (context.piece() != null)
            {
                move.piece = (AST.Piece)Visit(context.piece());
            }

            if (context.disambiguation() != null)
            {
                move.disambiguation = (AST.Disambiguation)Visit(context.disambiguation());
            }

            if (context.capture() != null)
            {
                move.capture = (AST.Capture)Visit(context.capture());
            }

            if (context.promotion() != null)
            {
                move.promotion = (AST.Promotion)Visit(context.promotion());
            }

            if (context.checkOrMate() != null)
            {
                move.checkOrMate = (AST.CheckOrMate)Visit(context.checkOrMate());
            }

            move.square = (AST.Square)Visit(context.square());

            return move;
        }

        public override AST.AST VisitCastling([NotNull] ChessParser.CastlingContext context)
        {
            AST.CastleMove move = new();

            if (context.GetText() == "O-O")
                move.direction = AST.CastleMove.CastleDirection.KingSide;
            else
                move.direction = AST.CastleMove.CastleDirection.QueenSide;
            
            return move;
        }

        public override AST.AST VisitEnPassant([NotNull] ChessParser.EnPassantContext context)
        {
            AST.EnPassantMove move = new();

            move.DestinationSquare = (AST.Square)Visit(context.square());

            return move;
        }

        public override AST.AST VisitPiece([NotNull] ChessParser.PieceContext context)
        {
            AST.Piece p = new();
            string piece = context.GetText();

            switch (piece)
            {
                case "K":
                    p.PieceType = AST.Piece.Pieces.King;
                    break;
                case "Q":
                    p.PieceType= AST.Piece.Pieces.Queen;
                    break;
                case "R":
                    p.PieceType = AST.Piece.Pieces.Rook;
                    break;
                case "B":
                    p.PieceType = AST.Piece.Pieces.Bishop;
                    break;
                case "N":
                    p.PieceType = AST.Piece.Pieces.Knight;
                    break;
                default:
                    break;
            }

            return p;
        }

        public override AST.AST VisitDisambiguation([NotNull] ChessParser.DisambiguationContext context)
        {
            AST.Disambiguation disambiguation = new();

            if (context.rank() != null)
            {
                disambiguation.rank = (AST.Rank)Visit(context.rank());
            }

            if (context.file() != null)
            {
                disambiguation.file = (AST.File)Visit(context.file());
            }

            return disambiguation;
        }

        public override AST.AST VisitCapture([NotNull] ChessParser.CaptureContext context)
        {
            return new AST.Capture();
        }

        public override AST.AST VisitSquare([NotNull] ChessParser.SquareContext context)
        {
            AST.Square square = new();

            square.file = (AST.File)Visit(context.file());
            square.rank = (AST.Rank)Visit(context.rank());

            return square;
        }

        public override AST.AST VisitFile([NotNull] ChessParser.FileContext context)
        {
            AST.File file = new();

            string fileName = context.GetText();

            switch (fileName)
            {
                case "a":
                    file.file = AST.File.Files.A; break;
                case "b":
                    file.file= AST.File.Files.B; break;
                case "c":
                    file.file = AST.File.Files.C; break;
                case "d":
                    file.file = AST.File.Files.D; break;
                case "e":
                    file.file = AST.File.Files.E; break;
                case "f":
                    file.file = AST.File.Files.F; break;
                case "g":
                    file.file = AST.File.Files.G; break;
                case "h":
                    file.file = AST.File.Files.H; break;
                default:
                    break;
            }

            return file;
        }

        public override AST.AST VisitRank([NotNull] ChessParser.RankContext context)
        {
            AST.Rank rank = new();

            string rankName = context.GetText();

            switch (rankName)
            {
                case "1":
                    rank.rank = AST.Rank.Ranks.R1; break;
                case "2":
                    rank.rank = AST.Rank.Ranks.R2; break;
                case "3":
                    rank.rank = AST.Rank.Ranks.R3; break;
                case "4":
                    rank.rank = AST.Rank.Ranks.R4; break;
                case "5":
                    rank.rank = AST.Rank.Ranks.R5; break;
                case "6":
                    rank.rank = AST.Rank.Ranks.R6; break;
                case "7":
                    rank.rank = AST.Rank.Ranks.R7; break;
                case "8":
                    rank.rank = AST.Rank.Ranks.R8; break;
                default:
                    break;
            }

            return rank;
        }

        public override AST.AST VisitPromotion([NotNull] ChessParser.PromotionContext context)
        {
            AST.Promotion promotion = new();

            promotion.TargetPiece = (AST.Piece)Visit(context.piece());

            return promotion;
        }

        public override AST.AST VisitCheckOrMate([NotNull] ChessParser.CheckOrMateContext context)
        {
            AST.CheckOrMate checkmate = new();

            string text = context.GetText();

            if (text == "+")
                checkmate.check = CheckOrMate.Check.Check;
            else
                checkmate.check = CheckOrMate.Check.CheckMate;

            return checkmate;
        }
    }
}
