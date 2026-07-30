using System.Globalization;
using System.Text;

namespace Chess.Core;

public sealed class ChessMatch
{
    private const string InitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private static readonly (int File, int Rank)[] KnightOffsets =
    [
        (-2, -1), (-2, 1), (-1, -2), (-1, 2),
        (1, -2), (1, 2), (2, -1), (2, 1)
    ];

    private static readonly (int File, int Rank)[] BishopDirections =
    [
        (-1, -1), (-1, 1), (1, -1), (1, 1)
    ];

    private static readonly (int File, int Rank)[] RookDirections =
    [
        (-1, 0), (1, 0), (0, -1), (0, 1)
    ];

    private Piece?[] _board;
    private Side _sideToMove;
    private CastlingRights _castlingRights;
    private int? _enPassantSquare;
    private int _halfmoveClock;
    private int _fullmoveNumber;
    private readonly List<string> _sanHistory;
    private readonly Dictionary<string, int> _positionCounts;

    public ChessMatch()
        : this(ParseFen(InitialFen), [])
    {
    }

    private ChessMatch(ParsedPosition position, List<string> sanHistory)
    {
        _board = position.Board;
        _sideToMove = position.SideToMove;
        _castlingRights = position.CastlingRights;
        _enPassantSquare = position.EnPassantSquare;
        _halfmoveClock = position.HalfmoveClock;
        _fullmoveNumber = position.FullmoveNumber;
        _sanHistory = sanHistory;
        _positionCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [GetPositionKey()] = 1
        };
        Outcome = EvaluateNaturalOutcome();
    }

    public Side SideToMove => _sideToMove;

    public int FullmoveNumber => _fullmoveNumber;

    public string Fen => ToFen();

    public string Ascii => ToAscii();

    public IReadOnlyList<string> SanHistory => _sanHistory;

    public IReadOnlyList<BoardPiece> Pieces => _board
        .Select((piece, index) => (Piece: piece, Index: index))
        .Where(item => item.Piece is not null)
        .Select(item => new BoardPiece(
            ToSquare(item.Index),
            item.Piece!.Value.Type,
            item.Piece.Value.Side))
        .ToArray();

    public GameOutcome? Outcome { get; private set; }

    public static ChessMatch FromFen(string fen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fen);
        return new ChessMatch(ParseFen(fen), []);
    }

    public static ChessMatch FromPgn(string pgn) => PgnReader.Read(pgn);

    public IReadOnlyList<ChessMove> GetLegalMoves()
    {
        if (Outcome is not null)
        {
            return [];
        }

        return GetCanonicalMoves()
            .Select(item => ToPublicMove(item.Move, item.San))
            .ToArray();
    }

    public bool TryPlaySan(string san, out ChessMove? move)
    {
        move = null;
        if (Outcome is not null || string.IsNullOrWhiteSpace(san))
        {
            return false;
        }

        var input = san.Trim();
        var candidate = GetCanonicalMoves()
            .FirstOrDefault(item => item.San.Equals(input, StringComparison.Ordinal));
        if (candidate.San is null)
        {
            return false;
        }

        ApplyMove(candidate.Move, candidate.San);
        move = ToPublicMove(candidate.Move, candidate.San);
        return true;
    }

    internal bool TryPlayPgnSan(string san, out ChessMove? move)
    {
        if (TryPlaySan(san, out move))
        {
            return true;
        }

        move = null;
        if (Outcome is not null)
        {
            return false;
        }

        var normalized = NormalizePgnSan(san);
        var matches = GetCanonicalMoves()
            .Where(item => NormalizePgnSan(item.San).Equals(normalized, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            return false;
        }

        ApplyMove(matches[0].Move, matches[0].San);
        move = ToPublicMove(matches[0].Move, matches[0].San);
        return true;
    }

    internal string Result => Outcome?.Winner switch
    {
        Side.White => "1-0",
        Side.Black => "0-1",
        null when Outcome is not null => "1/2-1/2",
        _ => "*"
    };

    internal void DeclareResult(string result)
    {
        Outcome = result switch
        {
            "1-0" => new GameOutcome(GameEndReason.DeclaredResult, Side.White),
            "0-1" => new GameOutcome(GameEndReason.DeclaredResult, Side.Black),
            "1/2-1/2" => new GameOutcome(GameEndReason.DeclaredResult, null),
            "*" => null,
            _ => throw new ChessNotationException($"Unsupported PGN result '{result}'.")
        };
    }

    private IReadOnlyList<CanonicalMove> GetCanonicalMoves()
    {
        var legalMoves = GenerateLegalMoves(
            _board,
            _sideToMove,
            _castlingRights,
            _enPassantSquare);
        var result = new CanonicalMove[legalMoves.Count];
        for (var index = 0; index < legalMoves.Count; index++)
        {
            result[index] = new CanonicalMove(
                legalMoves[index],
                GenerateSan(legalMoves[index], legalMoves));
        }

        return result;
    }

    private string GenerateSan(InternalMove move, IReadOnlyList<InternalMove> legalMoves)
    {
        var piece = _board[move.From]!.Value;
        var builder = new StringBuilder();
        if (move.Flags.HasFlag(MoveFlags.CastleKingSide))
        {
            builder.Append("O-O");
        }
        else if (move.Flags.HasFlag(MoveFlags.CastleQueenSide))
        {
            builder.Append("O-O-O");
        }
        else
        {
            var capture = _board[move.To] is not null || move.Flags.HasFlag(MoveFlags.EnPassant);
            if (piece.Type == PieceType.Pawn)
            {
                if (capture)
                {
                    builder.Append(FileOf(move.From));
                }
            }
            else
            {
                builder.Append(PieceLetter(piece.Type));
                AppendDisambiguation(builder, move, piece.Type, legalMoves);
            }

            if (capture)
            {
                builder.Append('x');
            }

            builder.Append(SquareName(move.To));
            if (move.Promotion is not null)
            {
                builder.Append('=').Append(PieceLetter(move.Promotion.Value));
            }
        }

        var next = SimulateMove(
            _board,
            move,
            _sideToMove,
            _castlingRights);
        var opponent = Opposite(_sideToMove);
        var opponentKing = FindKing(next.Board, opponent);
        if (IsSquareAttacked(next.Board, opponentKing, _sideToMove))
        {
            var replies = GenerateLegalMoves(
                next.Board,
                opponent,
                next.CastlingRights,
                next.EnPassantSquare);
            builder.Append(replies.Count == 0 ? '#' : '+');
        }

        return builder.ToString();
    }

    private void AppendDisambiguation(
        StringBuilder builder,
        InternalMove move,
        PieceType pieceType,
        IReadOnlyList<InternalMove> legalMoves)
    {
        var alternatives = legalMoves
            .Where(candidate =>
                candidate.To == move.To &&
                candidate.From != move.From &&
                _board[candidate.From] is { } piece &&
                piece.Type == pieceType)
            .ToArray();
        if (alternatives.Length == 0)
        {
            return;
        }

        var fileUnique = alternatives.All(candidate => FileOf(candidate.From) != FileOf(move.From));
        var rankUnique = alternatives.All(candidate => RankOf(candidate.From) != RankOf(move.From));
        if (fileUnique)
        {
            builder.Append(FileOf(move.From));
        }
        else if (rankUnique)
        {
            builder.Append(RankOf(move.From));
        }
        else
        {
            builder.Append(FileOf(move.From)).Append(RankOf(move.From));
        }
    }

    private void ApplyMove(InternalMove move, string san)
    {
        var piece = _board[move.From]!.Value;
        var capture = _board[move.To] is not null || move.Flags.HasFlag(MoveFlags.EnPassant);
        var next = SimulateMove(_board, move, _sideToMove, _castlingRights);

        _board = next.Board;
        _castlingRights = next.CastlingRights;
        _enPassantSquare = next.EnPassantSquare;
        _halfmoveClock = piece.Type == PieceType.Pawn || capture ? 0 : _halfmoveClock + 1;
        if (_sideToMove == Side.Black)
        {
            _fullmoveNumber++;
        }

        _sideToMove = Opposite(_sideToMove);
        _sanHistory.Add(san);

        var key = GetPositionKey();
        _positionCounts[key] = _positionCounts.GetValueOrDefault(key) + 1;
        Outcome = EvaluateNaturalOutcome();
    }

    private GameOutcome? EvaluateNaturalOutcome()
    {
        var legalMoves = GenerateLegalMoves(
            _board,
            _sideToMove,
            _castlingRights,
            _enPassantSquare);
        if (legalMoves.Count == 0)
        {
            var king = FindKing(_board, _sideToMove);
            return IsSquareAttacked(_board, king, Opposite(_sideToMove))
                ? new GameOutcome(GameEndReason.Checkmate, Opposite(_sideToMove))
                : new GameOutcome(GameEndReason.Stalemate, null);
        }

        if (HasInsufficientMaterial())
        {
            return new GameOutcome(GameEndReason.InsufficientMaterial, null);
        }

        if (_halfmoveClock >= 100)
        {
            return new GameOutcome(GameEndReason.FiftyMoveRule, null);
        }

        if (_positionCounts.GetValueOrDefault(GetPositionKey()) >= 3)
        {
            return new GameOutcome(GameEndReason.ThreefoldRepetition, null);
        }

        return null;
    }

    private bool HasInsufficientMaterial()
    {
        var nonKings = _board
            .Select((piece, index) => (Piece: piece, Index: index))
            .Where(item => item.Piece is { Type: not PieceType.King })
            .ToArray();
        if (nonKings.Any(item => item.Piece is { Type: PieceType.Pawn or PieceType.Rook or PieceType.Queen }))
        {
            return false;
        }

        if (nonKings.Length <= 1)
        {
            return true;
        }

        // Any number of bishops confined to one square color cannot produce mate.
        return nonKings.All(item => item.Piece is { Type: PieceType.Bishop }) &&
               nonKings.Select(item => (FileIndex(item.Index) + RankIndex(item.Index)) % 2)
                   .Distinct()
                   .Count() == 1;
    }

    private string ToFen()
    {
        var builder = new StringBuilder();
        for (var rank = 7; rank >= 0; rank--)
        {
            if (rank < 7)
            {
                builder.Append('/');
            }

            var empty = 0;
            for (var file = 0; file < 8; file++)
            {
                var piece = _board[Index(file, rank)];
                if (piece is null)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    builder.Append(empty);
                    empty = 0;
                }

                builder.Append(ToFenChar(piece.Value));
            }

            if (empty > 0)
            {
                builder.Append(empty);
            }
        }

        builder.Append(_sideToMove == Side.White ? " w " : " b ");
        builder.Append(CastlingText(_castlingRights));
        builder.Append(' ').Append(_enPassantSquare is null ? "-" : SquareName(_enPassantSquare.Value));
        builder.Append(' ').Append(_halfmoveClock.ToString(CultureInfo.InvariantCulture));
        builder.Append(' ').Append(_fullmoveNumber.ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    private string ToAscii()
    {
        var builder = new StringBuilder();
        builder.AppendLine("  ┌────────────────────────┐");
        for (var rank = 7; rank >= 0; rank--)
        {
            builder.Append(rank + 1).Append(" │");
            for (var file = 0; file < 8; file++)
            {
                var piece = _board[Index(file, rank)];
                builder.Append(' ')
                    .Append(piece is null ? '.' : ToFenChar(piece.Value))
                    .Append(' ');
            }

            builder.AppendLine("│");
        }

        builder.AppendLine("  └────────────────────────┘");
        builder.Append("    a  b  c  d  e  f  g  h");
        return builder.ToString();
    }

    private string GetPositionKey()
    {
        var fields = ToFen().Split(' ');
        var effectiveEnPassant = HasLegalEnPassantCapture() ? fields[3] : "-";
        return $"{fields[0]} {fields[1]} {fields[2]} {effectiveEnPassant}";
    }

    private bool HasLegalEnPassantCapture()
    {
        if (_enPassantSquare is null)
        {
            return false;
        }

        return GenerateLegalMoves(_board, _sideToMove, _castlingRights, _enPassantSquare)
            .Any(move => move.Flags.HasFlag(MoveFlags.EnPassant));
    }

    private static List<InternalMove> GenerateLegalMoves(
        Piece?[] board,
        Side side,
        CastlingRights castlingRights,
        int? enPassantSquare)
    {
        var pseudoMoves = GeneratePseudoLegalMoves(board, side, castlingRights, enPassantSquare);
        var legalMoves = new List<InternalMove>(pseudoMoves.Count);
        foreach (var move in pseudoMoves)
        {
            var next = SimulateMove(board, move, side, castlingRights);
            var king = FindKing(next.Board, side);
            if (!IsSquareAttacked(next.Board, king, Opposite(side)))
            {
                legalMoves.Add(move);
            }
        }

        return legalMoves;
    }

    private static List<InternalMove> GeneratePseudoLegalMoves(
        Piece?[] board,
        Side side,
        CastlingRights castlingRights,
        int? enPassantSquare)
    {
        var moves = new List<InternalMove>(64);
        for (var from = 0; from < board.Length; from++)
        {
            if (board[from] is not { } piece || piece.Side != side)
            {
                continue;
            }

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    AddPawnMoves(board, side, from, enPassantSquare, moves);
                    break;
                case PieceType.Knight:
                    AddJumpMoves(board, side, from, KnightOffsets, moves);
                    break;
                case PieceType.Bishop:
                    AddSlidingMoves(board, side, from, BishopDirections, moves);
                    break;
                case PieceType.Rook:
                    AddSlidingMoves(board, side, from, RookDirections, moves);
                    break;
                case PieceType.Queen:
                    AddSlidingMoves(board, side, from, BishopDirections, moves);
                    AddSlidingMoves(board, side, from, RookDirections, moves);
                    break;
                case PieceType.King:
                    AddKingMoves(board, side, from, castlingRights, moves);
                    break;
            }
        }

        return moves;
    }

    private static void AddPawnMoves(
        Piece?[] board,
        Side side,
        int from,
        int? enPassantSquare,
        List<InternalMove> moves)
    {
        var file = FileIndex(from);
        var rank = RankIndex(from);
        var direction = side == Side.White ? 1 : -1;
        var startRank = side == Side.White ? 1 : 6;
        var promotionRank = side == Side.White ? 7 : 0;
        var nextRank = rank + direction;

        if (IsOnBoard(file, nextRank))
        {
            var oneStep = Index(file, nextRank);
            if (board[oneStep] is null)
            {
                AddPawnMove(from, oneStep, nextRank == promotionRank, MoveFlags.None, moves);
                if (rank == startRank)
                {
                    var twoStep = Index(file, rank + (2 * direction));
                    if (board[twoStep] is null)
                    {
                        moves.Add(new InternalMove(from, twoStep, null, MoveFlags.DoublePawnPush));
                    }
                }
            }
        }

        foreach (var fileOffset in new[] { -1, 1 })
        {
            var targetFile = file + fileOffset;
            if (!IsOnBoard(targetFile, nextRank))
            {
                continue;
            }

            var target = Index(targetFile, nextRank);
            var targetPiece = board[target];
            if (targetPiece is { } captured && captured.Side != side && captured.Type != PieceType.King)
            {
                AddPawnMove(from, target, nextRank == promotionRank, MoveFlags.None, moves);
            }
            else if (target == enPassantSquare)
            {
                var capturedPawn = board[Index(targetFile, rank)];
                if (capturedPawn is { Type: PieceType.Pawn } pawn && pawn.Side != side)
                {
                    moves.Add(new InternalMove(from, target, null, MoveFlags.EnPassant));
                }
            }
        }
    }

    private static void AddPawnMove(
        int from,
        int to,
        bool promotion,
        MoveFlags flags,
        List<InternalMove> moves)
    {
        if (!promotion)
        {
            moves.Add(new InternalMove(from, to, null, flags));
            return;
        }

        moves.Add(new InternalMove(from, to, PieceType.Queen, flags));
        moves.Add(new InternalMove(from, to, PieceType.Rook, flags));
        moves.Add(new InternalMove(from, to, PieceType.Bishop, flags));
        moves.Add(new InternalMove(from, to, PieceType.Knight, flags));
    }

    private static void AddJumpMoves(
        Piece?[] board,
        Side side,
        int from,
        IReadOnlyList<(int File, int Rank)> offsets,
        List<InternalMove> moves)
    {
        var file = FileIndex(from);
        var rank = RankIndex(from);
        foreach (var offset in offsets)
        {
            var targetFile = file + offset.File;
            var targetRank = rank + offset.Rank;
            if (!IsOnBoard(targetFile, targetRank))
            {
                continue;
            }

            var target = Index(targetFile, targetRank);
            if (board[target] is null ||
                board[target] is { } piece && piece.Side != side && piece.Type != PieceType.King)
            {
                moves.Add(new InternalMove(from, target, null, MoveFlags.None));
            }
        }
    }

    private static void AddSlidingMoves(
        Piece?[] board,
        Side side,
        int from,
        IReadOnlyList<(int File, int Rank)> directions,
        List<InternalMove> moves)
    {
        var file = FileIndex(from);
        var rank = RankIndex(from);
        foreach (var direction in directions)
        {
            var targetFile = file + direction.File;
            var targetRank = rank + direction.Rank;
            while (IsOnBoard(targetFile, targetRank))
            {
                var target = Index(targetFile, targetRank);
                if (board[target] is null)
                {
                    moves.Add(new InternalMove(from, target, null, MoveFlags.None));
                }
                else
                {
                    if (board[target] is { } piece && piece.Side != side && piece.Type != PieceType.King)
                    {
                        moves.Add(new InternalMove(from, target, null, MoveFlags.None));
                    }

                    break;
                }

                targetFile += direction.File;
                targetRank += direction.Rank;
            }
        }
    }

    private static void AddKingMoves(
        Piece?[] board,
        Side side,
        int from,
        CastlingRights castlingRights,
        List<InternalMove> moves)
    {
        var offsets = BishopDirections.Concat(RookDirections).ToArray();
        AddJumpMoves(board, side, from, offsets, moves);

        var rank = side == Side.White ? 0 : 7;
        var kingStart = Index(4, rank);
        if (from != kingStart || IsSquareAttacked(board, kingStart, Opposite(side)))
        {
            return;
        }

        var kingSideRight = side == Side.White
            ? CastlingRights.WhiteKingSide
            : CastlingRights.BlackKingSide;
        if (castlingRights.HasFlag(kingSideRight) &&
            IsRook(board, Index(7, rank), side) &&
            board[Index(5, rank)] is null &&
            board[Index(6, rank)] is null &&
            !IsSquareAttacked(board, Index(5, rank), Opposite(side)) &&
            !IsSquareAttacked(board, Index(6, rank), Opposite(side)))
        {
            moves.Add(new InternalMove(
                from,
                Index(6, rank),
                null,
                MoveFlags.CastleKingSide));
        }

        var queenSideRight = side == Side.White
            ? CastlingRights.WhiteQueenSide
            : CastlingRights.BlackQueenSide;
        if (castlingRights.HasFlag(queenSideRight) &&
            IsRook(board, Index(0, rank), side) &&
            board[Index(1, rank)] is null &&
            board[Index(2, rank)] is null &&
            board[Index(3, rank)] is null &&
            !IsSquareAttacked(board, Index(3, rank), Opposite(side)) &&
            !IsSquareAttacked(board, Index(2, rank), Opposite(side)))
        {
            moves.Add(new InternalMove(
                from,
                Index(2, rank),
                null,
                MoveFlags.CastleQueenSide));
        }
    }

    private static bool IsSquareAttacked(Piece?[] board, int square, Side attacker)
    {
        var file = FileIndex(square);
        var rank = RankIndex(square);

        var pawnSourceRank = rank + (attacker == Side.White ? -1 : 1);
        foreach (var fileOffset in new[] { -1, 1 })
        {
            var pawnFile = file + fileOffset;
            if (IsOnBoard(pawnFile, pawnSourceRank) &&
                board[Index(pawnFile, pawnSourceRank)] is { Type: PieceType.Pawn } pawn &&
                pawn.Side == attacker)
            {
                return true;
            }
        }

        foreach (var offset in KnightOffsets)
        {
            var sourceFile = file + offset.File;
            var sourceRank = rank + offset.Rank;
            if (IsOnBoard(sourceFile, sourceRank) &&
                board[Index(sourceFile, sourceRank)] is { Type: PieceType.Knight } knight &&
                knight.Side == attacker)
            {
                return true;
            }
        }

        if (IsAttackedBySlider(board, file, rank, attacker, BishopDirections, PieceType.Bishop) ||
            IsAttackedBySlider(board, file, rank, attacker, RookDirections, PieceType.Rook))
        {
            return true;
        }

        foreach (var offset in BishopDirections.Concat(RookDirections))
        {
            var sourceFile = file + offset.File;
            var sourceRank = rank + offset.Rank;
            if (IsOnBoard(sourceFile, sourceRank) &&
                board[Index(sourceFile, sourceRank)] is { Type: PieceType.King } king &&
                king.Side == attacker)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAttackedBySlider(
        Piece?[] board,
        int file,
        int rank,
        Side attacker,
        IReadOnlyList<(int File, int Rank)> directions,
        PieceType sliderType)
    {
        foreach (var direction in directions)
        {
            var sourceFile = file + direction.File;
            var sourceRank = rank + direction.Rank;
            while (IsOnBoard(sourceFile, sourceRank))
            {
                var piece = board[Index(sourceFile, sourceRank)];
                if (piece is null)
                {
                    sourceFile += direction.File;
                    sourceRank += direction.Rank;
                    continue;
                }

                if (piece.Value.Side == attacker &&
                    (piece.Value.Type == sliderType || piece.Value.Type == PieceType.Queen))
                {
                    return true;
                }

                break;
            }
        }

        return false;
    }

    private static SimulatedPosition SimulateMove(
        Piece?[] board,
        InternalMove move,
        Side side,
        CastlingRights castlingRights)
    {
        var copy = (Piece?[])board.Clone();
        var piece = copy[move.From]!.Value;
        var captured = copy[move.To];
        copy[move.From] = null;

        if (move.Flags.HasFlag(MoveFlags.EnPassant))
        {
            var capturedSquare = move.To + (side == Side.White ? -8 : 8);
            captured = copy[capturedSquare];
            copy[capturedSquare] = null;
        }

        copy[move.To] = new Piece(move.Promotion ?? piece.Type, piece.Side);

        if (move.Flags.HasFlag(MoveFlags.CastleKingSide))
        {
            var rank = side == Side.White ? 0 : 7;
            copy[Index(5, rank)] = copy[Index(7, rank)];
            copy[Index(7, rank)] = null;
        }
        else if (move.Flags.HasFlag(MoveFlags.CastleQueenSide))
        {
            var rank = side == Side.White ? 0 : 7;
            copy[Index(3, rank)] = copy[Index(0, rank)];
            copy[Index(0, rank)] = null;
        }

        var rights = UpdateCastlingRights(
            castlingRights,
            piece,
            move.From,
            captured,
            move.To);
        int? enPassant = move.Flags.HasFlag(MoveFlags.DoublePawnPush)
            ? (move.From + move.To) / 2
            : null;
        return new SimulatedPosition(copy, rights, enPassant);
    }

    private static CastlingRights UpdateCastlingRights(
        CastlingRights rights,
        Piece movedPiece,
        int from,
        Piece? capturedPiece,
        int capturedOn)
    {
        if (movedPiece.Type == PieceType.King)
        {
            rights &= movedPiece.Side == Side.White
                ? ~(CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide)
                : ~(CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide);
        }
        else if (movedPiece.Type == PieceType.Rook)
        {
            rights = RemoveRookRight(rights, movedPiece.Side, from);
        }

        if (capturedPiece is { Type: PieceType.Rook } rook)
        {
            rights = RemoveRookRight(rights, rook.Side, capturedOn);
        }

        return rights;
    }

    private static CastlingRights RemoveRookRight(CastlingRights rights, Side side, int square)
    {
        if (side == Side.White && square == Index(0, 0))
        {
            return rights & ~CastlingRights.WhiteQueenSide;
        }

        if (side == Side.White && square == Index(7, 0))
        {
            return rights & ~CastlingRights.WhiteKingSide;
        }

        if (side == Side.Black && square == Index(0, 7))
        {
            return rights & ~CastlingRights.BlackQueenSide;
        }

        if (side == Side.Black && square == Index(7, 7))
        {
            return rights & ~CastlingRights.BlackKingSide;
        }

        return rights;
    }

    private static ParsedPosition ParseFen(string fen)
    {
        try
        {
            var fields = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 6)
            {
                throw new ChessNotationException("FEN must contain exactly six fields.");
            }

            var board = ParseFenBoard(fields[0]);
            var side = fields[1] switch
            {
                "w" => Side.White,
                "b" => Side.Black,
                _ => throw new ChessNotationException("FEN active color must be 'w' or 'b'.")
            };
            var rights = ParseCastlingRights(fields[2]);
            var enPassant = ParseEnPassant(fields[3], side);
            if (!int.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out var halfmove) ||
                halfmove < 0)
            {
                throw new ChessNotationException("FEN halfmove clock must be a non-negative integer.");
            }

            if (!int.TryParse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture, out var fullmove) ||
                fullmove < 1)
            {
                throw new ChessNotationException("FEN fullmove number must be a positive integer.");
            }

            ValidateKings(board);
            return new ParsedPosition(board, side, rights, enPassant, halfmove, fullmove);
        }
        catch (ChessNotationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ChessNotationException($"Invalid FEN: {exception.Message}", exception);
        }
    }

    private static Piece?[] ParseFenBoard(string placement)
    {
        var ranks = placement.Split('/');
        if (ranks.Length != 8)
        {
            throw new ChessNotationException("FEN placement must contain eight ranks.");
        }

        var board = new Piece?[64];
        for (var fenRank = 0; fenRank < 8; fenRank++)
        {
            var file = 0;
            foreach (var symbol in ranks[fenRank])
            {
                if (symbol is >= '1' and <= '8')
                {
                    file += symbol - '0';
                    continue;
                }

                if (file >= 8)
                {
                    throw new ChessNotationException("A FEN rank contains too many squares.");
                }

                var piece = FromFenChar(symbol);
                var rank = 7 - fenRank;
                if (piece.Type == PieceType.Pawn && rank is 0 or 7)
                {
                    throw new ChessNotationException("A pawn cannot be placed on the first or eighth rank.");
                }

                board[Index(file, rank)] = piece;
                file++;
            }

            if (file != 8)
            {
                throw new ChessNotationException("Every FEN rank must contain eight squares.");
            }
        }

        return board;
    }

    private static void ValidateKings(Piece?[] board)
    {
        if (board.Count(piece => piece is { Type: PieceType.King, Side: Side.White }) != 1 ||
            board.Count(piece => piece is { Type: PieceType.King, Side: Side.Black }) != 1)
        {
            throw new ChessNotationException("FEN must contain exactly one king for each side.");
        }

        var whiteKing = FindKing(board, Side.White);
        var blackKing = FindKing(board, Side.Black);
        if (Math.Abs(FileIndex(whiteKing) - FileIndex(blackKing)) <= 1 &&
            Math.Abs(RankIndex(whiteKing) - RankIndex(blackKing)) <= 1)
        {
            throw new ChessNotationException("Kings cannot occupy adjacent squares.");
        }
    }

    private static CastlingRights ParseCastlingRights(string text)
    {
        if (text == "-")
        {
            return CastlingRights.None;
        }

        var rights = CastlingRights.None;
        foreach (var symbol in text)
        {
            var right = symbol switch
            {
                'K' => CastlingRights.WhiteKingSide,
                'Q' => CastlingRights.WhiteQueenSide,
                'k' => CastlingRights.BlackKingSide,
                'q' => CastlingRights.BlackQueenSide,
                _ => throw new ChessNotationException($"Invalid FEN castling right '{symbol}'.")
            };
            if (rights.HasFlag(right))
            {
                throw new ChessNotationException("FEN castling rights cannot contain duplicates.");
            }

            rights |= right;
        }

        return rights;
    }

    private static int? ParseEnPassant(string text, Side side)
    {
        if (text == "-")
        {
            return null;
        }

        var square = ParseSquare(text);
        var expectedRank = side == Side.White ? 6 : 3;
        if (square.Rank != expectedRank)
        {
            throw new ChessNotationException("FEN en-passant square is inconsistent with the active color.");
        }

        return SquareIndex(square);
    }

    private static string NormalizePgnSan(string san)
    {
        var normalized = san.Trim()
            .Replace('0', 'O');
        while (normalized.Length > 0 && normalized[^1] is '+' or '#')
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    private static ChessMove ToPublicMove(InternalMove move, string san) => new(
        ToSquare(move.From),
        ToSquare(move.To),
        move.Promotion,
        san);

    private static Piece FromFenChar(char symbol)
    {
        var side = char.IsUpper(symbol) ? Side.White : Side.Black;
        var type = char.ToLowerInvariant(symbol) switch
        {
            'p' => PieceType.Pawn,
            'n' => PieceType.Knight,
            'b' => PieceType.Bishop,
            'r' => PieceType.Rook,
            'q' => PieceType.Queen,
            'k' => PieceType.King,
            _ => throw new ChessNotationException($"Invalid FEN piece '{symbol}'.")
        };
        return new Piece(type, side);
    }

    private static char ToFenChar(Piece piece)
    {
        var symbol = piece.Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => throw new InvalidOperationException("Unknown piece type.")
        };
        return piece.Side == Side.White ? char.ToUpperInvariant(symbol) : symbol;
    }

    private static char PieceLetter(PieceType type) => type switch
    {
        PieceType.Knight => 'N',
        PieceType.Bishop => 'B',
        PieceType.Rook => 'R',
        PieceType.Queen => 'Q',
        PieceType.King => 'K',
        _ => throw new InvalidOperationException("Pawns do not have a SAN piece letter.")
    };

    private static string CastlingText(CastlingRights rights)
    {
        if (rights == CastlingRights.None)
        {
            return "-";
        }

        var builder = new StringBuilder(4);
        if (rights.HasFlag(CastlingRights.WhiteKingSide))
        {
            builder.Append('K');
        }

        if (rights.HasFlag(CastlingRights.WhiteQueenSide))
        {
            builder.Append('Q');
        }

        if (rights.HasFlag(CastlingRights.BlackKingSide))
        {
            builder.Append('k');
        }

        if (rights.HasFlag(CastlingRights.BlackQueenSide))
        {
            builder.Append('q');
        }

        return builder.ToString();
    }

    private static bool IsRook(Piece?[] board, int square, Side side) =>
        board[square] is { Type: PieceType.Rook } rook && rook.Side == side;

    private static int FindKing(Piece?[] board, Side side)
    {
        for (var index = 0; index < board.Length; index++)
        {
            if (board[index] is { Type: PieceType.King } king && king.Side == side)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"The {side} king is missing.");
    }

    private static Square ParseSquare(string text)
    {
        if (text.Length != 2 || text[0] is < 'a' or > 'h' || text[1] is < '1' or > '8')
        {
            throw new ChessNotationException($"Invalid square '{text}'.");
        }

        return new Square(text[0], text[1] - '0');
    }

    private static Square ToSquare(int index) => new(FileOf(index), RankOf(index));

    private static int SquareIndex(Square square) => Index(square.File - 'a', square.Rank - 1);

    private static string SquareName(int index) => $"{FileOf(index)}{RankOf(index)}";

    private static char FileOf(int index) => (char)('a' + FileIndex(index));

    private static int RankOf(int index) => RankIndex(index) + 1;

    private static int FileIndex(int index) => index % 8;

    private static int RankIndex(int index) => index / 8;

    private static int Index(int file, int rank) => (rank * 8) + file;

    private static bool IsOnBoard(int file, int rank) =>
        file is >= 0 and < 8 && rank is >= 0 and < 8;

    private static Side Opposite(Side side) => side == Side.White ? Side.Black : Side.White;

    private readonly record struct Piece(PieceType Type, Side Side);

    private readonly record struct InternalMove(
        int From,
        int To,
        PieceType? Promotion,
        MoveFlags Flags);

    private readonly record struct CanonicalMove(InternalMove Move, string San);

    private readonly record struct SimulatedPosition(
        Piece?[] Board,
        CastlingRights CastlingRights,
        int? EnPassantSquare);

    private readonly record struct ParsedPosition(
        Piece?[] Board,
        Side SideToMove,
        CastlingRights CastlingRights,
        int? EnPassantSquare,
        int HalfmoveClock,
        int FullmoveNumber);

    [Flags]
    private enum CastlingRights
    {
        None = 0,
        WhiteKingSide = 1,
        WhiteQueenSide = 2,
        BlackKingSide = 4,
        BlackQueenSide = 8
    }

    [Flags]
    private enum MoveFlags
    {
        None = 0,
        DoublePawnPush = 1,
        EnPassant = 2,
        CastleKingSide = 4,
        CastleQueenSide = 8
    }
}
