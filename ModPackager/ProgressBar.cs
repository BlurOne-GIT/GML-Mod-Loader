public class ProgressBar
{
    private const int progressWidth = 50;
    public int Total { get; set; }
    private readonly int cursorTop;
    public ProgressBar(int total, int initial = 0)
    {
        Total = total;
        cursorTop = Console.CursorTop;
        Console.WriteLine();
    }

    public void UpdateProgress(int current)
    {
        // Don't use Console.SetCursorPosition(x, y) because it moves to y before x;
        int currentConsoleTop = Console.CursorTop;
        int currentConsoleLeft = Console.CursorLeft;
        Console.CursorLeft = 0;
        Console.CursorTop = cursorTop;
        int filledWidth = Total is 0 ? progressWidth : (int)Math.Floor(progressWidth * (double)current / Total);
        Console.Write("[" + new string('#', filledWidth) + new string('-', progressWidth - filledWidth) + "] " + current + "/" + Total + "                     ");
        Console.CursorLeft = currentConsoleLeft;
        Console.CursorTop = currentConsoleTop;
    }
}