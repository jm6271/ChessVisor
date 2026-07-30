namespace Chess.Core;

public enum Side
{
    White,
    Black
}

public enum PieceType
{
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King
}

public enum GameEndReason
{
    Checkmate,
    Stalemate,
    InsufficientMaterial,
    FiftyMoveRule,
    ThreefoldRepetition,
    DeclaredResult
}

public readonly record struct Square
{
    public Square(char file, int rank)
    {
        if (file is < 'a' or > 'h' || rank is < 1 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(file), "A square must be between a1 and h8.");
        }

        File = file;
        Rank = rank;
    }

    public char File { get; }

    public int Rank { get; }

    public override string ToString() => $"{File}{Rank}";
}

public readonly record struct ChessMove(
    Square From,
    Square To,
    PieceType? Promotion,
    string San);

public readonly record struct BoardPiece(
    Square Square,
    PieceType Type,
    Side Side);

public readonly record struct NotationSpan(
    int Offset,
    int Length,
    int Line,
    int Column);

public sealed record GameOutcome(GameEndReason Reason, Side? Winner);

public sealed class ChessNotationException : FormatException
{
    public ChessNotationException(string message)
        : base(message)
    {
    }

    public ChessNotationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ChessNotationException(
        string message,
        NotationSpan sourceSpan,
        string? token = null,
        int? moveNumber = null,
        Side? side = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        SourceSpan = sourceSpan;
        Token = token;
        MoveNumber = moveNumber;
        Side = side;
    }

    public NotationSpan? SourceSpan { get; }

    public string? Token { get; }

    public int? MoveNumber { get; }

    public Side? Side { get; }
}
