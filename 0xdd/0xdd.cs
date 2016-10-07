using System;
using System.IO;

//TODO: Hashing (with menu) (v1.1)
//TODO: Search from end of file (v0.7)
//TODO: Settings! (v0.8)

/*
    TODO: Edit mode (v0.9)
    TODO: Replace Action (v0.9)
    - Top right: Insert(INS)/Overwrite(OVR)
    - Cursor/Navigation mode?
    - Edit:
      - Dictionary<long, byte> Edits (FilePosition, Data)
      - Rendering: If byte at position, write that byte to display instead
      - Saving: Remove duplicates, loop through List and write (at positions)
      - Editing: If new data on same position, replace in Edits (overwrite)

0E 0F
D2 DD ..
^^    ^
highlighted (black on gray) while navigating
*/

namespace _0xdd
{
    #region Enumerations
    enum ErrorCode : byte
    {
        Success = 0,

        // File
        FileNotFound = 0x4,
        FileUnreadable = 0x5,
        FileAlreadyOpen = 0x6,
        FileUnauthorized = 0x7,

        // Position
        PositionOutOfBound = 0x10,

        // Dump
        DumberCannotWrite = 0x18,
        DumberCannotRead = 0x19,

        // Find
        FinderNoResult = 0x20,
        FinderEmptyString = 0x21,

        // Program
        ProgramNoParse = 0xA0,

        // CLI
        CLI_InvalidOffsetView = 0xC0,
        CLI_InvalidWidth = 0xC4,

        // Misc.
        OSNotSupported = 0xD0,
        NotImplemented = 0xD1,
        
        UnknownError = 0xFE,
    }

    enum OffsetView : byte
    {
        Hex, Dec, Oct
    }
    
    enum OperatingMode : byte
    {
        // READ, OVRW, INSR
        Read, Overwrite, Insert
    }
    #endregion

    #region Structs
    /// <summary>
    /// Result struch of a search.
    /// </summary>
    struct FindResult
    {
        public FindResult(long pPosition)
        {
            Position = pPosition;
            Error = ErrorCode.Success;
        }
        
        public FindResult(ErrorCode pError)
        {
            Error = pError;
            Position = -1;
        }

        public long Position;
        public ErrorCode Error;
    }
    #endregion

    static class _0xdd
    {
        #region Properties
        public static FileInfo File { get; private set; }
        public static FileStream Stream { get; private set; }
        public static ErrorCode LastError { get; private set; }
        public static byte[] DisplayBuffer { get; private set; }

        static int LastWindowHeight;
        static int LastWindowWidth;

        //TODO: re-add autoadjust + fixed width
        public static bool AutoAdjust { get; set; }
        public static OffsetView OffsetView { get; set; }
        public static int BytesPerRow { get; set; }
        
        static bool inApp = true;

        //public static byte BytePerGroup = 1;
        #endregion

        #region Methods        
        public static ErrorCode OpenFile(string path, OffsetView view = OffsetView.Hex, int bytesPerRow = 0)
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

            OffsetView = view;
            LastWindowHeight = Console.WindowHeight;
            LastWindowWidth = Console.WindowWidth;

            AutoAdjust = bytesPerRow <= 0;
            BytesPerRow = AutoAdjust ? Utils.GetBytesInRow() : bytesPerRow;

            try
            { // Mono can have some issues with these.
                Console.CursorVisible = false;
                Console.Title = File.Name;
            } catch { }

            Console.Clear();
            PrepareScreen();

            while (inApp)
                ReadUserKey();

            return LastError;
        }

        /// <summary>
        /// Read user input.
        /// </summary>
        static void ReadUserKey()
        {
            ConsoleKeyInfo k = Console.ReadKey(true);
            
            //if (AutoAdjust)
            if (LastWindowHeight != Console.WindowHeight ||
                LastWindowWidth != Console.WindowWidth)
            {
                BytesPerRow = Utils.GetBytesInRow();

                Console.Clear();
                PrepareScreen();

                LastWindowHeight = Console.WindowHeight;
                LastWindowWidth = Console.WindowWidth;
            }
            
            switch (k.Key)
            {
                case ConsoleKey.F5:
                    ReadFileAndUpdate(Stream.Position);
                    break;

                case ConsoleKey.Escape:
                    MenuBarPanel.Enter();
                    break;

                // Find byte
                case ConsoleKey.W:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        WindowSystem.PromptFindByte();
                    }
                    break;

                // Find data
                case ConsoleKey.J:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        WindowSystem.PromptSearchString();
                    }
                    break;

                // Goto
                case ConsoleKey.G:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        WindowSystem.PromptGoto();
                    }
                    break;

                // Offset base
                case ConsoleKey.O:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        WindowSystem.PromptOffset();
                    }
                    break;

                // Edit mode
                case ConsoleKey.E:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message("Not implemented. Sorry!");
                    }
                    break;

                // Replace
                case ConsoleKey.H:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message("Not implemented. Sorry!");
                    }
                    break;

                // Info
                case ConsoleKey.I:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message(
                            $"{File.Name} {Utils.GetEntryInfo(File)} {Utils.FormatSize(File.Length)}"
                        );
                    }
                    break;

                // Exit
                case ConsoleKey.X:
                    if (k.Modifiers == ConsoleModifiers.Control)
                        Exit();
                    break;

                // Dump
                case ConsoleKey.D:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message("Dumping...");
                        Dumper.Dump(File.FullName, BytesPerRow, OffsetView);
                        InfoPanel.Message("Dumping done!");
                    }
                    break;

                /*
                 * Data navigation
                 */

                case ConsoleKey.LeftArrow:
                    if (DisplayBuffer.Length < File.Length)
                        if (k.Modifiers == ConsoleModifiers.Control)
                        {
                            ReadFileAndUpdate(Stream.Position -
                                (Stream.Position % BytesPerRow));
                        }
                        else if (Stream.Position - 1 >= 0)
                        {
                            ReadFileAndUpdate(Stream.Position - 1);
                        }
                    break;
                case ConsoleKey.RightArrow:
                    if (DisplayBuffer.Length < File.Length)
                        if (k.Modifiers == ConsoleModifiers.Control)
                        {
                            long NewPos = Stream.Position +
                                (BytesPerRow - Stream.Position % BytesPerRow);

                            if (NewPos + DisplayBuffer.Length <= File.Length)
                                ReadFileAndUpdate(NewPos);
                            else
                                ReadFileAndUpdate(File.Length - DisplayBuffer.Length);
                        }
                        else if (Stream.Position + DisplayBuffer.Length + 1 <= File.Length)
                        {
                            ReadFileAndUpdate(Stream.Position + 1);
                        }
                    break;

                case ConsoleKey.UpArrow:
                    if (DisplayBuffer.Length < File.Length)
                        if (Stream.Position - BytesPerRow >= 0)
                        {
                            ReadFileAndUpdate(Stream.Position - BytesPerRow);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    break;
                case ConsoleKey.DownArrow:
                    if (DisplayBuffer.Length < File.Length)
                        if (Stream.Position + DisplayBuffer.Length + BytesPerRow <= File.Length)
                        {
                            ReadFileAndUpdate(Stream.Position + BytesPerRow);
                        }
                        else
                        {
                            ReadFileAndUpdate(File.Length - DisplayBuffer.Length);
                        }
                    break;

                case ConsoleKey.PageUp:
                    if (DisplayBuffer.Length < File.Length)
                        if (Stream.Position - DisplayBuffer.Length >= 0)
                        {
                            ReadFileAndUpdate(Stream.Position - DisplayBuffer.Length);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    break;
                case ConsoleKey.PageDown:
                    if (DisplayBuffer.Length < File.Length)
                        if (Stream.Position + (DisplayBuffer.Length * 2) <= File.Length)
                        {
                            ReadFileAndUpdate(Stream.Position += DisplayBuffer.Length);
                        }
                        else
                        {
                            ReadFileAndUpdate(File.Length - DisplayBuffer.Length);
                        }
                    break;

                case ConsoleKey.Home:
                    if (DisplayBuffer.Length < File.Length)
                        ReadFileAndUpdate(0);
                    break;
                case ConsoleKey.End:
                    if (DisplayBuffer.Length < File.Length)
                        ReadFileAndUpdate(File.Length - DisplayBuffer.Length);
                    break;
            }
        }

        /// <summary>
        /// Prepares the screen with the information needed.<para/>
        /// Initialize: AutoSizing, Buffer, TitlePanel, Readfile, MainPanel,
        /// InfoPanel, and ControlPanel.
        /// </summary>
        /// <remarks>Also used when resizing.</remarks>
        static void PrepareScreen()
        {
            if (BytesPerRow <= 0)
                BytesPerRow = Utils.GetBytesInRow();

            int bos = (Console.WindowHeight - 3) * BytesPerRow;

            DisplayBuffer = new byte[
                    File.Length < bos ?
                    File.Length : bos
                ];

            MenuBarPanel.Initialize();

            if (File.Length > 0)
            ReadFileAndUpdate(Stream.Position);
        }
        
        #region Read file
        /// <summary>
        /// Read file, update MainPanel, then update InfoPanel.
        /// </summary>
        /// <param name="position">New position.</param>
        static void ReadFileAndUpdate(long position)
        {
            ReadCurrentFile(position);
            MainPanel.Update();
            InfoPanel.Update();
        }

        /// <summary>
        /// Read the current file at a position.
        /// </summary>
        /// <param name="position">Position.</param>
        static void ReadCurrentFile(long position)
        {
            Stream.Position = position;
            Stream.Read(DisplayBuffer, 0, DisplayBuffer.Length);
            Stream.Position = position;
        }
        #endregion

        #region Goto
        /// <summary>
        /// Go to a specific position in the file.
        /// </summary>
        /// <param name="pPosition">Position</param>
        public static void Goto(long pPosition)
        {
            ReadFileAndUpdate(pPosition);
        }
        #endregion

        #region Exit
        /// <summary>
        /// When the user exits the program.
        /// </summary>
        /// <returns>Always <see cref="false"/>.</returns>
        /// <remarks>
        /// Returns false to return due to the while loop.
        /// </remarks>
        public static void Exit()
        {
            //Console.Clear();

            Console.SetCursorPosition(
                Console.WindowWidth - 1,
                Console.WindowHeight - 1
            );
            Console.CursorVisible = true;

            inApp = false;
        }
        #endregion
        #endregion

        #region Type extensions
        /// <summary>
        /// Converts into an octal number with 0 padding.
        /// </summary>
        /// <param name="l">Number.</param>
        /// <returns>String.</returns>
        public static string ToOct(this long l) => Convert.ToString(l, 8).PadLeft(8, '0');

        /// <summary>
        /// Converts into an octal number with 0 padding.
        /// </summary>
        /// <param name="l">Number.</param>
        /// <returns>String.</returns>
        public static string ToOct(this int l, int width) =>
            Convert.ToString(l, 8).PadLeft(width, '0');

        /// <summary>
        /// Returns a printable character if found.<para/>
        /// Between 0x20 (space) to 0x7E (~)
        /// </summary>
        /// <param name="b">Byte to transform.</param>
        /// <returns>ASCII character.</returns>
        public static char ToAscii(this byte b) => b < 0x20 || b > 0x7E ? '.' : (char)b;

        /// <summary>
        /// Gets the character for the upper bar depending on the
        /// offset base view.
        /// </summary>
        /// <param name="pView">This <see cref="global::_0xdd.OffsetView"/></param>
        /// <returns>Character.</returns>
        public static char GetChar(this OffsetView pView)
        {
            switch (pView)
            {
                case OffsetView.Hex:
                    return 'h';
                case OffsetView.Dec:
                    return 'd';
                case OffsetView.Oct:
                    return 'o';
                default:
                    return '?'; // ??????????
            }
        }

        public static int Code(this ErrorCode pCode) => (int)pCode;
        #endregion
    }
}