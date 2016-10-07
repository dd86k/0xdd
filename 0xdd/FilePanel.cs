using System;
using System.IO;
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

        public static FileInfo File { get; private set; }
        public static FileStream Stream { get; private set; }
        public static byte[] DisplayBuffer { get; private set; }

        public static int BufferSize => DisplayBuffer.Length;
        public static long FileSize => File.Length;
        public static long CurrentPosition => Stream.Position;

        /// <summary>
        /// Open the file from the path.
        /// </summary>
        /// <param name="path">Filepath.</param>
        /// <returns>Error code.</returns>
        public static ErrorCode Open(string path)
        {
            File = new FileInfo(path);

            if (!File.Exists)
                return ErrorCode.FileNotFound;

            try
            {
                Stream = File.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (UnauthorizedAccessException)
            {
                return ErrorCode.FileUnauthorized;
            }
            catch (IOException)
            {
                return ErrorCode.FileAlreadyOpen;
            }

            if (File.Length == 0)
                return ErrorCode.FileZero;

            return ErrorCode.Success;
        }
        
        /// <summary>
        /// Prepares the buffer.
        /// </summary>
        public static void Initialize()
        {
            // Bytes On Screen
            int bos = (Console.WindowHeight - 3) * _0xdd.BytesPerRow;

            DisplayBuffer = new byte[
                    FileSize < bos ? FileSize : bos
                ];

            MenuBarPanel.Initialize();
            
            Read(CurrentPosition);
            Update();
        }

        /// <summary>
        /// Read the current file at a position.
        /// </summary>
        /// <param name="position">Position.</param>
        public static void Read(long position)
        {
            Stream.Position = position;
            Stream.Read(DisplayBuffer, 0, DisplayBuffer.Length);
            Stream.Position = position;
        }

        public static void Refresh()
        {
            Read(Stream.Position);
        }

        /// <summary>
        /// Update the data onscreen from <see cref="DisplayBuffer"/>.
        /// </summary>
        public static void Update()
        {
            int width = Console.WindowWidth - 1;

            long len = File.Length; // File size
            long pos = Stream.Position; // File position

            OffsetPanel.Update();

            //TODO: Check if we can do a little pointer-play with the buffer.

            int d = 0;
            StringBuilder line, ascii;

            Console.SetCursorPosition(0, StartPosition);
            for (int li = 0; li < DisplayBuffer.Length; li += _0xdd.BytesPerRow) // LineIndex
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

                //TODO: If (pos + BytesPerRow) instead of the inner if

                ascii = new StringBuilder(_0xdd.BytesPerRow);
                // d = data (hex) index
                for (int i = 0; i < _0xdd.BytesPerRow; ++i, ++d)
                {
                    if (pos + d < len)
                    {
                        line.Append($"{DisplayBuffer[d]:X2} ");
                        ascii.Append(DisplayBuffer[d].ToAscii());
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
