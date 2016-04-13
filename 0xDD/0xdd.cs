using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

//TODO: Hashing (with menu) (v0.8)
//TODO: Search from end of file (v0.7)

//TODO: Do advanced control input scheme (v0.7)
// UserInput (advanced) - Will be able to move to different controls for input
// construction: Control[] (struct with ControlType as enum)
// output: ControlResults[]

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

        // File related
        FileNotFound = 0x4,
        FileUnreadable = 0x5,

        // Position related
        PositionOutOfBound = 0x8,

        // Dump related
        DumbCannotWrite = 0x10,
        DumbCannotRead = 0x11,

        // Find related
        FindNoResult = 0x20,
        FindEmptyString = 0x21,

        // CLI related
        CLI_InvalidOffsetView = 0xC0,
        CLI_InvalidWidth = 0xC4,
        
        UnknownError = 0xFE,
        Exit = 0xFF
    }

    enum OffsetView : byte
    {
        Hexadecimal, Decimal, Octal
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

    struct UserResponse
    {
        public UserResponse(ErrorCode pError)
        {
            Error = pError;
        }

        public bool Success
        {
            get
            {
                return Error == ErrorCode.Success;
            }
        }

        public ErrorCode Error;
    }
    #endregion

    static class _0xdd
    {
        #region Constants
        /// <summary>
        /// Extension of data dump files.
        /// </summary>
        const string EXTENSION = "hexdmp";
        #endregion

        #region Variables
        static FileInfo CurrentFileInfo;
        static FileStream CurrentFileStream;
        static OffsetView CurrentOffsetView;
        //static OperatingMode CurrentWritingMode;
        
        static byte[] Buffer;
        
        static bool Fullscreen;
        
        static int LastWindowHeight;
        static int LastWindowWidth;
        static string LastDataSearched;
        static byte LastByteSearched;

        static bool AutoSize;
        #endregion

        #region Methods        
        internal static ErrorCode Open(string pFilePath, OffsetView pView = OffsetView.Hexadecimal, int pBytesRow = 0)
        {
            CurrentFileInfo = new FileInfo(pFilePath);

            if (!CurrentFileInfo.Exists)
                return ErrorCode.FileNotFound;
            
            try
            {
                CurrentFileStream = CurrentFileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch
            {
                return ErrorCode.FileUnreadable;
            }

            AutoSize = pBytesRow == 0;
            MainPanel.BytesInRow = AutoSize ? Utils.GetBytesInRow() : pBytesRow;

            CurrentOffsetView = pView;
            LastWindowHeight = Console.WindowHeight;
            LastWindowWidth = Console.WindowWidth;

            Console.CursorVisible = false;
            Console.Clear();

            PrepareScreen();

            UserResponse ur = new UserResponse(ErrorCode.Success);

            while(ur.Success)
            {
                ReadUserKey(ref ur);
            }

            return ur.Error;
        }

        /// <summary>
        /// Read user input.
        /// </summary>
        static void ReadUserKey(ref UserResponse pUserResponse)
        {
            ConsoleKeyInfo k = Console.ReadKey(true);
            
            if (AutoSize)
                if (LastWindowHeight != Console.WindowHeight ||
                    LastWindowWidth != Console.WindowWidth)
                {
                    Console.Clear();
                    PrepareScreen();

                    LastWindowHeight = Console.WindowHeight;
                    LastWindowWidth = Console.WindowWidth;
                }
            
            switch (k.Key)
            {
                // -- Hidden shortcuts --

                case ConsoleKey.F5:
                    {
                        MainPanel.Update();
                    }
                    return;

                case ConsoleKey.F10:
                    {
                        ToggleFullscreenMode();
                    }
                    return;

                // -- Shown shortcuts --

                // Find byte
                case ConsoleKey.W:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFileInfo.Length - MainPanel.BytesOnScreen)
                        {
                            Message("Already at the end of the file.");
                            return;
                        }

                        long? t = Utils.GetNumberFromUser("Find byte:",
                            pSuggestion: LastByteSearched > 0 ? LastByteSearched.ToString() : null);

                        if (t == null)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }
                        
                        if (t < 0 || t > byte.MaxValue)
                        {
                            MainPanel.Update();
                            Message("A value between 0 and 255 is required.");
                        }
                        else
                        {
                            LastByteSearched = (byte)t;
                            MainPanel.Update();
                            Message("Searching...");
                            FindResult p = Find((byte)t, CurrentFileStream.Position + 1);

                            switch (p.Error)
                            {
                                case ErrorCode.Success:
                                    {
                                        Goto(--p.Position);
                                        if (p.Position > uint.MaxValue)
                                            Message($"Found {t:X2} at {p.Position:X16}");
                                        else
                                            Message($"Found {t:X2} at {p.Position:X8}");
                                    }
                                    break;
                                case ErrorCode.FileNotFound:
                                    Message($"File not found!");
                                    break;
                                case ErrorCode.PositionOutOfBound:
                                    Message($"Position out of bound.");
                                    break;
                                case ErrorCode.FindNoResult:
                                    Message($"No results. Input: 0x{t:X2}");
                                    break;

                                default:
                                    Message($"Unknown error occurred.");
                                    break;
                            }
                        }
                    }
                    return;

                // Find data
                case ConsoleKey.J:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFileInfo.Length - MainPanel.BytesOnScreen)
                        {
                            Message("Already at the end of the file.");
                            return;
                        }

                        if (MainPanel.BytesOnScreen >= CurrentFileInfo.Length)
                        {
                            Message("Not possible.");
                            return;
                        }

                        string t = Utils.GetUserInput("Find data:", pSuggestion: LastDataSearched);

                        if (t == null || t.Length == 0)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }
                        
                        LastDataSearched = t;
                        MainPanel.Update();
                        Message("Searching...");
                        FindResult p = FindData(t, CurrentFileStream.Position + 1);
                        
                        switch (p.Error)
                        {
                            case ErrorCode.Success:
                                Goto(p.Position);
                                Message($"Found {t} at position {p.Position}");
                                break;

                            case ErrorCode.FileNotFound:
                                Message("File not found.");
                                MainPanel.Update();
                                break;
                            case ErrorCode.FileUnreadable:
                                Message("File unreadable.");
                                MainPanel.Update();
                                break;
                            case ErrorCode.FindNoResult:
                                Message("No results.");
                                MainPanel.Update();
                                break;
                            case ErrorCode.PositionOutOfBound:
                                Message("Position out of bound.");
                                MainPanel.Update();
                                break;

                            default:
                                MainPanel.Update();
                                Message("Unknown error occurred.");
                                break;
                        }
                    }
                    return;

                // Goto
                case ConsoleKey.G:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (MainPanel.BytesOnScreen >= CurrentFileInfo.Length)
                        {
                            Message("Not possible.");
                            return;
                        }

                        if (CurrentFileStream.Position >= CurrentFileInfo.Length)
                        {
                            Message("Already at the end of the file.");
                            return;
                        }

                        long? t = Utils.GetNumberFromUser("Goto:");

                        if (t == null)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }

                        if (t >= 0 && t <= CurrentFileInfo.Length - MainPanel.BytesOnScreen)
                        {
                            Goto((long)t);
                        }
                        else
                        {
                            MainPanel.Update();
                            Message("Position out of bound!");
                        }
                    }
                    return;

                // Offset base
                case ConsoleKey.O:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        string c = Utils.GetUserInput("Hex, dec, oct?");

                        if (c == null || c.Length < 1)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }

                        switch (c[0])
                        {
                            case 'H': case 'h':
                                CurrentOffsetView = OffsetView.Hexadecimal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            case 'O': case 'o':
                                CurrentOffsetView = OffsetView.Octal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            case 'D': case 'd':
                                CurrentOffsetView = OffsetView.Decimal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            default:
                                Message("Invalid view mode!");
                                MainPanel.Update();
                                return;
                        }
                    }
                    return;

                // Edit mode
                case ConsoleKey.E:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Not implemented. Sorry!");
                    }
                    break;

                // Replace
                case ConsoleKey.H:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Not implemented. Sorry!");
                    }
                    return;

                // Info
                case ConsoleKey.I:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Message($"{Utils.GetEntryInfo(CurrentFileInfo)}  {Utils.GetFormattedSize(CurrentFileInfo.Length)}");
                    }
                    return;

                // Exit
                case ConsoleKey.X:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        pUserResponse.Error = ErrorCode.Exit;
                        Exit();
                    }
                    return;

                // Dump
                case ConsoleKey.D:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Dumping...");
                        Dump();
                        Message("Dumping done!");
                    }
                    return;

                // -- Data nagivation --
                case ConsoleKey.LeftArrow:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        ReadFileAndUpdate(CurrentFileStream.Position -
                            (CurrentFileStream.Position % MainPanel.BytesInRow));
                    }
                    else if (CurrentFileStream.Position - 1 >= 0)
                    {
                        ReadFileAndUpdate(CurrentFileStream.Position - 1);
                    }
                    return;
                case ConsoleKey.RightArrow:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        int b = MainPanel.BytesInRow;
                        long NewPos = CurrentFileStream.Position + (b - CurrentFileStream.Position % b);

                        if (NewPos + MainPanel.BytesOnScreen <= CurrentFileInfo.Length)
                            ReadFileAndUpdate(NewPos);
                        else
                            ReadFileAndUpdate(CurrentFileInfo.Length - MainPanel.BytesOnScreen);
                    }
                    else if (CurrentFileStream.Position + MainPanel.BytesOnScreen + 1 <= CurrentFileInfo.Length)
                    {
                        ReadFileAndUpdate(CurrentFileStream.Position + 1);
                    }
                    return;

                case ConsoleKey.UpArrow:
                    if (CurrentFileStream.Position - MainPanel.BytesInRow >= 0)
                    {
                        ReadFileAndUpdate(CurrentFileStream.Position - MainPanel.BytesInRow);
                    }
                    else
                    {
                        ReadFileAndUpdate(0);
                    }
                    return;
                case ConsoleKey.DownArrow:
                    if (CurrentFileStream.Position + MainPanel.BytesOnScreen + MainPanel.BytesInRow <= CurrentFileInfo.Length)
                    {
                        ReadFileAndUpdate(CurrentFileStream.Position + MainPanel.BytesInRow);
                    }
                    else
                    {
                        if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                            ReadFileAndUpdate(CurrentFileInfo.Length - MainPanel.BytesOnScreen);
                    }
                    return;

                case ConsoleKey.PageUp:
                    if (CurrentFileStream.Position - MainPanel.BytesOnScreen >= 0)
                    {
                        ReadFileAndUpdate(CurrentFileStream.Position - MainPanel.BytesOnScreen);
                    }
                    else
                    {
                        ReadFileAndUpdate(0);
                    }
                    return;
                case ConsoleKey.PageDown:
                    if (CurrentFileStream.Position + (MainPanel.BytesOnScreen * 2) <= CurrentFileInfo.Length)
                    {
                        ReadFileAndUpdate(CurrentFileStream.Position += MainPanel.BytesOnScreen);
                    }
                    else
                    {
                        ReadFileAndUpdate(CurrentFileInfo.Length - MainPanel.BytesOnScreen);
                    }
                    return;

                case ConsoleKey.Home:
                    ReadFileAndUpdate(0);
                    return;
                case ConsoleKey.End:
                    ReadFileAndUpdate(CurrentFileInfo.Length - MainPanel.BytesOnScreen);
                    return;
            }
        }

        /// <summary>
        /// Prepares the screen with the information needed.<para/>
        /// Initialize: AutoSizing, Buffer, TitlePanel, Readfile, MainPanel,
        /// InfoPanel, and ControlPanel.
        /// </summary>
        /// <remarks>
        /// Also used when resizing.
        /// </remarks>
        static void PrepareScreen()
        {
            if (AutoSize)
            {
                MainPanel.BytesInRow = Utils.GetBytesInRow();
            }

            Buffer = new byte[CurrentFileInfo.Length < MainPanel.BytesOnScreen ?
                (int)CurrentFileInfo.Length : MainPanel.BytesOnScreen];

            TitlePanel.Update();
            ReadFileAndUpdate(CurrentFileStream.Position);
            ControlPanel.Place();
        }

        /// <summary>
        /// Read the current file at a position.
        /// </summary>
        /// <param name="pBasePosition">Position.</param>
        static void ReadCurrentFile(long pBasePosition)
        {
            CurrentFileStream.Position = pBasePosition;
            CurrentFileStream.Read(Buffer, 0, Buffer.Length);
            CurrentFileStream.Position = pBasePosition;
        }

        /// <summary>
        /// 1. Read file. 2. Update MainPanel. 3. Update InfoPanel
        /// </summary>
        /// <param name="pPosition"></param>
        static void ReadFileAndUpdate(long pPosition)
        {
            ReadCurrentFile(pPosition);
            MainPanel.Update();
            InfoPanel.Update();
        }
        
        /// <summary>
        /// Displays a message on screen to inform the user.
        /// </summary>
        /// <param name="pMessage">Message to show.</param>
        static void Message(string pMessage)
        {
            Console.SetCursorPosition(0, InfoPanel.Position);
            Console.Write(s(Console.WindowWidth - 1));

            string msg = $"[ {pMessage} ]";
            Console.SetCursorPosition((Console.WindowWidth / 2) - (msg.Length / 2),
                InfoPanel.Position);

            Utils.ToggleColors();
            Console.Write(msg);
            Console.ResetColor();
        }

        /// <summary>
        /// Toggle fullscreen mode.
        /// </summary>
        static void ToggleFullscreenMode()
        {
            if (Fullscreen)
            { // Turning off
                Fullscreen = false;
            }
            else
            { // Turning on
                Fullscreen = true;
            }

            PrepareScreen();

            if (Fullscreen)
            {
                MainPanel.Update();
                InfoPanel.Update();
            }
        }

        /// <summary>
        /// Go to a specific position in the file.
        /// </summary>
        /// <param name="pPosition">Position</param>
        static void Goto(long pPosition)
        {
            ReadFileAndUpdate(pPosition);
        }

        /// <summary>
        /// File to dump as text data with the app's <see cref="MainPanel.BytesInRow"/>
        /// and <see cref="CurrentOffsetView"/>.
        /// </summary>
        /// <returns><see cref="ErrorCode"/></returns>
        static ErrorCode Dump()
        {
            return Dump(CurrentFileInfo.Name, MainPanel.BytesInRow, CurrentOffsetView);
        }

        /// <summary>
        /// File to dump as text data.
        /// </summary>
        /// <param name="pFileToDump">Output filename.</param>
        /// <param name="pBytesInRow">Number of bytes in a row.</param>
        /// <param name="pViewMode"><see cref="OffsetView"/> to use.</param>
        /// <returns><see cref="ErrorCode"/></returns>
        static public ErrorCode Dump(string pFileToDump, int pBytesInRow = 16, OffsetView pViewMode = OffsetView.Hexadecimal)
        {
            if (!File.Exists(pFileToDump))
                return ErrorCode.FileNotFound;
            
            // These variables are independant because I do
            // not want to mess with the global ones and it
            // may happen the user /d directly.
            FileInfo f = new FileInfo(pFileToDump);
            Buffer = new byte[pBytesInRow];

            using (StreamWriter sw = new StreamWriter($"{pFileToDump}.{EXTENSION}"))
            {
                if (!sw.BaseStream.CanWrite)
                    return ErrorCode.DumbCannotWrite;

                pBytesInRow = pBytesInRow == 0 ? 16 : pBytesInRow;

                sw.AutoFlush = true;

                sw.WriteLine(f.Name);
                sw.WriteLine();
                sw.WriteLine($"Size: {Utils.GetFormattedSize(f.Length)}");
                sw.WriteLine($"Attributes: {Utils.GetEntryInfo(f)}");
                sw.WriteLine($"File date: {f.CreationTime}");
                sw.WriteLine($"Dump date: {DateTime.Now}");
                sw.WriteLine();

                sw.Write($"Offset {pViewMode.GetChar()}  ");
                for (int i = 0; i < pBytesInRow; i++)
                {
                    sw.Write($"{i:X2} ");
                }
                sw.WriteLine();
                
                if (CurrentFileInfo == null)
                    return DumpFile(f.Open(FileMode.Open), sw, pViewMode, pBytesInRow);
                else
                    return DumpFile(CurrentFileStream, sw, pViewMode, pBytesInRow);
               
            }
        }
        
        /// <remarks>
        /// Only to be used with <see cref="Dump()"/>!
        /// </remarks>
        static ErrorCode DumpFile(FileStream pIn, StreamWriter pOut, OffsetView pViewMode, int pBytesInRow)
        {
            long line = 0;
            int BufferPositionHex = 0;
            int BufferPositionData = 0;
            string t = string.Empty;

            Buffer = new byte[pBytesInRow];

            long lastpos = CurrentFileStream.Position;

            if (!pIn.CanRead)
                return ErrorCode.DumbCannotRead;

            bool Done = false;

            while (!Done)
            {
                switch (pViewMode)
                {
                    case OffsetView.Hexadecimal:
                        t = $"{line:X8}  ";
                        break;

                    case OffsetView.Decimal:
                        t = $"{line:D8}  ";
                        break;

                    case OffsetView.Octal:
                        t = $"{ToOct(line)}  ";
                        break;
                }

                line += pBytesInRow;

                pIn.Read(Buffer, 0, pBytesInRow);

                for (int pos = 0; pos < pBytesInRow; pos++)
                {
                    if (BufferPositionHex < pIn.Length)
                        t += $"{Buffer[pos]:X2} ";
                    else
                        t += "   ";

                    BufferPositionHex++;
                }

                t += " ";

                for (int pos = 0; pos < pBytesInRow; pos++)
                {
                    if (BufferPositionData < pIn.Length)
                        t += Buffer[pos].ToAscii();
                    else
                    {
                        pOut.WriteLine(t);

                        CurrentFileStream.Position = lastpos;

                        return 0; // Done!
                    }

                    BufferPositionData++;
                }

                pOut.WriteLine(t);
            }

            return 0;
        }

        /// <summary>
        /// Find a byte starting at the current position.
        /// </summary>
        /// <param name="pData">Data.</param>
        /// <returns>Positon, if found.</returns>
        static FindResult Find(byte pData)
        {
           return Find(pData, CurrentFileStream.Position);
        }

        /// <summary>
        /// Find a byte at a specific position.
        /// </summary>
        /// <param name="pData">Data.</param>
        /// <param name="pPosition">Positon to start searching from.</param>
        /// <returns>Positon, if found.</returns>
        static FindResult Find(byte pData, long pPosition)
        {
            if (pPosition < 0 || pPosition > CurrentFileInfo.Length)
                return new FindResult(ErrorCode.PositionOutOfBound);

            if (!CurrentFileInfo.Exists)
                return new FindResult(ErrorCode.FileNotFound);
            
            CurrentFileStream.Position = pPosition;

            bool Continue = true;
            while (Continue)
            {
                if (pData == (byte)CurrentFileStream.ReadByte())
                    return new FindResult(CurrentFileStream.Position);

                if (CurrentFileStream.Position >= CurrentFileStream.Length)
                    Continue = false;
            }

            // If not found, place the position back it was before
            CurrentFileStream.Position = pPosition;

            return new FindResult(ErrorCode.FindNoResult);
        }

        /// <summary>
        /// Find a string of data.
        /// </summary>
        /// <param name="pData">Data as a string.</param>
        /// <returns><see cref="FindResult"/></returns>
        static FindResult FindData(string pData)
        {
            return FindData(pData, CurrentFileStream.Position);
        }

        /// <summary>
        /// Find a string of data with a given position.
        /// </summary>
        /// <param name="pData">Data as a string.</param>
        /// <param name="pPosition">Starting position.</param>
        /// <returns><see cref="FindResult"/></returns>
        /// <remarks>
        /// How does this work?
        /// Search every character, if the first one seems to be right,
        /// read the data and compare it.
        /// </remarks>
        static FindResult FindData(string pData, long pPosition)
        {
            if (pPosition < 0 || pPosition > CurrentFileInfo.Length)
                return new FindResult(ErrorCode.PositionOutOfBound);

            if (!CurrentFileInfo.Exists)
                return new FindResult(ErrorCode.FileNotFound);

            if (string.IsNullOrWhiteSpace(pData))
                return new FindResult(ErrorCode.FindEmptyString);

            CurrentFileStream.Position = pPosition;

            byte[] b = new byte[pData.Length];
            bool Continue = true;
            if (pData.StartsWith("/") && pData.EndsWith("/"))
            {
                RegexOptions rf = RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.CultureInvariant;

                Regex r = new Regex(pData.Trim(new char[] { '/' }), rf);

                Message("Searching with regex...");

                while (Continue)
                {
                    if (CurrentFileStream.Position + pData.Length > CurrentFileStream.Length)
                        Continue = false;

                    if (r.IsMatch(char.ToString((char)CurrentFileStream.ReadByte())))
                    {
                        if (pData.Length == 1)
                        {
                            return new FindResult(CurrentFileStream.Position - 1);
                        }
                        else
                        {
                            CurrentFileStream.Position--;
                            CurrentFileStream.Read(b, 0, b.Length);
                            if (r.IsMatch(Encoding.ASCII.GetString(b)))
                            {
                                return new FindResult(CurrentFileStream.Position - pData.Length);
                            }
                        }
                    }
                }
            }
            else // Copying pasting is good
            {
                while (Continue)
                {
                    if (CurrentFileStream.Position + pData.Length > CurrentFileStream.Length)
                        Continue = false;

                    if (pData[0] == (char)CurrentFileStream.ReadByte())
                    {
                        if (pData.Length == 1)
                        {
                            return new FindResult(CurrentFileStream.Position - 1);
                        }
                        else
                        {
                            CurrentFileStream.Position--;
                            CurrentFileStream.Read(b, 0, b.Length);
                            if (pData == Encoding.ASCII.GetString(b))
                            {
                                return new FindResult(CurrentFileStream.Position - pData.Length);
                            }
                        }
                    }
                }
            }

            CurrentFileStream.Position = pPosition - 1;

            return new FindResult(ErrorCode.FindNoResult);
        }

        /// <summary>
        /// When the user exits the program.
        /// </summary>
        /// <returns>Always <see cref="false"/>.</returns>
        /// <remarks>
        /// Returns false to return due to the while loop.
        /// </remarks>
        static bool Exit()
        {
            Console.Clear();

            Console.CursorVisible = true;

            return false;
        }
        #endregion

        #region Panels
        #region TitlePanel
        /// <summary>
        /// Filename
        /// </summary>
        static class TitlePanel
        {
            static internal void Update()
            {
                Utils.ToggleColors();

                Console.SetCursorPosition(0, 0);

                if (CurrentFileInfo.Name.Length <= Console.WindowWidth)
                {
                    Console.Write(CurrentFileInfo.Name + s(Console.WindowWidth - CurrentFileInfo.Name.Length));
                }
                else
                    Console.Write(CurrentFileInfo.Name.Substring(0, Console.WindowWidth));

                Console.ResetColor();
            }
        }
        #endregion

        #region OffsetPanel
        /// <summary>
        /// Shows offset base view and the offset on each byte.
        /// </summary>
        static class OffsetPanel
        {
            static internal int Position = 1;

            /// <summary>
            /// Update the offset map
            /// </summary>
            static internal void Update()
            {
                StringBuilder t = new StringBuilder($"Offset {CurrentOffsetView.GetChar()}  ");

                if (CurrentFileStream.Position > uint.MaxValue)
                    t.Append(" ");

                for (int i = 0; i < MainPanel.BytesInRow;)
                {
                    t.Append($"{i++:X2} ");
                }

                if (LastWindowHeight != Console.WindowHeight ||
                    LastWindowWidth != Console.WindowWidth)
                    t.Append(s(Console.WindowWidth - t.Length - 1)); // Force clean

                Console.SetCursorPosition(0, Position);
                Console.Write(t.ToString());
            }
        }
        #endregion

        #region MainPanel
        /// <summary>
        /// Main panel which represents the offset, data as bytes,
        /// and data as ASCII characters.
        /// </summary>
        internal static class MainPanel
        {
            /// <summary>
            /// Current cursor position for Edit mode. [x,y]
            /// </summary>
            //TODO: Decide on type for CursorPosition (v0.9)
            //static internal int[,] CursorPosition;

            /// <summary>
            /// Gets the position to start rendering on the console (Y axis).
            /// </summary>
            static internal int Position
            {
                get
                {
                    return Fullscreen ? 1 : 2;
                }
            }

            /// <summary>
            /// Gets the heigth of the main panel.
            /// </summary>
            static internal int FrameHeight
            {
                get
                {
                    return Fullscreen ?
                        Console.WindowHeight - 2 : Console.WindowHeight - 5;
                }
            }

            /// <summary>
            /// Gets or sets the number of bytes showed in a row.
            /// </summary>
            static internal int BytesInRow
            {
                get; set;
            }

            /// <summary>
            /// Gets the number of elements which can be shown in the main panel.
            /// </summary>
            static internal int BytesOnScreen
            {
                get
                {
                    return FrameHeight * BytesInRow;
                }
            }

            /// <summary>
            /// Update from Buffer.
            /// </summary>
            static internal void Update()
            {
                int od = 0;
                int oa = 0;
                long fpos = CurrentFileStream.Position;
                int fh = FrameHeight;
                
                StringBuilder t = new StringBuilder(Console.WindowWidth);
                OffsetPanel.Update();
                Console.SetCursorPosition(0, Position);
                for (int line = 0; line < fh; line++)
                {
                    switch (CurrentOffsetView)
                    {
                        case OffsetView.Hexadecimal:
                            t = new StringBuilder($"{(line * BytesInRow) + fpos:X8}  ");
                            break;

                        case OffsetView.Decimal:
                            t = new StringBuilder($"{(line * BytesInRow) + fpos:D8}  ");
                            break;

                        case OffsetView.Octal:
                            t = new StringBuilder($"{ToOct((line * BytesInRow) + fpos)}  ");
                            break;
                    }

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (fpos + od < CurrentFileInfo.Length)
                            t.Append($"{Buffer[od]:X2} ");
                        else
                            t.Append("   ");

                        ++od;
                    }

                    t.Append(" ");

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (fpos + oa < CurrentFileInfo.Length)
                            t.Append(Buffer[oa].ToAscii());
                        else
                        {
                            Console.SetCursorPosition(0, Position + line);
                            Console.Write(t.ToString());
                            return;
                        }

                        ++oa;
                    }

                    t.Append(" "); // 0xFFFFFFFF padding
                    
                    Console.WriteLine(t.ToString());
                }
            }
        }
        #endregion

        #region InfoPanel
        /// <summary>
        /// Current position information.
        /// </summary>
        static class InfoPanel
        {
            /// <summary>
            /// Position to start rendering on the console (Y axis).
            /// </summary>
            static internal int Position
            {
                get
                {
                    return Fullscreen ?
                        Console.WindowHeight - 1 : Console.WindowHeight - 3;
                }
            }

            /// <summary>
            /// Update the offset information
            /// </summary>
            static internal void Update()
            {
                long pos = CurrentFileStream.Position;

                decimal r = CurrentFileInfo.Length > 0 ?
                    Math.Round(((decimal)(pos + Buffer.Length) / CurrentFileInfo.Length) * 100) :
                    0;

                string t =
                    $"  DEC: {pos:D8} | HEX: {pos:X8} | OCT: {ToOct(pos)} | POS: {r,3}%";

                Console.SetCursorPosition(0, Position);
                Console.Write(t); // Force-clear any messages
            }
        }
        #endregion

        #region ControlPanel
        /// <summary>
        /// Panel that shows keyboard shortcuts.
        /// </summary>
        static class ControlPanel
        {
            const int ItemLength = 16;

            /// <summary>
            /// Places the control map on screen (e.g. ^T Try jumping and etc.)
            /// </summary>
            static internal void Place()
            {
                int width = Console.WindowWidth;

                Console.SetCursorPosition(0, Console.WindowHeight - 2);
                if (width >= ItemLength)     m("^W", " Find byte    ");
                if (width >= ItemLength * 2) m("^J", " Find data    ");
                if (width >= ItemLength * 3) m("^G", " Goto         ");
                if (width >= ItemLength * 4) m("^H", " Replace      ");
                //if (width >= ItemLength * 5) m("  ", "              ");
                Console.SetCursorPosition(0, Console.WindowHeight - 1);
                if (width >= ItemLength)     m("^X", " Exit         ");
                if (width >= ItemLength * 2) m("^O", " Offset base  ");
                if (width >= ItemLength * 3) m("^I", " Info         ");
                if (width >= ItemLength * 4) m("^D", " Dump         ");
                if (width >= ItemLength * 5) m("^E", " Edit mode");
            }
        }

        /// <summary>
        /// Write out a shortcut and its short description
        /// </summary>
        /// <param name="pShortcut">Shortcut, e.g. ^D</param>
        /// <param name="pTitle">Title, e.g. Dump</param>
        static void m(string pShortcut, string pTitle)
        {
            Utils.ToggleColors();
            Console.Write(pShortcut);
            Console.ResetColor();
            Console.Write(pTitle);
        }
        #endregion
        #endregion

        #region Small utils
        /// <summary>
        /// Generate a string with spaces with a desired length.
        /// </summary>
        /// <param name="l">Length</param>
        /// <returns>String</returns>
        static string s(int l) => new string(' ', l);
        #endregion

        #region Type extensions
        /// <summary>
        /// Converts into an octal number.
        /// </summary>
        /// <param name="c">Number.</param>
        /// <returns>String.</returns>
        static string ToOct(this long c) =>
            Convert.ToString(c, 8).PadLeft(8, '0');

        /// <summary>
        /// Returns a printable character if found.
        /// </summary>
        /// <param name="pIn">Byte to transform.</param>
        /// <returns>Console (Windows) readable character.</returns>
        static char ToAscii(this byte pIn) =>
            pIn < 0x20 || pIn > 0x7E ? '.' : (char)pIn;

        /// <summary>
        /// Gets the character for the upper bar depending on the
        /// offset base view.
        /// </summary>
        /// <param name="pView">This <see cref="OffsetView"/></param>
        /// <returns>Character.</returns>
        static char GetChar(this OffsetView pView)
        {
            switch (pView)
            {
                case OffsetView.Hexadecimal:
                    return 'h';
                case OffsetView.Decimal:
                    return 'd';
                case OffsetView.Octal:
                    return 'o';
                default:
                    return '?'; // ??????????
            }
        }

        static int GetBase(this OffsetView pView)
        {
            switch (pView)
            {
                case OffsetView.Hexadecimal:
                    return 16;
                case OffsetView.Decimal:
                    return 10;
                case OffsetView.Octal:
                    return 8;
                default:
                    return 1;
            }
        }

        static public int Int(this ErrorCode pCode) => (int)pCode;
        #endregion
    }

    static class Utils
    {
        #region Formatting
        const long SIZE_TB = 1099511627776;
        const long SIZE_GB = 1073741824;
        const long SIZE_MB = 1048576;
        const long SIZE_KB = 1024;

        static internal string GetFormattedSize(long pSize)
        {
            double s = pSize;

            if (s > SIZE_TB) // TB
                return $"{Math.Round(s / SIZE_TB, 2)} TB";
            else if (s > SIZE_GB) // GB
                return $"{Math.Round(s / SIZE_GB, 2)} GB";
            else if (s > SIZE_MB) // MB
                return $"{Math.Round(s / SIZE_MB, 2)} MB";
            else if (s > SIZE_KB) // KB
                return $"{Math.Round(s / SIZE_KB, 2)} KB";
            else // B
                return $"{pSize} B";
        }

        /// <summary>
        /// Gets file info and owner from <see cref="FileInfo"/>
        /// </summary>
        /// <param name="pFile">File.</param>
        /// <returns>Info as a string</returns>
        internal static string GetEntryInfo(this FileInfo pFile)
        {
            string o = "-"; // Never a directory

            FileAttributes fa = pFile.Attributes;

            o += fa.HasFlag(FileAttributes.Archive) ? "a" : "-";
            o += fa.HasFlag(FileAttributes.Compressed) ? "c" : "-";
            o += fa.HasFlag(FileAttributes.Encrypted) ? "e" : "-";
            o += fa.HasFlag(FileAttributes.ReadOnly) ? "r" : "-";
            o += fa.HasFlag(FileAttributes.System) ? "s" : "-";
            o += fa.HasFlag(FileAttributes.Hidden) ? "h" : "-";
            o += fa.HasFlag(FileAttributes.Temporary) ? "t" : "-";

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                o += "  " +
                    pFile.GetAccessControl()
                    .GetOwner(typeof(SecurityIdentifier))
                    .Translate(typeof(NTAccount));
            }

            return o;
        }
        #endregion
        
        #region User input
        /// <summary>
        /// Readline with a maximum length plus optional password mode.
        /// </summary>
        /// <param name="pLimit">Character limit</param>
        /// <param name="pPassword">Is password</param>
        /// <returns>User's input</returns>
        /// <remarks>v1.1.1 - 0xdd</remarks>
        internal static string ReadLine(int pLimit, string pSuggestion = null, bool pPassword = false)
        {
            StringBuilder o = new StringBuilder(pSuggestion ?? string.Empty);
            int Index = 0;
            bool Continue = true;
            int oleft = Console.CursorLeft; // Origninal Left Position
            int otop = Console.CursorTop; // Origninal Top Position

            if (pSuggestion != null)
            {
                Console.Write(pSuggestion);
                Index = pSuggestion.Length;
                Console.SetCursorPosition(oleft + Index, otop);
            }

            Console.CursorVisible = true;

            while (Continue)
            {
                ConsoleKeyInfo c = Console.ReadKey(true);

                switch (c.Key)
                {
                    // Ignore keys
                    case ConsoleKey.Tab:
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.DownArrow:
                        break;

                    // Cancel
                    case ConsoleKey.Escape:
                        Console.CursorVisible = false;
                        return string.Empty;

                    // Returns the string
                    case ConsoleKey.Enter:
                        Console.CursorVisible = false;
                        return o.ToString();

                    // Navigation
                    case ConsoleKey.LeftArrow:
                        if (Index > 0)
                        {
                            Console.SetCursorPosition(oleft + --Index, otop);
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (Index < o.Length)
                        {
                            Console.SetCursorPosition(oleft + ++Index, otop);
                        }
                        break;
                    case ConsoleKey.Home:
                        if (Index > 0)
                        {
                            Index = 0;
                            Console.SetCursorPosition(oleft, otop);
                        }
                        break;
                    case ConsoleKey.End:
                        if (Index < o.Length)
                        {
                            Index = o.Length;
                            Console.SetCursorPosition(oleft + Index, otop);
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (Index < o.Length)
                        {
                            // Erase whole from index
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o = o.Remove(Index, o.Length - Index);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                            else // Erase one character
                            {
                                o = o.Remove(Index, 1);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                        }
                        break;

                    case ConsoleKey.Backspace:
                        if (Index > 0)
                        {
                            // Erase whole from index
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o = o.Remove(0, Index);
                                Index = 0;
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                            else // Erase one character
                            {
                                o = o.Remove(--Index, 1);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                        }
                        break;

                    default:
                        if (o.Length < pLimit)
                        {
                            char h = c.KeyChar;

                            if (char.IsLetterOrDigit(h) || char.IsPunctuation(h) || char.IsSymbol(h) || char.IsWhiteSpace(h))
                            {
                                o.Insert(Index++, h);
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                Console.SetCursorPosition(oleft + Index, otop);
                            }
                        }
                        break;
                }
            }

            return string.Empty;
        }

        internal static long ReadValue(int pLimit, string pSuggestion = null)
        {
            string t = ReadLine(pLimit, pSuggestion);

            if (t.StartsWith("0x")) // Hexadecimal
            {
                return Convert.ToInt64(t, 16);
            }
            else if (t[0] == '0') // Octal
            {
                return Convert.ToInt64(t, 8);
            }
            else // Decimal
            {
                return long.Parse(t);
            }
        }

        internal static long? GetNumberFromUser(string pMessage, int pWidth = 27, int pHeight = 4, string pSuggestion = null)
        {
            GenerateInputBox(pMessage, pWidth, pHeight);

            long? t = null;

            try
            {
                t = ReadValue(pWidth - 2, pSuggestion);
            }
            catch { }

            Console.ResetColor();

            return t;
        }

        internal static string GetUserInput(string pMessage, int pWidth = 32, int pHeight = 4, string pSuggestion = null)
        {
            GenerateInputBox(pMessage, pWidth, pHeight);

            string t = ReadLine(pWidth - 2, pSuggestion: pSuggestion);

            Console.ResetColor();

            return t;
        }
        #endregion

        #region Console
        static void GenerateInputBox(string pMessage, int pWidth, int pHeight)
        {
            // -- Begin prepare box --
            int startx = (Console.WindowWidth / 2) - (pWidth / 2);
            int starty = (Console.WindowHeight / 2) - (pHeight / 2);

            Console.SetCursorPosition(startx, starty);
            Console.Write('┌');
            Console.Write(new string('─', pWidth - 2));
            Console.Write('┐');

            for (int i = 0; i < pHeight - 2; i++)
            {
                Console.SetCursorPosition(startx, starty + i + 1);
                Console.Write('│');
            }
            for (int i = 0; i < pHeight - 2; i++)
            {
                Console.SetCursorPosition(startx + pWidth - 1, starty + i + 1);
                Console.Write('│');
            }

            Console.SetCursorPosition(startx, starty + pHeight - 1);
            Console.Write('└');
            Console.Write(new string('─', pWidth - 2));
            Console.Write('┘');

            Console.SetCursorPosition(startx + 1, starty + 1);
            Console.Write(pMessage);
            if (pMessage.Length < pWidth - 2)
                Console.Write(new string(' ', pWidth - pMessage.Length - 2));
            // -- End prepare box --

            // -- Begin prepare text box --
            ToggleColors();
            Console.SetCursorPosition(startx + 1, starty + 2);
            Console.Write(new string(' ', pWidth - 2));
            Console.SetCursorPosition(startx + 1, starty + 2);
            // -- End prepare text box --
        }

        /// <summary>
        /// Toggles current ForegroundColor to black
        /// and BackgroundColor to gray.
        /// </summary>
        internal static void ToggleColors()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }

        internal static int GetBytesInRow()
        {
            return ((Console.WindowWidth - 10) / 4) - 1;
        }
        #endregion
    }
}