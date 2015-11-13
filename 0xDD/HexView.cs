using System;
using System.Collections.Generic;
using System.IO;

//TODO: Edit mode
// 0E 0F
// D2 DD ..
// ^^    ^
// highlighted (gray on black) while navigating

//TODO: Resize on Window resize
//TODO: Menu on Dump Action, showing menu:
// from..
// - Dump file
// - Dump file from position
// - Dump file from position + length
// - Dump view
// to..
// - File
// - Clipboard

/*
    Box of ideas (Lazy TODO/idea list)
    - Top right (under title): Insert(INS)/Overwrite(OVR) (bool)
    - Search: /regex/ (Begins with && ends with)
    - Edit: List<int, byte>(FilePosition, Byte)
      - Rendering: If byte at position, write that byte to display instead
      - Saving: Remove duplicates, loop through List and write
      - Editing: If new data on same position, replace
*/

namespace _0xdd
{
    static class _0xdd
    {
        #region Constants
        /// <summary>
        /// Extension of data dump files.
        /// </summary>
        const string NAME_EXTENSION = "datdmp";
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
        /// Temporary buffer used for on-screen display.
        /// </summary>
        /// <remarks>
        /// This doesn't use a lot of memory.
        /// If the main panel size is 16x19 (16 bytes on 19 lines,
        /// default with 80x24 terminal), the buffer will only use
        /// 309 bytes (0.3 KB) of additional memory.
        /// </remarks>
        static byte[] Buffer = new byte[0];

        /// <summary>
        /// If the user is in fullscreen mode, false by default.
        /// </summary>
        static bool Fullscreen;
        #endregion

        #region TitlePanel
        struct TitlePanel
        {
            static internal void Update()
            {
                ToggleColors();

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
        struct MainPanel
        {
            /// <summary>
            /// Position to start rendering on the console (Y axis).
            /// </summary>
            static internal int StartingTopPosition = 2;

            /// <summary>
            /// Gets or sets the heigth of the main panel.
            /// </summary>
            static internal int FrameHeight = Console.WindowHeight - 5;

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
            /// Update the section of the screen with the data.
            /// </summary>
            static internal void Update()
            {
                long filelen = CurrentFile.Length;

                int BufferOffsetHex = 0;
                int BufferOffsetData = 0;

                Console.SetCursorPosition(0, StartingTopPosition);

                for (int line = 0; line < FrameHeight; line++)
                {
                    switch (CurrentOffsetBaseView)
                    {
                        case OffsetBaseView.Hexadecimal:
                            Console.Write($"{((line * BytesInRow) + CurrentFilePosition).ToString("X8")}  ");
                            break;

                        case OffsetBaseView.Decimal:
                            Console.Write($"{((line * BytesInRow) + CurrentFilePosition).ToString("00000000")}  ");
                            break;

                        case OffsetBaseView.Octal:
                            Console.Write($"{Convert.ToString((line * BytesInRow) + CurrentFilePosition, 8), 8}  ");
                            break;
                    }

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (CurrentFilePosition + BufferOffsetData < filelen)
                            Console.Write($"{Buffer[BufferOffsetData].ToString("X2")} ");
                        else
                            Console.Write("   ");

                        BufferOffsetData++;
                    }

                    Console.Write(" ");

                    for (int x = 0; x < BytesInRow; x++)
                    {
                        if (CurrentFilePosition + BufferOffsetHex < filelen)
                            Console.Write($"{Buffer[BufferOffsetHex].ToSafeChar()}");
                        else
                        {
                            // End rendering completely
                            x += BytesInRow;
                            line += FrameHeight;
                        }

                        BufferOffsetHex++;
                    }

                    Console.WriteLine();
                }
            }
            
            static internal void Refresh()
            {
                Clear();
                ReadAndUpdate(CurrentFilePosition);
            }

            static internal void Clear()
            {
                Console.SetCursorPosition(0, StartingTopPosition);
                for (int i = 0; i < FrameHeight; i++)
                {
                    Console.Write(new string(' ', Console.WindowWidth));
                }
            }
        }
        #endregion

        #region InfoPanel
        /// <summary>
        /// Info panel: Offsets and current offsets (positions) are shown.
        /// </summary>
        struct InfoPanel
        {
            /// <summary>
            /// Position to start rendering on the console (Y axis).
            /// </summary>
            static internal int StartingTopPosition = Console.WindowHeight - 3;

            /// <summary>
            /// Update the offset information
            /// </summary>
            static internal void Update()
            {
                Console.SetCursorPosition(0, StartingTopPosition);
                string s = $"DEC: {CurrentFilePosition.ToString("D8")} | HEX: {CurrentFilePosition.ToString("X8")} | OCT: {Convert.ToString(CurrentFilePosition, 8), 8}";
                // Force clean last message.
                Console.Write(s + new string(' ', Console.WindowWidth - s.Length - 1));
            }
        }
        #endregion

        #region OffsetPanel
        /// <summary>
        /// Shows offset base view and the offset on each byte.
        /// e.g. Offset h  00 01 ...
        /// </summary>
        struct OffsetPanel
        {
            /// <summary>
            /// Update the offset map
            /// </summary>
            static internal void Update()
            {
                Console.SetCursorPosition(0, 1);
                Console.Write($"Offset {CurrentOffsetBaseView.GetChar()}  ");
                for (int i = 0; i < MainPanel.BytesInRow; i++)
                {
                    Console.Write($"{i.ToString("X2")} ");
                }
            }
        }
        #endregion

        #region ControlPanel
        struct ControlPanel
        {
            /// <summary>
            /// Places the control map on screen (e.g. ^T Try jumping )
            /// </summary>
            static internal void Place()
            {
                Console.SetCursorPosition(0, Console.WindowHeight - 2);

                ToggleColors();
                Console.Write("^K");
                Console.ResetColor();
                Console.Write(" Help         ");

                ToggleColors();
                Console.Write("^W");
                Console.ResetColor();
                Console.Write(" Find byte    ");

                ToggleColors();
                Console.Write("^J");
                Console.ResetColor();
                Console.Write(" Find data    ");

                ToggleColors();
                Console.Write("^G");
                Console.ResetColor();
                Console.Write(" Goto         ");

                ToggleColors();
                Console.Write("^H");
                Console.ResetColor();
                Console.Write(" Replace");

                // CHANGING LINE BOYS
                Console.WriteLine();

                ToggleColors();
                Console.Write("^X");
                Console.ResetColor();
                Console.Write(" Exit         ");

                ToggleColors();
                Console.Write("^I");
                Console.ResetColor();
                Console.Write(" Info         ");

                ToggleColors();
                Console.Write("^D");
                Console.ResetColor();
                Console.Write(" Dump         ");

                ToggleColors();
                Console.Write("^O");
                Console.ResetColor();
                Console.Write(" Offset base  ");
                
                ToggleColors();
                Console.Write("^E");
                Console.ResetColor();
                Console.Write(" Edit mode");
            }
        }
        #endregion

        #region EditingMode
        struct Edit
        {
            static internal void MoveLeft()
            {

            }
            static internal void MoveRight()
            {

            }
            static internal void MoveUp()
            {

            }
            static internal void MoveDown()
            {

            }
            static void Render()
            {

            }
        }
        #endregion

        #region Enumerations
        /// <summary>
        /// Enumeration of the different offset base views.
        /// </summary>
        internal enum OffsetBaseView : byte
        {
            Hexadecimal,
            Decimal,
            Octal
        }

        /// <summary>
        /// Current <see cref="OffsetBaseView"/>.
        /// </summary>
        static OffsetBaseView CurrentOffsetBaseView;

        /// <summary>
        /// Writing mode enumeration.
        /// </summary>
        enum WritingMode : byte
        {
            /// <summary>
            /// Normal mode. READ
            /// </summary>
            Read,
            /// <summary>
            /// Overwrite value. OVRW
            /// </summary>
            Overwrite,
            /// <summary>
            /// Insert before value. INSR
            /// </summary>
            Insert
        }

        /// <summary>
        /// Current <see cref="WritingMode"/>. Read by default.
        /// </summary>
        static WritingMode CurrentWritingMode = WritingMode.Read;
        #endregion

        #region Methods
        /// <summary>
        /// Open a file and starts the program with defaults.
        /// </summary>
        /// <param name="pFilePath">Path to the file.</param>
        internal static void Open(string pFilePath)
        {
            Open(pFilePath, OffsetBaseView.Hexadecimal, 16);
        }

        /// <summary>
        /// Open a file and starts the program.
        /// </summary>
        /// <param name="pFilePath">Path to the file.</param>
        /// <param name="pOffsetViewMode">Offset base to start with.</param>
        internal static void Open(string pFilePath, OffsetBaseView pOffsetViewMode, int pBytesRow)
        {
            if (!File.Exists(pFilePath))
                throw new FileNotFoundException
                {
                    Source = pFilePath
                };

            CurrentFile = new FileInfo(pFilePath);
            
            MainPanel.BytesInRow = pBytesRow;

            CurrentOffsetBaseView = pOffsetViewMode;

            PrepareScreen();

            MainPanel.Refresh();
            MainPanel.Update();

            // Someone was unhappy with the do {} while(); loop.
            while (ReadUserKey())
            { }
        }

        /// <summary>
        /// Prepares the screen with the information needed.
        /// </summary>
        static void PrepareScreen()
        {
            TitlePanel.Update();
            OffsetPanel.Update();
            InfoPanel.Update();
            ControlPanel.Place();
        }

        /// <summary>
        /// Read the current file at a position.
        /// </summary>
        /// <param name="pBasePosition">Position.</param>
        static void ReadCurrentFile(long pBasePosition)
        {
            int len;
            using (StreamReader sr = new StreamReader(CurrentFile.FullName))
            {
                sr.BaseStream.Position = pBasePosition;

                if (sr.BaseStream.Length < MainPanel.ScreenMaxBytes)
                    len = (int)sr.BaseStream.Length;
                else
                    len = MainPanel.ScreenMaxBytes;

                Buffer = new byte[len];

                sr.BaseStream.Read(Buffer, 0, len);
            }
        }

        /// <summary>
        /// Read the user's key.
        /// </summary>
        /// <returns>True if still using 0xdd.</returns>
        static bool ReadUserKey()
        {
            ConsoleKeyInfo cki = Console.ReadKey(true);
            
            switch (cki.Key)
            {
                // -- Hidden shortcuts --
                case ConsoleKey.F5:
                    MainPanel.Update();
                    break;

                case ConsoleKey.F10:
                    ToggleFullscreenMode();
                    break;

                // -- Shown shortcuts --
                // Help
                case ConsoleKey.F1:
                case ConsoleKey.K:
                    if (cki.Modifiers == ConsoleModifiers.Control ||
                        cki.Key == ConsoleKey.F1)
                        return ShowHelp();
                    break;

                // Find byte
                case ConsoleKey.W:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        int? t = GetUserInputForNumber("Find byte:");

                        if (t == null)
                        {
                            MainPanel.Update();
                            return true;
                        }

                        if (t < 0 || t > 0xFF)
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

                            if (p < 0)
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
                    break;

                // Find data
                case ConsoleKey.J:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        string t = GetUserInput("Find data:");

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
                        long p = Find(t, CurrentFilePosition + 1, Utilities.GetEncoding(CurrentFile.Name));

                        if (p < 0)
                        {
                            Message("Data could not be found.");
                            MainPanel.Update();
                        }
                        else
                        {
                            Goto(p - 1);
                            Message($"Found {t} at position {p - 1}");
                        }

                        return true;
                    }
                    break;

                // Goto
                case ConsoleKey.G:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        int? t = GetUserInputForNumber("Find byte:");

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
                    break;

                // Offset base
                case ConsoleKey.O:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        string c = GetUserInput("Hex, Dec, or Oct?:");

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
                                break;

                            case 'O':
                            case 'o':
                                CurrentOffsetBaseView = OffsetBaseView.Octal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                break;

                            case 'D':
                            case 'd':
                                CurrentOffsetBaseView = OffsetBaseView.Decimal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                break;

                            default:
                                Message("Invalid view mode!");
                                MainPanel.Update();
                                break;
                        }
                    }
                    break;

                // Replace
                case ConsoleKey.H:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        throw new NotImplementedException();
                    break;

                // Info
                case ConsoleKey.I:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        decimal ratioStart = Math.Round((decimal)CurrentFilePosition / CurrentFile.Length * 100);
                        decimal max = CurrentFile.Length < MainPanel.ScreenMaxBytes ? CurrentFile.Length : MainPanel.ScreenMaxBytes;
                        decimal ratioEnd = Math.Round(((CurrentFilePosition + max) / CurrentFile.Length) * 100);
                        Message($"Size: {Utilities.GetFormattedSize(CurrentFile.Length)} | Position: {ratioStart}~{ratioEnd}%");
                    }
                    break;

                // Exit
                case ConsoleKey.X:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        return Exit();
                    break;

                // Dump
                case ConsoleKey.D:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Dumping...");
                        Dump();
                        Message("Dumping done!");
                    }
                    break;
                    
                // -- Data nagivation --
                case ConsoleKey.LeftArrow:
                    if (CurrentWritingMode == WritingMode.Read)
                    {
                        if (CurrentFilePosition - 1 >= 0)
                        {
                            ReadAndUpdate(--CurrentFilePosition);
                        }
                    }
                    else
                    {

                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (CurrentWritingMode == WritingMode.Read)
                    {
                        if (CurrentFilePosition + MainPanel.ScreenMaxBytes + 1 <= CurrentFile.Length)
                        {
                            ReadAndUpdate(++CurrentFilePosition);
                        }
                    }
                    else
                    {

                    }
                    break;

                case ConsoleKey.UpArrow:
                    if (CurrentWritingMode == WritingMode.Read)
                    {
                        if (CurrentFilePosition - MainPanel.BytesInRow >= 0)
                        {
                            ReadAndUpdate(CurrentFilePosition -= MainPanel.BytesInRow);
                        }
                        else
                        {
                            ReadAndUpdate(CurrentFilePosition = 0);
                        }
                    }
                    else
                    {

                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (CurrentWritingMode == WritingMode.Read)
                    {
                        if (CurrentFilePosition + MainPanel.ScreenMaxBytes + MainPanel.BytesInRow <= CurrentFile.Length)
                        {
                            ReadAndUpdate(CurrentFilePosition += MainPanel.BytesInRow);
                        }
                        else
                        {
                            ReadAndUpdate(CurrentFilePosition = CurrentFile.Length - MainPanel.ScreenMaxBytes);
                        }
                    }
                    else
                    {

                    }
                    break;

                case ConsoleKey.PageUp:
                    if (CurrentWritingMode == WritingMode.Read)
                    {
                        if (CurrentFilePosition - MainPanel.ScreenMaxBytes >= 0)
                        {
                            ReadAndUpdate(CurrentFilePosition -= MainPanel.ScreenMaxBytes);
                        }
                        else
                        {
                            ReadAndUpdate(CurrentFilePosition = 0);
                        }
                    }
                    else
                    {

                    }

                    break;
                case ConsoleKey.PageDown:
                    if (CurrentWritingMode == WritingMode.Read)
                    {
                        if (CurrentFilePosition + (MainPanel.ScreenMaxBytes * 2) <= CurrentFile.Length)
                        {
                            ReadAndUpdate(CurrentFilePosition += MainPanel.ScreenMaxBytes);
                        }
                        else
                        {
                            ReadAndUpdate(CurrentFilePosition = CurrentFile.Length - MainPanel.ScreenMaxBytes);
                        }
                    }
                    else
                    {
                        
                    }
                    break;

                case ConsoleKey.Home:
                    if (CurrentWritingMode == WritingMode.Read)
                        ReadAndUpdate(CurrentFilePosition = 0);
                    else
                    {

                    }
                    break;
                case ConsoleKey.End:
                    if (CurrentWritingMode == WritingMode.Read)
                        ReadAndUpdate(CurrentFilePosition = CurrentFile.Length - MainPanel.ScreenMaxBytes);
                    else
                    {

                    }
                    break;
            }

            return true;
        }

        static void ReadAndUpdate(long pPosition)
        {
            ReadCurrentFile(pPosition);
            MainPanel.Update();
            InfoPanel.Update();
        }

        /// <summary>
        /// Toggles current ForegroundColor to black
        /// and BackgroundColor to gray.
        /// </summary>
        static void ToggleColors()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Displays a message on screen to inform the user.
        /// </summary>
        /// <param name="pMessage">Message to show.</param>
        static void Message(string pMessage)
        {
            Console.SetCursorPosition(0, InfoPanel.StartingTopPosition);
            Console.Write(new string(' ', Console.WindowWidth - 1));

            string msg = $"[ {pMessage} ]";
            Console.SetCursorPosition((Console.WindowWidth / 2) - (msg.Length / 2), InfoPanel.StartingTopPosition);

            ToggleColors();

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
                MainPanel.StartingTopPosition = 2;
                MainPanel.FrameHeight = Console.WindowHeight - 5;
                InfoPanel.StartingTopPosition = Console.WindowHeight - 3;
                Fullscreen = false;
                Console.Clear();
                ControlPanel.Place();
                OffsetPanel.Update();
                TitlePanel.Update();
                MainPanel.Refresh();
            }
            else
            { // Turning on
                MainPanel.StartingTopPosition = 1;
                MainPanel.FrameHeight = Console.WindowHeight - 2;
                InfoPanel.StartingTopPosition = Console.WindowHeight - 1;
                Fullscreen = true;
                Console.Clear();
                TitlePanel.Update();
                MainPanel.Refresh();
            }
        }

        static void Goto(long pPosition)
        {
            CurrentFilePosition = pPosition;
            ReadAndUpdate(CurrentFilePosition);
        }

        static string GetUserInput(string pMessage)
        {
            return GetUserInput(pMessage, 27, 4);
        }

        static int? GetUserInputForNumber(string pMessage)
        {
            return GetUserInputForNumber(pMessage, 27, 4);
        }

        static int? GetUserInputForNumber(string pMessage, int pWidth, int pHeight)
        {
            GenerateInputBox(pMessage, pWidth, pHeight);

            int? t = null;

            try
            {
                t = Utilities.ReadValue(pWidth - 2);
            }
            catch
            {

            }

            if (MainPanel.ScreenMaxBytes < CurrentFile.Length)
                ClearRange(pWidth, pHeight);
            else
                Console.ResetColor();

            return t;
        }

        static string GetUserInput(string pMessage, int pWidth, int pHeight)
        {
            int width = 25;
            int height = 4;

            GenerateInputBox(pMessage, width, height);

            string t = Utilities.ReadLine(pWidth - 2);

            if (MainPanel.ScreenMaxBytes < CurrentFile.Length)
                ClearRange(pWidth, pHeight);
            else
                Console.ResetColor();

            return t;
        }

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

        static void ClearRange(int pWidth, int pHeight)
        {
            Console.ResetColor();
            int startx = (Console.WindowWidth / 2) - (pWidth / 2);
            int starty = (Console.WindowHeight / 2) - (pHeight / 2);
            for (int i = 0; i < pHeight; i++)
            {
                Console.SetCursorPosition(startx, starty + i);
                Console.Write(new string(' ', pWidth));
            }
        }

        static void Dump()
        {
            Dump(CurrentFile.Name, MainPanel.BytesInRow, CurrentOffsetBaseView);
        }

        static internal int Dump(string pFileToDump, int pBytesInRow, OffsetBaseView pViewMode)
        {
            if (!File.Exists(pFileToDump))
                return 1;
            
            FileInfo file = new FileInfo(pFileToDump);
            int filelen = (int)file.Length;
            int line = 0;
            int BufferPositionHex = 0;
            int BufferPositionData = 0;
            // To not change the current buffer, we use a new one.
            // Or if we come from the /dump CLI parameter.
            Buffer = new byte[pBytesInRow];

            using (StreamWriter sw = new StreamWriter($"{pFileToDump}.{NAME_EXTENSION}"))
            {
                sw.WriteLine(file.Name);
                sw.WriteLine();

                sw.Write("Size: ");
                sw.Write(Utilities.GetFormattedSize(filelen));
                if (CurrentFile.Length < 1024)
                    sw.WriteLine($" ({filelen} B)");
                else
                    sw.WriteLine();

                sw.WriteLine();
                sw.WriteLine($"Attributes: {file.Attributes.ToString()}");
                sw.WriteLine($"Creation time: {file.CreationTime}");
                sw.WriteLine();

                sw.Write($"Offset {pViewMode.GetChar()}  ");
                for (int i = 0; i < pBytesInRow; i++)
                {
                    sw.Write($"{i.ToString("X2")} ");
                }
                sw.WriteLine();

                using (FileStream fs = file.OpenRead())
                {
                    bool finished = false;

                    while (!finished)
                    {
                        switch (pViewMode)
                        {
                            case OffsetBaseView.Hexadecimal:
                                sw.Write($"{line.ToString("X8")}  ");
                                break;

                            case OffsetBaseView.Decimal:
                                sw.Write($"{line.ToString("00000000")}  ");
                                break;

                            case OffsetBaseView.Octal:
                                sw.Write($"{Convert.ToString(line, 8), 8}  ");
                                break;
                        }

                        line += pBytesInRow;

                        fs.Read(Buffer, 0, pBytesInRow);

                        for (int pos = 0; pos < pBytesInRow; pos++)
                        {
                            if (BufferPositionHex < filelen)
                                sw.Write($"{Buffer[pos].ToString("X2")} ");
                            else
                                sw.Write("   ");

                            BufferPositionHex++;
                        }

                        sw.Write(" ");

                        for (int pos = 0; pos < pBytesInRow; pos++)
                        {
                            if (BufferPositionData < filelen)
                                sw.Write($"{Buffer[pos].ToSafeChar()}");
                            else
                                return 0;

                            BufferPositionData++;
                        }
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Find a byte starting at the CurrentFilePosition and
        /// return its found position.
        /// -1 means the data couldn't be found.
        /// -2 means that the file doesn't exist.
        /// -3 means that the given position is out of bound.
        /// </summary>
        /// <param name="pData">Data as a byte.</param>
        /// <returns>Positon. -1 being not found.</returns>
        static long Find(byte pData)
        {
           return Find(pData, CurrentFilePosition);
        }

        /// <summary>
        /// Find a byte in the current file and
        /// return its found position.
        /// -1 means the data couldn't be found.
        /// -2 means that the file doesn't exist.
        /// -3 means that the given position is out of bound.
        /// </summary>
        /// <param name="pData">Data as a byte.</param>
        /// <param name="pPosition">Positon to start searching from.</param>
        /// <returns>Found positon.</returns>
        static long Find(byte pData, long pPosition)
        {
            if (pPosition < 0 || pPosition > CurrentFile.Length)
                return -3;

            if (!CurrentFile.Exists)
                return -2;

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

            return -1;
        }

        /// <summary>
        /// Find a string of data.
        /// </summary>
        /// <param name="pData">Data as a string.</param>
        /// <param name="pEncoding">Encoding.</param>
        /// <returns>Position.</returns>
        static long Find(string pData, System.Text.Encoding pEncoding)
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
        static long Find(string pData, long pPosition, System.Text.Encoding pEncoding)
        {
            if (pPosition < 0 || pPosition > CurrentFile.Length)
                return -3;

            if (!CurrentFile.Exists)
                return -2;

            using (FileStream fs = CurrentFile.OpenRead())
            {
                fs.Position = pPosition;

                bool Continue = true;
                byte[] buffer = new byte[pData.Length];
                int length = pData.Length;
                while (Continue)
                {
                    if (fs.Position + length > fs.Length)
                        Continue = false;

                    if (pData[0] == (char)fs.ReadByte())
                    {
                        fs.Position--;

                        if (length == 1)
                            return fs.Position;
                        else
                        { // Let's not waste a re-read for a byte character.
                            fs.Read(buffer, 0, length);
                            if (pData == pEncoding.GetString(buffer))
                            {
                                fs.Position -= length - 1;
                                return fs.Position;
                            }
                        }
                    }
                }
            }

            return -1;
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

            return false;
        }
        #endregion

        #region Help
        static bool ShowHelp()
        {
            MainPanel.StartingTopPosition = 1;
            MainPanel.FrameHeight = Console.WindowHeight - 3;

            int pos = 0;
            bool inmenu = true;
            List<string> helplines = new List<string>();

            Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("_0xdd.Help.txt");
            using (StreamReader sr = new StreamReader(s))
            {
                int WinWidth = Console.WindowWidth - 1;

                while (!sr.EndOfStream)
                {
                    string tmp = sr.ReadLine();

                    if (tmp.Length > WinWidth)
                    {
                        int tmppos = 0;
                        bool finished = false;
                        while (!finished)
                        {
                            if (tmppos + WinWidth > tmp.Length)
                            {
                                helplines.Add(tmp.Substring(tmppos));
                                finished = true;
                            }
                            else
                            {
                                helplines.Add(tmp.Substring(tmppos, WinWidth));
                                tmppos += WinWidth;
                            }
                        }
                    }
                    else
                        helplines.Add(tmp);
                }

            }

            MainPanel.Clear();
            RenderHelp(ref helplines, pos);
            while (inmenu)
            {
                ConsoleKeyInfo cki = Console.ReadKey(true);

                switch (cki.Key)
                {
                    case ConsoleKey.X:
                        if (cki.Modifiers == ConsoleModifiers.Control)
                        {
                            // Exit completely
                            return Exit();
                        }
                        break;

                    case ConsoleKey.Escape:
                        // Exit help, not 0xdd.
                        inmenu = false;
                        break;

                    case ConsoleKey.UpArrow:
                        if (pos - 1 >= 0)
                        {
                            MainPanel.Clear();
                            RenderHelp(ref helplines, --pos);
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (pos + MainPanel.FrameHeight + 1 < helplines.Count)
                        {
                            MainPanel.Clear();
                            RenderHelp(ref helplines, ++pos);
                        }
                        break;

                    case ConsoleKey.PageUp:
                        if (pos - MainPanel.FrameHeight >= 0)
                        {
                            pos -= MainPanel.FrameHeight;
                            MainPanel.Clear();
                            RenderHelp(ref helplines, pos);
                        }
                        break;
                    case ConsoleKey.PageDown:
                        if (pos + (MainPanel.FrameHeight * 2) < helplines.Count)
                        {
                            pos += MainPanel.FrameHeight;
                            MainPanel.Clear();
                            RenderHelp(ref helplines, pos);
                        }
                        break;
                }
            }

            MainPanel.StartingTopPosition = 2;
            MainPanel.FrameHeight = Console.WindowHeight - 5;
            OffsetPanel.Update();
            InfoPanel.Update();
            MainPanel.Refresh();

            return true;
        }

        static void RenderHelp(ref List<string> helplines, int pPosition)
        {
            Console.SetCursorPosition(0, MainPanel.StartingTopPosition);
            for (int i = 0; i < MainPanel.FrameHeight; i++)
            {
                if (pPosition < helplines.Count)
                {
                    Console.WriteLine(helplines[pPosition]);
                    pPosition++;
                }
                else
                    return;
            }
        }
        #endregion

        #region Type extensions
        /// <summary>
        /// Returns a readable character if found.
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
        #endregion
    }
}
