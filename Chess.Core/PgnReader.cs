using System.Text;

namespace Chess.Core;

internal static class PgnReader
{
    private static readonly string[] Results = ["1-0", "0-1", "1/2-1/2", "*"];

    public static ChessMatch Read(string pgn) => ParseCore(pgn).Match;

    public static PgnGame Parse(string pgn) => ParseCore(pgn).Game;

    private static ParsedGame ParseCore(string pgn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pgn);

        var headerBlock = ParseHeaders(pgn);
        if (headerBlock.Headers.TryGetValue("SetUp", out var setup) &&
            setup.Value == "1" &&
            !headerBlock.Headers.ContainsKey("FEN"))
        {
            throw Error(
                "A PGN with SetUp \"1\" must include a FEN header.",
                setup.ValueSpan);
        }

        ChessMatch match;
        if (headerBlock.Headers.TryGetValue("FEN", out var fen))
        {
            try
            {
                match = ChessMatch.FromFen(fen.Value);
            }
            catch (ChessNotationException exception)
            {
                throw Error(
                    $"Invalid FEN header: {exception.Message}",
                    fen.ValueSpan,
                    fen.Value,
                    innerException: exception);
            }
        }
        else
        {
            match = new ChessMatch();
        }

        var initialFen = match.Fen;
        var gameComments = new List<PgnComment>();
        var plyBuilders = new List<PlyBuilder>();
        var pendingComments = new List<PgnComment>();
        SourceToken? resultToken = null;
        string? movetextResult = null;
        var afterMoveNumber = false;

        foreach (var sourceToken in Tokenize(pgn, headerBlock.MovetextOffset))
        {
            if (sourceToken.Kind == TokenKind.Comment)
            {
                var comment = new PgnComment(sourceToken.Text.Trim(), sourceToken.Span);
                if (plyBuilders.Count == 0 && !afterMoveNumber)
                {
                    gameComments.Add(comment);
                }
                else if (afterMoveNumber)
                {
                    pendingComments.Add(comment);
                }
                else
                {
                    plyBuilders[^1].CommentsAfter.Add(comment);
                }

                continue;
            }

            var token = StripMoveNumber(sourceToken, out var removedMoveNumber);
            if (token.Text.Length == 0)
            {
                afterMoveNumber = removedMoveNumber;
                continue;
            }

            afterMoveNumber = false;
            if (token.Text.StartsWith('$') ||
                token.Text.Equals("e.p.", StringComparison.OrdinalIgnoreCase) ||
                token.Text.Equals("ep", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var nagIndex = token.Text.IndexOf('$');
            if (nagIndex >= 0)
            {
                token = token.Slice(0, nagIndex);
            }

            if (Results.Contains(token.Text, StringComparer.Ordinal))
            {
                if (movetextResult is not null &&
                    !movetextResult.Equals(token.Text, StringComparison.Ordinal))
                {
                    throw Error(
                        "PGN movetext contains conflicting result tokens.",
                        token.Span,
                        token.Text);
                }

                movetextResult = token.Text;
                resultToken = token;
                continue;
            }

            if (movetextResult is not null)
            {
                throw Error(
                    "PGN contains moves after its result token.",
                    token.Span,
                    token.Text,
                    match.FullmoveNumber,
                    match.SideToMove);
            }

            var annotationLength = 0;
            while (annotationLength < token.Text.Length &&
                   token.Text[token.Text.Length - annotationLength - 1] is '!' or '?')
            {
                annotationLength++;
            }

            var annotation = annotationLength == 0
                ? string.Empty
                : token.Text[^annotationLength..];
            var sanToken = annotationLength == 0
                ? token
                : token.Slice(0, token.Text.Length - annotationLength);
            var san = sanToken.Text.Replace('0', 'O');
            var moveNumber = match.FullmoveNumber;
            var side = match.SideToMove;
            var fenBefore = match.Fen;
            if (!match.TryPlayPgnSan(san, out var move) || move is null)
            {
                var sideLabel = side == Side.White ? "White" : "Black";
                throw Error(
                    $"'{sanToken.Text}' is not a legal SAN move for {sideLabel}.",
                    sanToken.Span,
                    sanToken.Text,
                    moveNumber,
                    side);
            }

            var builder = new PlyBuilder(
                plyBuilders.Count,
                moveNumber,
                side,
                move.Value,
                annotation,
                fenBefore,
                match.Fen,
                token.Span);
            builder.CommentsBefore.AddRange(pendingComments);
            pendingComments.Clear();
            plyBuilders.Add(builder);
        }

        if (pendingComments.Count > 0)
        {
            if (plyBuilders.Count == 0)
            {
                gameComments.AddRange(pendingComments);
            }
            else
            {
                plyBuilders[^1].CommentsAfter.AddRange(pendingComments);
            }
        }

        headerBlock.Headers.TryGetValue("Result", out var headerResultEntry);
        var headerResult = headerResultEntry?.Value;
        ValidateResult(headerResult, "header", headerResultEntry?.ValueSpan);
        ValidateResult(movetextResult, "movetext", resultToken?.Span);

        if (headerResult is not null &&
            movetextResult is not null &&
            !headerResult.Equals(movetextResult, StringComparison.Ordinal))
        {
            throw Error(
                $"PGN header result '{headerResult}' does not match movetext result '{movetextResult}'.",
                resultToken!.Value.Span,
                movetextResult);
        }

        var declaredResult = movetextResult ?? headerResult;
        var declaredResultSpan = resultToken?.Span ?? headerResultEntry?.ValueSpan;
        if (declaredResult is not null)
        {
            if (match.Outcome is not null)
            {
                if (!match.Result.Equals(declaredResult, StringComparison.Ordinal))
                {
                    throw Error(
                        $"PGN result '{declaredResult}' conflicts with the board outcome '{match.Result}'.",
                        declaredResultSpan!.Value,
                        declaredResult);
                }
            }
            else if (declaredResult != "*")
            {
                match.DeclareResult(declaredResult);
            }
        }

        var headers = headerBlock.Headers.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Value,
            StringComparer.OrdinalIgnoreCase);
        var plies = plyBuilders.Select(builder => builder.Build()).ToArray();
        var game = new PgnGame(
            headers,
            initialFen,
            plies,
            gameComments,
            declaredResult,
            match.Fen,
            match.Outcome);
        return new ParsedGame(match, game);
    }

    private static HeaderBlock ParseHeaders(string pgn)
    {
        var headers = new Dictionary<string, HeaderEntry>(StringComparer.OrdinalIgnoreCase);
        var offset = 0;
        while (offset < pgn.Length)
        {
            var lineStart = offset;
            var lineEnd = pgn.IndexOf('\n', offset);
            if (lineEnd < 0)
            {
                lineEnd = pgn.Length;
            }

            var contentEnd = lineEnd > lineStart && pgn[lineEnd - 1] == '\r'
                ? lineEnd - 1
                : lineEnd;
            var line = pgn[lineStart..contentEnd];
            var leadingWhitespace = line.Length - line.TrimStart().Length;
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                offset = lineEnd < pgn.Length ? lineEnd + 1 : lineEnd;
                continue;
            }

            if (!trimmed.StartsWith('['))
            {
                return new HeaderBlock(headers, lineStart);
            }

            var trimmedOffset = lineStart + leadingWhitespace;
            var entry = ParseHeader(pgn, trimmed, trimmedOffset);
            if (!headers.TryAdd(entry.Name, entry))
            {
                throw Error(
                    $"Duplicate PGN header '{entry.Name}'.",
                    entry.NameSpan,
                    entry.Name);
            }

            offset = lineEnd < pgn.Length ? lineEnd + 1 : lineEnd;
        }

        return new HeaderBlock(headers, pgn.Length);
    }

    private static HeaderEntry ParseHeader(string pgn, string line, int absoluteOffset)
    {
        var lineSpan = SpanAt(pgn, absoluteOffset, line.Length);
        if (!line.EndsWith(']'))
        {
            throw Error($"Malformed PGN header '{line}'.", lineSpan, line);
        }

        var space = line.IndexOfAny([' ', '\t'], 1);
        if (space <= 1)
        {
            throw Error($"Malformed PGN header '{line}'.", lineSpan, line);
        }

        var name = line[1..space];
        var valueStart = space;
        while (valueStart < line.Length && char.IsWhiteSpace(line[valueStart]))
        {
            valueStart++;
        }

        if (valueStart >= line.Length || line[valueStart] != '"')
        {
            throw Error($"Malformed PGN header '{line}'.", lineSpan, line);
        }

        var closingQuote = FindClosingQuote(line, valueStart + 1);
        if (closingQuote < 0 ||
            line[(closingQuote + 1)..^1].Trim().Length != 0)
        {
            throw Error($"Malformed PGN header '{line}'.", lineSpan, line);
        }

        var rawValue = line[(valueStart + 1)..closingQuote];
        var valueSpan = SpanAt(
            pgn,
            absoluteOffset + valueStart + 1,
            rawValue.Length);
        var value = UnescapeHeader(rawValue, valueSpan);
        return new HeaderEntry(
            name,
            value,
            SpanAt(pgn, absoluteOffset + 1, name.Length),
            valueSpan);
    }

    private static int FindClosingQuote(string line, int start)
    {
        var escaped = false;
        for (var index = start; index < line.Length; index++)
        {
            if (!escaped && line[index] == '"')
            {
                return index;
            }

            if (!escaped && line[index] == '\\')
            {
                escaped = true;
                continue;
            }

            escaped = false;
        }

        return -1;
    }

    private static string UnescapeHeader(string value, NotationSpan span)
    {
        var builder = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '\\')
            {
                if (index + 1 >= value.Length || value[index + 1] is not ('\\' or '"'))
                {
                    throw Error(
                        "A PGN header contains an invalid escape sequence.",
                        span with
                        {
                            Offset = span.Offset + index,
                            Length = Math.Min(2, value.Length - index),
                            Column = span.Column + index
                        },
                        value[index..Math.Min(value.Length, index + 2)]);
                }

                index++;
            }

            builder.Append(value[index]);
        }

        return builder.ToString();
    }

    private static IEnumerable<SourceToken> Tokenize(string pgn, int startOffset)
    {
        var variationStarts = new Stack<int>();
        var index = startOffset;
        while (index < pgn.Length)
        {
            if (char.IsWhiteSpace(pgn[index]))
            {
                index++;
                continue;
            }

            if (pgn[index] == '{')
            {
                var start = index++;
                while (index < pgn.Length && pgn[index] != '}')
                {
                    index++;
                }

                if (index >= pgn.Length)
                {
                    throw Error(
                        "PGN contains an unterminated brace comment.",
                        SpanAt(pgn, start, 1),
                        "{");
                }

                var contentStart = start + 1;
                var contentLength = index - contentStart;
                index++;
                if (variationStarts.Count == 0)
                {
                    yield return new SourceToken(
                        TokenKind.Comment,
                        pgn.Substring(contentStart, contentLength),
                        SpanAt(pgn, start, index - start));
                }

                continue;
            }

            if (pgn[index] == '}')
            {
                throw Error(
                    "PGN contains an unmatched closing brace comment.",
                    SpanAt(pgn, index, 1),
                    "}");
            }

            if (pgn[index] == ';')
            {
                var start = index++;
                var contentStart = index;
                while (index < pgn.Length && pgn[index] is not '\r' and not '\n')
                {
                    index++;
                }

                if (variationStarts.Count == 0)
                {
                    yield return new SourceToken(
                        TokenKind.Comment,
                        pgn[contentStart..index],
                        SpanAt(pgn, start, index - start));
                }

                continue;
            }

            if (pgn[index] == '(')
            {
                variationStarts.Push(index);
                index++;
                continue;
            }

            if (pgn[index] == ')')
            {
                if (variationStarts.Count == 0)
                {
                    throw Error(
                        "PGN contains an unmatched closing variation.",
                        SpanAt(pgn, index, 1),
                        ")");
                }

                variationStarts.Pop();
                index++;
                continue;
            }

            var tokenStart = index;
            while (index < pgn.Length &&
                   !char.IsWhiteSpace(pgn[index]) &&
                   pgn[index] is not '{' and not '}' and not ';' and not '(' and not ')')
            {
                index++;
            }

            if (variationStarts.Count == 0)
            {
                yield return new SourceToken(
                    TokenKind.Text,
                    pgn[tokenStart..index],
                    SpanAt(pgn, tokenStart, index - tokenStart));
            }
        }

        if (variationStarts.Count > 0)
        {
            var start = variationStarts.Last();
            throw Error(
                "PGN contains an unterminated variation.",
                SpanAt(pgn, start, 1),
                "(");
        }
    }

    private static SourceToken StripMoveNumber(SourceToken token, out bool removed)
    {
        var index = 0;
        while (index < token.Text.Length && char.IsAsciiDigit(token.Text[index]))
        {
            index++;
        }

        if (index == 0 || index >= token.Text.Length || token.Text[index] != '.')
        {
            removed = false;
            return token;
        }

        while (index < token.Text.Length && token.Text[index] == '.')
        {
            index++;
        }

        removed = true;
        return token.Slice(index, token.Text.Length - index);
    }

    private static void ValidateResult(string? result, string source, NotationSpan? span)
    {
        if (result is not null && !Results.Contains(result, StringComparer.Ordinal))
        {
            throw Error(
                $"Unsupported PGN {source} result '{result}'.",
                span!.Value,
                result);
        }
    }

    private static ChessNotationException Error(
        string message,
        NotationSpan span,
        string? token = null,
        int? moveNumber = null,
        Side? side = null,
        Exception? innerException = null) =>
        new(message, span, token, moveNumber, side, innerException);

    private static NotationSpan SpanAt(string text, int offset, int length)
    {
        var line = 1;
        var column = 1;
        for (var index = 0; index < offset; index++)
        {
            if (text[index] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return new NotationSpan(offset, length, line, column);
    }

    private sealed record HeaderEntry(
        string Name,
        string Value,
        NotationSpan NameSpan,
        NotationSpan ValueSpan);

    private sealed record HeaderBlock(
        Dictionary<string, HeaderEntry> Headers,
        int MovetextOffset);

    private readonly record struct ParsedGame(ChessMatch Match, PgnGame Game);

    private enum TokenKind
    {
        Text,
        Comment
    }

    private readonly record struct SourceToken(
        TokenKind Kind,
        string Text,
        NotationSpan Span)
    {
        public SourceToken Slice(int start, int length) => new(
            Kind,
            Text.Substring(start, length),
            Span with
            {
                Offset = Span.Offset + start,
                Length = length,
                Column = Span.Column + start
            });
    }

    private sealed class PlyBuilder(
        int index,
        int moveNumber,
        Side side,
        ChessMove move,
        string annotation,
        string fenBefore,
        string fenAfter,
        NotationSpan sourceSpan)
    {
        public List<PgnComment> CommentsBefore { get; } = [];

        public List<PgnComment> CommentsAfter { get; } = [];

        public PgnPly Build() => new(
            index,
            moveNumber,
            side,
            move,
            annotation,
            fenBefore,
            fenAfter,
            sourceSpan,
            CommentsBefore.AsReadOnly(),
            CommentsAfter.AsReadOnly());
    }
}
