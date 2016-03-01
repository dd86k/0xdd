using System;
using System.IO;
using System.Text;

//TODO: Edit mode
// 0E 0F
// D2 DD ..
// ^^    ^
// highlighted (gray on black) while navigating

//TODO: Consider if we want to keep the file open in a SteamReader in memory
// for faster access (instead of re-opening the open over and over)

/*
    Box of ideas (Lazy TODO/idea list)
    - Top right (under title): Insert(INS)/Overwrite(OVR) (bool)
    - Search: /regex/ (Begins with && ends with '/')
    - Edit: Dictionary<long, byte> (FilePosition, NewByte)
      - Rendering: If byte at position, write that byte to display instead
      - Saving: Remove duplicates, loop through List and write
      - Editing: If new data on same position, replace
*/

namespace _0xdd
{
    enum ErrorCode : byte
    {
        Success = 0,

        FileNotFound = 0x4,
        FileUnreadable = 0x5,

        FindNoResult = 0x8,

        PositionOutOfBound = 0x16,

        UnknownError = 0xFF
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
        /// Current position in the file.
        /// </summary>
        static long CurrentFilePosition;

        /// <summary>
        /// Information about the current file.
        /// </summary>
        static FileInfo CurrentFile;

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
        static byte[] Buffer = new byte[0];

        /// <summary>
        /// If the user is in fullscreen mode, false by default.
        /// </summary>
        static bool Fullscreen;

        // Last window sizes.
        static int LastWindowHeight;
        static int LastWindowWidth;

        static FileStream CurrentFileStream;
        #endregion

        #region TitlePanel
        static class TitlePanel
        {
            static internal void Update()
            {
                Utils.ToggleColors();

                Console.SetCursorPosition(0, 0);
                Console.Write(CurrentFile.Name);
                Console.Write(new string(' ', Console.WindowWidth - CurrentFile.Name.Length));

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
            /// Position to start rendering on the console (Y axis).
            /// </summary>
            static internal int TopPosition = 2;

            /// <summary>
            /// Gets or sets the heigth of the main panel.
            /// </summary>
            static internal int FrameHeight
            {
                get
                {
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
                
                Console.SetCursorPosition(0, TopPosition);

                for (int line = 0; line < FrameHeight; line++)
                {
                    switch (CurrentOffsetBaseView)
                    {
                        case OffsetBaseView.Hexadecimal:
                            t = $"{((line * BytesInRow) + CurrentFilePosition):X8}  ";
                            break;

                        case OffsetBaseView.Decimal:
                            t = $"{((line * BytesInRow) + CurrentFilePosition):D8}  ";
                            break;

                        case OffsetBaseView.Octal:
                            t = $"{ToOct((line * BytesInRow) + CurrentFilePosition)}  ";
                            break;
                    }

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (CurrentFilePosition + BufferOffsetData < l)
                            t += $"{Buffer[BufferOffsetData]:X2} ";
                        else
                            t += "   ";

                        BufferOffsetData++;
                    }

                    t += " ";

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (CurrentFilePosition + BufferOffsetText < l)
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
                ReadFileAndUpdate(CurrentFilePosition);
            }

            static internal void Clear()
            {
                Console.SetCursorPosition(0, TopPosition);
                int i = 0;
                for (int line = TopPosition; i < FrameHeight; line++)
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
                    return Console.WindowHeight - 3;
                }
            }

            /// <summary>
            /// Update the offset information
            /// </summary>
            static internal void Update()
            {
                Console.SetCursorPosition(0, Position);
                string s = $"  DEC: {CurrentFilePosition:D8} | HEX: {CurrentFilePosition:X8} | OCT: {ToOct(CurrentFilePosition)}";

                Console.Write(s + new string(' ', Console.WindowWidth - s.Length - 1));
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
            /// <summary>
            /// Update the offset map
            /// </summary>
            static internal void Update()
            {
                string t = $"Offset {CurrentOffsetBaseView.GetChar()}  ";

                if (CurrentFilePosition > uint.MaxValue)
                    t += " ";

                for (int i = 0; i < MainPanel.BytesInRow;)
                {
                    t += $"{i++:X2} ";
                }

                Console.SetCursorPosition(0, 1);
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

        #region Enumerations

        /// <summary>
        /// Current <see cref="OffsetBaseView"/>.
        /// </summary>
        static OffsetBaseView CurrentOffsetBaseView;


        /// <summary>
        /// Current <see cref="OperatingMode"/>. Read by default.
        /// </summary>
        static OperatingMode CurrentWritingMode = OperatingMode.Read;
        #endregion

        #region Methods
        internal static int Open(string pFilePath)
        {
            return Open(pFilePath, OffsetBaseView.Hexadecimal, Utils.GetBytesInRow());
        }
        
        internal static int Open(string pFilePath, OffsetBaseView pOffsetViewMode, int pBytesRow)
        {
            if (!File.Exists(pFilePath))
                return (int)ErrorCode.FileNotFound;
            
            MainPanel.BytesInRow = pBytesRow;

            Console.CursorVisible = false;

            CurrentOffsetBaseView = pOffsetViewMode;
            LastWindowHeight = Console.WindowHeight;
            LastWindowWidth = Console.WindowWidth;

            CurrentFile = new FileInfo(pFilePath);

            CurrentFileStream = CurrentFile.Open(FileMode.Open); // Open, for now

            Buffer = new byte[CurrentFile.Length < MainPanel.ScreenMaxBytes ?
                     (int)CurrentFile.Length : MainPanel.ScreenMaxBytes];

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
            MainPanel.BytesInRow = Utils.GetBytesInRow();

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
        }

        /// <summary>
        /// Read the user's key.
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
                    MainPanel.Refresh();
                    return true;

                case ConsoleKey.F10:
                    ToggleFullscreenMode();
                    return true;

                // -- Shown shortcuts --

                // Find byte
                case ConsoleKey.W:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        long? t = Utils.GetNumberFromUser("Find byte:", MainPanel.ScreenMaxBytes, CurrentFile.Length);

                        if (t == null)
                        {
                            MainPanel.Update();
                            return true;
                        }

                        if (t < 0 || t > byte.MaxValue)
                        {
                            MainPanel.Update();
                            Message("A byte is a value between 0 and 255.");
                        }
                        else
                        {
                            if (CurrentFilePosition >= CurrentFile.Length)
                            {
                                Message("Already at the end of the file.");
                                return true;
                            }

                            MainPanel.Update();
                            Message("Searching...");
                            long p = Find((byte)t, CurrentFilePosition + 1);

                            if (p > 1)
                            {
                                Message("Data could not be found.");
                            }
                            else
                            {
                                Goto(p - 1);
                                Message($"Found {t} at position {p - 1}");
                            }
                        }
                    }
                    return true;

                // Find data
                case ConsoleKey.J:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        string t = Utils.GetUserInput("Find data:", MainPanel.ScreenMaxBytes, CurrentFile.Length);

                        if (t == null || t.Length == 0)
                        {
                            MainPanel.Update();
                            return true;
                        }

                        if (CurrentFilePosition >= CurrentFile.Length)
                        {
                            Message("Already at the end of the file.");
                            return true;
                        }

                        Message("Searching...");
                        FindResult p = Find(t, CurrentFilePosition + 1, Utils.GetEncoding(CurrentFile.Name));
                        
                        switch (p.Error)
                        {
                            case ErrorCode.FileNotFound:
                                Message("Data could not be found.");
                                MainPanel.Update();
                                break;
                            case ErrorCode.FileUnreadable:
                                Message("Data could not be found.");
                                MainPanel.Update();
                                break;
                            case ErrorCode.FindNoResult:
                                Message("Data could not be found.");
                                MainPanel.Update();
                                break;
                            case ErrorCode.PositionOutOfBound:
                                Message("Data could not be found.");
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
                        long? t = Utils.GetNumberFromUser("Goto:", MainPanel.ScreenMaxBytes, CurrentFile.Length);

                        if (t == null)
                        {
                            MainPanel.Update();
                            return true;
                        }

                        if (t >= 0 && t <= CurrentFile.Length - MainPanel.ScreenMaxBytes)
                            Goto((long)t);
                        else
                        {
                            Message("Position out of bound!");
                            MainPanel.Update();
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
                            Message("Field was empty!");
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

                // Replace
                case ConsoleKey.H:
                    if (input.Modifiers == ConsoleModifiers.Control)
                        throw new NotImplementedException();
                    return true;

                // Info
                case ConsoleKey.I:
                    if (input.Modifiers == ConsoleModifiers.Control)
                    {
                        decimal ratioStart = Math.Round((decimal)CurrentFilePosition / CurrentFile.Length * 100);
                        decimal max = CurrentFile.Length < MainPanel.ScreenMaxBytes ? CurrentFile.Length : MainPanel.ScreenMaxBytes;
                        decimal ratioEnd = Math.Round(((CurrentFilePosition + max) / CurrentFile.Length) * 100);
                        Message($"Size: {Utils.GetFormattedSize(CurrentFile.Length)} | Position: {ratioStart}~{ratioEnd}%");
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
                        if (CurrentFilePosition - 1 >= 0)
                        {
                            ReadFileAndUpdate(--CurrentFilePosition);
                        }
                    }
                    return true;
                case ConsoleKey.RightArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFilePosition + MainPanel.ScreenMaxBytes + 1 <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(++CurrentFilePosition);
                        }
                    }
                    return true;

                case ConsoleKey.UpArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFilePosition - MainPanel.BytesInRow >= 0)
                        {
                            ReadFileAndUpdate(CurrentFilePosition -= MainPanel.BytesInRow);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFilePosition = 0);
                        }
                    }
                    return true;
                case ConsoleKey.DownArrow:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFilePosition + MainPanel.ScreenMaxBytes + MainPanel.BytesInRow <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFilePosition += MainPanel.BytesInRow);
                        }
                        else
                        {
                            if (MainPanel.ScreenMaxBytes < CurrentFile.Length)
                                ReadFileAndUpdate(CurrentFilePosition = CurrentFile.Length - MainPanel.ScreenMaxBytes);
                        }
                    }
                    return true;

                case ConsoleKey.PageUp:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFilePosition - MainPanel.ScreenMaxBytes >= 0)
                        {
                            ReadFileAndUpdate(CurrentFilePosition -= MainPanel.ScreenMaxBytes);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFilePosition = 0);
                        }
                    }
                    return true;
                case ConsoleKey.PageDown:
                    if (CurrentWritingMode == OperatingMode.Read)
                    {
                        if (CurrentFilePosition + (MainPanel.ScreenMaxBytes * 2) <= CurrentFile.Length)
                        {
                            ReadFileAndUpdate(CurrentFilePosition += MainPanel.ScreenMaxBytes);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFilePosition = CurrentFile.Length - MainPanel.ScreenMaxBytes);
                        }
                    }
                    return true;

                case ConsoleKey.Home:
                    if (CurrentWritingMode == OperatingMode.Read)
                        ReadFileAndUpdate(CurrentFilePosition = 0);
                    return true;
                case ConsoleKey.End:
                    if (CurrentWritingMode == OperatingMode.Read)
                        ReadFileAndUpdate(CurrentFilePosition = CurrentFile.Length - MainPanel.ScreenMaxBytes);
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
                MainPanel.TopPosition = 2;
                //MainPanel.FrameHeight = Console.WindowHeight - 5;
                //InfoPanel.TopPosition = Console.WindowHeight - 3;
                Fullscreen = false;
                Console.Clear();
                ControlPanel.Place();
                OffsetPanel.Update();
                TitlePanel.Update();
                MainPanel.Refresh();
            }
            else
            { // Turning on
                MainPanel.TopPosition = 1;
                //MainPanel.FrameHeight = Console.WindowHeight - 2;
                //InfoPanel.TopPosition = Console.WindowHeight - 1;
                Fullscreen = true;
                Console.Clear();
                TitlePanel.Update();
                MainPanel.Refresh();
            }
        }

        static void Goto(long pPosition)
        {
            ReadFileAndUpdate(CurrentFilePosition = pPosition);
        }

        static void Dump()
        {
            Dump(CurrentFile.Name, MainPanel.BytesInRow, CurrentOffsetBaseView, true);
        }

        static public int Dump(string pFileToDump, int pBytesInRow, OffsetBaseView pViewMode, bool pInApp)
        {
            if (!File.Exists(pFileToDump))
                return 1;
            
            // These variables are independant because I do
            // not want to mess with the global ones and it
            // may happen the user /d directly.
            FileInfo file = new FileInfo(pFileToDump);
            long filelen = file.Length;
            long line = 0;
            int BufferPositionHex = 0;
            int BufferPositionData = 0;
            Buffer = new byte[pBytesInRow];

            using (StreamWriter sw = new StreamWriter($"{pFileToDump}.{EXTENSION}"))
            {
                sw.AutoFlush = true;

                sw.WriteLine(file.Name);
                sw.WriteLine();
                sw.WriteLine($"Size: {Utils.GetFormattedSize(filelen)}");
                sw.WriteLine($"Attributes: {file.Attributes}");
                sw.WriteLine($"File date: {file.CreationTime}");
                sw.WriteLine($"Dump date: {DateTime.Now}");
                sw.WriteLine();

                sw.Write($"Offset {pViewMode.GetChar()}  ");
                for (int i = 0; i < pBytesInRow; i++)
                {
                    sw.Write($"{i:X2} ");
                }
                sw.WriteLine();

                string t = string.Empty;
                using (FileStream fs = file.OpenRead())
                {
                    bool finished = false;

                    while (!finished)
                    {
                        if (line / filelen % 10 == 0)
                        {
                            //TODO: Progress
                            if (pInApp)
                                Message("");
                            else
                                Console.Write("");
                        }

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

                        fs.Read(Buffer, 0, pBytesInRow);

                        for (int pos = 0; pos < pBytesInRow; pos++)
                        {
                            if (BufferPositionHex < filelen)
                                t += $"{Buffer[pos]:X2} ";
                            else
                                t += "   ";

                            BufferPositionHex++;
                        }

                        t += " ";

                        for (int pos = 0; pos < pBytesInRow; pos++)
                        {
                            if (BufferPositionData < filelen)
                                t += $"{Buffer[pos].ToSafeChar()}";
                            else
                                return 0; // Done!

                            BufferPositionData++;
                        }

                        sw.WriteLine(t);
                    }
                }
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
           return Find(pData, CurrentFilePosition);
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
        static FindResult Find(string pData, Encoding pEncoding)
        {
            return Find(pData, CurrentFilePosition, pEncoding);
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
        static FindResult Find(string pData, long pPosition, Encoding pEncoding)
        {
            if (pPosition < 0 || pPosition > CurrentFile.Length)
                return new FindResult(ErrorCode.PositionOutOfBound);

            if (!CurrentFile.Exists)
                return new FindResult(ErrorCode.FileNotFound);

            using (FileStream fs = CurrentFile.OpenRead())
            {
                fs.Position = pPosition;

                bool Continue = true;
                byte[] buffer = new byte[pData.Length];
                int stringlength = pData.Length;
                while (Continue)
                {
                    if (fs.Position + stringlength > fs.Length)
                        Continue = false;

                    if (pData[0] == (char)fs.ReadByte())
                    {
                        if (stringlength == 1)
                            return new FindResult(fs.Position - 1);
                        else
                        {
                            fs.Read(buffer, 0, stringlength);
                            if (pData == pEncoding.GetString(buffer))
                            {
                                return new FindResult(fs.Position - stringlength - 1);
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

        #region
        static string ToOct(long c)
        {
            if (c > int.MaxValue)
                return $"{Convert.ToString(c, 8).FillZeros(16),16}";
            else
                return $"{Convert.ToString(c, 8).FillZeros(8),8}";
        }
        #endregion

        #region Type extensions
        /// <summary>
        /// Returns a printable character if found.
        /// </summary>
        /// <param name="pIn">Byte to transform.</param>
        /// <returns>Readable character.</returns>
        static char ToSafeChar(this byte pIn)
        {
            // If out of bound.
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
                throw new FormatException();

            return new string('0', pLength - pString.Length) + pString;
        }
        #endregion
    }
}
