using System.Collections.ObjectModel;

namespace Chess.Core;

public sealed record PgnComment(string Text, NotationSpan SourceSpan);

public sealed record PgnPly(
    int Index,
    int MoveNumber,
    Side Side,
    ChessMove Move,
    string Annotation,
    string FenBefore,
    string FenAfter,
    NotationSpan SourceSpan,
    IReadOnlyList<PgnComment> CommentsBefore,
    IReadOnlyList<PgnComment> CommentsAfter)
{
    public string DisplaySan => $"{Move.San}{Annotation}";
}

public sealed class PgnGame
{
    internal PgnGame(
        IDictionary<string, string> headers,
        string initialFen,
        IEnumerable<PgnPly> moves,
        IEnumerable<PgnComment> comments,
        string? result,
        string finalFen,
        GameOutcome? outcome)
    {
        Headers = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase));
        InitialFen = initialFen;
        Moves = Array.AsReadOnly(moves.ToArray());
        Comments = Array.AsReadOnly(comments.ToArray());
        Result = result;
        FinalFen = finalFen;
        Outcome = outcome;
    }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string InitialFen { get; }

    public IReadOnlyList<PgnPly> Moves { get; }

    public IReadOnlyList<PgnComment> Comments { get; }

    public string? Result { get; }

    public string FinalFen { get; }

    public GameOutcome? Outcome { get; }

    public static PgnGame Parse(string pgn) => PgnReader.Parse(pgn);
}
