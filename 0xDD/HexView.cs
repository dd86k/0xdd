using System;
using System.IO;
using static System.Console;
using static System.IO.File;
using static System.IO.Path;

//TODO: Edit mode

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
        
        struct CurrentFile
        {
            static internal string Path
            {
                get; set;
            }

            static internal string Filename
            {
                get
                {
                    if (Path != null && Path != string.Empty)
                        return GetFileName(Path);
                    else
                        return string.Empty;
                }
            }

            static internal string FilenameWithoutExtension
            {
                get
                {
                    if (Path != null && Path != string.Empty)
                        return GetFileNameWithoutExtension(Path);
                    else
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Current <see cref="OffsetViewMode"/> mode.
        /// </summary>
        static OffsetViewMode CurrentOffsetViewMode;

        /// <summary>
        /// Length of the current file.
        /// </summary>
        static long FileLength = 0;

        /// <summary>
        /// Offset height.
        /// </summary>
        static int OffsetHeight = WindowHeight - 5;

        static byte[,] Buffer = new byte[OffsetHeight, 16];

        readonly static ConsoleColor fgOriginal = ForegroundColor;
        readonly static ConsoleColor bgOriginal = BackgroundColor;
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

            CurrentFile.Path = pFilePath;
            CurrentOffsetViewMode = pOffsetViewMode;

            UpdateTitleMap();
            UpdateOffsetMap();
            PlaceControlMap();

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
            using (StreamReader sr = new StreamReader(CurrentFile.Path))
            {
                sr.BaseStream.Position = pBaseOffset;
                FileLength = sr.BaseStream.Length;

                for (int y = 0; y < OffsetHeight; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        //TODO: Improve buffer usage (Fix unecessary 0xFF's)
                        Buffer[y, x] = (byte)sr.Read();
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
                    return false;

                case ConsoleKey.X:
                    //if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
                    if (cki.Modifiers == ConsoleModifiers.Control)
                    {
                        return Exit();
                    }
                    break;

                case ConsoleKey.Home:
                    CurrentOffset = 0;
                    break;
                case ConsoleKey.End:
                    CurrentOffset = (int)FileLength - OffsetHeight;
                    break;

                case ConsoleKey.PageUp:
                    if (CurrentOffset - SCROLL_PAGE >= 0)
                        CurrentOffset -= SCROLL_PAGE;
                    break;
                case ConsoleKey.PageDown:
                    if (CurrentOffset + SCROLL_PAGE < FileLength)
                        CurrentOffset += SCROLL_PAGE;
                    break;
                    
                case ConsoleKey.UpArrow:
                    if (CurrentOffset - SCROLL_LINE >= 0)
                        CurrentOffset -= SCROLL_LINE;
                    break;
                case ConsoleKey.DownArrow:
                    if (CurrentOffset + SCROLL_LINE < FileLength)
                        CurrentOffset += SCROLL_LINE;
                    break;
            }

            Read(CurrentOffset);

            return true;
        }

        /// <summary>
        /// Update the section of the screen with the data.
        /// </summary>
        static void UpdateMainScreen()
        {
            SetCursorPosition(0, 2);
            for (int y = 0; y < OffsetHeight; y++)
            {
                switch (CurrentOffsetViewMode)
                {
                    case OffsetViewMode.Hexadecimal:
                        // 00000010
                        Write($"{((y * 0x10) + CurrentOffset).ToString("X8")}  ");
                        break;
                    case OffsetViewMode.Decimal:
                        //       16
                        Write($"{(y * 10) + CurrentOffset, 8}  ");
                        break;
                    case OffsetViewMode.Octal:
                        //       20
                        Write($"{Convert.ToString((y * 8) + CurrentOffset, 8)}  ");
                        break;
                }
                

                for (int x = 0; x < 16; x++)
                {
                    Write($"{Buffer[y, x].ToString("X2")} ");
                }

                Write(" ");
                
                for (int x = 0; x < 16; x++)
                {
                    Write($"{Buffer[y, x].ToChar()}");
                }

                WriteLine();
            }

            UpdateInfoMap();
        }

        /// <summary>
        /// Update the upper bar.
        /// </summary>
        static void UpdateTitleMap()
        {
            ToggleColors();
            
            SetCursorPosition(0, 0);
            // Should I include the project's name in the upper bar?
            Write($"{CurrentFile.Filename}");
            Write(new string(' ', WindowWidth - CurrentFile.Filename.Length));

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
            Write($"{NAME_OFFSET} (DEC): {CurrentOffset, -12} {NAME_OFFSET} (HEX): {CurrentOffset.ToString("X8"), 8}  {NAME_OFFSET} (OCT): {Convert.ToString(CurrentOffset, 8), -10}");
        }

        /// <summary>
        /// Places the control map on screen (e.g. ^D Die)
        /// </summary>
        static void PlaceControlMap()
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
            //TODO: void Message(string)
        }

        static int ReadValue()
        {
            //TODO: int ReadValue()
            throw new NotImplementedException();
        }

        /// <summary>
        /// The user is exiting the application.
        /// So we prepare the departure.
        /// </summary>
        /// <returns>Always true.</returns>
        /// <remarks>It's false due to the while loop.</remarks>
        static bool Exit()
        {
            // Past characters are still on screen:
            WriteLine();
            WriteLine();
            // Or: Clear screen
            //Clear();

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
            }

            return '?';
        }
        #endregion
    }
}
