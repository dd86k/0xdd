using System;
using System.IO;

namespace _0xdd
{
    static class Utils
    {
        const long SIZE_TB = 1099511627776;
        const long SIZE_GB = 1073741824;
        const long SIZE_MB = 1048576;
        const long SIZE_KB = 1024;

        const int DECIMALS = 3;

        public static string FormatSize(long size)
        {
            return FormatSizeDecimal(size);
        }

        public static string FormatSizeDecimal(decimal size)
        {
            if (size > SIZE_TB)
                return $"{Math.Round(size / SIZE_TB, DECIMALS)} TB";
            else if (size > SIZE_GB)
                return $"{Math.Round(size / SIZE_GB, DECIMALS)} GB";
            else if (size > SIZE_MB)
                return $"{Math.Round(size / SIZE_MB, DECIMALS)} MB";
            else if (size > SIZE_KB)
                return $"{Math.Round(size / SIZE_KB, DECIMALS)} KB";
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
            char* o = stackalloc char[9];
            *o = '-'; // Never a directory

            int n = (int)file.Attributes;
            *++o = (n & 0x20) > 0 ?   'a' : '-'; // Archive
            *++o = (n & 0x800) > 0 ?  'c' : '-'; // Compressed
            *++o = (n & 0x4000) > 0 ? 'e' : '-'; // Encrypted
            *++o = (n & 1) > 0 ?      'r' : '-'; // Read only
            *++o = (n & 4) > 0 ?      's' : '-'; // System
            *++o = (n & 2) > 0 ?      'h' : '-'; // Hidden
            *++o = (n & 0x100) > 0 ?  't' : '-'; // Temporary
            *++o = '\0'; // Safety measure
            
            // Pointer moved 8 times.
            return new string(o - 8);
        }

        /// <summary>
        /// Centers text in padding, guarantees provided length.
        /// </summary>
        /// <param name="text">Text to center.</param>
        /// <param name="width">Length of the new string.</param>
        /// <returns>Padded string.</returns>
        public static unsafe string Center(this string text, int width)
        {
            if (text.Length > width)
                text = text.Substring(0, width);

            int s = (width / 2) - (text.Length / 2);
            string t = new string(' ', width);
            fixed (char* pt = t)
                for (int i = 0; i < text.Length; i++)
                    pt[s + i] = text[i];

            return t;
        }

        internal static int GetBytesInRow()
        {
            return ((Console.WindowWidth - 10) / 4) - 1;
        }
    }
}