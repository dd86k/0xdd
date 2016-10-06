using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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
        public static FileInfo CurrentFile { get; private set; }
        public static FileStream CurrentFileStream { get; private set; }

        public static OffsetView CurrentOffsetView { get; private set; }
        public static ErrorCode LastError { get; private set; }

        public static byte[] DisplayBuffer { get; private set; }

        static int LastWindowHeight;
        static int LastWindowWidth;

        #region Settings
        //TODO: re-add autoadjust + fixed width
        public static bool AutoAdjust;
        public static int BytesPerRow;

        static byte _lastByte;
        private static string _lastData;

        //public static byte BytePerGroup = 1;
        #endregion
        #endregion

        #region Methods        
        public static ErrorCode OpenFile(string path, OffsetView view = OffsetView.Hex, int bytesPerRow = 0)
        {
            CurrentFile = new FileInfo(path);

            if (!CurrentFile.Exists)
                return ErrorCode.FileNotFound;

            try
            {
                CurrentFileStream = CurrentFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (UnauthorizedAccessException)
            {
                return ErrorCode.FileUnauthorized;
            }
            catch (IOException)
            {
                return ErrorCode.FileAlreadyOpen;
            }

            CurrentOffsetView = view;
            LastWindowHeight = Console.WindowHeight;
            LastWindowWidth = Console.WindowWidth;

            AutoAdjust = bytesPerRow <= 0;
            BytesPerRow = AutoAdjust ? Utils.GetBytesInRow() : bytesPerRow;

            try
            {
                Console.CursorVisible = false;
                Console.Title = CurrentFile.Name;
            } catch { }
            Console.Clear();

            PrepareScreen();

            while (ReadUserKey());

            return LastError;
        }

        /// <summary>
        /// Read user input.
        /// </summary>
        static bool ReadUserKey()
        { //TODO: Recude cyclomatic complexity to under 25
          // - Place most of the code into new functions
            ConsoleKeyInfo k = Console.ReadKey(true);
            
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
                    ReadFileAndUpdate(CurrentFileStream.Position);
                    break;

                case ConsoleKey.Escape:
                    MenuBarPanel.Enter();
                    break;

                // Find byte
                case ConsoleKey.W:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFile.Length - MainPanel.BytesOnScreen)
                        {
                            InfoPanel.Message("Already at the end of the file.");
                            break;
                        }

                        long t = Utils.GetNumberFromUser("Find byte:",
                            suggestion: _lastByte.ToString("X2"));

                        if (t == -1)
                        {
                            MainPanel.Update();
                            InfoPanel.Message("Canceled.");
                            break;
                        }
                        
                        if (t < 0 || t > byte.MaxValue)
                        {
                            MainPanel.Update();
                            InfoPanel.Message("A value between 0 and 255 is required.");
                        }
                        else
                        {
                            MainPanel.Update();
                            InfoPanel.Message("Searching...");
                            long p = Finder.FindByte(
                                _lastByte = (byte)t, CurrentFileStream,
                                CurrentFile, CurrentFileStream.Position + 1
                            );

                            if (p > 0)
                            {
                                Goto(--p);
                                if (p > uint.MaxValue)
                                    InfoPanel.Message($"Found {t:X2} at {p:X16}");
                                else
                                    InfoPanel.Message($"Found {t:X2} at {p:X8}");
                            }
                            else
                            {
                                switch (p)
                                {
                                    case -1:
                                        InfoPanel.Message($"No results.");
                                        break;
                                    case -2:
                                        InfoPanel.Message($"Position out of bound.");
                                        break;
                                    case -3:
                                        InfoPanel.Message($"File not found!");
                                        break;

                                    default:
                                        InfoPanel.Message($"Unknown error occurred. (0x{p:X2})");
                                        break;
                                }
                            }
                        }
                    }
                    break;

                // Find data
                case ConsoleKey.J:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFile.Length - MainPanel.BytesOnScreen)
                        {
                            InfoPanel.Message("Already at the end of the file.");
                            break;
                        }

                        if (MainPanel.BytesOnScreen >= CurrentFile.Length)
                        {
                            InfoPanel.Message("Not possible.");
                            break;
                        }

                        _lastData = Utils.GetUserInput("Find data:", suggestion: _lastData);

                        if (_lastData == null || _lastData.Length == 0)
                        {
                            MainPanel.Update();
                            InfoPanel.Message("Canceled.");
                            break;
                        }
                        
                        MainPanel.Update();
                        InfoPanel.Message("Searching...");
                        Finder.FindString(_lastData, CurrentFileStream, CurrentFile, CurrentFileStream.Position + 1);
                    }
                    break;

                // Goto
                case ConsoleKey.G:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (MainPanel.BytesOnScreen >= CurrentFile.Length)
                        {
                            InfoPanel.Message("Not possible.");
                            break;
                        }

                        if (CurrentFileStream.Position >= CurrentFile.Length)
                        {
                            InfoPanel.Message("Already at the end of the file.");
                            break;
                        }

                        long t = Utils.GetNumberFromUser("Goto:");

                        if (t == -1)
                        {
                            MainPanel.Update();
                            InfoPanel.Message("Canceled.");
                            break;
                        }

                        if (t >= 0 && t <= CurrentFile.Length - MainPanel.BytesOnScreen)
                        {
                            Goto(t);
                        }
                        else
                        {
                            MainPanel.Update();
                            InfoPanel.Message("Position out of bound!");
                        }
                    }
                    break;

                // Offset base
                case ConsoleKey.O:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        string c = Utils.GetUserInput("Hex, Dec, Oct?");

                        if (c == null || c.Length < 1)
                        {
                            InfoPanel.Message("Canceled.");
                            MainPanel.Update();
                            break;
                        }

                        switch (c[0])
                        {
                            case 'H': case 'h':
                                CurrentOffsetView = OffsetView.Hex;
                                OffsetPanel.Update();
                                InfoPanel.Update();
                                break;

                            case 'O': case 'o':
                                CurrentOffsetView = OffsetView.Oct;
                                OffsetPanel.Update();
                                InfoPanel.Update();
                                break;

                            case 'D': case 'd':
                                CurrentOffsetView = OffsetView.Dec;
                                OffsetPanel.Update();
                                InfoPanel.Update();
                                break;

                            default:
                                InfoPanel.Message("Invalid view mode!");
                                break;
                        }

                        MainPanel.Update();
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
                        InfoPanel.Message($"{Utils.GetEntryInfo(CurrentFile)} {Utils.FormatSize(CurrentFile.Length)}");
                    }
                    break;

                // Exit
                case ConsoleKey.X:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        LastError = ErrorCode.Success;
                        return Exit();
                    }
                    break;

                // Dump
                case ConsoleKey.D:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message("Dumping...");
                        Dumper.Dump(CurrentFile.FullName,
                            BytesPerRow, CurrentOffsetView);
                        InfoPanel.Message("Dumping done!");
                    }
                    break;

                // -- Data nagivation --
                case ConsoleKey.LeftArrow:
                    if (MainPanel.BytesOnScreen < CurrentFile.Length)
                        if (k.Modifiers == ConsoleModifiers.Control)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position -
                                (CurrentFileStream.Position % BytesPerRow));
                        }
                        else if (CurrentFileStream.Position - 1 >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - 1);
                        }
                    break;
                case ConsoleKey.RightArrow:
                    if (MainPanel.BytesOnScreen < CurrentFile.Length)
                        if (k.Modifiers == ConsoleModifiers.Control)
                        {
                            long NewPos = CurrentFileStream.Position +
                                (BytesPerRow - CurrentFileStream.Position % BytesPerRow);

                            if (NewPos + MainPanel.BytesOnScreen <= CurrentFile.Length)
                                ReadFileAndUpdate(NewPos);
                            else
                                ReadFileAndUpdate(CurrentFile.Length - MainPanel.BytesOnScreen);
                        }
                        else if (CurrentFileStream.Position + MainPanel.BytesOnScreen + 1 <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position + 1);
                        }
                    break;

                case ConsoleKey.UpArrow:
                    if (MainPanel.BytesOnScreen < CurrentFile.Length)
                        if (CurrentFileStream.Position - BytesPerRow >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - BytesPerRow);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    break;
                case ConsoleKey.DownArrow:
                    if (MainPanel.BytesOnScreen < CurrentFile.Length)
                        if (CurrentFileStream.Position + MainPanel.BytesOnScreen + BytesPerRow <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position + BytesPerRow);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFile.Length - MainPanel.BytesOnScreen);
                        }
                    break;

                case ConsoleKey.PageUp:
                    if (MainPanel.BytesOnScreen < CurrentFile.Length)
                        if (CurrentFileStream.Position - MainPanel.BytesOnScreen >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - MainPanel.BytesOnScreen);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    break;
                case ConsoleKey.PageDown:
                    if (MainPanel.BytesOnScreen < CurrentFile.Length)
                        if (CurrentFileStream.Position + (MainPanel.BytesOnScreen * 2) <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position += MainPanel.BytesOnScreen);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFile.Length - MainPanel.BytesOnScreen);
                        }
                    break;

                case ConsoleKey.Home:
                    if (MainPanel.BytesOnScreen < CurrentFile.Length)
                        ReadFileAndUpdate(0);
                    break;
                case ConsoleKey.End:
                    if (MainPanel.BytesOnScreen < CurrentFile.Length)
                        ReadFileAndUpdate(CurrentFile.Length - MainPanel.BytesOnScreen);
                    break;
            }

            return true;
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

            DisplayBuffer = new byte[
                    CurrentFile.Length < MainPanel.BytesOnScreen ?
                    CurrentFile.Length : MainPanel.BytesOnScreen
                ];

            MenuBarPanel.Initialize();

            if (CurrentFile.Length > 0)
            ReadFileAndUpdate(CurrentFileStream.Position);
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
            CurrentFileStream.Position = position;
            CurrentFileStream.Read(DisplayBuffer, 0, DisplayBuffer.Length);
            CurrentFileStream.Position = position;
        }
        #endregion

        #region Goto
        /// <summary>
        /// Go to a specific position in the file.
        /// </summary>
        /// <param name="pPosition">Position</param>
        static void Goto(long pPosition)
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
        static bool Exit()
        {
            //Console.Clear();

            Console.SetCursorPosition(
                Console.WindowWidth - 1,
                Console.WindowHeight - 1
            );
            Console.CursorVisible = true;

            return false;
        }
        #endregion
        #endregion

        #region Type extensions
        /// <summary>
        /// Converts into an octal number.
        /// </summary>
        /// <param name="l">Number.</param>
        /// <returns>String.</returns>
        public static string ToOct(this long l) => Convert.ToString(l, 8).PadLeft(8, '0');

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
        /// <param name="pView">This <see cref="OffsetView"/></param>
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