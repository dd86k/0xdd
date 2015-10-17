using System;
using System.IO;

//TODO: Edit mode

/*
    Box of ideas (Lazy TODO list)
    - --dump
    - Scrollbar (-ish)
    - Top right (under title): Insert(INS)/Overwrite(OVR)
    - Help section (F1)
    - Search: /regex/ (Begins with && ends with)
    - no args -> empty buffer/new file
    - Message(ProgressBar=bool) -> Progress bar (Dump) -> [ Done! ]
    - nagivation syncs (e.g. 32 - 33 -> 0 instead of just not doing it)
    - align offset (dividable by ElementsWidth)
*/

namespace ConHexView
{
    static class HexView
    {
        #region Constants
        static int SCROLL_LINE
        {
            get
            {
                return ElementsWidth;
            }
        }
        static int SCROLL_PAGE
        {
            get
            {
                return FrameHeight * ElementsWidth;
            }
        }

        /// <summary>
        /// Offset attribute name to write out at
        /// 
        /// </summary>
        /// <example>
        /// Offset (HEX): 000000A0
        /// </example>
        const string NAME_OFFSET = "Offset";

        /// <summary>
        /// Extension of data dump files.
        /// </summary>
        const string NAME_EXTENSION = "datdmp";
        #endregion

        #region Properties
        /// <summary>
        /// Current position in the file.
        /// </summary>
        static int CurrentFilePosition = 0;

        /// <summary>
        /// Information about the current file.
        /// </summary>
        static FileInfo CurrentFile;

        /// <summary>
        /// Current <see cref="OffsetViewMode"/>.
        /// </summary>
        static OffsetViewMode CurrentOffsetViewMode;

        /// <summary>
        /// Height of the main frame. (Hex)
        /// </summary>
        static int FrameHeight = Console.WindowHeight - 5;

        /// <summary>
        /// Data width, default is 16.
        /// </summary>
        static int ElementsWidth = 16;

        static int FrameCapacity = ElementsWidth * FrameHeight;

        static byte[] Buffer = new byte[0];

        /// <summary>
        /// Fullscreen mode, false by default.
        /// </summary>
        static bool Fullscreen;
        #endregion

        #region Enumerations
        /// <summary>
        /// Enumeration of different offset views.
        /// </summary>
        public enum OffsetViewMode
        {
            Hexadecimal,
            Decimal,
            Octal
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Open a file and starts the program.
        /// </summary>
        /// <param name="pFilePath">Path to the file.</param>
        public static void Open(string pFilePath)
        {
            Open(pFilePath, OffsetViewMode.Hexadecimal);
        }

        /// <summary>
        /// Open a file and starts the program.
        /// </summary>
        /// <param name="pFilePath">Path to the file.</param>
        /// <param name="pOffsetViewMode">Offset view to start with.</param>
        public static void Open(string pFilePath, OffsetViewMode pOffsetViewMode)
        {
            if (!File.Exists(pFilePath))
                throw new FileNotFoundException
                {
                    Source = pFilePath
                };

            CurrentFile = new FileInfo(pFilePath);

            CurrentOffsetViewMode = pOffsetViewMode;

            Read();

            UpdateTitleMap();
            PlaceOffsetMap();
            UpdateMainScreen();
            UpdateInfoMap();
            PlaceMainControlMap();

            // Someone was unhappy with the do {} while() loop.
            while (ReadUserKey())
            { }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Read the current file.
        /// </summary>
        static void Read()
        {
            Read(CurrentFilePosition);
        }

        /// <summary>
        /// Read the current file at a certain position.
        /// </summary>
        /// <param name="pBaseOffset">Position.</param>
        static void Read(int pBaseOffset)
        {
            using (StreamReader sr = new StreamReader(CurrentFile.FullName))
            {
                sr.BaseStream.Position = pBaseOffset;
                
                int len =
                    sr.BaseStream.Length < ElementsWidth * FrameHeight ?
                    (int)sr.BaseStream.Length :
                    ElementsWidth * FrameHeight;

                Buffer = new byte[len];

                for (int x = 0; x < len; x++)
                {
                    Buffer[x] = (byte)sr.Read();
                }
            }
        }

        /// <summary>
        /// Read the user's key.
        /// </summary>
        /// <returns><see cref="true"/> if still using the program.</returns>
        static bool ReadUserKey()
        {
            ConsoleKeyInfo cki = Console.ReadKey(true);

            switch (cki.Key)
            {
                case ConsoleKey.Escape:
                    return Exit();

                // -- Hidden shortcuts --
                case ConsoleKey.F11:
                        //TODO: "Fullscreen" mode
                    break;

                // -- Shown shortcuts --
                // Help
                case ConsoleKey.F1:
                case ConsoleKey.K:
                    if (cki.Modifiers == ConsoleModifiers.Control &&
                        cki.Key == ConsoleKey.K || cki.Key == ConsoleKey.F1)
                        throw new NotImplementedException();
                    break;

                // Find
                case ConsoleKey.W:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        throw new NotImplementedException();
                    break;

                // Info
                case ConsoleKey.I:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        Message($"Size: {CurrentFile.Length} | Postion: {Math.Round(((decimal)CurrentFilePosition / CurrentFile.Length) * 100)}%");
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
                    if (CurrentFilePosition - 1 >= 0)
                    {
                        CurrentFilePosition--;
                        ReadAndUpdate(CurrentFilePosition, SCROLL_PAGE);
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (CurrentFilePosition + 1 < CurrentFile.Length)
                    {
                        CurrentFilePosition++;
                        ReadAndUpdate(CurrentFilePosition);
                    }
                    break;

                case ConsoleKey.UpArrow:
                    if (CurrentFilePosition - SCROLL_LINE >= 0)
                    {
                        CurrentFilePosition -= SCROLL_LINE;
                        ReadAndUpdate(CurrentFilePosition);
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (CurrentFilePosition + SCROLL_LINE < CurrentFile.Length)
                    {
                        CurrentFilePosition += SCROLL_LINE;
                        ReadAndUpdate(CurrentFilePosition);
                    }
                    break;

                case ConsoleKey.PageUp:
                    if (CurrentFilePosition - SCROLL_PAGE >= 0)
                    {
                        CurrentFilePosition -= SCROLL_PAGE;
                        ReadAndUpdate(CurrentFilePosition);
                    }
                    break;
                case ConsoleKey.PageDown:
                    if (CurrentFilePosition + SCROLL_PAGE < CurrentFile.Length)
                    {
                        CurrentFilePosition += SCROLL_PAGE;
                        ReadAndUpdate(CurrentFilePosition);
                    }
                    break;

                case ConsoleKey.Home:
                    CurrentFilePosition = 0;
                    ReadAndUpdate(CurrentFilePosition);
                    break;
                case ConsoleKey.End:
                    CurrentFilePosition = (int)(CurrentFile.Length) - (FrameHeight * ElementsWidth);
                    ReadAndUpdate(CurrentFilePosition);
                    break;
            }

            return true;
        }

        static void ReadAndUpdate(int pOffset)
        {
            ReadAndUpdate(pOffset, FrameHeight);
        }

        static void ReadAndUpdate(int pOffset, int pLength)
        {
            Read(pOffset);
            UpdateMainScreen();
            UpdateInfoMap();
        }

        /// <summary>
        /// Update the section of the screen with the data.
        /// </summary>
        static void UpdateMainScreen()
        {
            int filelen = (int)CurrentFile.Length;

            int lines = CurrentFilePosition + (FrameHeight * ElementsWidth) > filelen ?
                //TODO: Fix this line
                ((filelen - (CurrentFilePosition + FrameHeight)) / ElementsWidth) :
                FrameHeight;

            int BufferOffsetHex = 0;
            int BufferOffsetData = 0;

            Console.SetCursorPosition(0, 2);

            for (int line = 0; line < lines; line++)
            {
                switch (CurrentOffsetViewMode)
                {
                    case OffsetViewMode.Hexadecimal:
                        Console.Write($"{((line * ElementsWidth) + CurrentFilePosition).ToString("X8")}  ");
                        break;

                    case OffsetViewMode.Decimal:
                        Console.Write($"{((line * ElementsWidth) + CurrentFilePosition).ToString("00000000")}  ");
                        break;

                    case OffsetViewMode.Octal:
                        Console.Write($"{Convert.ToString((line * ElementsWidth) + CurrentFilePosition, 8), 8}  ");
                        break;
                }

                for (int x = 0; x < ElementsWidth; x++)
                {
                    if (CurrentFilePosition + BufferOffsetData < filelen)
                        Console.Write($"{Buffer[BufferOffsetData].ToString("X2")} ");
                    else
                        Console.Write("   ");

                    BufferOffsetData++;
                }

                Console.Write(" ");

                for (int x = 0; x < ElementsWidth; x++)
                {
                    if (CurrentFilePosition + BufferOffsetHex < filelen)
                        Console.Write($"{Buffer[BufferOffsetHex].ToSafeChar()}");
                    else
                        Console.Write(" ");

                    BufferOffsetHex++;
                }

                Console.WriteLine();
            }

            if (FrameHeight > lines)
            {
                // Force-fill the void with spaces in case the user scrolls up
                for (int line = FrameHeight + 2; line > FrameHeight - lines; line--)
                {
                    Console.SetCursorPosition(0, line);
                    Console.Write(new string(' ', Console.WindowWidth));
                }
            }
        }

        /// <summary>
        /// Update the upper bar.
        /// </summary>
        static void UpdateTitleMap()
        {
            ToggleColors();

            Console.SetCursorPosition(0, 0);
            Console.Write(CurrentFile.Name);
            Console.Write(new string(' ', Console.WindowWidth - CurrentFile.Name.Length));

            Console.ResetColor();
        }

        /// <summary>
        /// Update the offset map
        /// </summary>
        static void PlaceOffsetMap()
        {
            Console.SetCursorPosition(0, 1);
            Console.Write($"Offset {CurrentOffsetViewMode.GetChar()}  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
        }

        /// <summary>
        /// Update the offset information
        /// </summary>
        static void UpdateInfoMap()
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 3);
            Console.Write($"{NAME_OFFSET} (DEC): {CurrentFilePosition.ToString("00000000")} | {NAME_OFFSET} (HEX): {CurrentFilePosition.ToString("X8")} | {NAME_OFFSET} (OCT): {Convert.ToString(CurrentFilePosition, 8), 8}");
        }

        /// <summary>
        /// Places the control map on screen (e.g. ^J Try jumping)
        /// </summary>
        static void PlaceMainControlMap()
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 2);

            ToggleColors();
            Console.Write("^K");
            Console.ResetColor();
            Console.Write(" Help         ");

            ToggleColors();
            Console.Write("^W");
            Console.ResetColor();
            Console.Write(" Find         ");

            ToggleColors();
            Console.Write("^G");
            Console.ResetColor();
            Console.Write(" Goto line    ");

            ToggleColors();
            Console.Write("^H");
            Console.ResetColor();
            Console.Write(" Replace      ");

            ToggleColors();
            Console.Write("^E");
            Console.ResetColor();
            Console.Write(" Edit mode");

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
            Console.Write("^V");
            Console.ResetColor();
            Console.Write(" Offset view  ");

            ToggleColors();
            Console.Write("^A");
            Console.ResetColor();
            Console.Write(" Data view");
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
            Console.SetCursorPosition(0, Console.WindowHeight - 3);
            Console.Write(new string(' ', Console.WindowWidth));

            string msg = $"[ {pMessage} ]";
            Console.SetCursorPosition((Console.WindowWidth / 2) - (msg.Length / 2), Console.WindowHeight - 3);

            ToggleColors();

            Console.Write(msg);

            Console.ResetColor();
        }

        static int ReadValue()
        {
            //TODO: int ReadValue()
            throw new NotImplementedException();
        }

        static void Dump()
        {
            Dump($"{CurrentFile.Name}.{NAME_EXTENSION}", CurrentOffsetViewMode);
        }

        static internal void Dump(string pPath, OffsetViewMode pViewMode)
        {
            // Force refresh information
            FileInfo file = new FileInfo(pPath);

            using (StreamWriter sw = new StreamWriter($"{pPath}.{NAME_EXTENSION}"))
            {
                sw.WriteLine(file.Name);
                sw.WriteLine();
                sw.WriteLine($"Size: {file.Length} Bytes");
                sw.WriteLine($"Attributes: {file.Attributes}");
                sw.WriteLine($"Creation time: {file.CreationTime}");
                sw.WriteLine();

                sw.WriteLine($"Offset {pViewMode.GetChar()}  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");

                using (FileStream fs = file.OpenRead())
                {
                    int line = 0;
                    int BufferPositionHex = 0;
                    int BufferPositionData = 0;
                    int filelen = (int)file.Length;
                    byte[] buffer = new byte[ElementsWidth];

                    bool finished = false;

                    while (!finished)
                    {
                        switch (pViewMode)
                        {
                            case OffsetViewMode.Hexadecimal:
                                sw.Write($"{(line + CurrentFilePosition).ToString("X8")}  ");
                                break;

                            case OffsetViewMode.Decimal:
                                sw.Write($"{(line + CurrentFilePosition).ToString("00000000")}  ");
                                break;

                            case OffsetViewMode.Octal:
                                sw.Write($"{Convert.ToString(line + CurrentFilePosition, 8), 8}  ");
                                break;
                        }

                        line += ElementsWidth;

                        for (int c = 0; c < ElementsWidth; c++)
                        {
                            byte b = (byte)fs.ReadByte();

                            buffer[c] = b;
                        }

                        for (int pos = 0; pos < ElementsWidth; pos++)
                        {
                            if (BufferPositionHex < filelen)
                                sw.Write($"{buffer[pos].ToString("X2")} ");
                            else
                                sw.Write("   ");

                            BufferPositionHex++;
                        }

                        sw.Write(" ");

                        for (int pos = 0; pos < ElementsWidth; pos++)
                        {
                            if (BufferPositionData < filelen)
                                sw.Write($"{buffer[pos].ToSafeChar()}");
                            else
                                finished = true;

                            BufferPositionData++;
                        }

                        sw.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// The user is exiting the application.
        /// So we prepare the departure.
        /// </summary>
        /// <returns>Always false.</returns>
        /// <remarks>It's false due to the while loop.</remarks>
        static bool Exit()
        {
            Console.Clear();

            return false;
        }
        #endregion

        #region Type extensions
        /// <summary>
        /// Returns a safe character for the console (cmd) to display.
        /// </summary>
        /// <param name="pIn">Byte to transform.</param>
        /// <returns>Safe console character.</returns>
        static char ToSafeChar(this byte pIn)
        {
            if (pIn < 0x20 || pIn > 0x7F)
                return '.';
            else
                return (char)pIn;
        }

        /// <summary>
        /// Gets the character for the upper bar depending on the
        /// offset view mode.
        /// </summary>
        /// <param name="pObject">This <see cref="OffsetViewMode"/></param>
        /// <returns>Character.</returns>
        static char GetChar(this OffsetViewMode pObject)
        {
            switch (pObject)
            {
                case OffsetViewMode.Hexadecimal:
                    return 'h';
                case OffsetViewMode.Decimal:
                    return 'd';
                case OffsetViewMode.Octal:
                    return 'o';
                default:
                    return '?';
            }
        }
        #endregion
    }

    /// <summary>
    /// Progressbar specifically for the hex viewer.
    /// </summary>
    class HexViewProgressBar
    {
        //TODO: HexViewProgressBar
    }
}
