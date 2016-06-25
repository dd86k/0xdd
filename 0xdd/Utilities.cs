using System;
using System.IO;
using System.Security.Principal;
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

        static internal string FormatSize(long pSize)
        {
            return FormatSize(pSize);
        }

        static internal string FormatSizeDemical(decimal pSize)
        {
            if (pSize > SIZE_TB)
                return $"{Math.Round(pSize / SIZE_TB, 2)} TB";
            else if (pSize > SIZE_GB)
                return $"{Math.Round(pSize / SIZE_GB, 2)} GB";
            else if (pSize > SIZE_MB)
                return $"{Math.Round(pSize / SIZE_MB, 2)} MB";
            else if (pSize > SIZE_KB)
                return $"{Math.Round(pSize / SIZE_KB, 2)} KB";
            else
                return $"{pSize} B";
        }

        /// <summary>
        /// Gets file info and owner from <see cref="FileInfo"/>
        /// </summary>
        /// <param name="pFile">File.</param>
        /// <returns>Info as a string</returns>
        internal static string GetEntryInfo(this FileInfo pFile)
        {
            string o = "-"; // Never a directory

            FileAttributes fa = pFile.Attributes;

            o += fa.HasFlag(FileAttributes.Archive) ? "a" : "-";
            o += fa.HasFlag(FileAttributes.Compressed) ? "c" : "-";
            o += fa.HasFlag(FileAttributes.Encrypted) ? "e" : "-";
            o += fa.HasFlag(FileAttributes.ReadOnly) ? "r" : "-";
            o += fa.HasFlag(FileAttributes.System) ? "s" : "-";
            o += fa.HasFlag(FileAttributes.Hidden) ? "h" : "-";
            o += fa.HasFlag(FileAttributes.Temporary) ? "t" : "-";

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                o += "  " +
                    pFile.GetAccessControl()
                    .GetOwner(typeof(SecurityIdentifier))
                    .Translate(typeof(NTAccount));
            }

            return o;
        }
        #endregion

        #region User input
        /// <summary>
        /// Readline with a maximum length plus optional password mode.
        /// </summary>
        /// <param name="pLimit">Character limit</param>
        /// <param name="pPassword">Is password</param>
        /// <returns>User's input</returns>
        /// <remarks>v1.1.1 - 0xdd</remarks>
        internal static string ReadLine(int pLimit, string pSuggestion = null, bool pPassword = false)
        {
            StringBuilder o = new StringBuilder(pSuggestion ?? string.Empty);
            int Index = 0;
            bool Continue = true;
            int oleft = Console.CursorLeft; // Origninal Left Position
            int otop = Console.CursorTop; // Origninal Top Position

            if (pSuggestion != null)
            {
                Console.Write(pSuggestion);
                Index = pSuggestion.Length;
                Console.SetCursorPosition(oleft + Index, otop);
            }

            Console.CursorVisible = true;

            while (Continue)
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
                        return string.Empty;

                    // Returns the string
                    case ConsoleKey.Enter:
                        Console.CursorVisible = false;
                        return o.ToString();

                    // Navigation
                    case ConsoleKey.LeftArrow:
                        if (Index > 0)
                        {
                            Console.SetCursorPosition(oleft + --Index, otop);
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (Index < o.Length)
                        {
                            Console.SetCursorPosition(oleft + ++Index, otop);
                        }
                        break;
                    case ConsoleKey.Home:
                        if (Index > 0)
                        {
                            Index = 0;
                            Console.SetCursorPosition(oleft, otop);
                        }
                        break;
                    case ConsoleKey.End:
                        if (Index < o.Length)
                        {
                            Index = o.Length;
                            Console.SetCursorPosition(oleft + Index, otop);
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (Index < o.Length)
                        {
                            // Erase whole from index
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o = o.Remove(Index, o.Length - Index);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                            else // Erase one character
                            {
                                o = o.Remove(Index, 1);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                        }
                        break;

                    case ConsoleKey.Backspace:
                        if (Index > 0)
                        {
                            // Erase whole from index
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o = o.Remove(0, Index);
                                Index = 0;
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                            else // Erase one character
                            {
                                o = o.Remove(--Index, 1);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                        }
                        break;

                    default:
                        if (o.Length < pLimit)
                        {
                            char h = c.KeyChar;

                            if (char.IsLetterOrDigit(h) || char.IsPunctuation(h) || char.IsSymbol(h) || char.IsWhiteSpace(h))
                            {
                                o.Insert(Index++, h);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                        }
                        break;
                }
            }

            return string.Empty;
        }

        internal static long ReadValue(int pLimit, string pSuggestion = null)
        {
            string t = ReadLine(pLimit, pSuggestion);

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

        internal static long? GetNumberFromUser(string pMessage, int pWidth = 27, int pHeight = 4, string pSuggestion = null)
        {
            GenerateInputBox(pMessage, pWidth, pHeight);

            long? t = null;

            try
            {
                t = ReadValue(pWidth - 2, pSuggestion);
            }
            catch { }

            Console.ResetColor();

            return t;
        }

        internal static string GetUserInput(string pMessage, int pWidth = 32, int pHeight = 4, string pSuggestion = null)
        {
            GenerateInputBox(pMessage, pWidth, pHeight);

            string t = ReadLine(pWidth - 2, pSuggestion: pSuggestion);

            Console.ResetColor();

            return t;
        }
        #endregion

        #region Console
        static void GenerateInputBox(string pMessage, int pWidth, int pHeight)
        {
            // -- Begin prepare box --
            int startx = (Console.WindowWidth / 2) - (pWidth / 2);
            int starty = (Console.WindowHeight / 2) - (pHeight / 2);

            Console.SetCursorPosition(startx, starty);
            Console.Write('┌');
            Console.Write(new string('─', pWidth - 2));
            Console.Write('┐');

            for (int i = 0; i < pHeight - 2; i++)
            {
                Console.SetCursorPosition(startx, starty + i + 1);
                Console.Write('│');
            }
            for (int i = 0; i < pHeight - 2; i++)
            {
                Console.SetCursorPosition(startx + pWidth - 1, starty + i + 1);
                Console.Write('│');
            }

            Console.SetCursorPosition(startx, starty + pHeight - 1);
            Console.Write('└');
            Console.Write(new string('─', pWidth - 2));
            Console.Write('┘');

            Console.SetCursorPosition(startx + 1, starty + 1);
            Console.Write(pMessage);
            if (pMessage.Length < pWidth - 2)
                Console.Write(new string(' ', pWidth - pMessage.Length - 2));
            // -- End prepare box --

            // -- Begin prepare text box --
            ToggleColors();
            Console.SetCursorPosition(startx + 1, starty + 2);
            Console.Write(new string(' ', pWidth - 2));
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
            return ((Console.WindowWidth - 10) / 4) - 2;
        }
        #endregion
    }
}
