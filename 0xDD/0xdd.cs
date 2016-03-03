using System;
using System.IO;
using System.Text;

//TODO: Edit mode
// 0E 0F
// D2 DD ..
// ^^    ^
// highlighted (gray on black) while navigating

/*
    Box of ideas (Lazy TODO/idea list)
    - Top right: Insert(INS)/Overwrite(OVR)
    - Search: /regex/ (Begins && ends with '/')
    - Edit:
      - Dictionary<long, byte> (FilePosition, Data)
      - Rendering: If byte at position, write that byte to display instead
      - Saving: Remove duplicates, loop through List and write
      - Editing: If new data on same position, replace
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
        PositionOutOfBound = 0x16,

        // Dump related
        DumbCannotWrite = 0x32,
        DumbCannotRead = 0x33,

        // Find related
        FindNoResult = 0x64,
        FindEmptyString = 0x65,
        
        UnknownError = 0xFE
    }
    
    internal enum OffsetBaseView : byte
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
        #region Constants
        /// <summary>
        /// Extension of data dump files.
        /// </summary>
        const string EXTENSION = "hexdmp";
        #endregion

        #region General properties
        /// <summary>
        /// Information about the current file.
        /// </summary>
        static FileInfo CurrentFile;
        static FileStream CurrentFileStream;

        /// <summary>
        /// Buffer used for on-screen display.
        /// </summary>
        /// <remarks>
        /// This doesn't use a lot of memory.
        /// If the main panel size is 16x19 (16 bytes on 19 lines,
        /// default with 80x24 terminal), the buffer will only use
        /// 309 bytes (0.3 KB) of additional memory plus the memory for
        /// a tiny array, which is a very few more bytes.
        /// </remarks>
        static byte[] Buffer;
        
        static bool Fullscreen;

        // Last window sizes.
        static int LastWindowHeight;
        static int LastWindowWidth;

        static bool AutoSize;

        static OffsetBaseView CurrentOffsetBaseView;
        static OperatingMode CurrentWritingMode;
        #endregion

        #region Methods
        internal static ErrorCode Open(string pFilePath)
        {
            return Open(pFilePath, OffsetBaseView.Hexadecimal, Utils.GetBytesInRow());
        }
        
        internal static ErrorCode Open(string pFilePath, OffsetBaseView pOffsetViewMode, int pBytesRow)
        {
            if (pBytesRow > 0)
            {
                MainPanel.BytesInRow = pBytesRow;
            }
            else
            {
                AutoSize = true;
                MainPanel.BytesInRow = Utils.GetBytesInRow();
            }

            CurrentFile = new FileInfo(pFilePath);

            CurrentWritingMode = OperatingMode.Read;

            Console.CursorVisible = false;

            CurrentOffsetBaseView = pOffsetViewMode;
            LastWindowHeight = Console.WindowHeight;
            LastWindowWidth = Console.WindowWidth;
            
            try
            {
                CurrentFileStream = CurrentFile.Open(FileMode.Open); // Open, for now
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: File unreadable. ({ex.GetType()} - 0x{ex.HResult:X8})");
                return ErrorCode.FileUnreadable;
            }

            PrepareScreen();

            ///TODO: Find a way to return any <see cref="ErrorCode"/> through that loop
            
            while (ReadUserKey()) { }

            return 0;
        }

        /// <summary>
        /// Prepares the screen with the information needed.
        /// </summary>
        static void PrepareScreen()
        {
            if (AutoSize)
            {
                MainPanel.BytesInRow = Utils.GetBytesInRow();
            }

            Buffer = new byte[CurrentFile.Length < MainPanel.ScreenMaxBytes ?
                (int)CurrentFile.Length : MainPanel.ScreenMaxBytes];

            TitlePanel.Update();
            ReadCurrentFile(0);
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
        /// Read the user's input.
        /// </summary>
        /// <returns>Returns true if still using 0xdd.</returns>
        static bool ReadUserKey()
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
                        MainPanel.Refresh();
                    }
                    return true;

                case ConsoleKey.F10:
                    {
                        ToggleFullscreenMode();
                    }
                    return true;

                // -- Shown shortcuts --

                // Find byte
                case ConsoleKey.W:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFile.Length)
                        {
                            Message("Already at the end of the file.");
                            return true;
                        }

                        long? t = Utils.GetNumberFromUser("Find byte:", MainPanel.ScreenMaxBytes, CurrentFile.Length);

                        if (t == null)
                        {
                            MainPanel.Update();
                            return true;
                        }
                        
                        if (t < 0 || t > byte.MaxValue)
                        {
                            MainPanel.Update();
                            Message("A value between 0 and 255 is required.");
                        }
                        else
                        {
                            MainPanel.Update();
                            Message("Searching...");
                            long p = Find((byte)t, CurrentFileStream.Position + 1);

                            if (p > 1)
                            {
                                Message($"Byte {t:X2} could not be found.");
                            }
                            else
                            {
                                Goto(p - 1);
                                Message($"Found {t:X2} at position {p - 1}");
                            }
                        }
                    }
                    return true;

                // Find data
                case ConsoleKey.J:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFile.Length)
                        {
                            Message("Already at the end of the file.");
                            return true;
                        }

                        string t = Utils.GetUserInput("Find data:", MainPanel.ScreenMaxBytes, CurrentFile.Length);

                        if (t == null || t.Length == 0)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return true;
                        }

                        MainPanel.Update();
                        Message("Searching...");
                        FindResult p = Find(t, CurrentFileStream.Position + 1);
                        
                        switch (p.Error)
                        {
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
                                Goto(p.Position - 1);
                                Message($"Found {t} at position {p.Position - 1}");
                                break;
                        }
                    }
                    return true;

                // Goto
                case ConsoleKey.G:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFile.Length)
                        {
                            Message("Already at the end of the file.");
                            return true;
                        }

                        long? t = Utils.GetNumberFromUser("Goto:", MainPanel.ScreenMaxBytes, CurrentFile.Length);

                        if (t == null)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return true;
                        }

                        if (t >= 0 && t <= CurrentFile.Length - MainPanel.ScreenMaxBytes)
                        {
                            Goto((long)t);
                        }
                        else
                        {
                            MainPanel.Update();
                            Message("Position out of bound!");
                        }
                    }
                    return true;

                // Offset base
                case ConsoleKey.O:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        string c = Utils.GetUserInput("Hex|Dec|Oct?:", MainPanel.ScreenMaxBytes, CurrentFile.Length);

                        if (c == null || c.Length < 1)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return true;
                        }

                        switch (c[0])
                        {
                            case 'H':
                            case 'h':
                                CurrentOffsetBaseView = OffsetBaseView.Hexadecimal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return true;

                            case 'O':
                            case 'o':
                                CurrentOffsetBaseView = OffsetBaseView.Octal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return true;

                            case 'D':
                            case 'd':
                                CurrentOffsetBaseView = OffsetBaseView.Decimal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return true;

                            default:
                                Message("Invalid view mode!");
                                MainPanel.Update();
                                return true;
                        }
                    }
                    return true;

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
                    return true;

                // Info
                case ConsoleKey.I:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        Message($"Size: {Utils.GetFormattedSize(CurrentFile.Length)}");
                    }
                    return true;

                // Exit
                case ConsoleKey.X:
                    if (input.Modifiers == ConsoleModifiers.Control)
                        return Exit();
                    return true;

                // Dump
                case ConsoleKey.D:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Dumping...");
                        Dump();
                        Message("Dumping done!");
                    }
                    return true;

                // -- Data nagivation --
                case ConsoleKey.LeftArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position - 1 >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - 1);
                        }
                    }
                    return true;
                case ConsoleKey.RightArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position + MainPanel.ScreenMaxBytes + 1 <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position + 1);
                        }
                    }
                    return true;

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
                    return true;
                case ConsoleKey.DownArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position + MainPanel.ScreenMaxBytes + MainPanel.BytesInRow <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position + MainPanel.BytesInRow);
                        }
                        else
                        {
                            if (MainPanel.ScreenMaxBytes < CurrentFile.Length)
                                ReadFileAndUpdate(CurrentFile.Length - MainPanel.ScreenMaxBytes);
                        }
                    }
                    return true;

                case ConsoleKey.PageUp:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position - MainPanel.ScreenMaxBytes >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - MainPanel.ScreenMaxBytes);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    }
                    return true;
                case ConsoleKey.PageDown:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFileStream.Position + (MainPanel.ScreenMaxBytes * 2) <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position += MainPanel.ScreenMaxBytes);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position = CurrentFile.Length - MainPanel.ScreenMaxBytes);
                        }
                    }
                    return true;

                case ConsoleKey.Home:
                    if (CurrentWritingMode == OperatingMode.Read)
                        ReadFileAndUpdate(0);
                    return true;
                case ConsoleKey.End:
                    if (CurrentWritingMode == OperatingMode.Read)
                        ReadFileAndUpdate(CurrentFile.Length - MainPanel.ScreenMaxBytes);
                    return true;
            }

            return true;
        }

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
            Console.Write(new string(' ', Console.WindowWidth - 1));

            string msg = $"[ {pMessage} ]";
            Console.SetCursorPosition((Console.WindowWidth / 2) - (msg.Length / 2),
                InfoPanel.Position);

            Utils.ToggleColors();

            Console.Write(msg);

            Console.ResetColor();
        }

        /// <summary>
        /// Toggle the fullscreen mode.
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

        static ErrorCode Dump()
        {
            return Dump(CurrentFile.Name, MainPanel.BytesInRow, CurrentOffsetBaseView);
        }

        static public ErrorCode Dump(string pFileToDump, int pBytesInRow, OffsetBaseView pViewMode)
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

        static ErrorCode DumpFile(FileStream pIn, StreamWriter pOut, OffsetBaseView pViewMode, int pBytesInRow)
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
                    case OffsetBaseView.Hexadecimal:
                        t = $"{line:X8}  ";
                        break;

                    case OffsetBaseView.Decimal:
                        t = $"{line:D8}  ";
                        break;

                    case OffsetBaseView.Octal:
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
        /// Find a byte starting at the CurrentFilePosition and
        /// </summary>
        /// <param name="pData">Data as a byte.</param>
        /// <returns>Positon.</returns>
        static long Find(byte pData)
        {
           return Find(pData, CurrentFileStream.Position);
        }

        /// <summary>
        /// Find a byte in the current file and
        /// return its found position.
        /// </summary>
        /// <param name="pData">Data as a byte.</param>
        /// <param name="pPosition">Positon to start searching from.</param>
        /// <returns>Found positon.</returns>
        static long Find(byte pData, long pPosition)
        {
            if (pPosition < 0 || pPosition > CurrentFile.Length)
                return (int)ErrorCode.PositionOutOfBound;

            if (!CurrentFile.Exists)
                return (int)ErrorCode.FileNotFound;

            using (FileStream fs = CurrentFile.OpenRead())
            {
                fs.Position = pPosition;

                bool Continue = true;
                while (Continue)
                {
                    if (pData == (byte)fs.ReadByte())
                        return fs.Position;

                    if (fs.Position >= fs.Length)
                        Continue = false;
                }
            }

            return (int)ErrorCode.FindNoResult;
        }

        /// <summary>
        /// Find a string of data.
        /// </summary>
        /// <param name="pData">Data as a string.</param>
        /// <param name="pEncoding">Encoding.</param>
        /// <returns>Position.</returns>
        static FindResult FindData(string pData)
        {
            return Find(pData, CurrentFileStream.Position);
        }

        /// <summary>
        /// Find a string of data with a given position.
        /// </summary>
        /// <param name="pData">Data as a string.</param>
        /// <param name="pPosition">Starting position.</param>
        /// <param name="pEncoding">Encoding.</param>
        /// <returns>Found position.</returns>
        /// <remarks>
        /// How does this work?
        /// Search every character, if one seems to be right.
        /// Read the data and compare it.
        /// </remarks>
        static FindResult Find(string pData, long pPosition)
        {
            if (pPosition < 0 || pPosition > CurrentFile.Length)
                return new FindResult(ErrorCode.PositionOutOfBound);

            if (!CurrentFile.Exists)
                return new FindResult(ErrorCode.FileNotFound);

            if (string.IsNullOrWhiteSpace(pData))
                return new FindResult(ErrorCode.FindEmptyString);
            
            byte[] b = new byte[pData.Length];
            bool Continue = true;
            while (Continue)
            {
                if (CurrentFileStream.Position + pData.Length > CurrentFileStream.Length)
                    Continue = false;

                if (pData[0] == (char)CurrentFileStream.ReadByte())
                {
                    if (pData.Length == 1)
                    {
                        CurrentFileStream.Position--;
                        return new FindResult(CurrentFileStream.Position + 1);
                    }
                    else
                    {
                        CurrentFileStream.Read(b, 0, b.Length);
                        if (pData == Encoding.ASCII.GetString(b))
                        {
                            CurrentFileStream.Position = CurrentFileStream.Position - pData.Length - 1;
                            return new FindResult(CurrentFileStream.Position + 1);
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
        static class TitlePanel
        {
            static internal void Update()
            {
                Utils.ToggleColors();

                Console.SetCursorPosition(0, 0);

                if (CurrentFile.Name.Length <= Console.WindowWidth)
                {
                    Console.Write(CurrentFile.Name);
                    Console.Write(new string(' ', Console.WindowWidth - CurrentFile.Name.Length));
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
                    if (Fullscreen)
                        return 1;
                    else
                        return 2;
                }
            }

            /// <summary>
            /// Gets the heigth of the main panel.
            /// </summary>
            static internal int FrameHeight
            {
                get
                {
                    if (Fullscreen)
                        return Console.WindowHeight - 2;
                    else
                        return Console.WindowHeight - 5;
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
            static internal int ScreenMaxBytes
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
                long l = CurrentFile.Length;

                int BufferOffsetData = 0;
                int BufferOffsetText = 0;

                string t = string.Empty;
                
                OffsetPanel.Update();
                
                Console.SetCursorPosition(0, Position);

                for (int line = 0; line < FrameHeight; line++)
                {
                    switch (CurrentOffsetBaseView)
                    {
                        case OffsetBaseView.Hexadecimal:
                            t = $"{((line * BytesInRow) + CurrentFileStream.Position):X8}  ";
                            break;

                        case OffsetBaseView.Decimal:
                            t = $"{((line * BytesInRow) + CurrentFileStream.Position):D8}  ";
                            break;

                        case OffsetBaseView.Octal:
                            t = $"{ToOct((line * BytesInRow) + CurrentFileStream.Position)}  ";
                            break;
                    }

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (CurrentFileStream.Position + BufferOffsetData < l)
                            t += $"{Buffer[BufferOffsetData]:X2} ";
                        else
                            t += "   ";

                        BufferOffsetData++;
                    }

                    t += " ";

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (CurrentFileStream.Position + BufferOffsetText < l)
                            t += $"{Buffer[BufferOffsetText].ToSafeChar()}";
                        else
                        {
                            Console.Write(t);
                            return;
                        }

                        BufferOffsetText++;
                    }

                    t += " ";

                    Console.WriteLine(t);
                }
            }

            static internal void Refresh()
            {
                Clear();
                ReadFileAndUpdate(CurrentFileStream.Position);
            }

            static internal void Clear()
            {
                Console.SetCursorPosition(0, Position);
                int i = 0;
                for (int line = Position; i < FrameHeight; line++)
                {
                    Console.SetCursorPosition(0, line);
                    Console.Write(new string(' ', Console.WindowWidth));
                    i++;
                }
            }
        }
        #endregion

        #region InfoPanel
        /// <summary>
        /// Info panel: Offsets and current offsets (positions) are shown.
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
                    if (Fullscreen)
                        return Console.WindowHeight - 1;
                    else
                        return Console.WindowHeight - 3;
                }
            }

            /// <summary>
            /// Update the offset information
            /// </summary>
            static internal void Update()
            {
                decimal r =
                    Math.Round(((CurrentFileStream.Position +
                    (decimal)(CurrentFile.Length < MainPanel.ScreenMaxBytes ? CurrentFile.Length : MainPanel.ScreenMaxBytes))
                    / CurrentFile.Length) * 100);

                string s = $"  DEC: {CurrentFileStream.Position:D8} | HEX: {CurrentFileStream.Position:X8} | OCT: {ToOct(CurrentFileStream.Position)} | POS: {r}%";

                Console.SetCursorPosition(0, Position);
                Console.Write(s + new string(' ', Console.WindowWidth - s.Length - 1)); // Force-clear any messages
            }
        }
        #endregion

        #region OffsetPanel
        /// <summary>
        /// Shows offset base view and the offset on each byte.
        /// e.g. Offset h  00 01 ...
        /// </summary>
        static class OffsetPanel
        {
            static internal int Position = 1;

            /// <summary>
            /// Update the offset map
            /// </summary>
            static internal void Update()
            {
                string t = $"Offset {CurrentOffsetBaseView.GetChar()}  ";

                if (CurrentFileStream.Position > uint.MaxValue)
                    t += " ";

                for (int i = 0; i < MainPanel.BytesInRow;)
                {
                    t += $"{i++:X2} ";
                }

                Console.SetCursorPosition(0, Position);
                Console.Write(t);
            }
        }
        #endregion

        #region ControlPanel
        static class ControlPanel
        {
            /// <summary>
            /// Places the control map on screen (e.g. ^T Try jumping)
            /// </summary>
            static internal void Place()
            {
                Console.SetCursorPosition(0, Console.WindowHeight - 2);

                WriteWhite("^ ");
                Console.Write("              ");

                WriteWhite("^W");
                Console.Write(" Find byte    ");

                WriteWhite("^J");
                Console.Write(" Find data    ");

                WriteWhite("^G");
                Console.Write(" Goto         ");

                WriteWhite("^H");
                Console.Write(" Replace");

                // CHANGING LINE BOYS
                Console.WriteLine();

                WriteWhite("^X");
                Console.Write(" Exit         ");

                WriteWhite("^I");
                Console.Write(" Info         ");

                WriteWhite("^D");
                Console.Write(" Dump         ");

                WriteWhite("^O");
                Console.Write(" Offset base  ");

                WriteWhite("^E");
                Console.Write(" Edit mode");
            }
        }

        static void WriteWhite(string pText)
        {
            Utils.ToggleColors();
            Console.Write(pText);
            Console.ResetColor();
        }
        #endregion
        #endregion

        #region Type extensions
        static string ToOct(this long c)
        {
            if (c > int.MaxValue)
                return $"{Convert.ToString(c, 8).FillZeros(16),16}";
            else
                return $"{Convert.ToString(c, 8).FillZeros(8),8}";
        }

        /// <summary>
        /// Returns a printable character if found.
        /// </summary>
        /// <param name="pIn">Byte to transform.</param>
        /// <returns>Readable character.</returns>
        static char ToSafeChar(this byte pIn)
        {
            if (pIn < 0x20 || pIn > 0x7E)
                return '.';
            else
                return (char)pIn;
        }

        /// <summary>
        /// Gets the character for the upper bar depending on the
        /// offset base view.
        /// </summary>
        /// <param name="pObject">This <see cref="OffsetBaseView"/></param>
        /// <returns>Character.</returns>
        static char GetChar(this OffsetBaseView pObject)
        {
            switch (pObject)
            {
                case OffsetBaseView.Hexadecimal:
                    return 'h';
                case OffsetBaseView.Decimal:
                    return 'd';
                case OffsetBaseView.Octal:
                    return 'o';
                default:
                    return '?'; // ??????????
            }
        }

        static string FillZeros(this string pString, int pLength)
        {
            if (pLength < pString.Length)
                throw new FormatException("FillZeros(this string, int) - Length");

            return new string('0', pLength - pString.Length) + pString;
        }
        #endregion
    }
}
