using System;
using System.Text;

namespace _0xdd
{
    static class Dialog
    {
        static byte _lastByte;
        static string _lastString;

        public static void GenerateWindow(
            int width = 27, int height = 12,
            string title = null, string text = null,
            int left = -1, int top = -1)
        {
            // Preparations

            if (left == -1)
                left = (Console.WindowWidth / 2) - (width / 2);

            if (top == -1)
                top = (Console.WindowHeight / 2) - (height / 2);

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;

            string t = new string(' ', width);

            // Titlebar

            Console.SetCursorPosition(left, top);

            if (title != null)
                Console.Write(title.Center(width));
            else
                Console.Write(t);

            // Window

            Console.BackgroundColor = ConsoleColor.Gray;

            int ymax = height + top;
            for (int y = top + 1; y < ymax; ++y)
            {
                Console.SetCursorPosition(left, y);
                Console.Write(t);
            }

            // Text

            if (text != null)
            {
                Console.SetCursorPosition(++left, top += 2);

                if (text.Contains("\n"))
                {
                    string[] lines = text.Split('\n');
                    for (int i = 0; i < lines.Length; ++i, ++top)
                    {
                        Console.SetCursorPosition(left, top);
                        Console.Write(lines[i].Center(width - 2));
                    }
                }
                else
                {
                    Console.Write(text.Center(width - 2));
                }
            }

            Console.ResetColor();
        }

        public static void PromptFindByte()
        {
            if (FilePanel.CurrentPosition + FilePanel.BufferSize >= FilePanel.FileSize)
            {
                InfoPanel.Message("Already at the end of the file.");
                return;
            }

            long t = GetNumberFromUser("Find byte:",
                suggestion: $"0x{_lastByte:X2}");

            if (t == -1)
            {
                FilePanel.Update();
                InfoPanel.Message("Canceled.");
                return;
            }

            if (t < 0 || t > byte.MaxValue)
            {
                FilePanel.Update();
                InfoPanel.Message("A value between 0 and 255 is required.");
            }
            else
            {
                FilePanel.Update();
                InfoPanel.Message("Searching...");
                long p = Finder.FindByte(
                    _lastByte = (byte)t,
                    FilePanel.FileIO,
                    FilePanel.File,
                    FilePanel.CurrentPosition + 1
                );

                if (p > 0)
                {
                    MainApp.Goto(--p);
                    if (p > uint.MaxValue)
                        InfoPanel.Message($"Found {t:X2} at {p:X16}");
                    else
                        InfoPanel.Message($"Found {t:X2} at {p:X8}");
                }
                else
                {
                    switch (p)
                    {
                        case -1:
                            InfoPanel.Message($"No results.");
                            break;
                        case -2:
                            InfoPanel.Message($"Position out of bound.");
                            break;
                        case -3:
                            InfoPanel.Message($"File not found!");
                            break;

                        default:
                            InfoPanel.Message($"Unknown error occurred. (0x{p:X2})");
                            break;
                    }
                }
            }
        }

        public static void PromptSearchString()
        {
            if (FilePanel.CurrentPosition >= FilePanel.FileSize - FilePanel.BufferSize)
            {
                InfoPanel.Message("Already at the end of the file.");
                return;
            }

            if (FilePanel.BufferSize >= FilePanel.FileSize)
            {
                InfoPanel.Message("Not possible.");
                return;
            }

            _lastString = GetUserInput("Find data:", suggestion: _lastString);

            if (_lastString == null || _lastString.Length == 0)
            {
                FilePanel.Update();
                InfoPanel.Message("Canceled.");
                return;
            }

            FilePanel.Update();
            InfoPanel.Message("Searching...");
            long t = Finder.FindString(
                _lastString,
                FilePanel.FileIO,
                FilePanel.File,
                FilePanel.CurrentPosition + 1
            );

            switch (t)
            {
                case -1:
                    InfoPanel.Message($"No results.");
                    break;
                case -2:
                    InfoPanel.Message($"Position out of bound.");
                    break;
                case -3:
                    InfoPanel.Message($"File not found!");
                    break;

                default:
                    MainApp.Goto(t);
                    InfoPanel.Message($"Found at {t:X2}h.");
                    break;
            }
        }

        public static void PromptGoto()
        {
            if (FilePanel.BufferSize >= FilePanel.FileSize)
            {
                InfoPanel.Message("Not possible.");
                return;
            }

            if (FilePanel.CurrentPosition >= FilePanel.FileSize - FilePanel.BufferSize)
            {
                InfoPanel.Message("Already at the end of the file.");
                return;
            }

            long t = GetNumberFromUser("Goto:");

            if (t == -1)
            {
                FilePanel.Update();
                InfoPanel.Message("Canceled.");
                return;
            }

            if (t >= 0 && t <= FilePanel.FileSize - FilePanel.BufferSize)
            {
                MainApp.Goto(t);
            }
            else
            {
                FilePanel.Update();
                InfoPanel.Message("Position out of bound!");
            }
        }

        public static void PromptOffset()
        {
            string c = GetUserInput("Hex, Dec, Oct?");

            if (c == null || c.Length < 1)
            {
                InfoPanel.Message("Canceled.");
                FilePanel.Update();
                return;
            }

            switch (c[0])
            {
                case 'H': case 'h':
                    MainApp.OffsetView = OffsetView.Hex;
                    OffsetPanel.Update();
                    InfoPanel.Update();
                    break;

                case 'O': case 'o':
                    MainApp.OffsetView = OffsetView.Oct;
                    OffsetPanel.Update();
                    InfoPanel.Update();
                    break;

                case 'D': case 'd':
                    MainApp.OffsetView = OffsetView.Dec;
                    OffsetPanel.Update();
                    InfoPanel.Update();
                    break;

                default:
                    InfoPanel.Message("Invalid view mode!");
                    break;
            }

            FilePanel.Update();
        }

        /// <summary>
        /// Readline with a maximum length plus optional password mode.
        /// </summary>
        /// <param name="limit">Character limit</param>
        /// <param name="password">Is password</param>
        /// <returns>User's input</returns>
        /// <remarks>v1.1.1 - 0xdd</remarks>
        static string ReadLine(int limit, string suggestion = null)
        {
            StringBuilder o = new StringBuilder(suggestion ?? string.Empty);
            int index = 0;
            bool ongoing = true;
            int oleft = Console.CursorLeft; // Origninal Left Position
            int otop = Console.CursorTop; // Origninal Top Position

            if (suggestion != null)
            {
                Console.Write(suggestion);
                index = suggestion.Length;
                Console.SetCursorPosition(oleft + index, otop);
            }

            Console.CursorVisible = true;

            while (ongoing)
            {
                ConsoleKeyInfo c = Console.ReadKey(true);

                switch (c.Key)
                {
                    // Ignore keys
                    case ConsoleKey.Tab:
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.DownArrow:
                        break;

                    // Cancel
                    case ConsoleKey.Escape:
                        Console.CursorVisible = false;
                        Console.ResetColor();
                        return null;

                    // Returns the string
                    case ConsoleKey.Enter:
                        Console.CursorVisible = false;
                        Console.ResetColor();
                        return o.ToString();

                    // Navigation
                    case ConsoleKey.LeftArrow:
                        if (index > 0)
                        {
                            Console.SetCursorPosition(oleft + --index, otop);
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (index < o.Length)
                        {
                            Console.SetCursorPosition(oleft + ++index, otop);
                        }
                        break;
                    case ConsoleKey.Home:
                        if (index > 0)
                        {
                            index = 0;
                            Console.SetCursorPosition(oleft, otop);
                        }
                        break;
                    case ConsoleKey.End:
                        if (index < o.Length)
                        {
                            index = o.Length;
                            Console.SetCursorPosition(oleft + index, otop);
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (index < o.Length)
                        {
                            // Erase whole from index
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o = o.Remove(index, o.Length - index);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(o.ToString() + new string(' ', limit - o.Length));
                                Console.SetCursorPosition(oleft + index, otop);
                            }
                            else // Erase one character
                            {
                                o = o.Remove(index, 1);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(o.ToString() + new string(' ', limit - o.Length));
                                Console.SetCursorPosition(oleft + index, otop);
                            }
                        }
                        break;

                    case ConsoleKey.Backspace:
                        if (index > 0)
                        {
                            // Erase whole from index
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o.Remove(0, index);
                                index = 0;
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(o.ToString() + new string(' ', limit - o.Length));
                                Console.SetCursorPosition(oleft + index, otop);
                            }
                            else // Erase one character
                            {
                                o.Remove(--index, 1);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(o.ToString() + new string(' ', limit - o.Length));
                                Console.SetCursorPosition(oleft + index, otop);
                            }
                        }
                        break;

                    default:
                        if (o.Length < limit)
                        {
                            char h = c.KeyChar;

                            if (char.IsLetterOrDigit(h) || char.IsPunctuation(h) ||
                                char.IsSymbol(h) || char.IsWhiteSpace(h))
                            {
                                o.Insert(index++, h);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(o.ToString() + new string(' ', limit - o.Length));
                                Console.SetCursorPosition(oleft + index, otop);
                            }
                        }
                        break;
                }
            }

            return null;
        }

        internal static long ReadValue(int limit, string suggestion = null)
        {
            string t = ReadLine(limit, suggestion);

            Console.ResetColor();

            if (t.Length > 0)
                if (t.StartsWith("0x")) // Hexadecimal
                {
                    return Convert.ToInt64(t, 16);
                }
                else if (t[0] == '0') // Octal
                {
                    return Convert.ToInt64(t, 8);
                }
                else // Decimal
                {
                    return long.Parse(t);
                }
            else return -1;
        }

        internal static long GetNumberFromUser(string message, int width = 27, int height = 4, string suggestion = null)
        {
            GenerateInputBox(message, width, height);

            long t = -1;

            t = ReadValue(width - 2, suggestion);

            return t;
        }

        internal static string GetUserInput(string message, int width = 32, int height = 4, string suggestion = null)
        {
            GenerateInputBox(message, width, height);

            return ReadLine(width - 2, suggestion: suggestion);
        }

        static void GenerateInputBox(string message, int width, int height)
        {
            // -- Begin prepare box --
            int startx = (Console.WindowWidth / 2) - (width / 2);
            int starty = (Console.WindowHeight / 2) - (height / 2);

            string line = new string('─', width - 2);

            Console.SetCursorPosition(startx, starty);
            Console.Write('┌');
            Console.Write(line);
            Console.Write('┐');

            for (int i = 0; i < height - 2; i++)
            {
                Console.SetCursorPosition(startx, starty + i + 1);
                Console.Write('│');
            }
            for (int i = 0; i < height - 2; i++)
            {
                Console.SetCursorPosition(startx + width - 1, starty + i + 1);
                Console.Write('│');
            }

            Console.SetCursorPosition(startx, starty + height - 1);
            Console.Write('└');
            Console.Write(line);
            Console.Write('┘');

            Console.SetCursorPosition(startx + 1, starty + 1);
            Console.Write(message);
            if (message.Length < width - 2)
                Console.Write(new string(' ', width - message.Length - 2));
            // -- End prepare box --

            // -- Begin prepare text box --
            ToggleColors();
            Console.SetCursorPosition(startx + 1, starty + 2);
            Console.Write(new string(' ', width - 2));
            Console.SetCursorPosition(startx + 1, starty + 2);
            // -- End prepare text box --
        }

        static void ToggleColors()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }
    }
}
