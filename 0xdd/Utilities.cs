using System;
using System.IO;
using System.Text;

namespace _0xdd
{
    static class Utils
    {
        #region Formatting
        const long SIZE_TB = 1099511627776;
        const long SIZE_GB = 1073741824;
        const long SIZE_MB = 1048576;
        const long SIZE_KB = 1024;

        const int DECIMALS = 3;

        public static string FormatSize(long size)
        {
            return FormatSizeDecimal(size);
        }

        public static string FormatSizeDecimal(decimal size)
        {
            if (size > SIZE_TB)
                return $"{Math.Round(size / SIZE_TB, DECIMALS)} TB";
            else if (size > SIZE_GB)
                return $"{Math.Round(size / SIZE_GB, DECIMALS)} GB";
            else if (size > SIZE_MB)
                return $"{Math.Round(size / SIZE_MB, DECIMALS)} MB";
            else if (size > SIZE_KB)
                return $"{Math.Round(size / SIZE_KB, DECIMALS)} KB";
            else
                return $"{size} B";
        }

        /// <summary>
        /// Gets file info and owner from <see cref="FileInfo"/>
        /// </summary>
        /// <param name="file">File.</param>
        /// <returns>Info as a string</returns>
        public static unsafe string GetEntryInfo(this FileInfo file)
        {
            char* o = stackalloc char[9];
            *o = '-'; // Never a directory

            int n = (int)file.Attributes;
            *++o = (n & 0x20) > 0 ?   'a' : '-'; // Archive
            *++o = (n & 0x800) > 0 ?  'c' : '-'; // Compressed
            *++o = (n & 0x4000) > 0 ? 'e' : '-'; // Encrypted
            *++o = (n & 1) > 0 ?      'r' : '-'; // Read only
            *++o = (n & 4) > 0 ?      's' : '-'; // System
            *++o = (n & 2) > 0 ?      'h' : '-'; // Hidden
            *++o = (n & 0x100) > 0 ?  't' : '-'; // Temporary
            *++o = '\0'; // Safety measure

            return new string(o - 8); // Pointer moved 8 times.
        }
        #endregion

        #region User input
        /// <summary>
        /// Readline with a maximum length plus optional password mode.
        /// </summary>
        /// <param name="limit">Character limit</param>
        /// <param name="password">Is password</param>
        /// <returns>User's input</returns>
        /// <remarks>v1.1.1 - 0xdd</remarks>
        internal static string ReadLine(int limit, string suggestion = null, bool password = false)
        { //TODO: Rewrite ReadLine(int) to reduce cyclomatic complexity
            StringBuilder o = new StringBuilder(suggestion ?? string.Empty);
            int i = 0;
            bool g = true;
            int oleft = Console.CursorLeft; // Origninal Left Position
            int otop = Console.CursorTop; // Origninal Top Position

            if (suggestion != null)
            {
                Console.Write(suggestion);
                i = suggestion.Length;
                Console.SetCursorPosition(oleft + i, otop);
            }

            Console.CursorVisible = true;

            while (g)
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
                        if (i > 0)
                        {
                            Console.SetCursorPosition(oleft + --i, otop);
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (i < o.Length)
                        {
                            Console.SetCursorPosition(oleft + ++i, otop);
                        }
                        break;
                    case ConsoleKey.Home:
                        if (i > 0)
                        {
                            i = 0;
                            Console.SetCursorPosition(oleft, otop);
                        }
                        break;
                    case ConsoleKey.End:
                        if (i < o.Length)
                        {
                            i = o.Length;
                            Console.SetCursorPosition(oleft + i, otop);
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (i < o.Length)
                        {
                            // Erase whole from index
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o = o.Remove(i, o.Length - i);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', limit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(password ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + i, otop);
                            }
                            else // Erase one character
                            {
                                o = o.Remove(i, 1);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', limit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(password ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + i, otop);
                            }
                        }
                        break;

                    case ConsoleKey.Backspace:
                        if (i > 0)
                        {
                            // Erase whole from index
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o = o.Remove(0, i);
                                i = 0;
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', limit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(password ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + i, otop);
                            }
                            else // Erase one character
                            {
                                o = o.Remove(--i, 1);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', limit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(password ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + i, otop);
                            }
                        }
                        break;

                    default:
                        if (o.Length < limit)
                        {
                            char h = c.KeyChar;

                            if (char.IsLetterOrDigit(h) || char.IsPunctuation(h) || char.IsSymbol(h) || char.IsWhiteSpace(h))
                            {
                                o.Insert(i++, h);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', limit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(password ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + i, otop);
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
        #endregion

        #region Console
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

        /// <summary>
        /// Toggles current ForegroundColor to black
        /// and BackgroundColor to gray.
        /// </summary>
        internal static void ToggleColors()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }

        internal static int GetBytesInRow()
        {
            return ((Console.WindowWidth - 10) / 4) - 1;
        }
        #endregion
    }
}