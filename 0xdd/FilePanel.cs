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
        public static FileStream FileIO { get; private set; }
        public static byte[] DisplayBuffer { get; private set; }

        public static int BufferSize => DisplayBuffer.Length;
        public static long FileSize => File.Length;
        public static long CurrentPosition => FileIO.Position;

        /// <summary>
        /// Open the file from the path.
        /// </summary>
        /// <param name="path">Filepath.</param>
        /// <returns>Error code.</returns>
        public static ErrorCode Open(string path)
        {
            try
            {
                FileIO =
                    (File = new FileInfo(path))
                    .Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException)
            {
                return ErrorCode.FileNotFound;
            }
            catch (UnauthorizedAccessException)
            {
                return ErrorCode.FileUnauthorized;
            }
            catch
            {
                return ErrorCode.UnknownError;
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
            int bos = (Console.WindowHeight - 3) * MainApp.BytesPerRow;

            DisplayBuffer = new byte[FileSize < bos ? FileSize : bos];
            
            Read(CurrentPosition);
            Update();
        }

        /// <summary>
        /// Read the current file at a position.
        /// </summary>
        /// <param name="position">Position.</param>
        public static void Read(long position)
        {
            FileIO.Position = position;
            FileIO.Read(DisplayBuffer, 0, DisplayBuffer.Length);
            FileIO.Position = position;
        }

        public static void Refresh()
        {
            Read(FileIO.Position);
        }

        /// <summary>
        /// Update the data onscreen from <see cref="DisplayBuffer"/>.
        /// </summary>
        public static void Update()
        {
            int width = Console.WindowWidth - 1;

            long len = File.Length, pos = FileIO.Position;

            //TODO: Check if we can do a little pointer-play with the buffer.

            int d = 0, bpr = MainApp.BytesPerRow;

            StringBuilder
                line = new StringBuilder(width),
                ascii = new StringBuilder(bpr);

            OffsetPanel.Update();

            Console.SetCursorPosition(0, StartPosition);
            for (int i = 0; i < DisplayBuffer.Length; i += bpr)
            {
                line.Clear();
                switch (MainApp.OffsetView)
                {
                    default:
                        line.Append($"{pos + i:X8}  ");
                        break;

                    case OffsetView.Dec:
                        line.Append($"{pos + i:D8}  ");
                        break;

                    case OffsetView.Oct:
                        line.Append($"{MainApp.ToOct(pos + i, 8)}  ");
                        break;
                }

                ascii.Clear();
                if (pos + i + bpr < len)
                {
                    for (int bi = 0; bi < bpr; ++bi, ++d)
                    {
                        line.Append($"{DisplayBuffer[d]:X2} ");
                        ascii.Append(DisplayBuffer[d].ToAscii());
                    }

                    Console.Write(line.ToString());
                    Console.Write(' '); // In case of "over FFFF_FFFFh" padding
                    Console.Write(ascii.ToString());
                    Console.WriteLine(' ');
                }
                else
                {
                    long h = len - (pos + i);

                    for (int bi = 0; bi < h; ++bi, ++d)
                    {
                        line.Append($"{DisplayBuffer[d]:X2} ");
                        ascii.Append(DisplayBuffer[d].ToAscii());
                    }

                    Console.Write(line.ToString());
                    Console.SetCursorPosition(Console.WindowWidth - bpr - 5, Console.CursorTop);
                    Console.Write(ascii.ToString());
                    return;
                }
            }
        }
    }
}
