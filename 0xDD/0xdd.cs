﻿using System;
using System.IO;
using System.Text;

//TODO: Hashing (with menu)

//TODO: Implement different display formats
/*
- ASCII
. . . H . . 8 . . . . . . . . . H . . H . X . H . p . H . x   U H
- Bit
11001100 11101011 00000000 01001000 10000011 11000100 00111000
- Bytes (Default, already there)
cc eb 00 48 83 c4 38 c3 cc cc cc cc cc cc cc cc  ...H..8.........
- Long
1208019916 -1019689853  -858993460  -858993460  1220840264
- Long Hex
4800ebcc c338c483 cccccccc cccccccc 48c48b48 48105889 48187089
- Long Unsigned
1208019916 3275277443 3435973836 3435973836 1220840264 1209030793
- Short
-5172  18432 -15229 -15560 -13108 -13108 -13108 -13108 -29880
- Short Hex
ebcc 4800 c483 c338 cccc cccc cccc cccc 8b48 48c4 5889 4810 7089
- Short Unsigned
60364 18432 50307 49976 52428 52428 52428 52428 35656 18628 22665
- Unicode (Dump only)
. 䠀 쒃 쌸 쳌 쳌 쳌 쳌 譈 䣄 墉 䠐 炉 䠘 碉 唠 赈 . . 䣿 . . . 譈 댅 . 䠀 쐳 襈 . . 䰀 .
*/

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
    /*
    enum DisplayFormat : byte
    {
        ASCII,
        Bit,
        Bytes, // Default
        Long,
        LongHex,
        LongUnsigned,
        Short,
        ShortHex,
        ShortUnsigned,
        Unicode
    }
    */
    enum OffsetBaseView : byte
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
                if (Error == ErrorCode.Success)
                    return true;
                else
                    return false;
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

        /// <summary>
        /// Active buffer used for on-screen display.
        /// </summary>
        /// <remarks>
        /// Do not worry, this doesn't use a lot of memory.
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

            if (pBytesRow > 0)
            {
                MainPanel.BytesInRow = pBytesRow;
            }
            else
            {
                AutoSize = true;
                MainPanel.BytesInRow = Utils.GetBytesInRow();
            }

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
        /// Prepares the screen with the information needed.
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

                        long? t = Utils.GetNumberFromUser("Find byte:");

                        if (t == null)
                        {
                            MainPanel.Update();
                            return;
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
                            FindResult p = Find((byte)t, CurrentFileStream.Position + 1);

                            switch (p.Error)
                            {
                                case ErrorCode.Success:
                                    {
                                        Goto(--p.Position);
                                        if (p.Position > uint.MaxValue)
                                            Message($"Found 0x{t:X2} at {p.Position:X16}");
                                        else
                                            Message($"Found 0x{t:X2} at {p.Position:X8}");
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

                        string t = Utils.GetUserInput("Find data:");

                        if (t == null || t.Length == 0)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }

                        MainPanel.Update();
                        Message("Searching...");
                        FindResult p = Find(t, CurrentFileStream.Position + 1);
                        
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
                        string c = Utils.GetUserInput("Hex|Dec|Oct?:");

                        if (c == null || c.Length < 1)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }

                        switch (c[0])
                        {
                            case 'H':
                            case 'h':
                                CurrentOffsetBaseView = OffsetBaseView.Hexadecimal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            case 'O':
                            case 'o':
                                CurrentOffsetBaseView = OffsetBaseView.Octal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            case 'D':
                            case 'd':
                                CurrentOffsetBaseView = OffsetBaseView.Decimal;
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
        /// <param name="pViewMode"><see cref="OffsetBaseView"/> to use.</param>
        /// <returns><see cref="ErrorCode"/></returns>
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
        
        /// <remarks>
        /// Only to be used with <see cref="Dump()"/>!
        /// </remarks>
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
            return Find(pData, CurrentFileStream.Position);
        }

        /// <summary>
        /// Find a string of data with a given position.
        /// </summary>
        /// <param name="pData">Data as a string.</param>
        /// <param name="pPosition">Starting position.</param>
        /// <returns><see cref="FindResult"/></returns>
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

            CurrentFileStream.Position = pPosition;

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
                        return new FindResult(CurrentFileStream.Position - 1);
                    }
                    else
                    {
                        CurrentFileStream.Position--;
                        CurrentFileStream.Read(b, 0, b.Length);
                        if (pData == Encoding.ASCII.GetString(b))
                        {
                            CurrentFileStream.Position = CurrentFileStream.Position - pData.Length;

                            FindResult f = new FindResult(ErrorCode.Success);
                            f.Position = CurrentFileStream.Position;

                            return f;
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
                        case OffsetBaseView.Hexadecimal:
                            t = new StringBuilder($"{((line * BytesInRow) + CurrentFileStream.Position):X8}  ");
                            break;

                        case OffsetBaseView.Decimal:
                            t = new StringBuilder($"{((line * BytesInRow) + CurrentFileStream.Position):D8}  ");
                            break;

                        case OffsetBaseView.Octal:
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
                //TODO: Make a property to "buffer" the number of rendered characters
                // -> CurrentFile.Length < MainPanel.MaxBytes ? CurrentFile.Length : MainPanel.MaxBytes
                decimal r =
                    Math.Round(((CurrentFileStream.Position +
                    (decimal)(CurrentFile.Length < MainPanel.MaxBytes ? CurrentFile.Length : MainPanel.MaxBytes))
                    / CurrentFile.Length) * 100);

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
            /// <summary>
            /// Places the control map on screen (e.g. ^T Try jumping and etc.)
            /// </summary>
            static internal void Place()
            {
                //TODO: Adjust Place() depending on screen width
                // Place the most important actions first

                Console.SetCursorPosition(0, Console.WindowHeight - 2);

                WriteWhite("^U");
                Console.Write(" Display type ");

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

                WriteWhite("^O");
                Console.Write(" Offset base  ");
                
                WriteWhite("^I");
                Console.Write(" Info         ");

                WriteWhite("^D");
                Console.Write(" Dump         ");
                
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

        #region Small utils
        /// <summary>
        /// Generate a string with spaces with a desired length.
        /// </summary>
        /// <param name="l">Length</param>
        /// <returns>String</returns>
        static string s(int l) =>
            new string(' ', l);
        #endregion

        #region Type extensions
        /// <summary>
        /// Converts into an octal number.
        /// </summary>
        /// <param name="c">Number.</param>
        /// <returns>String.</returns>
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
        /// <returns>Console readable character.</returns>
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

        /// <summary>
        /// Fill zeros with a string.
        /// </summary>
        /// <param name="pString">Input.</param>
        /// <param name="pLength">Desired length.</param>
        /// <returns>String-zero-filed.</returns>
        static string FillZeros(this string pString, int pLength) =>
            new string('0', pLength - pString.Length) + pString;
        #endregion
    }
}