using System;
using System.IO;

namespace _0xdd
{
    static class Utils
    {
        const long SIZE_TB = SIZE_GB * 1024,
                   SIZE_GB = SIZE_MB * 1024,
                   SIZE_MB = SIZE_KB * 1024,
                   SIZE_KB = 1024;

        public static string FormatSize(long size)
        {
            return FormatSizeDecimal(size);
        }

        public static string FormatSizeDecimal(decimal size)
        {
            if (size > SIZE_TB)
                return $"{Math.Round(size / SIZE_TB, 2)} TB";
            else if (size > SIZE_GB)
                return $"{Math.Round(size / SIZE_GB, 2)} GB";
            else if (size > SIZE_MB)
                return $"{Math.Round(size / SIZE_MB, 2)} MB";
            else if (size > SIZE_KB)
                return $"{Math.Round(size / SIZE_KB, 2)} KB";
            else
                return $"{size} B";
        }

        /// <summary>
        /// Gets file info and owner from <see cref="FileInfo"/>
        /// </summary>
        /// <param name="file">File.</param>
        /// <returns>Info as a string</returns>
        public static unsafe string GetEntryInfo(this FileInfo file)
        {
            char* o = stackalloc char[8];

            int n = (int)file.Attributes;
            *o   = (n & 0x20) != 0 ?   'a' : '-'; // Archive
            *++o = (n & 0x800) != 0 ?  'c' : '-'; // Compressed
            *++o = (n & 0x4000) != 0 ? 'e' : '-'; // Encrypted
            *++o = (n & 1) != 0 ?      'r' : '-'; // Read only
            *++o = (n & 4) != 0 ?      's' : '-'; // System
            *++o = (n & 2) != 0 ?      'h' : '-'; // Hidden
            *++o = (n & 0x100) != 0 ?  't' : '-'; // Temporary
            *++o = '\0';
            
            // Pointer moved 7 times.
            return new string(o - 7);
        }

        /// <summary>
        /// Centers text in padding, guarantees provided length.
        /// </summary>
        /// <param name="text">Text to center.</param>
        /// <param name="width">Length of the new string.</param>
        /// <returns>Padded string.</returns>
        public static unsafe string Center(this string text, int width)
        {
            int l = text.Length > width ? width : text.Length;
            int s = (width / 2) - (l / 2);

            char* t = stackalloc char[width + 1];
            fixed (char* tptext = text)
            {
                char* ptext = tptext, max = tptext + l, // In
                      pt = t, smax = t + width, m = t + s; // Out

                while (pt < m)
                    *pt++ = ' ';

                while (*ptext != '\0' && ptext < max)
                    *pt++ = *ptext++;

                while (pt < smax)
                    *pt++ = ' ';

                *pt = '\0';
            }

            return new string(t);
        }

        public static ConsoleColor Invert(this ConsoleColor cc) =>
            (ConsoleColor)(~(int)cc & 0xF);

        public static int GetBytesInRow()
        {
            return ((Console.WindowWidth - 10) / 4) - 1;
        }
    }
}