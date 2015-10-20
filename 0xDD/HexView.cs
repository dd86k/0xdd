using System;
using System.IO;

//TODO: Edit mode

/*
    Box of ideas (Lazy TODO list)
    - Scrollbar (-ish), you know style
    - Top right (under title): Insert(INS)/Overwrite(OVR)
    - Search: /regex/ (Begins with && ends with)
    - Message(ProgressBar=bool) -> Progress bar (Dump) -> [ Done! ]
    - nagivation syncs (e.g. 32 - 33 -> 0 instead of just not doing it)
    - align offset (dividable by ElementsWidth)
*/

namespace ConHexView
{
    static class HexView
    {
        #region Constants
        
        /// <summary>
        /// Extension of data dump files.
        /// </summary>
        const string NAME_EXTENSION = "datdmp";
        #endregion

        #region General properties
        /// <summary>
        /// Number of elements to move by a line.
        /// </summary>
        static int SCROLL_LINE
        {
            get
            {
                return MainPanel.NumberOfBytesInRow;
            }
        }

        /// <summary>
        /// Number of elements to move by a page.
        /// </summary>
        static int SCROLL_PAGE
        {
            get
            {
                return MainPanel.MaximumNumberOfBytesOnScreen;
            }
        }

        /// <summary>
        /// Current position in the file.
        /// </summary>
        static long CurrentFilePosition = 0;

        /// <summary>
        /// Information about the current file.
        /// </summary>
        static FileInfo CurrentFile;

        /// <summary>
        /// Temporary buffer used for on-screen display.
        /// </summary>
        static byte[] Buffer = new byte[0];

        /// <summary>
        /// Fullscreen mode, false by default.
        /// </summary>
        static bool Fullscreen;
        #endregion

        #region MainPanel properties
        /// <summary>
        /// Main panel which represents the offset, data as bytes,
        /// and data as ASCII characters.
        /// </summary>
        struct MainPanel
        {
            /// <summary>
            /// Gets or sets the heigth of the main panel.
            /// </summary>
            static internal int FrameHeight
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the number of bytes showed in a row.
            /// </summary>
            static internal int NumberOfBytesInRow
            {
                get; set;
            }

            /// <summary>
            /// Gets the number of elements which can be shown in the main panel.
            /// </summary>
            static internal int MaximumNumberOfBytesOnScreen
            {
                get
                {
                    return FrameHeight * NumberOfBytesInRow;
                }
            }
        }
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

        /// <summary>
        /// Current <see cref="OffsetViewMode"/>.
        /// </summary>
        static OffsetViewMode CurrentOffsetViewMode;
        #endregion

        #region Internal methods
        /// <summary>
        /// Open a file and starts the program.
        /// </summary>
        /// <param name="pFilePath">Path to the file.</param>
        internal static void Open(string pFilePath)
        {
            Open(pFilePath, OffsetViewMode.Hexadecimal, 16);
        }

        /// <summary>
        /// Open a file and starts the program.
        /// </summary>
        /// <param name="pFilePath">Path to the file.</param>
        /// <param name="pOffsetViewMode">Offset view to start with.</param>
        internal static void Open(string pFilePath, OffsetViewMode pOffsetViewMode, int pBytesRow)
        {
            if (!File.Exists(pFilePath))
                throw new FileNotFoundException
                {
                    Source = pFilePath
                };

            CurrentFile = new FileInfo(pFilePath);

            MainPanel.FrameHeight = Console.WindowHeight - 5;
            MainPanel.NumberOfBytesInRow = pBytesRow;

            CurrentOffsetViewMode = pOffsetViewMode;

            ReadCurrentFile();

            UpdateTitleMap();
            PlaceOffsetMap();
            UpdateMainScreen();
            UpdateInfoMap();
            PlaceMainControlMap();

            // Someone was unhappy with the do {} while() loop.
            while (ReadUserKey())
            { }
        }

        internal static void OpenClipboard()
        {
            //TODO: void OpenClipboard()

            bool foundData = false;
            object Data;

            // Specifies a Windows bitmap format.
            if (System.Windows.Forms.Clipboard.ContainsData("Bitmap"))
            {

                foundData = true;
            }
            // Specifies a comma-separated value (CSV) format, which is a common interchange format used by spreadsheets.
            // This format is not used directly by Windows Forms.
            if (System.Windows.Forms.Clipboard.ContainsData("CommaSeperatedValues") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows device-independent bitmap (DIB) format.
            if (System.Windows.Forms.Clipboard.ContainsData("Dib") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows Data Interchange Format (DIF), which Windows Forms does not directly use.
            if (System.Windows.Forms.Clipboard.ContainsData("Dif") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows enhanced metafile format.
            if (System.Windows.Forms.Clipboard.ContainsData("EnhancedMetafile") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows file drop format, which Windows Forms does not directly use.
            if (System.Windows.Forms.Clipboard.ContainsData("FileDrop") && !foundData)
            {

                foundData = true;
            }
            // Specifies text in the HTML Clipboard format.
            if (System.Windows.Forms.Clipboard.ContainsData("Html") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows culture format, which Windows Forms does not directly use.
            if (System.Windows.Forms.Clipboard.ContainsData("Locale") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows metafile format, which Windows Forms does not directly use.
            if (System.Windows.Forms.Clipboard.ContainsData("MetafilePict") && !foundData)
            {

                foundData = true;
            }
            // Specifies the standard Windows original equipment manufacturer (OEM) text format.
            if (System.Windows.Forms.Clipboard.ContainsData("OemText") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows palette format.
            if (System.Windows.Forms.Clipboard.ContainsData("Palette") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows pen data format, which consists of pen strokes for handwriting software;
            // Windows Forms does not use this format.
            if (System.Windows.Forms.Clipboard.ContainsData("PenData") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Resource Interchange File Format (RIFF) audio format,
            // which Windows Forms does not directly use.
            if (System.Windows.Forms.Clipboard.ContainsData("Riff") && !foundData)
            {

                foundData = true;
            }
            // Specifies text consisting of Rich Text Format (RTF) data.
            if (System.Windows.Forms.Clipboard.ContainsData("Rtf") && !foundData)
            {

                foundData = true;
            }
            // Specifies a format that encapsulates any type of Windows Forms object.
            if (System.Windows.Forms.Clipboard.ContainsData("Serializable") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows Forms string class format, which Windows Forms uses to store string objects.
            if (System.Windows.Forms.Clipboard.ContainsData("StringFormat") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows symbolic link format, which Windows Forms does not directly use.
            if (System.Windows.Forms.Clipboard.ContainsData("SymbolicLink") && !foundData)
            {

                foundData = true;
            }
            // Specifies the standard ANSI text format.
            if (System.Windows.Forms.Clipboard.ContainsData("Text") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Tagged Image File Format (TIFF), which Windows Forms does not directly use.
            if (System.Windows.Forms.Clipboard.ContainsData("Tiff") && !foundData)
            {

                foundData = true;
            }
            // Specifies the standard Windows Unicode text format.
            if (System.Windows.Forms.Clipboard.ContainsData("UnicodeText") && !foundData)
            {

                foundData = true;
            }
            // Specifies the wave audio format, which Windows Forms does not directly use.
            if (System.Windows.Forms.Clipboard.ContainsData("WaveAudio") && !foundData)
            {

                foundData = true;
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Read the current file.
        /// </summary>
        static void ReadCurrentFile()
        {
            ReadCurrentFile(CurrentFilePosition);
        }

        /// <summary>
        /// Read the current file at a position.
        /// </summary>
        /// <param name="pBasePosition">Position.</param>
        static void ReadCurrentFile(long pBasePosition)
        {
            using (StreamReader sr = new StreamReader(CurrentFile.FullName))
            {
                sr.BaseStream.Position = pBasePosition;
                
                int len =
                    sr.BaseStream.Length < MainPanel.MaximumNumberOfBytesOnScreen ?
                    (int)sr.BaseStream.Length :
                    MainPanel.MaximumNumberOfBytesOnScreen;

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
                    //TODO: Menu at ConsoleKey.Escape
                    break;

                // -- Hidden shortcuts --
                case ConsoleKey.F11:
                    ToggleFullscreenMode();
                    break;

                case ConsoleKey.O:
                        //TODO: Open Dialog
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

                // Goto
                case ConsoleKey.G:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        throw new NotImplementedException();
                    break;

                // Replace
                case ConsoleKey.H:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        throw new NotImplementedException();
                    break;

                // Info
                case ConsoleKey.I:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        Message(
                            $"Size: {CurrentFile.Length} | Postion: {Math.Round(((decimal)CurrentFilePosition / CurrentFile.Length) * 100)}%"
                        );
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
                        ReadAndUpdate(--CurrentFilePosition, SCROLL_PAGE);
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (CurrentFilePosition + (MainPanel.MaximumNumberOfBytesOnScreen) + 1 <= CurrentFile.Length)
                    {
                        ReadAndUpdate(++CurrentFilePosition);
                    }
                    break;

                case ConsoleKey.UpArrow:
                    if (CurrentFilePosition - SCROLL_LINE >= 0)
                    {
                        ReadAndUpdate(CurrentFilePosition -= SCROLL_LINE);
                    }
                    else
                    {
                        //TODO: Round if it reaches the beginning
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (CurrentFilePosition + (MainPanel.MaximumNumberOfBytesOnScreen) + SCROLL_LINE <= CurrentFile.Length)
                    {
                        ReadAndUpdate(CurrentFilePosition += SCROLL_LINE);
                    }
                    else
                    {
                        //TODO: Round if it reaches end
                    }
                    break;

                case ConsoleKey.PageUp:
                    if (CurrentFilePosition - SCROLL_PAGE >= 0)
                    {
                        ReadAndUpdate(CurrentFilePosition -= SCROLL_PAGE);
                    }
                    else
                    {
                        //TODO: Round if it reaches the beginning
                    }
                    break;
                case ConsoleKey.PageDown:
                    if (CurrentFilePosition + (MainPanel.MaximumNumberOfBytesOnScreen) + SCROLL_PAGE <= CurrentFile.Length)
                    {
                        ReadAndUpdate(CurrentFilePosition += SCROLL_PAGE);
                    }
                    else
                    {
                        //TODO: Round if it reaches end
                    }
                    break;

                case ConsoleKey.Home:
                    //TODO: Fix ^Home not working
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        ReadAndUpdate(CurrentFilePosition = 0);
                    }
                    else
                    {
                        //TODO: Align to offset *******0
                    }
                    break;
                case ConsoleKey.End:
                    //TODO: Fix ^End not working
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        CurrentFilePosition = CurrentFile.Length - (MainPanel.MaximumNumberOfBytesOnScreen);
                        ReadAndUpdate(CurrentFilePosition);
                    }
                    else
                    {
                        //TODO: Align to offset *******F
                    }
                    break;
            }

            return true;
        }

        static void ReadAndUpdate(long pOffset)
        {
            ReadAndUpdate(pOffset, MainPanel.FrameHeight);
        }

        static void ReadAndUpdate(long pOffset, int pLength)
        {
            ReadCurrentFile(pOffset);
            UpdateMainScreen();
            UpdateInfoMap();
        }

        /// <summary>
        /// Update the section of the screen with the data.
        /// </summary>
        static void UpdateMainScreen()
        {
            long filelen = CurrentFile.Length;
            /*
            long lines = CurrentFilePosition + (FrameHeight * ElementsWidth) > filelen ?
                //TODO: Fix this line
                ((filelen - (CurrentFilePosition + FrameHeight)) / ElementsWidth) + 1:
                FrameHeight;
            */
            long lines = MainPanel.FrameHeight;

            int BufferOffsetHex = 0;
            int BufferOffsetData = 0;

            Console.SetCursorPosition(0, 2);

            for (int line = 0; line < lines; line++)
            {
                switch (CurrentOffsetViewMode)
                {
                    case OffsetViewMode.Hexadecimal:
                        Console.Write($"{((line * MainPanel.NumberOfBytesInRow) + CurrentFilePosition).ToString("X8")}  ");
                        break;

                    case OffsetViewMode.Decimal:
                        Console.Write($"{((line * MainPanel.NumberOfBytesInRow) + CurrentFilePosition).ToString("00000000")}  ");
                        break;

                    case OffsetViewMode.Octal:
                        Console.Write($"{Convert.ToString((line * MainPanel.NumberOfBytesInRow) + CurrentFilePosition, 8), 8}  ");
                        break;
                }

                for (int x = 0; x < MainPanel.NumberOfBytesInRow; x++)
                {
                    if (CurrentFilePosition + BufferOffsetData < filelen)
                        Console.Write($"{Buffer[BufferOffsetData].ToString("X2")} ");
                    else
                        Console.Write("   ");

                    BufferOffsetData++;
                }

                Console.Write(" ");

                for (int x = 0; x < MainPanel.NumberOfBytesInRow; x++)
                {
                    if (CurrentFilePosition + BufferOffsetHex < filelen)
                        Console.Write($"{Buffer[BufferOffsetHex].ToSafeChar()}");
                    else
                        Console.Write(" ");

                    BufferOffsetHex++;
                }

                Console.WriteLine();
            }
            
            /*
            if (lines < FrameHeight)
            {
                // Force-fill the void with spaces in case the user scrolls up
                for (int line = FrameHeight + 2;line >  FrameHeight - lines; line--)
                {
                    Console.SetCursorPosition(0, line);
                    Console.Write(new string(' ', Console.WindowWidth));
                }
            }
            */
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
            Console.Write(
                $"DEC: {CurrentFilePosition.ToString("00000000")} | HEX: {CurrentFilePosition.ToString("X8")} | OCT: {Convert.ToString(CurrentFilePosition, 8), 8}"
            );
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

        static void ToggleFullscreenMode()
        {
            // TODO: void ToggleFullscreenMode();
            /*
            FrameHeight = Console.WindowHeight - 1;
            ReadAndUpdate(CurrentFilePosition);
            */
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
            int filelen = (int)file.Length;
            int line = 0;
            int BufferPositionHex = 0;
            int BufferPositionData = 0;
            byte[] buffer = new byte[MainPanel.NumberOfBytesInRow];

            using (StreamWriter sw = new StreamWriter($"{pPath}.{NAME_EXTENSION}"))
            {
                sw.WriteLine(file.Name);
                sw.WriteLine();
                sw.Write("Size:");

                if (filelen > Math.Pow(1024, 3)) // GB
                {
                    sw.Write($" {Math.Round(filelen / Math.Pow(1024, 3), 2)} GB");
                    sw.Write($" ({filelen} B)");
                }
                else if (filelen > Math.Pow(1024, 2)) // MB
                {
                    sw.Write($" {Math.Round(filelen / Math.Pow(1024, 2), 2)} MB");
                    sw.Write($" ({filelen} B)");
                }
                else if (filelen > 1024) // KB
                {
                    sw.Write($" {Math.Round((decimal)filelen / 1024, 2)} KB");
                    sw.Write($" ({filelen} B)");
                }
                else
                {
                    sw.Write($" {filelen} B");
                }

                sw.WriteLine();
                sw.WriteLine($"Attributes: {file.Attributes.ToString()}");
                sw.WriteLine($"Creation time: {file.CreationTime}");
                sw.WriteLine();

                sw.WriteLine($"Offset {pViewMode.GetChar()}  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");

                using (FileStream fs = file.OpenRead())
                {
                    bool finished = false;

                    while (!finished)
                    {
                        switch (pViewMode)
                        {
                            case OffsetViewMode.Hexadecimal:
                                sw.Write($"{line.ToString("X8")}  ");
                                break;

                            case OffsetViewMode.Decimal:
                                sw.Write($"{line.ToString("00000000")}  ");
                                break;

                            case OffsetViewMode.Octal:
                                sw.Write($"{Convert.ToString(line, 8), 8}  ");
                                break;
                        }

                        line += MainPanel.NumberOfBytesInRow;

                        for (int c = 0; c < MainPanel.NumberOfBytesInRow; c++)
                        {
                            byte b = (byte)fs.ReadByte();

                            buffer[c] = b;
                        }

                        for (int pos = 0; pos < MainPanel.NumberOfBytesInRow; pos++)
                        {
                            if (BufferPositionHex < filelen)
                                sw.Write($"{buffer[pos].ToString("X2")} ");
                            else
                                sw.Write("   ");

                            BufferPositionHex++;
                        }

                        sw.Write(" ");

                        for (int pos = 0; pos < MainPanel.NumberOfBytesInRow; pos++)
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

        #region Type extensions
        /// <summary>
        /// Returns a readable character if found.
        /// </summary>
        /// <param name="pIn">Byte to transform.</param>
        /// <returns>Readable character.</returns>
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
