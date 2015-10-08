using System;
using System.IO;
using static System.Console;
using static System.IO.File;
using static System.IO.Path;

//TODO: Edit mode

/*
    Box of ideas (Lazy TODO list)
    - --scrollable (BufferHeight) (if possible)
    - currenfile fileinfo
    - --dump
    - Scrollbar (-ish)
    - Top right: Insert(INS)/Overwrite(OVR)
    - Help section (F1)
    - Search: /regex/ (Begins with && ends with)
    - Info: Lines, Length
    - no args -> empty buffer
    - Message() -> [ Example message! ]
    - Message(ProgressBar=bool) -> Progress bar (Dump) -> [ Done! ]
*/

namespace ConHexView
{
    static class HexView
    {
        #region Constants
        const int SCROLL_LINE = 0x10;
        const int SCROLL_SECTION = 0x80;
        const int SCROLL_PAGE = 0x0100;
        const string NAME_OFFSET = "Offset";
        #endregion

        #region Properties
        /// <summary>
        /// Index.
        /// </summary>
        static int CurrentOffset = 0;

        static FileInfo CurrentFile;

        /// <summary>
        /// Current <see cref="OffsetViewMode"/> mode.
        /// </summary>
        static OffsetViewMode CurrentOffsetViewMode;

        /// <summary>
        /// Offset height.
        /// </summary>
        static int OffsetHeight = WindowHeight - 5;

        static byte[] Buffer = new byte[OffsetHeight * 16];
        #endregion

        #region Enumerations
        /// <summary>
        /// Enumeration of different offset views.
        /// </summary>
        public enum OffsetViewMode
        {
            // Offset h
            // 00000010
            Hexadecimal,
            // Offset d
            // 00000016
            Decimal,
            // Offset o
            // 00000020
            Octal
        }
        #endregion

        #region Public methods
        public static void Open(string pFilePath)
        {
            Open(pFilePath, OffsetViewMode.Hexadecimal);
        }

        public static void Open(string pFilePath, OffsetViewMode pOffsetViewMode)
        {
            if (!Exists(pFilePath))
                throw new FileNotFoundException
                {
                    Source = pFilePath
                };

            CurrentFile = new FileInfo(pFilePath);

            CurrentOffsetViewMode = pOffsetViewMode;

            UpdateTitleMap();
            UpdateOffsetMap();
            UpdateInfoMap();
            PlaceMainControlMap();

            Read();

            do
            {
                UpdateMainScreen();
            } while (ReadUserKey());
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Read the current file.
        /// </summary>
        static void Read()
        {
            Read(0);
        }

        /// <summary>
        /// Read the current file at a certain position.
        /// </summary>
        /// <param name="pBaseOffset">Position.</param>
        static void Read(int pBaseOffset)
        {
            //TODO: Do smart reading to reduce the input lag (you know)

            using (StreamReader sr = new StreamReader(CurrentFile.FullName))
            {
                sr.BaseStream.Position = pBaseOffset;

                for (int y = 0; y < OffsetHeight; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        Buffer[x] = (byte)sr.Read();
                    }
                }
            }
        }

        /// <summary>
        /// Read the user's key.
        /// </summary>
        /// <returns><see cref="true"/> if using the program.</returns>
        static bool ReadUserKey()
        {
            ConsoleKeyInfo cki = ReadKey(true);

            switch (cki.Key)
            {
                case ConsoleKey.Escape:
                    return Exit();

                case ConsoleKey.X:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        return Exit();
                    break;

                case ConsoleKey.I:
                    if (cki.Modifiers == ConsoleModifiers.Control)
                        //TODO: Better Info function
                        Message($"Size: {CurrentFile.Length}");
                    break;

                case ConsoleKey.Home:
                    CurrentOffset = 0;
                    ReadAndUpdate(CurrentOffset);
                    break;
                case ConsoleKey.End:
                    CurrentOffset = (int)CurrentFile.Length - OffsetHeight;
                    ReadAndUpdate(CurrentOffset);
                    break;

                case ConsoleKey.PageUp:
                    if (CurrentOffset - SCROLL_PAGE >= 0)
                    {
                        CurrentOffset -= SCROLL_PAGE;
                        ReadAndUpdate(CurrentOffset, SCROLL_PAGE);
                    }
                    break;
                case ConsoleKey.PageDown:
                    if (CurrentOffset + SCROLL_PAGE < CurrentFile.Length)
                    {
                        CurrentOffset += SCROLL_PAGE;
                        ReadAndUpdate(CurrentOffset, SCROLL_PAGE);
                    }
                    break;
                    
                case ConsoleKey.UpArrow:
                    if (CurrentOffset - SCROLL_LINE >= 0)
                    {
                        CurrentOffset -= SCROLL_LINE;
                        ReadAndUpdate(CurrentOffset, SCROLL_PAGE);
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (CurrentOffset + SCROLL_LINE < CurrentFile.Length)
                    {
                        CurrentOffset += SCROLL_LINE;
                        ReadAndUpdate(CurrentOffset, SCROLL_PAGE);
                    }
                    break;
            }

            return true;
        }

        static void ReadAndUpdate(int pOffset)
        {
            ReadAndUpdate(pOffset, OffsetHeight);
        }

        static void ReadAndUpdate(int pOffset, int pLength)
        {
            //TODO: Update void ReadAndUpdate(int, int)
            Read(pOffset);
            UpdateMainScreen();
            UpdateInfoMap();
        }

        static void UpdateMainScreen()
        {
            UpdateMainScreen(Buffer.Length);
        }

        /// <summary>
        /// Update the section of the screen with the data.
        /// </summary>
        static void UpdateMainScreen(int pLength)
        {
            SetCursorPosition(0, 2);

            //TODO: Find a way to stop rendering before total length
            // Hint:
            // CurrentOffset [+ index] <= CurrentFile.Length

            for (int y = 0; y < pLength; y += 0x10)
            {
                switch (CurrentOffsetViewMode)
                {
                    case OffsetViewMode.Hexadecimal:
                        // 00000010
                        Write($"{((y * 0x10) + CurrentOffset).ToString("X8")}  ");
                        break;
                    case OffsetViewMode.Decimal:
                        // 00000016
                        Write($"{(y * 16) + CurrentOffset.ToString("00000000")}");
                        break;
                    case OffsetViewMode.Octal:
                        //       20
                        Write($"{Convert.ToString((y * 16) + CurrentOffset, 8), 8}  ");
                        break;
                }
                
                //TODO: you know
                for (int x = y; x < 16; x++)
                {
                    Write($"{Buffer[x].ToString("X2")} ");
                }

                Write(" ");
                
                for (int x = 0; x < 16; x++)
                {
                    Write($"{Buffer[x].ToChar()}");
                }

                WriteLine();
            }
        }

        /// <summary>
        /// Update the upper bar.
        /// </summary>
        static void UpdateTitleMap()
        {
            ToggleColors();
            
            SetCursorPosition(0, 0);
            // Should I include the project's name in the upper bar?
            Write($"{CurrentFile.Name}");
            Write(new string(' ', WindowWidth - CurrentFile.Name.Length));

            ResetColor();
        }

        /// <summary>
        /// Update the offset map (left view)
        /// </summary>
        static void UpdateOffsetMap()
        {
            SetCursorPosition(0, 1);
            Write($"Offset {CurrentOffsetViewMode.GetChar()}  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
        }

        /// <summary>
        /// Update the data map (right view)
        /// </summary>
        static void UpdateInfoMap()
        {
            SetCursorPosition(0, WindowHeight - 3);
            // CAN'T THIS LINE BE ANY LONGER?
            Write($@"
{NAME_OFFSET} (DEC): {CurrentOffset.ToString("00000000")} |
{NAME_OFFSET} (HEX): {CurrentOffset.ToString("X8")} |
{NAME_OFFSET} (OCT): {Convert.ToString(CurrentOffset, 8), 8}");
        }

        /// <summary>
        /// Places the control map on screen (e.g. ^D Die)
        /// </summary>
        static void PlaceMainControlMap()
        {
            SetCursorPosition(0, WindowHeight - 2);

            ToggleColors();
            Write("^K");
            ResetColor();
            Write(" Help         ");

            ToggleColors();
            Write("^F");
            ResetColor();
            Write(" Find         ");

            ToggleColors();
            Write("^G");
            ResetColor();
            Write(" Goto line    ");

            ToggleColors();
            Write("^H");
            ResetColor();
            Write(" Replace      ");

            ToggleColors();
            Write("^E");
            ResetColor();
            Write(" Edit mode");

            WriteLine();

            ToggleColors();
            Write("^X");
            ResetColor();
            Write(" Exit         ");

            ToggleColors();
            Write("^I");
            ResetColor();
            Write(" Info         ");

            ToggleColors();
            Write("^D");
            ResetColor();
            Write(" Dump         ");

            ToggleColors();
            Write("^V");
            ResetColor();
            Write(" Offset view  ");

            ToggleColors();
            Write("^A");
            ResetColor();
            Write(" Data view");
        }

        /// <summary>
        /// Toggles current ForegroundColor to black
        /// and BackgroundColor to gray.
        /// </summary>
        static void ToggleColors()
        {
            ForegroundColor = ConsoleColor.Black;
            BackgroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Displays a message on screen to inform the user.
        /// </summary>
        /// <param name="pMessage">Message to show.</param>
        static void Message(string pMessage)
        {
            SetCursorPosition(0, WindowHeight - 3);
            Write(new string(' ', WindowWidth));

            string msg = $"[ {pMessage} ]";
            SetCursorPosition((WindowWidth / 2) - (msg.Length / 2), WindowHeight - 3);

            ToggleColors();

            Write(msg);

            ResetColor();
        }

        static int ReadValue()
        {
            //TODO: int ReadValue()
            throw new NotImplementedException();
        }

        static void Dump()
        {
            //TODO: Finish void Dump()
            using (StreamWriter sw = new StreamWriter($"{CurrentFile.Name}.txt"))
            {
                sw.WriteLine(CurrentFile.Name);
                sw.WriteLine();

                using (StreamReader sr = new StreamReader(CurrentFile.FullName))
                {

                }
            }
        }

        /// <summary>
        /// The user is exiting the application.
        /// So we prepare the departure.
        /// </summary>
        /// <returns>Always true.</returns>
        /// <remarks>It's false due to the while loop.</remarks>
        static bool Exit()
        {
            Clear();

            return false;
        }
        #endregion

        #region Type extensions
        /// <summary>
        /// Returns a safe character for the console (cmd) to display.
        /// </summary>
        /// <param name="pIn">Byte to transform.</param>
        /// <returns>Safe console character.</returns>
        static char ToChar(this byte pIn)
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
}
