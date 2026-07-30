using Chess.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ChessVisor.Pages;

public partial class Home : IAsyncDisposable
{
    private const long MaxPgnBytes = 2 * 1024 * 1024;
    private const int AnimationDuration = 360;
    private const string PgnPlaceholder =
        "[Event \"My game\"]\n[White \"White\"]\n[Black \"Black\"]\n\n1. e4 e5 2. Nf3 Nc6";
    private static readonly double[] Speeds = [0.5, 1, 2];
    private static readonly ThemeOption[] Themes =
    [
        new("system", "System", "◐"),
        new("light", "Light", "☀"),
        new("dark", "Dark", "☾")
    ];

    private string _pgnText = string.Empty;
    private string? _fileName;
    private PgnGame? _game;
    private ChessNotationException? _notationError;
    private int _currentPosition;
    private string _displayFen = new ChessMatch().Fen;
    private bool _boardFlipped;
    private bool _isPlaying;
    private bool _isAnimating;
    private bool _animationActive;
    private bool _animationReverse;
    private ChessMove? _animationMove;
    private double _speed = 1;
    private string _theme = "system";
    private string _announcement = "Board ready.";
    private ElementReference _pgnTextArea;
    private CancellationTokenSource? _playbackCancellation;

    private bool HasMoves => _game?.Moves.Count > 0;

    private bool CanGoNext =>
        _game is not null && _currentPosition < _game.Moves.Count;

    private ChessMove? CurrentMove =>
        _game is not null && _currentPosition > 0
            ? _game.Moves[_currentPosition - 1].Move
            : null;

    private Side CurrentSide => ChessMatch.FromFen(_displayFen).SideToMove;

    private Side TopSide => _boardFlipped ? Side.White : Side.Black;

    private Side BottomSide => _boardFlipped ? Side.Black : Side.White;

    private string TopPlayer =>
        TopSide == Side.White ? Header("White", "White") : Header("Black", "Black");

    private string BottomPlayer =>
        BottomSide == Side.White ? Header("White", "White") : Header("Black", "Black");

    private string PositionLabel
    {
        get
        {
            if (_game is null || _currentPosition == 0)
            {
                return "Starting position";
            }

            var ply = _game.Moves[_currentPosition - 1];
            var prefix = ply.Side == Side.White ? $"{ply.MoveNumber}." : $"{ply.MoveNumber}...";
            return $"{prefix}{ply.DisplaySan}";
        }
    }

    private string ErrorTitle
    {
        get
        {
            if (_notationError is null)
            {
                return string.Empty;
            }

            if (_notationError.MoveNumber is not { } moveNumber ||
                _notationError.Side is not { } side)
            {
                return "This PGN needs a correction";
            }

            var separator = side == Side.White ? "." : "...";
            return $"Move {moveNumber}{separator}{_notationError.Token} is invalid";
        }
    }

    private string ResultLabel =>
        _game?.Result ?? Header("Result", "*");

    private bool HasEventDetails =>
        _game is not null &&
        (_game.Headers.ContainsKey("Event") ||
         _game.Headers.ContainsKey("Site") ||
         _game.Headers.ContainsKey("Date"));

    private string EventDetails =>
        string.Join(
            " · ",
            new[] { Header("Event", ""), Header("Site", ""), Header("Date", "") }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

    private IReadOnlyList<PgnComment> ActiveComments
    {
        get
        {
            if (_game is null)
            {
                return [];
            }

            if (_currentPosition == 0)
            {
                return _game.Comments;
            }

            var ply = _game.Moves[_currentPosition - 1];
            return ply.CommentsBefore.Concat(ply.CommentsAfter).ToArray();
        }
    }

    private IEnumerable<MoveRow> MoveRows =>
        _game?.Moves
            .GroupBy(move => move.MoveNumber)
            .Select(group => new MoveRow(
                group.Key,
                group.FirstOrDefault(move => move.Side == Side.White),
                group.FirstOrDefault(move => move.Side == Side.Black)))
        ?? [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var storedTheme = await JS.InvokeAsync<string?>("chessVisor.getTheme");
        if (Themes.Any(option => option.Value == storedTheme))
        {
            _theme = storedTheme!;
            StateHasChanged();
        }
    }

    private Task LoadGameAsync()
    {
        StopPlayback();
        _notationError = null;
        try
        {
            _game = PgnGame.Parse(_pgnText);
            _currentPosition = 0;
            _displayFen = _game.InitialFen;
            _animationMove = null;
            _announcement = $"Loaded a game with {_game.Moves.Count} moves.";
        }
        catch (ChessNotationException exception)
        {
            _notationError = exception;
            _game = null;
            _currentPosition = 0;
            _displayFen = new ChessMatch().Fen;
            _announcement = ErrorTitle;
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            _notationError = new ChessNotationException(
                string.IsNullOrWhiteSpace(_pgnText)
                    ? "Paste or open a PGN before loading."
                    : exception.Message);
            _game = null;
            _announcement = "The PGN could not be loaded.";
        }

        return Task.CompletedTask;
    }

    private async Task OpenFileAsync(InputFileChangeEventArgs args)
    {
        StopPlayback();
        _notationError = null;
        var file = args.File;
        _fileName = file.Name;
        if (file.Size > MaxPgnBytes)
        {
            _notationError = new ChessNotationException(
                $"{file.Name} is larger than the 2 MB file limit.");
            return;
        }

        try
        {
            await using var stream = file.OpenReadStream(MaxPgnBytes);
            using var reader = new StreamReader(stream);
            _pgnText = await reader.ReadToEndAsync();
            await LoadGameAsync();
        }
        catch (IOException exception)
        {
            _notationError = new ChessNotationException(
                $"The file could not be read: {exception.Message}",
                exception);
        }
    }

    private async Task TogglePlaybackAsync()
    {
        if (_isPlaying)
        {
            StopPlayback();
            return;
        }

        if (!HasMoves || _game is null)
        {
            return;
        }

        if (_currentPosition >= _game.Moves.Count)
        {
            await GoToPositionAsync(0, false, CancellationToken.None);
        }

        _isPlaying = true;
        _playbackCancellation = new CancellationTokenSource();
        var token = _playbackCancellation.Token;
        try
        {
            while (_game is not null &&
                   _currentPosition < _game.Moves.Count &&
                   !token.IsCancellationRequested)
            {
                await GoToPositionAsync(_currentPosition + 1, true, token);
                var rest = Math.Max(80, PlaybackInterval - AnimationDuration);
                await Task.Delay(rest, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isPlaying = false;
            _isAnimating = false;
            _animationActive = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private int PlaybackInterval => (int)(1050 / _speed);

    private async Task PreviousAsync()
    {
        StopPlayback();
        await GoToPositionAsync(_currentPosition - 1, true, CancellationToken.None);
    }

    private async Task NextAsync()
    {
        StopPlayback();
        await GoToPositionAsync(_currentPosition + 1, true, CancellationToken.None);
    }

    private async Task RestartAsync()
    {
        StopPlayback();
        await GoToPositionAsync(0, false, CancellationToken.None);
    }

    private async Task GoToEndAsync()
    {
        StopPlayback();
        if (_game is not null)
        {
            await GoToPositionAsync(_game.Moves.Count, false, CancellationToken.None);
        }
    }

    private async Task SeekAsync(int position)
    {
        StopPlayback();
        await GoToPositionAsync(
            position,
            Math.Abs(position - _currentPosition) == 1,
            CancellationToken.None);
    }

    private async Task GoToPositionAsync(
        int position,
        bool animate,
        CancellationToken cancellationToken)
    {
        if (_game is null || _isAnimating)
        {
            return;
        }

        position = Math.Clamp(position, 0, _game.Moves.Count);
        if (position == _currentPosition)
        {
            return;
        }

        var adjacent = Math.Abs(position - _currentPosition) == 1;
        if (!animate || !adjacent)
        {
            _currentPosition = position;
            _displayFen = FenForPosition(position);
            _animationMove = null;
            AnnouncePosition();
            await InvokeAsync(StateHasChanged);
            await JS.InvokeVoidAsync("chessVisor.scrollToPly", _currentPosition);
            return;
        }

        _isAnimating = true;
        _animationReverse = position < _currentPosition;
        var plyIndex = _animationReverse ? position : _currentPosition;
        _animationMove = _game.Moves[plyIndex].Move;
        _animationActive = false;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(20, cancellationToken);
        _animationActive = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(AnimationDuration, cancellationToken);
        _currentPosition = position;
        _displayFen = FenForPosition(position);
        _animationActive = false;
        _animationMove = null;
        _isAnimating = false;
        AnnouncePosition();
        await InvokeAsync(StateHasChanged);
        await JS.InvokeVoidAsync("chessVisor.scrollToPly", _currentPosition);
    }

    private string FenForPosition(int position)
    {
        if (_game is null || position == 0)
        {
            return _game?.InitialFen ?? new ChessMatch().Fen;
        }

        return _game.Moves[position - 1].FenAfter;
    }

    private void AnnouncePosition()
    {
        _announcement = $"{PositionLabel}. {CurrentSide} to move.";
    }

    private async Task HandleBoardKeyAsync(KeyboardEventArgs args)
    {
        if (args.Key is "ArrowLeft")
        {
            await PreviousAsync();
        }
        else if (args.Key is "ArrowRight")
        {
            await NextAsync();
        }
        else if (args.Key is " " or "Spacebar")
        {
            await TogglePlaybackAsync();
        }
        else if (args.Key is "Home")
        {
            await RestartAsync();
        }
        else if (args.Key is "End")
        {
            await GoToEndAsync();
        }
    }

    private async Task JumpToErrorAsync()
    {
        if (_notationError?.SourceSpan is not { } span)
        {
            return;
        }

        await JS.InvokeVoidAsync(
            "chessVisor.selectRange",
            _pgnTextArea,
            span.Offset,
            Math.Max(1, span.Length));
    }

    private async Task SetThemeAsync(string theme)
    {
        _theme = theme;
        await JS.InvokeVoidAsync("chessVisor.setTheme", theme);
    }

    private void FlipBoard() => _boardFlipped = !_boardFlipped;

    private string Header(string name, string fallback) =>
        _game?.Headers.TryGetValue(name, out var value) == true &&
        !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private bool IsActive(PgnPly ply) =>
        _currentPosition == ply.Index + 1;

    private static bool HasComment(PgnPly ply) =>
        ply.CommentsBefore.Count > 0 || ply.CommentsAfter.Count > 0;

    private void StopPlayback()
    {
        _isPlaying = false;
        _playbackCancellation?.Cancel();
        _playbackCancellation?.Dispose();
        _playbackCancellation = null;
    }

    public ValueTask DisposeAsync()
    {
        StopPlayback();
        return ValueTask.CompletedTask;
    }

    private sealed record ThemeOption(string Value, string Label, string Icon);

    private sealed record MoveRow(int MoveNumber, PgnPly? White, PgnPly? Black);
}
