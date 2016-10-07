using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _0xdd
{
    /// <summary>
    /// Main panel which represents the offset, data as bytes,
    /// and data as ASCII characters.
    /// </summary>
    internal static class MainPanel
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
        /// Gets the heigth of the main panel.
        /// </summary>
        static internal int FrameHeight => Console.WindowHeight - 3;

        /// <summary>
        /// Gets the number of elements which can be shown in the main panel.
        /// </summary>
        static internal int BytesOnScreen => FrameHeight * _0xdd.BytesPerRow;

        /// <summary>
        /// Update from Buffer.
        /// </summary>
        static internal void Update()
        {
            int fh = FrameHeight;

            int width = Console.WindowWidth;

            long len = _0xdd.File.Length; // File size
            long pos = _0xdd.Stream.Position; // File position

            OffsetPanel.Update();

            //TODO: char[]* as buffer? Would it be too much trouble?

            int d = 0;
            Console.SetCursorPosition(0, StartPosition);
            StringBuilder line, ascii;
            for (int li = 0; li < fh; ++li) // LineIndex
            {
                switch (_0xdd.OffsetView)
                {
                    default:
                        line = new StringBuilder($"{(li * _0xdd.BytesPerRow) + pos:X8}  ", width);
                        break;

                    case OffsetView.Dec:
                        line = new StringBuilder($"{(li * _0xdd.BytesPerRow) + pos:D8}  ", width);
                        break;

                    case OffsetView.Oct:
                        line = new StringBuilder($"{_0xdd.ToOct((li * _0xdd.BytesPerRow) + pos)}  ", width);
                        break;
                }

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
