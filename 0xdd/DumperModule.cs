using System;
using System.IO;
using System.Text;

namespace _0xdd
{
    static class Dumper
    {
        /// <summary>
        /// Extension of data dump files.
        /// </summary>
        public const string EXTENSION = "hexdmp";

        /// <summary>
        /// File to dump as text data.
        /// </summary>
        /// <param name="filepath">Output filename.</param>
        /// <param name="bytesInRow">Number of bytes in a row.</param>
        /// <param name="offsetView"><see cref="OffsetView"/> to use.</param>
        /// <returns><see cref="ErrorCode"/></returns>
        public static ErrorCode Dump(string filepath,
            int bytesInRow = 16, OffsetView offsetView = OffsetView.Hex)
        {
            FileInfo f = new FileInfo(filepath);

            if (!f.Exists)
                return ErrorCode.FileNotFound;

            using (FileStream inStream = f.OpenRead())
            using (StreamWriter outStream = new StreamWriter($"{filepath}.{EXTENSION}"))
            {
                if (!outStream.BaseStream.CanWrite)
                    return ErrorCode.DumberCannotWrite;

                outStream.AutoFlush = true;

                outStream.WriteLine(f.Name);
                outStream.WriteLine();
                outStream.WriteLine($"Size: {Utils.FormatSize(f.Length)}");
                outStream.WriteLine($"Attributes: {Utils.GetEntryInfo(f)}");
                outStream.WriteLine($"File date: {f.CreationTime}");
                outStream.WriteLine($"Dump date: {DateTime.Now}");
                outStream.WriteLine();

                outStream.Write($"Offset {offsetView.GetChar()}  ");
                for (int i = 0; i < bytesInRow; i++)
                {
                    outStream.Write($"{i:X2} ");
                }
                outStream.WriteLine();

                return DumpFile(inStream, outStream, bytesInRow, offsetView);
            }
        }

        /// <remarks>
        /// Only to be used with <see cref="Dump()"/>!
        /// </remarks>
        static ErrorCode DumpFile(FileStream inStream, StreamWriter outStream,
            int bytesInRow = 16, OffsetView offsetView = OffsetView.Hex)
        {
            //TODO: Update DumpFile

            if (!inStream.CanRead)
                return ErrorCode.DumberCannotRead;

            if (!outStream.BaseStream.CanWrite)
                return ErrorCode.DumberCannotWrite;

            long offset = 0;

            byte[] buffer = new byte[bytesInRow];
            long len = bytesInRow;

            bool working = true;

            StringBuilder line, ascii;
            while (working)
            {
                switch (offsetView)
                {
                    default:
                        line = new StringBuilder($"{offset:X8}  ");
                        break;

                    case OffsetView.Dec:
                        line = new StringBuilder($"{offset:D8}  ");
                        break;

                    case OffsetView.Oct:
                        line = new StringBuilder($"{_0xdd.ToOct(offset, 8)}  ");
                        break;
                }

                offset += bytesInRow;

                ascii = new StringBuilder(bytesInRow);

                int read = inStream.Read(buffer, 0, bytesInRow);
                if (read == buffer.Length)
                {
                    for (int i = 0; i < bytesInRow; ++i)
                    {
                        line.Append($"{buffer[i]:X2} ");
                        ascii.Append(buffer[i].ToAscii());
                    }
                }
                else
                {
                    for (int i = 0; i < read; ++i)
                    {
                        line.Append($"{buffer[i]:X2} ");
                        ascii.Append(buffer[i].ToAscii());
                    }

                    outStream.Write(line.ToString());
                    outStream.Write(new string(' ', 
                        ((bytesInRow * 3) - (read * 3)) + 1
                    ));
                    outStream.WriteLine(ascii.ToString());

                    return ErrorCode.Success;
                }

                outStream.Write(line.ToString());
                outStream.Write(' ');
                outStream.WriteLine(ascii.ToString());
            }

            return ErrorCode.Success;
        }
    }
}
