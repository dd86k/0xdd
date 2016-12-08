using System;

namespace _0xdd
{
    /// <summary>
    /// Current position information.
    /// </summary>
    static class InfoPanel
    {
        /// <summary>
        /// If there's an active message on screen.
        /// </summary>
        static bool _msg = false;

        /// <summary>
        /// Starting position to rendering on the console (Y axis).
        /// </summary>
        public static int StartPosition => Console.WindowHeight - 1;

        /// <summary>
        /// Update the offset information
        /// </summary>
        public static void Update()
        {
            int top = StartPosition;
            long pos = FilePanel.CurrentPosition;
            long r = (((pos + FilePanel.BufferSize) * 100) / FilePanel.FileSize);

            if (_msg)
            {
                Clear();
                _msg = false;
            }

            Console.SetCursorPosition(0, top);
            Console.Write($" DEC:{pos:D8} | HEX:{pos:X8} | OCT:{Main0xddApp.ToOct(pos, 8)}      ");
            Console.SetCursorPosition(Console.WindowWidth - 6, top);
            Console.Write($"{r,3}%");
        }

        public static void Clear()
        {
            Console.SetCursorPosition(0, StartPosition);
            Console.Write(new string(' ', Console.WindowWidth - 1));
        }

        /// <summary>
        /// Displays a message on screen to inform the user.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public static void Message(string message)
        {
            Clear();

            string msg = $"[ {message} ]";

            ToggleColors();

            Console.SetCursorPosition(
                (Console.WindowWidth / 2) - (msg.Length / 2),
                StartPosition
            );
            Console.Write(msg);

            Console.ResetColor();

            _msg = true;
        }

        static void ToggleColors()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }
    }
}
