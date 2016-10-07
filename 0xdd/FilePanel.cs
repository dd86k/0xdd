using System;
using System.Text;

namespace _0xdd
{
    /// <summary>
    /// Main panel which represents the offset, data as bytes,
    /// and data as ASCII characters.
    /// </summary>
    internal static class FilePanel
    {
        /// <summary>
        /// Current cursor position for Edit mode.
        /// </summary>
        //static internal long CursorPosition;

        /// <summary>
        /// Gets the position to start rendering on the console (Y axis).
        /// </summary>
        const int StartPosition = 2;

        /// <summary>
        /// Update from Buffer.
        /// </summary>
        static internal void Update()
        {
            int width = Console.WindowWidth - 1;

            long len = _0xdd.File.Length; // File size
            long pos = _0xdd.Stream.Position; // File position

            OffsetPanel.Update();

            /* TODO: Check if we can do a little pointer-play with the buffer. */

            int d = 0;
            Console.SetCursorPosition(0, StartPosition);
            StringBuilder line, ascii;
            for (int li = 0; li < _0xdd.DisplayBuffer.Length; li += _0xdd.BytesPerRow) // LineIndex
            {
                switch (_0xdd.OffsetView)
                {
                    default:
                        line = new StringBuilder($"{pos + li:X8}  ", width);
                        break;

                    case OffsetView.Dec:
                        line = new StringBuilder($"{pos + li:D8}  ", width);
                        break;

                    case OffsetView.Oct:
                        line = new StringBuilder($"{_0xdd.ToOct(pos + li)}  ", width);
                        break;
                }

                //TODO: If (pos + BytesPerRow) instead

                ascii = new StringBuilder(_0xdd.BytesPerRow);
                // d = data (hex) index
                for (int i = 0; i < _0xdd.BytesPerRow; ++i, ++d)
                {
                    if (pos + d < len)
                    {
                        line.Append($"{_0xdd.DisplayBuffer[d]:X2} ");
                        ascii.Append(_0xdd.DisplayBuffer[d].ToAscii());
                    }
                    else
                    {
                        Console.Write(line.ToString());
                        Console.SetCursorPosition(Console.WindowWidth - _0xdd.BytesPerRow - 5, Console.CursorTop);
                        Console.Write(ascii.ToString());
                        return;
                    }
                }

                Console.Write(line.ToString());
                Console.Write(' '); // over 0xFFFFFFFF padding
                Console.Write(ascii.ToString());
                Console.WriteLine(' ');
            }
        }
    }
}
