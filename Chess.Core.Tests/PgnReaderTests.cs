using Chess.Core;
using Xunit;

namespace Chess.Core.Tests;

public sealed class PgnReaderTests
{
    [Fact]
    public void Parse_ProducesReplayFramesAndRetainsMainlineComments()
    {
        const string pgn =
            """
            [Event "Commented game"]
            [White "Ada"]
            [Black "Grace"]
            [Result "*"]

            {Opening note} 1. {Before e4} e4 {King pawn opening} e5
            2. Nf3 (2. Bc4?! Nf6 {ignored}) Nc6 *
            """;

        var game = PgnGame.Parse(pgn);

        Assert.Equal("Commented game", game.Headers["Event"]);
        Assert.Equal(4, game.Moves.Count);
        Assert.Equal("e4", game.Moves[0].Move.San);
        Assert.Equal("Nf3", game.Moves[2].DisplaySan);
        Assert.Equal("Opening note", Assert.Single(game.Comments).Text);
        Assert.Equal("Before e4", Assert.Single(game.Moves[0].CommentsBefore).Text);
        Assert.Equal("King pawn opening", Assert.Single(game.Moves[0].CommentsAfter).Text);
        Assert.DoesNotContain(
            game.Moves.SelectMany(move => move.CommentsAfter),
            comment => comment.Text.Contains("ignored", StringComparison.Ordinal));
        Assert.NotEqual(game.InitialFen, game.FinalFen);
    }

    [Fact]
    public void Parse_SupportsFenStartsAndPromotion()
    {
        const string pgn =
            """
            [SetUp "1"]
            [FEN "4k3/P7/8/8/8/8/8/4K3 w - - 0 1"]
            [Result "*"]

            1. a8=Q+ *
            """;

        var game = PgnGame.Parse(pgn);

        var move = Assert.Single(game.Moves);
        Assert.Equal(PieceType.Queen, move.Move.Promotion);
        Assert.Contains(
            ChessMatch.FromFen(move.FenAfter).Pieces,
            piece => piece.Square == new Square('a', 8) &&
                     piece.Type == PieceType.Queen &&
                     piece.Side == Side.White);
    }

    [Fact]
    public void Parse_SupportsCastlingAndEnPassant()
    {
        const string castling =
            """
            1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. O-O *
            """;
        const string enPassant =
            """
            [SetUp "1"]
            [FEN "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1"]

            1. exd6 *
            """;

        var castleGame = PgnGame.Parse(castling);
        var castlePosition = ChessMatch.FromFen(castleGame.FinalFen);
        Assert.Contains(
            castlePosition.Pieces,
            piece => piece.Type == PieceType.King &&
                     piece.Side == Side.White &&
                     piece.Square == new Square('g', 1));
        Assert.Contains(
            castlePosition.Pieces,
            piece => piece.Type == PieceType.Rook &&
                     piece.Side == Side.White &&
                     piece.Square == new Square('f', 1));

        var enPassantGame = PgnGame.Parse(enPassant);
        var enPassantPosition = ChessMatch.FromFen(enPassantGame.FinalFen);
        Assert.DoesNotContain(
            enPassantPosition.Pieces,
            piece => piece.Side == Side.Black && piece.Type == PieceType.Pawn);
        Assert.Contains(
            enPassantPosition.Pieces,
            piece => piece.Side == Side.White &&
                     piece.Type == PieceType.Pawn &&
                     piece.Square == new Square('d', 6));
    }

    [Fact]
    public void Parse_ReportsExactIllegalMoveLocation()
    {
        const string pgn =
            "[Result \"*\"]\n\n1. e4 e5\n2. Nf3 Qh9 *";
        var expectedOffset = pgn.IndexOf("Qh9", StringComparison.Ordinal);

        var exception = Assert.Throws<ChessNotationException>(() => PgnGame.Parse(pgn));

        Assert.Equal("Qh9", exception.Token);
        Assert.Equal(2, exception.MoveNumber);
        Assert.Equal(Side.Black, exception.Side);
        Assert.Equal(
            new NotationSpan(expectedOffset, 3, 4, 8),
            exception.SourceSpan);
    }

    [Fact]
    public void Parse_ReportsStructuralErrorLocation()
    {
        const string pgn = "1. e4 e5 ) 2. Nf3";
        var expectedOffset = pgn.IndexOf(')');

        var exception = Assert.Throws<ChessNotationException>(() => PgnGame.Parse(pgn));

        Assert.Contains("unmatched closing variation", exception.Message);
        Assert.Equal(expectedOffset, exception.SourceSpan?.Offset);
        Assert.Equal(1, exception.SourceSpan?.Length);

        var braceException = Assert.Throws<ChessNotationException>(
            () => PgnGame.Parse("1. e4 } e5"));
        Assert.Contains("unmatched closing brace", braceException.Message);
        Assert.Equal(6, braceException.SourceSpan?.Offset);
    }

    [Fact]
    public void Parse_RejectsUnterminatedCommentsAndResultConflicts()
    {
        var commentException = Assert.Throws<ChessNotationException>(
            () => PgnGame.Parse("1. e4 {unfinished"));
        Assert.Contains("unterminated brace comment", commentException.Message);

        var resultException = Assert.Throws<ChessNotationException>(
            () => PgnGame.Parse(
                """
                [Result "1-0"]

                1. e4 e5 0-1
                """));
        Assert.Contains("does not match", resultException.Message);
    }

    [Fact]
    public void FromPgn_RemainsCompatibleWithFinalPositionApi()
    {
        const string pgn = "1. e4 e5 2. Nf3 Nc6 *";

        var match = ChessMatch.FromPgn(pgn);
        var game = PgnGame.Parse(pgn);

        Assert.Equal(game.FinalFen, match.Fen);
        Assert.Equal(["e4", "e5", "Nf3", "Nc6"], match.SanHistory);
    }

    [Fact]
    public void FromFen_ReportsTheCheckedKingSquare()
    {
        var safePosition = new ChessMatch();
        var checkedPosition = ChessMatch.FromFen(
            "4k3/8/8/8/8/8/4R3/4K3 b - - 0 1");

        Assert.Null(safePosition.CheckedKingSquare);
        Assert.Equal(new Square('e', 8), checkedPosition.CheckedKingSquare);
        Assert.False(checkedPosition.IsCheckmate);
    }

    [Fact]
    public void FromFen_ReportsCheckmate()
    {
        var position = ChessMatch.FromFen(
            "7k/6Q1/6K1/8/8/8/8/8 b - - 0 1");

        Assert.Equal(new Square('h', 8), position.CheckedKingSquare);
        Assert.True(position.IsCheckmate);
        Assert.Equal(GameEndReason.Checkmate, position.Outcome?.Reason);
    }
}
