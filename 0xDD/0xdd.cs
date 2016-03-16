using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

//TODO: Hashing (with menu) (medium, long, v0.7)
//TODO: Goto beginning of line (easy-medium, short, v0.6)
//TODO: Search from end of file (easy, medium, v0.6)

/*
    TODO: Edit mode (medium-hard, long, v0.9)
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
highlighted (gray on black) while navigating
*/

namespace _0xdd
{
    #region Enumerations
    enum ErrorCode : byte
    {
        Success = 0,

        // File related
        FileNotFound = 4,
        FileUnreadable = 5,

        // Position related
        PositionOutOfBound = 8,

        // Dump related
        DumbCannotWrite = 16,
        DumbCannotRead = 17,

        // Find related
        FindNoResult = 32,
        FindEmptyString = 33,

        // CLI related
        CLI_InvalidOffsetView = 0xC0,
        CLI_InvalidWidth = 0xC4,
        
        UnknownError = 0xFE
    }

    enum OffsetView : byte
    {
        Hexadecimal,
        Decimal,
        Octal
    }
    
    enum OperatingMode : byte
    {
        Read, // READ
        Overwrite, // OVRW
        Insert // INSR
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

        #region General properties
        static FileInfo CurrentFile;
        static FileStream CurrentFileStream;
        
        static byte[] Buffer;
        
        static bool Fullscreen;
        
        static int LastWindowHeight;
        static int LastWindowWidth;

        static bool AutoSize;

        static OffsetView CurrentOffsetBaseView;
        static OperatingMode CurrentWritingMode;
        
        static string LastDataSearched;
        static byte LastByteSearched;
        #endregion

        #region Methods        
        internal static ErrorCode Open(string pFilePath, OffsetView pOffsetViewMode = OffsetView.Hexadecimal, int pBytesRow = 0)
        {
            if (!File.Exists(pFilePath))
                return ErrorCode.FileNotFound;

            CurrentFile = new FileInfo(pFilePath);
            
            try
            {
                CurrentFileStream = CurrentFile.Open(FileMode.Open); // Open, for now
            }
            catch
            {
                return ErrorCode.FileUnreadable;
            }

            AutoSize = pBytesRow > 0;
            MainPanel.BytesInRow = AutoSize ? pBytesRow : Utils.GetBytesInRow();

            CurrentWritingMode = OperatingMode.Read;

            Console.CursorVisible = false;

            CurrentOffsetBaseView = pOffsetViewMode;
            LastWindowHeight = Console.WindowHeight;
            LastWindowWidth = Console.WindowWidth;

            PrepareScreen();

            UserResponse ur = new UserResponse(ErrorCode.Success);
            
            while (ur.Success)
            {
                ReadUserKey(ref ur);
            }

            return ur.Error;
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

            Buffer = new byte[CurrentFile.Length < MainPanel.MaxBytes ?
                (int)CurrentFile.Length : MainPanel.MaxBytes];

            TitlePanel.Update();
            ReadCurrentFile(CurrentFileStream.Position);
            MainPanel.Update();
            InfoPanel.Update();
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
        /// Read user input.
        /// </summary>
        /// <returns>Returns true if still using 0xdd.</returns>
        static void ReadUserKey(ref UserResponse pUserResponse)
        {
            ConsoleKeyInfo input = Console.ReadKey(true);
            
            if (LastWindowHeight != Console.WindowHeight ||
                LastWindowWidth != Console.WindowWidth)
            {
                Console.Clear();
                PrepareScreen();

                LastWindowHeight = Console.WindowHeight;
                LastWindowWidth = Console.WindowWidth;
            }
            
            switch (input.Key)
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
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFile.Length - MainPanel.MaxBytes)
                        {
                            Message("Already at the end of the file.");
                            return;
                        }

                        if (MainPanel.MaxBytes >= CurrentFile.Length)
                        {
                            Message("Not possible.");
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
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFile.Length - MainPanel.MaxBytes)
                        {
                            Message("Already at the end of the file.");
                            return;
                        }

                        if (MainPanel.MaxBytes >= CurrentFile.Length)
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
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        if (MainPanel.MaxBytes >= CurrentFile.Length)
                        {
                            Message("Not possible.");
                            return;
                        }

                        if (CurrentFileStream.Position >= CurrentFile.Length)
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

                        if (t >= 0 && t <= CurrentFile.Length - MainPanel.MaxBytes)
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
                    if (input.Modifiers == ConsoleModifiers.Control)
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
                                CurrentOffsetBaseView = OffsetView.Hexadecimal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            case 'O': case 'o':
                                CurrentOffsetBaseView = OffsetView.Octal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            case 'D': case 'd':
                                CurrentOffsetBaseView = OffsetView.Decimal;
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
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Not implemented. Sorry!");
                    }
                    break;

                // Replace
                case ConsoleKey.H:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Not implemented. Sorry!");
                    }
                    return;

                // Info
                case ConsoleKey.I:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        Message($"Size: {Utils.GetFormattedSize(CurrentFile.Length)}");
                    }
                    return;

                // Exit
                case ConsoleKey.X:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        Exit();
                    }
                    return;

                // Dump
                case ConsoleKey.D:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Dumping...");
                        Dump();
                        Message("Dumping done!");
                    }
                    return;

                // -- Data nagivation --
                case ConsoleKey.LeftArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position - 1 >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - 1);
                        }
                    }
                    return;
                case ConsoleKey.RightArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position + MainPanel.MaxBytes + 1 <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position + 1);
                        }
                    }
                    return;

                case ConsoleKey.UpArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position - MainPanel.BytesInRow >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - MainPanel.BytesInRow);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    }
                    return;
                case ConsoleKey.DownArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position + MainPanel.MaxBytes + MainPanel.BytesInRow <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position + MainPanel.BytesInRow);
                        }
                        else
                        {
                            if (MainPanel.MaxBytes < CurrentFile.Length)
                                ReadFileAndUpdate(CurrentFile.Length - MainPanel.MaxBytes);
                        }
                    }
                    return;

                case ConsoleKey.PageUp:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position - MainPanel.MaxBytes >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - MainPanel.MaxBytes);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    }
                    return;
                case ConsoleKey.PageDown:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position + (MainPanel.MaxBytes * 2) <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position += MainPanel.MaxBytes);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFile.Length - MainPanel.MaxBytes);
                        }
                    }
                    return;

                case ConsoleKey.Home:
                    if (CurrentWritingMode == OperatingMode.Read)
                        ReadFileAndUpdate(0);
                    return;
                case ConsoleKey.End:
                    if (CurrentWritingMode == OperatingMode.Read)
                        ReadFileAndUpdate(CurrentFile.Length - MainPanel.MaxBytes);
                    return;
            }
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

        static void Goto(long pPosition)
        {
            ReadFileAndUpdate(pPosition);
        }

        /// <summary>
        /// File to dump as text data with the app's <see cref="MainPanel.BytesInRow"/>
        /// and <see cref="CurrentOffsetBaseView"/>.
        /// </summary>
        /// <returns><see cref="ErrorCode"/></returns>
        static ErrorCode Dump()
        {
            return Dump(CurrentFile.Name, MainPanel.BytesInRow, CurrentOffsetBaseView);
        }

        /// <summary>
        /// File to dump as text data.
        /// </summary>
        /// <param name="pFileToDump">Output filename.</param>
        /// <param name="pBytesInRow">Number of bytes in a row.</param>
        /// <param name="pViewMode"><see cref="OffsetView"/> to use.</param>
        /// <returns><see cref="ErrorCode"/></returns>
        static public ErrorCode Dump(string pFileToDump, int pBytesInRow, OffsetView pViewMode)
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
                sw.WriteLine($"Attributes: {f.Attributes}");
                sw.WriteLine($"File date: {f.CreationTime}");
                sw.WriteLine($"Dump date: {DateTime.Now}");
                sw.WriteLine();

                sw.Write($"Offset {pViewMode.GetChar()}  ");
                for (int i = 0; i < pBytesInRow; i++)
                {
                    sw.Write($"{i:X2} ");
                }
                sw.WriteLine();
                
                if (CurrentFile == null)
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
                        t += Buffer[pos].ToSafeChar();
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
            if (pPosition < 0 || pPosition > CurrentFile.Length)
                return new FindResult(ErrorCode.PositionOutOfBound);

            if (!CurrentFile.Exists)
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
            if (pPosition < 0 || pPosition > CurrentFile.Length)
                return new FindResult(ErrorCode.PositionOutOfBound);

            if (!CurrentFile.Exists)
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

                if (CurrentFile.Name.Length <= Console.WindowWidth)
                {
                    Console.Write(CurrentFile.Name);
                    Console.Write(s(Console.WindowWidth - CurrentFile.Name.Length));
                }
                else
                    Console.Write(CurrentFile.Name.Substring(0, Console.WindowWidth));

                Console.ResetColor();
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
            static internal int MaxBytes
            {
                get
                {
                    return FrameHeight * BytesInRow;
                }
            }

            /// <summary>
            /// Update the section of the screen from the buffer.
            /// </summary>
            static internal void Update()
            {
                int BufferOffsetData = 0;
                int BufferOffsetText = 0;

                StringBuilder t = new StringBuilder();
                
                OffsetPanel.Update();
                
                Console.SetCursorPosition(0, Position);

                for (int line = 0; line < FrameHeight; line++)
                {
                    switch (CurrentOffsetBaseView)
                    {
                        case OffsetView.Hexadecimal:
                            t = new StringBuilder($"{(line * BytesInRow) + CurrentFileStream.Position:X8}  ");
                            break;

                        case OffsetView.Decimal:
                            t = new StringBuilder($"{(line * BytesInRow) + CurrentFileStream.Position:D8}  ");
                            break;

                        case OffsetView.Octal:
                            t = new StringBuilder($"{ToOct((line * BytesInRow) + CurrentFileStream.Position)}  ");
                            break;
                    }

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (CurrentFileStream.Position + BufferOffsetData < CurrentFile.Length)
                            t.Append($"{Buffer[BufferOffsetData]:X2} ");
                        else
                            t.Append("   ");

                        BufferOffsetData++;
                    }

                    t.Append(" ");

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (CurrentFileStream.Position + BufferOffsetText < CurrentFile.Length)
                            t.Append($"{Buffer[BufferOffsetText].ToSafeChar()}");
                        else
                        {
                            Console.Write(t.ToString());
                            return;
                        }

                        BufferOffsetText++;
                    }

                    t.Append(" ");

                    Console.WriteLine(t.ToString());
                }
            }
        }
        #endregion

        #region InfoPanel
        /// <summary>
        /// Current offset and position.
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
                decimal r =
                    Math.Round(((decimal)(CurrentFileStream.Position + Buffer.Length) / CurrentFile.Length) * 100);

                string s = $"  DEC: {CurrentFileStream.Position:D8} | HEX: {CurrentFileStream.Position:X8} | OCT: {ToOct(CurrentFileStream.Position)} | POS: {r}%";

                Console.SetCursorPosition(0, Position);
                Console.Write(s + _0xdd.s(Console.WindowWidth - s.Length - 1)); // Force-clear any messages
            }
        }
        #endregion

        #region OffsetPanel
        /// <summary>
        /// Shows offset base view and the offset on each byte.
        /// e.g. Offset h  00 01 02 .. ..
        /// </summary>
        static class OffsetPanel
        {
            static internal int Position = 1;

            /// <summary>
            /// Update the offset map
            /// </summary>
            static internal void Update()
            {
                StringBuilder t = new StringBuilder($"Offset {CurrentOffsetBaseView.GetChar()}  ");

                if (CurrentFileStream.Position > uint.MaxValue)
                    t.Append(" ");

                for (int i = 0; i < MainPanel.BytesInRow;)
                {
                    t.Append($"{i++:X2} ");
                }

                Console.SetCursorPosition(0, Position);
                Console.Write(t.ToString());
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
                //TODO: Adjust Place() depending on screen width
                // Place the most important actions first

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
            Convert.ToString(c, 8).FillZeros(8);

        /// <summary>
        /// Returns a printable character if found.
        /// </summary>
        /// <param name="pIn">Byte to transform.</param>
        /// <returns>Console (Windows) readable character.</returns>
        static char ToSafeChar(this byte pIn) =>
            pIn < 0x20 || pIn > 0x7E ? '.' : (char)pIn;

        /// <summary>
        /// Gets the character for the upper bar depending on the
        /// offset base view.
        /// </summary>
        /// <param name="pObject">This <see cref="OffsetView"/></param>
        /// <returns>Character.</returns>
        static char GetChar(this OffsetView pObject)
        {
            switch (pObject)
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

        /// <summary>
        /// Fill zeros with a string.
        /// </summary>
        /// <param name="pString">Input.</param>
        /// <param name="pLength">Desired length.</param>
        /// <returns>String-zero-filed.</returns>
        /// <remark>
        /// If the desired length is smaller than the input,
        /// the desired length will be the same length as
        /// the input.
        /// </remark>
        static string FillZeros(this string pString, int pLength) =>
            new string('0',
                (pLength < pString.Length ? pString.Length : pLength) - pString.Length) + pString;

        static public int Int(this ErrorCode pCode) => (int)pCode;
        #endregion
    }

    static class Utils
    {
        #region Formatting
        static internal string GetFormattedSize(long pSize)
        {
            if (pSize > Math.Pow(1024, 3)) // GB
                return $"{Math.Round(pSize / Math.Pow(1024, 3), 2)} GB";
            else if (pSize > Math.Pow(1024, 2)) // MB
                return $"{Math.Round(pSize / Math.Pow(1024, 2), 2)} MB";
            else if (pSize > 1024) // KB
                return $"{Math.Round((double)pSize / 1024, 1)} KB";
            else // B
                return $"{pSize} B";
        }
        #endregion

        #region User input
        /// <summary>
        /// Readline with a maximum length plus optional password mode.
        /// </summary>
        /// <param name="pLimit">Character limit</param>
        /// <param name="pPassword">Is password</param>
        /// <returns>User's input</returns>
        /// <remarks>v1.1</remarks>
        internal static string ReadLine(int pLimit, string pSuggestion = null, bool pPassword = false)
        {
            StringBuilder o = pSuggestion == null ? new StringBuilder() : new StringBuilder(pSuggestion);
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
                        Continue =
                            Console.CursorVisible = false;
                        return string.Empty;

                    // Returns the string
                    case ConsoleKey.Enter:
                        Continue = false;
                        if (o.Length > 0)
                            return o.ToString();
                        break;

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

                    case ConsoleKey.Backspace:
                        if (Index > 0)
                        {
                            // Erase whole
                            //TODO: Erase from index (easy, medium, v0.6)
                            if (c.Modifiers == ConsoleModifiers.Control)
                            {
                                o = new StringBuilder();
                                Index = 0;
                                Console.SetCursorPosition(oleft, otop);
                                Console.Write(new string(' ', pLimit));
                                Console.SetCursorPosition(oleft, otop);
                            }
                            else // Erase one character
                            {
                                if (Index > 0)
                                {
                                    o = o.Remove(--Index, 1);
                                    Console.SetCursorPosition(oleft, otop);
                                    Console.Write(new string(' ', pLimit));
                                    Console.SetCursorPosition(oleft, otop);
                                    Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                                    Console.SetCursorPosition(oleft + Index, otop);
                                }
                            }
                        }
                        break;

                    default:
                        if (o.Length < pLimit)
                        {
                            o.Insert(Index++, c.KeyChar);
                            Console.SetCursorPosition(oleft, otop);
                            Console.Write(new string(' ', pLimit));
                            Console.SetCursorPosition(oleft, otop);
                            Console.Write(pPassword ? new string('*', o.Length) : o.ToString());
                            Console.SetCursorPosition(oleft + Index, otop);
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