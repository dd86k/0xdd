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

        public static void Initialize()
        {
            Console.SetCursorPosition(0, TopPosition);
            Console.Write($"Offset {Main0xddApp.OffsetView.GetChar()}  ");
        }

        public static void Draw()
        {
            Initialize();
            Update();
        }

        /// <summary>
        /// Update the offset map
        /// </summary>
        public static void Update()
        {
            StringBuilder t = new StringBuilder(Console.WindowWidth - 10);

            if (FilePanel.CurrentPosition > uint.MaxValue)
                t.Append(' ');

            switch (Main0xddApp.OffsetView)
            {
                default:
                    for (int i = 0; i < Main0xddApp.BytesPerRow; ++i)
                        t.Append($"{i:X2} ");
                    break;

                case OffsetView.Dec:
                    for (int i = 0; i < Main0xddApp.BytesPerRow; ++i)
                        t.Append($"{i:D2} ");
                    break;

                case OffsetView.Oct:
                    for (int i = 0; i < Main0xddApp.BytesPerRow; ++i)
                        t.Append($"{i.ToOct(2)} ");
                    break;
            }

            Console.SetCursorPosition(10, TopPosition);
            Console.Write(t.ToString());
        }
    }
}
