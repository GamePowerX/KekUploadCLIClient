using System.Text;

namespace KekUploadCLIClient;

/// <summary>
///     Console progressbar
/// </summary>
public class ProgressBar : IDisposable
{
    private readonly TextWriter? _originalWriter;
    private readonly ProgressWriter? _writer;

    public ProgressBar()
    {
        if (Program.Silent) return;
        _originalWriter = Console.Out;
        _writer = new ProgressWriter(_originalWriter);
        Console.SetOut(_writer);
    }

    public float CurrentProgress => _writer!.CurrentProgress;

    public void Dispose()
    {
        if (Program.Silent) return;
        if (_originalWriter != null) Console.SetOut(_originalWriter);
        _writer?.ClearProgressBar();
        GC.SuppressFinalize(this);
    }

    public void SetProgress(float f)
    {
        if (Program.Silent) return;
        if (_writer == null) return;
        _writer.CurrentProgress = f;
        _writer.RedrawProgress();
    }

    public void SetProgress(int i)
    {
        SetProgress((float) i);
    }

    public void Increment(float f)
    {
        if (_writer == null) return;
        _writer.CurrentProgress += f;
        _writer.RedrawProgress();
    }

    public void Increment(int i)
    {
        Increment((float) i);
    }

    private class ProgressWriter : TextWriter
    {
        private const string ProgressTemplate = "[{0}] {1:n2}%";
        private const int AllocatedTemplateSpace = 11;
        private readonly TextWriter? _consoleOut;
        private readonly object _syncLock = new();

        private float _currentProgress;

        public ProgressWriter(TextWriter? consoleOut)
        {
            _consoleOut = consoleOut;
            RedrawProgress();
        }

        public override Encoding Encoding => Encoding.UTF8;

        public float CurrentProgress
        {
            get => _currentProgress;
            set
            {
                _currentProgress = value;
                if (_currentProgress > 100)
                    _currentProgress = 100;
                else if (CurrentProgress < 0) _currentProgress = 0;
            }
        }

        private void DrawProgressBar()
        {
            lock (_syncLock)
            {
                var availableSpace = Console.BufferWidth - AllocatedTemplateSpace;
                var percentAmount = (int) (availableSpace * (CurrentProgress / 100));
                var col = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                var progressBar = string.Concat(new string('=', percentAmount),
                    new string(' ', availableSpace - percentAmount));
                _consoleOut?.Write(ProgressTemplate, progressBar, CurrentProgress);
                Console.ForegroundColor = col;
            }
        }

        public void RedrawProgress()
        {
            lock (_syncLock)
            {
                var lastLineWidth = Console.CursorLeft;
                var consoleH = Console.WindowTop + Console.WindowHeight - 1;
                Console.SetCursorPosition(0, consoleH);
                DrawProgressBar();
                Console.SetCursorPosition(lastLineWidth, consoleH - 1);
            }
        }

        private void ClearLineEnd()
        {
            lock (_syncLock)
            {
                var lineEndClear = Console.BufferWidth - Console.CursorLeft - 1;
                _consoleOut?.Write(new string(' ', lineEndClear));
            }
        }

        public void ClearProgressBar()
        {
            lock (_syncLock)
            {
                var lastLineWidth = Console.CursorLeft;
                var consoleH = Console.WindowTop + Console.WindowHeight - 1;
                Console.SetCursorPosition(0, consoleH);
                ClearLineEnd();
                Console.SetCursorPosition(lastLineWidth, consoleH - 1);
            }
        }

        public override void Write(char value)
        {
            lock (_syncLock)
            {
                _consoleOut?.Write(value);
            }
        }

        public override void Write(string? value)
        {
            lock (_syncLock)
            {
                _consoleOut?.Write(value);
            }
        }

        public override void WriteLine(string? value)
        {
            lock (_syncLock)
            {
                _consoleOut?.Write(value);
                _consoleOut?.Write(Environment.NewLine);
                ClearLineEnd();
                _consoleOut?.Write(Environment.NewLine);
                RedrawProgress();
            }
        }

        public override void WriteLine(string format, params object?[] arg)
        {
            WriteLine(string.Format(format, arg));
        }

        public override void WriteLine(int i)
        {
            WriteLine(i.ToString());
        }
    }
}