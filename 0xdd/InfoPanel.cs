﻿using System;

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
            long pos = _0xdd.Stream.Position;
            long r = (((pos + _0xdd.DisplayBuffer.Length) * 100) / _0xdd.File.Length);

            if (_msg)
            {
                Console.SetCursorPosition(0, top);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                _msg = false;
            }

            Console.SetCursorPosition(0, top);
            Console.Write($" DEC:{pos:D8} | HEX:{pos:X8} | OCT:{_0xdd.ToOct(pos)}          ");
            Console.SetCursorPosition(Console.WindowWidth - 6, top);
            Console.Write($"{r,3}%");
        }

        /// <summary>
        /// Displays a message on screen to inform the user.
        /// </summary>
        /// <param name="pMessage">Message to show.</param>
        public static void Message(string pMessage)
        {
            Console.SetCursorPosition(0, StartPosition);
            Console.Write(new string(' ', Console.WindowWidth - 1));

            string msg = $"[ {pMessage} ]";

            WindowSystem.ToggleColors();

            Console.SetCursorPosition(
                (Console.WindowWidth / 2) - (msg.Length / 2),
                StartPosition);
            Console.Write(msg);

            Console.ResetColor();

            _msg = true;
        }
    }
}