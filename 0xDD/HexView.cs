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
        /// Current offset in the file.
        /// </summary>
        static int CurrentFileOffset = 0;

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
        static int FrameHeight = WindowHeight - 5;

        /// <summary>
        /// Data width, default is 16.
        /// </summary>
        static int ElementsWidth = 16;

        static int FrameCapacity = ElementsWidth * FrameHeight;

        static byte[] Buffer = new byte[0];
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

            Read();

            UpdateTitleMap();
            PlaceOffsetMap();
            UpdateMainScreen();
            UpdateInfoMap();
            PlaceMainControlMap();

            do
            {

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

                // -- Nagivation --
                case ConsoleKey.Home:
                    CurrentFileOffset = 0;
                    ReadAndUpdate(CurrentFileOffset);
                    break;
                case ConsoleKey.End:
                    CurrentFileOffset = (int)(CurrentFile.Length) - FrameHeight;
                    ReadAndUpdate(CurrentFileOffset);
                    break;

                case ConsoleKey.PageUp:
                    if (CurrentFileOffset - SCROLL_PAGE >= 0)
                    {
                        CurrentFileOffset -= SCROLL_PAGE;
                        ReadAndUpdate(CurrentFileOffset, SCROLL_PAGE);
                    }
                    break;
                case ConsoleKey.PageDown:
                    if (CurrentFileOffset + SCROLL_PAGE < CurrentFile.Length)
                    {
                        CurrentFileOffset += SCROLL_PAGE;
                        ReadAndUpdate(CurrentFileOffset, SCROLL_PAGE);
                    }
                    break;
                    
                case ConsoleKey.UpArrow:
                    if (CurrentFileOffset - SCROLL_LINE >= 0)
                    {
                        CurrentFileOffset -= SCROLL_LINE;
                        ReadAndUpdate(CurrentFileOffset, SCROLL_PAGE);
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (CurrentFileOffset + SCROLL_LINE < CurrentFile.Length)
                    {
                        CurrentFileOffset += SCROLL_LINE;
                        ReadAndUpdate(CurrentFileOffset, SCROLL_PAGE);
                    }
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
            //TODO: Update void ReadAndUpdate(int, int)
            Read(pOffset);
            UpdateMainScreen();
            UpdateInfoMap();
        }

        /// <summary>
        /// Update the section of the screen with the data.
        /// </summary>
        static void UpdateMainScreen()
        {
            int buflen = Buffer.Length;

            int filelen = (int)CurrentFile.Length;

            int lines = CurrentFileOffset + (FrameHeight * ElementsWidth) > filelen ?
                (filelen - (CurrentFileOffset + FrameHeight)) / ElementsWidth :
                FrameHeight;

            int BufferOffsetHex = 0;
            int BufferOffsetData = 0;

            SetCursorPosition(0, 2);

            for (int line = 0; line < lines; line++)
            {
                switch (CurrentOffsetViewMode)
                {
                    case OffsetViewMode.Hexadecimal:
                        Write($"{((line * ElementsWidth) + CurrentFileOffset).ToString("X8")}  ");
                        break;

                    case OffsetViewMode.Decimal:
                        Write($"{((line * ElementsWidth) + CurrentFileOffset).ToString("00000000")}  ");
                        break;

                    case OffsetViewMode.Octal:
                        Write($"{Convert.ToString((line * ElementsWidth) + CurrentFileOffset, 8), 8}  ");
                        break;
                }

                for (int x = 0; x < ElementsWidth; x++)
                {
                    if (CurrentFileOffset + BufferOffsetData < filelen)
                        Write($"{Buffer[BufferOffsetData].ToString("X2")} ");
                    else
                        Write("   ");

                    BufferOffsetData++;
                }

                Write(" ");

                for (int x = 0; x < ElementsWidth; x++)
                {
                    if (CurrentFileOffset + BufferOffsetHex < filelen)
                        Write($"{Buffer[BufferOffsetHex].ToSafeChar()}");
                    else
                        Write(" ");

                    BufferOffsetHex++;
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
            Write(CurrentFile.Name);
            Write(new string(' ', WindowWidth - CurrentFile.Name.Length));

            ResetColor();
        }

        /// <summary>
        /// Update the offset map
        /// </summary>
        static void PlaceOffsetMap()
        {
            SetCursorPosition(0, 1);
            Write($"Offset {CurrentOffsetViewMode.GetChar()}  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
        }

        /// <summary>
        /// Update the offset information
        /// </summary>
        static void UpdateInfoMap()
        {
            SetCursorPosition(0, WindowHeight - 3);
            // CAN'T THIS LINE BE ANY LONGER?
            Write($"{NAME_OFFSET} (DEC): {CurrentFileOffset.ToString("00000000")} | {NAME_OFFSET} (HEX): {CurrentFileOffset.ToString("X8")} | {NAME_OFFSET} (OCT): {Convert.ToString(CurrentFileOffset, 8), 8}");
        }

        /// <summary>
        /// Places the control map on screen (e.g. ^J Try jumping)
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
            using (StreamWriter sw = new StreamWriter($"{CurrentFile.Name}.{NAME_EXTENSION}"))
            {
                sw.Write(CurrentFile.Name);
                sw.Write(" - ");
                sw.Write(CurrentFile.Length);
                sw.WriteLine();
                sw.WriteLine();

                using (FileStream fs = CurrentFile.OpenRead())
                {
                    fs.CopyTo(sw.BaseStream);
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
}
