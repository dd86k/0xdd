using System;
using System.IO;
using System.Windows.Forms;

//TODO: Edit mode
//TODO: Resize on Window resize

/*
    Box of ideas (Lazy TODO/idea list)
    - Scrollbar (-ish), you know, style
    - Top right (under title): Insert(INS)/Overwrite(OVR) (bool)
    - Search: /regex/ (Begins with && ends with)
    - Message(ProgressBar=bool) -> Progress bar (Dump) -> [ Done! ]
    - nagivation syncs (e.g. 32 - 33 -> 0 instead of just not doing it)
    - align offset (dividable by ElementsWidth?)
    - Edit: List<int, byte>(FilePosition, Byte)
      - Rendering: If byte at position, write that byte to display instead
      - Saving: Remove duplicates, loop through List and write
      - Editing: If new data on same position, replace
    - open memory process!!!!!!!!!!! (Windows)
    - Dump buffer only at the current position
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
                    switch (CurrentOffsetViewMode)
                    {
                        case OffsetViewMode.Hexadecimal:
                            Console.Write($"{((line * BytesInRow) + CurrentFilePosition).ToString("X8")}  ");
                            break;

                        case OffsetViewMode.Decimal:
                            Console.Write($"{((line * BytesInRow) + CurrentFilePosition).ToString("00000000")}  ");
                            break;

                        case OffsetViewMode.Octal:
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
        }

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
                string s = $"DEC: {CurrentFilePosition.ToString("00000000")} | HEX: {CurrentFilePosition.ToString("X8")} | OCT: {Convert.ToString(CurrentFilePosition, 8),8}";
                // Force clean last message.
                Console.Write(s + new string(' ', Console.WindowWidth - s.Length - 1));
            }
        }
        #endregion

        #region Enumerations
        /// <summary>
        /// Offset view enumeration.
        /// </summary>
        internal enum OffsetViewMode : byte
        {
            Hexadecimal,
            Decimal,
            Octal
        }

        /// <summary>
        /// Current <see cref="OffsetViewMode"/>.
        /// </summary>
        static OffsetViewMode CurrentOffsetViewMode;

        /// <summary>
        /// Writing mode enumeration.
        /// </summary>
        enum WritingMode : byte
        {
            Overwrite,
            Insert
        }

        /// <summary>
        /// Current <see cref="WritingMode"/>.
        /// </summary>
        static WritingMode CurrentWritingMode;
        #endregion

        #region Internal methods
        /// <summary>
        /// Open a file and starts the program with defaults.
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
            
            MainPanel.BytesInRow = pBytesRow;

            CurrentOffsetViewMode = pOffsetViewMode;

            PrepareScreen();

            Refresh();
            MainPanel.Update();

            // Someone was unhappy with the do {} while(); loop.
            while (ReadUserKey())
            { }
        }

        internal static void OpenClipboard()
        {
            //TODO: void OpenClipboard()

            bool foundData = false;
            object Data;

            // Specifies a Windows bitmap format.
            if (Clipboard.ContainsData("Bitmap"))
            {

                foundData = true;
            }
            // Specifies a comma-separated value (CSV) format, which is a common interchange format used by spreadsheets.
            // This format is not used directly by Windows Forms.
            if (Clipboard.ContainsData("CommaSeperatedValues") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows device-independent bitmap (DIB) format.
            if (Clipboard.ContainsData("Dib") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows Data Interchange Format (DIF), which Windows Forms does not directly use.
            if (Clipboard.ContainsData("Dif") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows enhanced metafile format.
            if (Clipboard.ContainsData("EnhancedMetafile") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows file drop format, which Windows Forms does not directly use.
            if (Clipboard.ContainsData("FileDrop") && !foundData)
            {

                foundData = true;
            }
            // Specifies text in the HTML Clipboard format.
            if (Clipboard.ContainsData("Html") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows culture format, which Windows Forms does not directly use.
            if (Clipboard.ContainsData("Locale") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows metafile format, which Windows Forms does not directly use.
            if (Clipboard.ContainsData("MetafilePict") && !foundData)
            {

                foundData = true;
            }
            // Specifies the standard Windows original equipment manufacturer (OEM) text format.
            if (Clipboard.ContainsData("OemText") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows palette format.
            if (Clipboard.ContainsData("Palette") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows pen data format, which consists of pen strokes for handwriting software;
            // Windows Forms does not use this format.
            if (Clipboard.ContainsData("PenData") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Resource Interchange File Format (RIFF) audio format,
            // which Windows Forms does not directly use.
            if (Clipboard.ContainsData("Riff") && !foundData)
            {

                foundData = true;
            }
            // Specifies text consisting of Rich Text Format (RTF) data.
            if (Clipboard.ContainsData("Rtf") && !foundData)
            {

                foundData = true;
            }
            // Specifies a format that encapsulates any type of Windows Forms object.
            if (Clipboard.ContainsData("Serializable") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows Forms string class format, which Windows Forms uses to store string objects.
            if (Clipboard.ContainsData("StringFormat") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Windows symbolic link format, which Windows Forms does not directly use.
            if (Clipboard.ContainsData("SymbolicLink") && !foundData)
            {

                foundData = true;
            }
            // Specifies the standard ANSI text format.
            if (Clipboard.ContainsData("Text") && !foundData)
            {

                foundData = true;
            }
            // Specifies the Tagged Image File Format (TIFF), which Windows Forms does not directly use.
            if (Clipboard.ContainsData("Tiff") && !foundData)
            {

                foundData = true;
            }
            // Specifies the standard Windows Unicode text format.
            if (Clipboard.ContainsData("UnicodeText") && !foundData)
            {

                foundData = true;
            }
            // Specifies the wave audio format, which Windows Forms does not directly use.
            if (Clipboard.ContainsData("WaveAudio") && !foundData)
            {

                foundData = true;
            }

            if (!foundData)
            {
                Message("The clipboard is empty.");
                return;
            }
        }

        /// <summary>
        /// Prepares the screen with the information needed.
        /// </summary>
        static void PrepareScreen()
        {
            UpdateTitlePanel();
            PlaceOffsetPanel();
            InfoPanel.Update();
            PlaceControlPanel();
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Read the current file.
        /// </summary>
        static void Refresh()
        {
            ReadAndUpdate(CurrentFilePosition);
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
                    sr.BaseStream.Length < MainPanel.ScreenMaxBytes ?
                    (int)sr.BaseStream.Length :
                    MainPanel.ScreenMaxBytes;

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
        /// <returns>True if still using 0xdd.</returns>
        static bool ReadUserKey()
        {
            ConsoleKeyInfo cki = Console.ReadKey(true);

            switch (cki.Key)
            {
                case ConsoleKey.Escape:
                    //TODO: Menu at ConsoleKey.Escape
                    break;

                // -- Hidden shortcuts --
                case ConsoleKey.F10:
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
                    {
                        int Position = -1;
                        bool gotNumber = false;
                        while (!gotNumber)
                        {
                            string t = ReadValue("Goto position:");
                            if (int.TryParse(t, out Position))
                            {
                                if (Position >= 0 && Position <= CurrentFile.Length)
                                {
                                    Goto(Position);
                                    gotNumber = true;
                                }
                                else
                                    Message("Position out of bound!");
                            }
                            else
                                Message("Need a number!");
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
                        ReadAndUpdate(--CurrentFilePosition, MainPanel.ScreenMaxBytes);
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (CurrentFilePosition + (MainPanel.ScreenMaxBytes) + 1 <= CurrentFile.Length)
                    {
                        ReadAndUpdate(++CurrentFilePosition);
                    }
                    break;

                case ConsoleKey.UpArrow:
                    if (CurrentFilePosition - MainPanel.BytesInRow >= 0)
                    {
                        ReadAndUpdate(CurrentFilePosition -= MainPanel.BytesInRow);
                    }
                    else
                    {
                        //TODO: Round if it reaches the beginning
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (CurrentFilePosition + (MainPanel.ScreenMaxBytes) + MainPanel.BytesInRow <= CurrentFile.Length)
                    {
                        ReadAndUpdate(CurrentFilePosition += MainPanel.BytesInRow);
                    }
                    else
                    {
                        //TODO: Round if it reaches end
                    }
                    break;

                case ConsoleKey.PageUp:
                    if (CurrentFilePosition - MainPanel.ScreenMaxBytes >= 0)
                    {
                        ReadAndUpdate(CurrentFilePosition -= MainPanel.ScreenMaxBytes);
                    }
                    else
                    {
                        //TODO: Round if it reaches the beginning
                    }
                    break;
                case ConsoleKey.PageDown:
                    if (CurrentFilePosition + (MainPanel.ScreenMaxBytes) + MainPanel.ScreenMaxBytes <= CurrentFile.Length)
                    {
                        ReadAndUpdate(CurrentFilePosition += MainPanel.ScreenMaxBytes);
                    }
                    else
                    {
                        //TODO: Round if it reaches end
                    }
                    break;

                case ConsoleKey.Home:
                    ReadAndUpdate(CurrentFilePosition = 0);
                    break;
                case ConsoleKey.End:
                    ReadAndUpdate(CurrentFilePosition = CurrentFile.Length - MainPanel.ScreenMaxBytes);
                    break;
            }

            return true;
        }

        static void ReadAndUpdate(long pPosition)
        {
            ReadAndUpdate(pPosition, MainPanel.FrameHeight);
        }

        static void ReadAndUpdate(long pPosition, int pLength)
        {
            ReadCurrentFile(pPosition);
            MainPanel.Update();
            InfoPanel.Update();
        }

        /// <summary>
        /// Update the upper bar.
        /// </summary>
        static void UpdateTitlePanel()
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
        static void PlaceOffsetPanel()
        {
            Console.SetCursorPosition(0, 1);
            Console.Write($"Offset {CurrentOffsetViewMode.GetChar()}  ");
            for (int i = 0; i < MainPanel.BytesInRow; i++)
            {
                Console.Write($"{i.ToString("X2")} ");
            }
        }

        /// <summary>
        /// Places the control map on screen (e.g. ^T Try jumping )
        /// </summary>
        static void PlaceControlPanel()
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
            Console.Write(" Goto         ");

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
            Console.SetCursorPosition(0, InfoPanel.StartingTopPosition);
            Console.Write(new string(' ', Console.WindowWidth - 1));

            string msg = $"[ {pMessage} ]";
            Console.SetCursorPosition((Console.WindowWidth / 2) - (msg.Length / 2), InfoPanel.StartingTopPosition);

            ToggleColors();

            Console.Write(msg);

            Console.ResetColor();
        }

        static void ToggleFullscreenMode()
        {
            if (Fullscreen)
            { // Turning off
                MainPanel.StartingTopPosition = 2;
                MainPanel.FrameHeight = Console.WindowHeight - 5;
                InfoPanel.StartingTopPosition = Console.WindowHeight - 3;
                Fullscreen = false;
                Console.Clear();
                PlaceControlPanel();
                PlaceOffsetPanel();
                UpdateTitlePanel();
                Refresh();
            }
            else
            { // Turning on
                MainPanel.StartingTopPosition = 1;
                MainPanel.FrameHeight = Console.WindowHeight - 2;
                InfoPanel.StartingTopPosition = Console.WindowHeight - 1;
                Fullscreen = true;
                Console.Clear();
                UpdateTitlePanel();
                Refresh();
            }
        }

        static void Goto(int pPosition)
        {
            CurrentFilePosition = pPosition;
            ReadAndUpdate(CurrentFilePosition);
        }

        static string ReadValue(string pMessage)
        {
            int width = 27;
            int height = 4;

            int startx = (Console.WindowWidth / 2) - (width / 2);
            int starty = (Console.WindowHeight / 2) - (height / 2);

            Console.SetCursorPosition(startx, starty);
            Console.Write('┌');
            Console.Write(new string('─', width - 2));
            Console.Write('┐');

            for (int i = 0; i < height - 2; i++)
            {
                Console.SetCursorPosition(startx, starty + i + 1);
                Console.Write('│');
            }
            for (int i = 0; i < height - 2; i++)
            {
                Console.SetCursorPosition(startx + width - 1, starty + i + 1);
                Console.Write('│');
            }

            Console.SetCursorPosition(startx, starty + height - 1);
            Console.Write('└');
            Console.Write(new string('─', width - 2));
            Console.Write('┘');
            
            Console.SetCursorPosition(startx + 1, starty + 1);
            Console.Write(pMessage);
            if (pMessage.Length < width - 2)
                Console.Write(new string(' ', width - pMessage.Length - 2));

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.SetCursorPosition(startx + 1, starty + 2);
            Console.Write(new string(' ', width - 2));

            Console.SetCursorPosition(startx + 1, starty + 2);
            string t = ConsoleTools.ReadLine(width - 2);
            Console.ResetColor();
            return t;
        }

        static void Dump()
        {
            Dump(CurrentFile.Name, MainPanel.BytesInRow, CurrentOffsetViewMode);
        }

        static internal int Dump(string pFileToDump, int pBytesInRow, OffsetViewMode pViewMode)
        {
            if (!File.Exists(pFileToDump))
                return 1;
            
            FileInfo file = new FileInfo(pFileToDump);
            int filelen = (int)file.Length;
            int line = 0;
            int BufferPositionHex = 0;
            int BufferPositionData = 0;
            // To not change the current buffer, we use a new one.
            byte[] buffer = new byte[pBytesInRow];

            using (StreamWriter sw = new StreamWriter($"{pFileToDump}.{NAME_EXTENSION}"))
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
                    sw.Write($" {Math.Round((double)filelen / 1024, 1)} KB");
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

                        line += pBytesInRow;

                        for (int c = 0; c < pBytesInRow; c++)
                        {
                            byte b = (byte)fs.ReadByte();

                            buffer[c] = b;
                        }

                        for (int pos = 0; pos < pBytesInRow; pos++)
                        {
                            if (BufferPositionHex < filelen)
                                sw.Write($"{buffer[pos].ToString("X2")} ");
                            else
                                sw.Write("   ");

                            BufferPositionHex++;
                        }

                        sw.Write(" ");

                        for (int pos = 0; pos < pBytesInRow; pos++)
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

            return 0;
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
                    return '?'; // ??????????
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
