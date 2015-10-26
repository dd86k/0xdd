using System;

/*
    Tools for the command prompt and terminal.
*/

namespace _0xdd
{
    static class ConsoleTools
    {
        #region Read
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
            System.Text.StringBuilder _out = new System.Text.StringBuilder();
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
                                _out = new System.Text.StringBuilder();
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

                            if (pPassword)
                                Console.Write('*');
                            else
                                Console.Write(c.KeyChar);
                        }
                        break;
                }
            }

            return string.Empty;
        }
        #endregion
    }
}