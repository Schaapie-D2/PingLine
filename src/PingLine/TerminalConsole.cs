using System;
using System.Text.RegularExpressions;

namespace PingLine;

public static class TerminalConsole
{
    private static readonly List<int> lineLengths = new();
    private static int currentLineLength;

    private static int bufferLeft;
    private static int bufferTop;

    private static int overwriteDepth = 0;

    private static readonly Regex AnsiEscape = new(@"\x1b\[[0-9;?]*[a-zA-Z]", RegexOptions.Compiled);

    private static int VisibleLength(string s) => AnsiEscape.Replace(s, "").Length;

    public static void Clear()
    {
        lineLengths.Clear();
        currentLineLength = 0;
        bufferLeft = 0;
        bufferTop = 0;

        Console.Clear();
    }

    public static void Write(string content)
    {
        if (content.Length == 0) return;

        if (overwriteDepth > 0)
        {
            Console.Write(content);
            return;
        }

        var normalized = content.Replace("\r\n", "\n");
        var segments = normalized.Split('\n');

        for (int i = 0; i < segments.Length; i++)
        {
            if (i > 0)
            {
                lineLengths.Add(currentLineLength);
                bufferTop += RowsForLength(currentLineLength);
                currentLineLength = 0;
                bufferLeft = 0;
            }

            int segmentVisibleLength = VisibleLength(segments[i]);
            currentLineLength += segmentVisibleLength;

            int width = Math.Max(1, Console.WindowWidth);
            bufferLeft = currentLineLength % width == 0 && currentLineLength > 0
                ? width
                : currentLineLength % width;
        }

        Console.Write(content);
    }

    public static void WriteLine(string content)
    {
        Write(content);
        Write("\n");
    }

    public static void WriteLine() => Write("\n");
    
    public static IDisposable BeginOverwrite()
    {
        overwriteDepth++;
        return new OverwriteScope();
    }

    private sealed class OverwriteScope : IDisposable
    {
        private bool disposed;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            overwriteDepth--;
        }
    }

    public static int SetBufferCursorPosition(int left, int top)
    {
        int calculated = CalculateLinesWritten();
        int windowTop = Math.Max(0, calculated - Console.WindowHeight);
        int screenTop = top - windowTop;
        int overshoot = Math.Min(0, screenTop);

        screenTop = Math.Max(0, screenTop);

        Console.SetCursorPosition(left, screenTop);

        return overshoot;
    }

    public static (int Left, int Top) GetBufferCursorPosition()
    {
        return (bufferLeft, bufferTop);
    }

    public static int CalculateLinesWritten()
    {
        int lineCount = 0;

        foreach (int length in lineLengths)
            lineCount += RowsForLength(length);

        if (currentLineLength > 0)
            lineCount += RowsForLength(currentLineLength);

        return lineCount;
    }

    private static int RowsForLength(int length)
    {
        int width = Math.Max(1, Console.WindowWidth);
        return Math.Max(1, (length + width - 1) / width);
    }
}