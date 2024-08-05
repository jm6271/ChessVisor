namespace ChessNotationParser.AST;

public class AST
{
    public NODE_TYPE type = NODE_TYPE.AST;
    public enum NODE_TYPE
    {
        AST,
        Parse,
        Move,
        StandardMove,
        CastleMove,
        EnPassantMove,
        Piece,
        Disambiguation,
        Capture,
        Square,
        File,
        Rank,
        Promotion,
        CheckOrMate,
    }
}

public class Parse : AST
{
    public Parse()
    {
        type = NODE_TYPE.Parse;
    }

    public Move move = new();
}

public class Move : AST
{
    public Move()
    {
        type = NODE_TYPE.Move;
    }
}

public class StandardMove : Move
{
    public StandardMove()
    {
        type = NODE_TYPE.StandardMove;
    }

    public Piece? piece;
    public Disambiguation? disambiguation;
    public Capture? capture;
    public Square square = new();
    public Promotion? promotion;
    public CheckOrMate? checkOrMate;
}

public class CastleMove : Move
{
    public CastleMove()
    {
        type = NODE_TYPE.CastleMove;
    }

    public enum CastleDirection
    {
        KingSide,
        QueenSide
    }

    public CastleDirection direction = CastleDirection.KingSide;
}

public class EnPassantMove : Move
{
    public EnPassantMove()
    {
        type = NODE_TYPE.EnPassantMove;
    }

    public Square DestinationSquare = new();
}

public class Piece : AST
{
    public Piece()
    {
        type = NODE_TYPE.Piece;
    }

    public enum Pieces
    {
        King,
        Queen,
        Rook,
        Bishop,
        Knight
    }

    public Pieces PieceType = Pieces.King;
}

public class Disambiguation : AST
{
    public Disambiguation()
    {
        type = NODE_TYPE.Disambiguation;
    }

    public File? file;
    public Rank? rank;
}

public class Capture : AST
{
    public Capture()
    {
        type = NODE_TYPE.Capture;
    }
}

public class Square : AST
{
    public Square()
    {
        type = NODE_TYPE.Square;
    }

    public File file = new();
    public Rank rank = new();
}

public class File : AST
{
    public File()
    {
        type = NODE_TYPE.File;
    }

    public enum Files
    {
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H
    }

    public Files file = Files.A;
}

public class Rank : AST
{
    public Rank()
    {
        type = NODE_TYPE.Rank;
    }

    public enum Ranks
    {
        R1,
        R2,
        R3,
        R4,
        R5,
        R6,
        R7,
        R8
    }

    public Ranks rank = Ranks.R1;
}

public class Promotion : AST
{
    public Promotion()
    {
        type = NODE_TYPE.Promotion;
        TargetPiece.PieceType = Piece.Pieces.Queen;
    }

    public Piece TargetPiece = new();
}

public class CheckOrMate : AST
{
    public CheckOrMate()
    {
        type = NODE_TYPE.CheckOrMate;
    }

    public enum Check 
    {
        Check,
        CheckMate,
    }

    public Check check = Check.Check;
}
