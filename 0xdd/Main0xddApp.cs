using System;

//TODO: Hashing (with menu) (v1.1)
//TODO: Search from end of file (v0.7)
//TODO: Settings! (v0.8)

/*
    TODO: Edit mode (v0.9)
    TODO: Replace Action (v0.9)
    - Top right: Insert(INS)/Overwrite(OVR)
    - Cursor/Navigation mode?
    - Edit:
      - Dictionary<long, byte> Edits (FilePosition, Data)
      - Rendering: If byte at position, write that byte to display instead
      - Saving: Remove duplicates, loop through List and write (at positions)
      - Editing: If new data on same position, replace in Edits (overwrite)

0E 0F
D2 DD ..
^^    ^
highlighted (black on gray) while navigating
*/

namespace _0xdd
{
    #region Enumerations
    enum ErrorCode : byte
    {
        Success = 0,

        // File
        FileNotFound = 0x4,
        FileUnreadable = 0x5,
        FileAlreadyOpen = 0x6,
        FileUnauthorized = 0x7,
        FileZero = 0x8,

        // Position
        PositionOutOfBound = 0x10,

        // Dump
        DumberCannotWrite = 0x18,
        DumberCannotRead = 0x19,

        // Find
        FinderNoResult = 0x20,
        FinderEmptyString = 0x21,

        // Program
        ProgramNoParse = 0xA0,

        // CLI
        CLI_InvalidOffsetView = 0xC0,
        CLI_InvalidWidth = 0xC4,

        // Misc.
        OSNotSupported = 0xD0,
        NotImplemented = 0xD1,
        
        UnknownError = 0xFE,
    }

    enum OffsetView : byte
    {
        Hex, Dec, Oct
    }
    
    enum OperatingMode : byte
    {
        // READ, OVRW, INSR
        Read, Overwrite, Insert
    }
    #endregion

    static class Main0xddApp
    {
        #region Properties
        public static ErrorCode LastError { get; private set; }

        static int LastWindowHeight;
        static int LastWindowWidth;

        //TODO: re-add autoadjust + fixed width
        public static bool AutoAdjust { get; set; }
        public static OffsetView OffsetView { get; set; }
        public static int BytesPerRow { get; set; }
        
        static bool inApp = true;

        //public static byte BytePerGroup = 1;
        #endregion
    
        public static ErrorCode Open(string path,
            OffsetView view = OffsetView.Hex, int bytesPerRow = 0)
        {
            LastError = FilePanel.Open(path);

            if (LastError != ErrorCode.Success)
                return LastError;
            
            if (BytesPerRow <= 0)
                BytesPerRow = Utils.GetBytesInRow();

            OffsetView = view;
            LastWindowHeight = Console.WindowHeight;
            LastWindowWidth = Console.WindowWidth;

            AutoAdjust = bytesPerRow <= 0;
            BytesPerRow = AutoAdjust ? Utils.GetBytesInRow() : bytesPerRow;

            try
            { // Mono can have some issues with these.
                Console.Title = $"{Program.Name} •️ {FilePanel.File.Name}";
                Console.CursorVisible = false;
            } catch { }

            Console.Clear();

            MenuBarPanel.Initialize();
            OffsetPanel.Initialize();
            FilePanel.Initialize();
            InfoPanel.Update();

            while (inApp)
                ReadUserKey();

            return LastError;
        }

        /// <summary>
        /// Read user input.
        /// </summary>
        static void ReadUserKey()
        {
            ConsoleKeyInfo k = Console.ReadKey(true);
            
            //if (AutoAdjust)
            if (LastWindowHeight != Console.WindowHeight ||
                LastWindowWidth != Console.WindowWidth)
            {
                BytesPerRow = Utils.GetBytesInRow();

                Console.Clear();
                FilePanel.Initialize();
                MenuBarPanel.Draw();

                LastWindowHeight = Console.WindowHeight;
                LastWindowWidth = Console.WindowWidth;
            }
            
            switch (k.Key)
            {
                /*
                 * Navigation
                 */

                case ConsoleKey.LeftArrow:
                    if (FilePanel.BufferSize < FilePanel.FileSize)
                        if (k.Modifiers == ConsoleModifiers.Control)
                        {
                            Goto(FilePanel.CurrentPosition -
                                (FilePanel.CurrentPosition % BytesPerRow));
                        }
                        else if (FilePanel.CurrentPosition - 1 >= 0)
                        {
                            Goto(FilePanel.CurrentPosition - 1);
                        }
                    break;
                case ConsoleKey.RightArrow:
                    if (FilePanel.BufferSize < FilePanel.FileSize)
                        if (k.Modifiers == ConsoleModifiers.Control)
                        {
                            long NewPos = FilePanel.CurrentPosition +
                                (BytesPerRow - FilePanel.CurrentPosition % BytesPerRow);

                            if (NewPos + FilePanel.BufferSize <= FilePanel.FileSize)
                                Goto(NewPos);
                            else
                                Goto(FilePanel.FileSize - FilePanel.BufferSize);
                        }
                        else if (FilePanel.CurrentPosition + FilePanel.BufferSize + 1 <= FilePanel.FileSize)
                        {
                            Goto(FilePanel.CurrentPosition + 1);
                        }
                    break;

                case ConsoleKey.UpArrow:
                    if (FilePanel.BufferSize < FilePanel.FileSize)
                        if (FilePanel.CurrentPosition - BytesPerRow >= 0)
                        {
                            Goto(FilePanel.CurrentPosition - BytesPerRow);
                        }
                        else
                        {
                            Goto(0);
                        }
                    break;
                case ConsoleKey.DownArrow:
                    if (FilePanel.BufferSize < FilePanel.FileSize)
                        if (FilePanel.CurrentPosition + FilePanel.BufferSize + BytesPerRow <= FilePanel.FileSize)
                        {
                            Goto(FilePanel.CurrentPosition + BytesPerRow);
                        }
                        else
                        {
                            Goto(FilePanel.FileSize - FilePanel.BufferSize);
                        }
                    break;

                case ConsoleKey.PageUp:
                    if (FilePanel.BufferSize < FilePanel.FileSize)
                        if (FilePanel.CurrentPosition - FilePanel.BufferSize >= 0)
                        {
                            Goto(FilePanel.CurrentPosition - FilePanel.BufferSize);
                        }
                        else
                        {
                            Goto(0);
                        }
                    break;
                case ConsoleKey.PageDown:
                    if (FilePanel.BufferSize < FilePanel.FileSize)
                        if (FilePanel.CurrentPosition + (FilePanel.BufferSize * 2) <= FilePanel.FileSize)
                        {
                            Goto(FilePanel.CurrentPosition + FilePanel.BufferSize);
                        }
                        else
                        {
                            Goto(FilePanel.FileSize - FilePanel.BufferSize);
                        }
                    break;

                case ConsoleKey.Home:
                    if (FilePanel.BufferSize < FilePanel.FileSize)
                        Goto(0);
                    break;
                case ConsoleKey.End:
                    if (FilePanel.BufferSize < FilePanel.FileSize)
                        Goto(FilePanel.FileSize - FilePanel.BufferSize);
                    break;

                /*
                 * Actions
                 */

                case ConsoleKey.F5:
                    FilePanel.Refresh();
                    break;

                case ConsoleKey.Escape:
                    MenuBarPanel.Enter();
                    break;

                // Find byte
                case ConsoleKey.W:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Dialog.PromptFindByte();
                    }
                    break;

                // Find data
                case ConsoleKey.J:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Dialog.PromptSearchString();
                    }
                    break;

                // Goto
                case ConsoleKey.G:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Dialog.PromptGoto();
                    }
                    break;

                // Offset base
                case ConsoleKey.O:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Dialog.PromptOffset();
                    }
                    break;

                // Edit mode
                case ConsoleKey.E:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message("Not implemented. Sorry!");
                    }
                    break;

                // Replace
                case ConsoleKey.H:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message("Not implemented. Sorry!");
                    }
                    break;

                // Info
                case ConsoleKey.I:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message(
                            $"{FilePanel.File.Name} {Utils.GetEntryInfo(FilePanel.File)} {Utils.FormatSize(FilePanel.FileSize)}"
                        );
                    }
                    break;

                // Exit
                case ConsoleKey.X:
                    if (k.Modifiers == ConsoleModifiers.Control)
                        Exit();
                    break;

                // Dump
                case ConsoleKey.D:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        InfoPanel.Message("Dumping...");
                        Dumper.Dump(FilePanel.File.FullName, BytesPerRow, OffsetView);
                        InfoPanel.Message("Dumping done!");
                    }
                    break;
            }
        }

        
        
        /// <summary>
        /// Read file, update MainPanel, then update InfoPanel.
        /// </summary>
        /// <param name="position">New position.</param>
        public static void Goto(long position)
        {
            FilePanel.Read(position);
            FilePanel.Update();
            InfoPanel.Update();
        }

        /// <summary>
        /// Signal the loops to end, and exit program.
        /// </summary>
        public static void Exit()
        {
            Console.ResetColor();
            Console.SetCursorPosition(
                Console.WindowWidth - 1,
                Console.WindowHeight - 1
            );
            Console.WriteLine();
            Console.CursorVisible = true;

            inApp = false;
        }

        public static string ToOct(this int l, int width) =>
            Convert.ToString(l, 8).PadLeft(width, '0');

        public static string ToOct(this long l, int width) =>
            Convert.ToString(l, 8).PadLeft(width, '0');

        /// <summary>
        /// Returns a printable character if found.<para/>
        /// Between 0x20 (space) to 0x7E (~)
        /// </summary>
        /// <param name="b">Byte to transform.</param>
        /// <returns>ASCII character.</returns>
        public static char ToAscii(this byte b) =>
            b < 0x20 || b > 0x7E ? '.' : (char)b;

        /// <summary>
        /// Gets the character for the upper bar depending on the
        /// offset base view.
        /// </summary>
        /// <param name="v">This <see cref="global::_0xdd.OffsetView"/></param>
        /// <returns>Character.</returns>
        public static char GetChar(this OffsetView v)
        {
            switch (v)
            {
                case OffsetView.Hex:
                    return 'h';
                case OffsetView.Dec:
                    return 'd';
                case OffsetView.Oct:
                    return 'o';
                default:
                    return '?'; // ??????????
            }
        }

        public static int ToInt(this ErrorCode code) => (int)code;
    }
}