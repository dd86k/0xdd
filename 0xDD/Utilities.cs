using System;
using System.Text;

namespace _0xdd
{
    static class Utils
    {
        #region Formatting
        static internal string GetFormattedSize(long pSize)
        {
            if (pSize > Math.Pow(1024, 3)) // GB
                return $"{Math.Round(pSize / Math.Pow(1024, 3), 2)} GB";
            else if (pSize > Math.Pow(1024, 2)) // MB
                return $"{Math.Round(pSize / Math.Pow(1024, 2), 2)} MB";
            else if (pSize > 1024) // KB
                return $"{Math.Round((double)pSize / 1024, 1)} KB";
            else // B
                return $"{pSize} B";
        }
        #endregion

        #region User input
        /// <summary>
        /// Readline with a maximum length.
        /// </summary>
        /// <param name="pLimit">Limit in characters</param>
        /// <returns>User's input</returns>
        internal static string ReadLine(int pLimit)
        {
            return ReadLine(pLimit, false);
        }

        /// <summary>
        /// Readline with a maximum length plus optional password mode.
        /// </summary>
        /// <param name="pLimit">Character limit</param>
        /// <param name="pPassword">Is password</param>
        /// <returns>User's input</returns>
        internal static string ReadLine(int pLimit, bool pPassword)
        {
            StringBuilder _out = new StringBuilder();
            int Index = 0;
            bool gotString = false;
            int OrigninalLeftPosition = Console.CursorLeft;

            while (!gotString)
            {
                ConsoleKeyInfo c = Console.ReadKey(true);

                switch (c.Key)
                {
                    // Ignore keys
                    case ConsoleKey.Tab:
                        break;

                    // Cancel
                    case ConsoleKey.Escape:
                        gotString = true;
                        return string.Empty;

                    // Returns the string
                    case ConsoleKey.Enter:
                        gotString = true;
                        if (_out.Length > 0)
                            return _out.ToString();
                        break;

                    case ConsoleKey.Backspace:
                        if (Index > 0)
                        {
                            // Erase whole
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                _out = new StringBuilder();
                                Index = 0;
                                Console.SetCursorPosition(OrigninalLeftPosition, Console.CursorTop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(OrigninalLeftPosition, Console.CursorTop);
                            }
                            // Erase one character
                            else
                            {
                                _out = _out.Remove(_out.Length - 1, 1);
                                Index--;
                                Console.SetCursorPosition(OrigninalLeftPosition + Index, Console.CursorTop);
                                Console.Write(' ');
                                Console.SetCursorPosition(OrigninalLeftPosition + Index, Console.CursorTop);
                            }
                        }
                        break;

                    default:
                        if (Index < pLimit)
                        {
                            _out.Append(c.KeyChar);
                            Index++;

                            Console.Write(pPassword ? '*' : c.KeyChar);
                        }
                        break;
                }
            }

            return string.Empty;
        }

        internal static long ReadValue(int pLimit)
        {
            string t = ReadLine(pLimit);
            
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

        internal static string GetUserInput(string pMessage, int pMaxBytes, long pCurrentFileLength)
        {
            return GetUserInput(pMessage, 27, 4, pMaxBytes, pCurrentFileLength);
        }

        internal static long? GetNumberFromUser(string pMessage, int pMaxBytes, long pCurrentFileLength)
        {
            return GetNumberFromUser(pMessage, 27, 4, pMaxBytes, pCurrentFileLength);
        }

        internal static long? GetNumberFromUser(string pMessage, int pWidth, int pHeight, int pMaxBytes, long pCurrentFileLength)
        {
            GenerateInputBox(pMessage, pWidth, pHeight);

            long? t = null;

            try
            {
                t = ReadValue(pWidth - 2);
            }
            catch
            {

            }
            
            Console.ResetColor();

            return t;
        }

        internal static string GetUserInput(string pMessage, int pWidth, int pHeight, int pMaxBytes, long pCurrentFileLength)
        {
            int width = 27;
            int height = 4;

            GenerateInputBox(pMessage, width, height);

            string t = ReadLine(pWidth - 2);
            
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
            return ((Console.WindowWidth - 10) / 4) - 1;
        }
        #endregion
    }
}
