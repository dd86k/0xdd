using System;

/*
    Various tools for the command prompt and terminal.
*/

namespace ConHexView
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
            int _index = 0;
            bool _get = true;
            int OrigninalLeft = Console.CursorLeft;

            while (_get)
            {
                ConsoleKeyInfo c = Console.ReadKey(true);

                switch (c.Key)
                {
                    // Ignore keys
                    case ConsoleKey.Tab:
                        break;

                    // Returns the string
                    case ConsoleKey.Enter:
                        _get = false;
                        break;

                    case ConsoleKey.Backspace:
                        if (_index > 0)
                        {
                            // Erase whole
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                _out = new System.Text.StringBuilder();
                                _index = 0;
                                Console.SetCursorPosition(OrigninalLeft, Console.CursorTop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(OrigninalLeft, Console.CursorTop);
                            }
                            // Erase one character
                            else
                            {
                                _out = _out.Remove(_out.Length - 1, 1);
                                _index--;
                                Console.SetCursorPosition(OrigninalLeft + _index, Console.CursorTop);
                                Console.Write(' ');
                                Console.SetCursorPosition(OrigninalLeft + _index, Console.CursorTop);
                            }
                        }
                        break;

                    default:
                        if (_index < pLimit)
                        {
                            _out.Append(c.KeyChar);
                            _index++;

                            if (pPassword)
                                Console.Write('*');
                            else
                                Console.Write(c.KeyChar);
                        }
                        break;
                }
            }

            if (_out.Length > 0) return _out.ToString();
            return null;
        }
        #endregion
    }
}