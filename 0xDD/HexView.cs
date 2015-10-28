using System;
using System.IO;

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
    - Dump buffer only at the current position feature
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
            while (ReadUserKey()) { }
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
                {
                    len = (int)sr.BaseStream.Length;
                    Buffer = new byte[len];
                }
                else
                {
                    len = MainPanel.ScreenMaxBytes;
                    Buffer = new byte[len];
                }

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

                //case ConsoleKey.:
                        //TODO: Open Dialog
                    //break;

                // -- Shown shortcuts --
                // Help
                case ConsoleKey.F1:
                case ConsoleKey.K:
                    if (cki.Modifiers == ConsoleModifiers.Control &&
                        cki.Key == ConsoleKey.K ||
                        cki.Key == ConsoleKey.F1)
                        ShowHelp();
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

                            if (t.Length == 0)
                            {
                                MainPanel.Update();
                                break;
                            }

                            // Hex
                            if (t.StartsWith("0x"))
                            {
                                try
                                {
                                    Position = Convert.ToInt32(t, 16);
                                }
                                catch (Exception)
                                {
                                    Message("Need a valid number!");
                                    //break;
                                }
                            }
                            // Oct
                            else if (t[0] == '0')
                            {
                                try
                                {
                                    Position = Convert.ToInt32(t, 8);
                                }
                                catch (Exception)
                                {
                                    Message("Need a valid number!");
                                    //break;
                                }
                            }
                            // Dec
                            else
                            {
                                if (!int.TryParse(t, out Position))
                                {
                                    Message("Need a valid number!");
                                    //break;
                                }
                            }

                            if (Position >= 0 && Position <= CurrentFile.Length - MainPanel.ScreenMaxBytes)
                            {
                                Goto(Position);
                                gotNumber = true;
                            }
                            else
                                Message("Position out of bound!");
                        }
                    }
                    break;

                // Offset base
                case ConsoleKey.O:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        string c = ReadValue("Hex, Dec, or Oct?:");

                        if (c == null || c.Length < 1)
                        {
                            MainPanel.Update();
                            Message("Field was empty!");
                            break;
                        }

                        switch (c[0])
                        {
                            case 'H':
                            case 'h':
                                CurrentOffsetBaseView = OffsetBaseView.Hexadecimal;
                                PlaceOffsetPanel();
                                MainPanel.Update();
                                // In case of remaining message.
                                InfoPanel.Update();
                                break;

                            case 'O':
                            case 'o':
                                CurrentOffsetBaseView = OffsetBaseView.Octal;
                                PlaceOffsetPanel();
                                MainPanel.Update();
                                InfoPanel.Update();
                                break;

                            case 'D':
                            case 'd':
                                CurrentOffsetBaseView = OffsetBaseView.Decimal;
                                PlaceOffsetPanel();
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
                        Message(
                            $"Size: {CurrentFile.Length} | StartPos: {Math.Round(((decimal)CurrentFilePosition / CurrentFile.Length) * 100)}% | EndPos: {Math.Round(((decimal)(CurrentFilePosition + MainPanel.ScreenMaxBytes) / CurrentFile.Length) * 100)}%"
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
                    if (CurrentFilePosition + MainPanel.ScreenMaxBytes + MainPanel.BytesInRow <= CurrentFile.Length)
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
                    if (CurrentFilePosition + (MainPanel.ScreenMaxBytes * 2) <= CurrentFile.Length)
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
            Console.Write($"Offset {CurrentOffsetBaseView.GetChar()}  ");
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
            Console.Write("^O");
            Console.ResetColor();
            Console.Write(" Offset base  ");

            /*
            ToggleColors();
            Console.Write("^");
            Console.ResetColor();
            Console.Write(" ");
            */
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
                MainPanel.Refresh();
            }
            else
            { // Turning on
                MainPanel.StartingTopPosition = 1;
                MainPanel.FrameHeight = Console.WindowHeight - 2;
                InfoPanel.StartingTopPosition = Console.WindowHeight - 1;
                Fullscreen = true;
                Console.Clear();
                UpdateTitlePanel();
                MainPanel.Refresh();
            }
        }

        static void Goto(int pPosition)
        {
            CurrentFilePosition = pPosition;
            ReadAndUpdate(CurrentFilePosition);
        }

        static string ReadValue(string pMessage)
        {
            return ReadValue(pMessage, 27, 4);
        }

        static string ReadValue(string pMessage, int pWidth, int pHeight)
        {
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

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.SetCursorPosition(startx + 1, starty + 2);
            Console.Write(new string(' ', pWidth - 2));

            Console.SetCursorPosition(startx + 1, starty + 2);
            string t = ConsoleTools.ReadLine(pWidth - 2);
            Console.ResetColor();
            return t;
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
            byte[] buffer = new byte[pBytesInRow];

            using (StreamWriter sw = new StreamWriter($"{pFileToDump}.{NAME_EXTENSION}"))
            {
                sw.WriteLine(file.Name);
                sw.WriteLine();
                sw.Write("Size: ");

                if (filelen > Math.Pow(1024, 3)) // GB
                {
                    sw.Write($"{Math.Round(filelen / Math.Pow(1024, 3), 2)} GB");
                    sw.Write($"({filelen} B)");
                }
                else if (filelen > Math.Pow(1024, 2)) // MB
                {
                    sw.Write($"{Math.Round(filelen / Math.Pow(1024, 2), 2)} MB");
                    sw.Write($"({filelen} B)");
                }
                else if (filelen > 1024) // KB
                {
                    sw.Write($"{Math.Round((double)filelen / 1024, 1)} KB");
                    sw.Write($"({filelen} B)");
                }
                else
                {
                    sw.Write($"{filelen} B");
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

        #region Help
        static void ShowHelp()
        {
            MainPanel.StartingTopPosition = 2;
            MainPanel.FrameHeight = Console.WindowHeight - 2;

            int pos = 0;
            MainPanel.Clear();
            RenderHelp(pos);
            while (HelpKeyDown(ref pos))
            {
                MainPanel.Clear();
                RenderHelp(pos);
            }

            PlaceOffsetPanel();
            MainPanel.Update();
            MainPanel.StartingTopPosition = 1;
            MainPanel.FrameHeight = Console.WindowHeight - 5;
        }

        static bool HelpKeyDown(ref int pPosition)
        {
            ConsoleKeyInfo cki = Console.ReadKey(true);

            switch (cki.Key)
            {
                case ConsoleKey.X:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        return Exit();
                    break;

                case ConsoleKey.Escape:
                    return false;

                case ConsoleKey.UpArrow:
                    pPosition -= 1;
                    break;
                case ConsoleKey.DownArrow:
                    pPosition += 1;
                    break;

                case ConsoleKey.PageUp:
                    pPosition -= MainPanel.FrameHeight;
                    break;
                case ConsoleKey.PageDown:
                    pPosition += MainPanel.FrameHeight;
                    break;
            }

            //RenderHelp(pPosition);
            return true;
        }

        static void RenderHelp(int pPosition)
        {
            //todo: rawr by line pls maybe strin array????
            Console.SetCursorPosition(0, MainPanel.StartingTopPosition);
            Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("_0xdd.Help.txt");
            using (StreamReader sr = new StreamReader(s))
            {
                sr.BaseStream.Position = pPosition;
                for (int i = 0; i < MainPanel.FrameHeight; i++)
                {
                    Console.WriteLine(sr.ReadLine());
                }
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
            if (pIn < 0x20 || pIn > 0x7F)
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

    /// <summary>
    /// Progressbar specifically for the hex viewer.
    /// </summary>
    class HexViewProgressBar
    {
        //TODO: HexViewProgressBar
    }
}
