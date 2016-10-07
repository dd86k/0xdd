using System;
using System.Text;

namespace _0xdd
{
    /// <summary>
    /// Shows offset base view and the offset on each byte.
    /// </summary>
    public static class OffsetPanel
    {
        const int TopPosition = 1;

        /// <summary>
        /// Update the offset map
        /// </summary>
        static internal void Update()
        {
            StringBuilder t = new StringBuilder($"Offset {_0xdd.OffsetView.GetChar()}  ");

            if (_0xdd.Stream.Position > uint.MaxValue)
                t.Append(' ');

            switch (_0xdd.OffsetView)
            {
                default:
                    for (int i = 0; i < _0xdd.BytesPerRow; ++i)
                        t.Append($"{i:X2} ");
                    break;

                case OffsetView.Dec:
                    for (int i = 0; i < _0xdd.BytesPerRow; ++i)
                        t.Append($"{i:00} ");
                    break;

                case OffsetView.Oct:
                    for (int i = 0; i < _0xdd.BytesPerRow; ++i)
                        t.Append($"{i.ToOct(2)} ");
                    break;
            }
            /*
            if (LastWindowHeight != Console.WindowHeight || LastWindowWidth != Console.WindowWidth)
                t.Append(new string(' ', Console.WindowWidth - t.Length - 1)); // Force clean
            */
            Console.SetCursorPosition(0, TopPosition);
            Console.Write(t.ToString());
        }
    }
}
