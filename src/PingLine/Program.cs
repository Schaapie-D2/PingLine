using PingLine.Notification;
using System.Diagnostics;
using System.Text;

namespace PingLine;

internal class Program
{
    private static bool stopApp = false;

    private static void Main(string[] args)
    {
        Console.Title = "PingLine";

        Console.ForegroundColor = ConsoleColor.White;
        TerminalConsole.Write("Hello! Welcome to ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        TerminalConsole.Write("PingLine");
        Console.ForegroundColor = ConsoleColor.White;
        TerminalConsole.WriteLine("!\nUse the help command to start configurating!\n");

        NotificationManager.Load();

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.White;
            TerminalConsole.WriteLine("Saving...");
            NotificationManager.Save();
        };

        StringBuilder inputBuffer = new StringBuilder();
        var stopwatch = Stopwatch.StartNew();
        float lastTime = 0;
        int processCounter = 100 * 60;

        while (!stopApp)
        {
            float currentTime = (float)stopwatch.Elapsed.TotalSeconds;
            float deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            if (processCounter == 100 * 60)
            {
                NotificationManager.ProcessNotifiers().GetAwaiter().GetResult();
                processCounter = 0;
            }

            NotificationManager.UpdateAnimatedImageFrames(deltaTime);

            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    string command = inputBuffer.ToString();
                    inputBuffer.Clear();

                    Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
                    executeCommand(command);
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (inputBuffer.Length > 0)
                    {
                        inputBuffer.Remove(inputBuffer.Length - 1, 1);
                        TerminalConsole.Write("\b \b");
                    }
                }
                else if (keyInfo.KeyChar != '\u0000')
                {
                    inputBuffer.Append(keyInfo.KeyChar);
                    TerminalConsole.Write($"{keyInfo.KeyChar}");
                }
            }

            Thread.Sleep(10);
            processCounter++;
        }
    }

    private static void executeCommand(string input)
    {
        var res = parseArgs(input);
        var command = res.command.ToLower();
        var args = res.args;
        bool skipRewrite = false;

        bool CheckLeng(int len)
        {
            if (args.Count >= len) return false;

            Console.ForegroundColor = ConsoleColor.Red;
            TerminalConsole.WriteLine($"Invalid args. command needs atleast {len} args");
            Console.ForegroundColor = ConsoleColor.White;
            skipRewrite = true;
            return true;
        }

        switch (command)
        {
            case "help":
                help();
                skipRewrite = true;
                break;
            case "clear":
                TerminalConsole.Clear();
                skipRewrite = true;
                break;
            case "rewrite":
                // already done
                break;
            case "save":
                NotificationManager.Save();
                break;
            case "exit":
                stopApp = true;
                break;

            case "newping":
                if(CheckLeng(2)) break;
                NotificationManager.NewPingLine(args[0].ToLower(), args[1]);
                break;
            case "delping":
                if (CheckLeng(1)) break;
                NotificationManager.DeletePingLine(args[0]);
                break;
            case "lspingid":
                foreach (var n in NotificationManager.Notifiers)
                    TerminalConsole.WriteLine($"{n.GetTypeName()}:{n.id}");
                skipRewrite = true;
                break;
            case "lspingtype":
                listPingTypes();
                skipRewrite = true;
                break;
            case "go":
                if (args.Count == 1)
                    NotificationManager.GoToNotificationLink(args[0]);
                else
                    NotificationManager.GoToNotificationLink(null);
                break;

            default:
                Console.ForegroundColor = ConsoleColor.Red;
                TerminalConsole.WriteLine("Command not found");
                Console.ForegroundColor = ConsoleColor.White;
                skipRewrite = true;
                break;
        }

        if (!skipRewrite) NotificationManager.RewriteNotificationLines();
    }

    private static (string command, List<string> args) parseArgs(string input)
    {
        var args = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }

        var command = args.Count > 0 ? args[0] : string.Empty;
        if (args.Count > 0) args.RemoveAt(0);

        return (command, args);
    }

    private static void help()
    {
        TerminalConsole.WriteLine("Notation: <required> | [optional]");
        TerminalConsole.WriteLine();
        TerminalConsole.WriteLine("GENERAL COMMANDS");
        TerminalConsole.WriteLine("help                           - Show this help menu");
        TerminalConsole.WriteLine("clear                          - Clear the console");
        TerminalConsole.WriteLine("rewrite                        - Clear screen and redraw all notification lines");
        TerminalConsole.WriteLine("save                           - Save the configuration");
        TerminalConsole.WriteLine("exit                           - Close the application");
        TerminalConsole.WriteLine();
        TerminalConsole.WriteLine("PING MANAGEMENT");
        TerminalConsole.WriteLine("newping <type> <id>            - Create a new ping");
        TerminalConsole.WriteLine("  Example: newping youtube NewYoutubePing");
        TerminalConsole.WriteLine("           newping time NewTimePing");
        TerminalConsole.WriteLine();
        TerminalConsole.WriteLine("delping <id>                   - Delete a ping");
        TerminalConsole.WriteLine("  Example: delping UC123456");
        TerminalConsole.WriteLine();
        TerminalConsole.WriteLine("lspingid                       - List all ping IDs");
        TerminalConsole.WriteLine("lspingtype                     - List all ping types");
        TerminalConsole.WriteLine("go [id]                        - Open the ping link");
        TerminalConsole.WriteLine("  Example: go UC123456");
        TerminalConsole.WriteLine("           go");
        TerminalConsole.WriteLine();
        listPingTypes();
    }

    private static void listPingTypes()
    {
        TerminalConsole.WriteLine("Available Ping Types:");
        TerminalConsole.WriteLine("- Youtube  (youtube)");
        TerminalConsole.WriteLine("- Twitter  (twitter)");
        TerminalConsole.WriteLine("- Bluesky  (bluesky)");
        TerminalConsole.WriteLine("- Tumblr   (tumblr)");
        TerminalConsole.WriteLine("- RSS 1.0  (rss1)");
        TerminalConsole.WriteLine("- RSS 2.0  (rss2)");
        TerminalConsole.WriteLine("- Atom 1.0 (atom1)");
        TerminalConsole.WriteLine("- Time     (time)");
        TerminalConsole.WriteLine("- Timer    (timer)");
    }
}