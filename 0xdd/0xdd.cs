using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

//TODO: Hashing (with menu) (v1.1)
//TODO: Search from end of file (v0.7)

//TODO: Do advanced control input scheme (v0.7)
// UserInput (advanced) - Will be able to move to different controls for input
// construction: Control[] (struct with ControlType as enum)
// output: ControlResults[]

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

        // Process
        ProcessNotFound = 0x8,
        ProcessUnreadable = 0x9,
        ProcessAccessViolation = 0xA,
        ProcessNoSeDebugMode = 0xB,
        ProcessNoPrevilege = 0xC,

        // Position
        PositionOutOfBound = 0x10,

        // Dump
        DumbCannotWrite = 0x18,
        DumbCannotRead = 0x19,

        // Find
        FindNoResult = 0x20,
        FindEmptyString = 0x21,

        // Program
        ProgramNoParse = 0xA0,

        // CLI
        CLI_InvalidOffsetView = 0xC0,
        CLI_InvalidWidth = 0xC4,

        // Misc.
        OSNotSupported = 0xD0,
        NotImplemented = 0xD1,
        
        UnknownError = 0xFE,
        Exit = 0xFF,
    }

    enum OffsetView : byte
    {
        Hexadecimal, Decimal, Octal
    }
    
    enum OperatingMode : byte
    {
        // READ, OVRW, INSR
        Read, Overwrite, Insert
    }

    enum EntryType : byte
    {
        File, Process, MemoryRegion
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
            Code = pError;
        }

        public bool Success
        {
            get
            {
                return Code == ErrorCode.Success;
            }
        }

        public ErrorCode Code;
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

        #region Variables
        static FileInfo CurrentFileInfo;
        static FileStream CurrentFileStream;

        static Process CurrentProcess;
        static IntPtr CurrentProcessHandle;

        static OffsetView CurrentOffsetView;
        static EntryType CurrentEntryType;
        //static OperatingMode CurrentOperatingMode;

        static byte[] DisplayBuffer;

        static long Position;
        
        static bool FullscreenMode;
        static bool AutoSize;
        
        static int LastWindowHeight;
        static int LastWindowWidth;
        static string LastDataSearched;
        static byte LastByteSearched;
        #endregion

        #region Methods        
        internal static ErrorCode OpenFile(string pFilePath, OffsetView pView = OffsetView.Hexadecimal, int pBytesRow = 0)
        {
            CurrentEntryType = EntryType.File;

            {
                CurrentFileInfo = new FileInfo(pFilePath);

                if (!CurrentFileInfo.Exists)
                    return ErrorCode.FileNotFound;

                try
                {
                    CurrentFileStream = CurrentFileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch
                {
                    return ErrorCode.FileUnreadable;
                }

                PrepareOpen(pBytesRow, pView);
            }

            UserResponse ur = new UserResponse(ErrorCode.Success);

            while(ur.Success)
            {
                ReadUserKey(ref ur);
            }

            return ur.Code;
        }
        
        internal static ErrorCode OpenProcess(string pProcess, OffsetView pView = OffsetView.Hexadecimal, int pBytesRow = 0)
        {
            CurrentEntryType = EntryType.Process;

            {
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                    return ErrorCode.OSNotSupported;
                
                if (Regex.IsMatch(pProcess, "(b|biggest|s|smallest):(p|private|s|shared|w|ws|workingset)\b", RegexOptions.ECMAScript))
                {
                    Process[] list = Process.GetProcesses();

                    //TODO: IComparer<Process>
                }
                
                if (Regex.IsMatch(pProcess, @"^#\d", RegexOptions.ECMAScript))
                { // PID
                    int pid;
                    if (!int.TryParse(pProcess.Replace("#", string.Empty), out pid))
                        return ErrorCode.ProgramNoParse;

                    try
                    {
                        CurrentProcess = Process.GetProcessById(pid);
                    }
                    catch
                    {
                        return ErrorCode.ProcessNotFound;
                    }
                }
                else
                { // Process name

                    //TODO: Notation to process selection (by index of its name, size)
                    /*
                        index: processname - 0 based?
                        size: processname
                    */

                    Process[] pl = Process.GetProcessesByName(pProcess);

                    if (pl.Length > 0)
                    {
                        if (pl.Length == 1)
                            CurrentProcess = pl[0];
                        else
                        {
                            CurrentProcess = pl[0]; // temporary
                        }
                    }
                    else
                        return ErrorCode.ProcessNotFound;
                }

#warning OpenProcess is in development

                try
                {
                    IntPtr hToken;
                    LUID luidSEDebugNameValue;
                    TOKEN_PRIVILEGES tkpPrivileges;

#if DEBUG
                    Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
#endif

                    if (kernel32.OpenProcessToken(kernel32.GetCurrentProcess(), Win32Types.TOKEN_ADJUST_PRIVILEGES | Win32Types.TOKEN_QUERY, out hToken))
                    {
                        Debug.WriteLine("OpenProcessToken() successfully");
                    }
                    else
                    {
                        Debug.WriteLine($"OpenProcessToken() failed [0x{Marshal.GetLastWin32Error():X8}]. SeDebugPrivilege is not available");
                        return ErrorCode.ProcessNoSeDebugMode;
                    }

                    if (advapi32.LookupPrivilegeValue(null, Win32Types.SE_DEBUG_NAME, out luidSEDebugNameValue))
                    {
                        Debug.WriteLine("LookupPrivilegeValue() successfully");
                    }
                    else
                    {
                        Debug.WriteLine($"LookupPrivilegeValue() failed [0x{Marshal.GetLastWin32Error()}]. SeDebugPrivilege is not available");
                        kernel32.CloseHandle(hToken);
                        return ErrorCode.ProcessNoPrevilege;
                    }

                    tkpPrivileges.PrivilegeCount = 1;
                    tkpPrivileges.Luid = luidSEDebugNameValue;
                    tkpPrivileges.Attributes = Win32Types.SE_PRIVILEGE_ENABLED;

                    if (advapi32.AdjustTokenPrivileges(hToken, false, ref tkpPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                    {
                        Debug.WriteLine("SeDebugPrivilege is now available");
                    }
                    else
                    {
                        Debug.WriteLine($"LookupPrivilegeValue() failed [0x{Marshal.GetLastWin32Error()}]. SeDebugPrivilege is not available");
                    }
                    kernel32.CloseHandle(hToken);

                    CurrentProcessHandle =
                        kernel32.OpenProcess(
                            kernel32.PROCESS_QUERY_INFORMATION | kernel32.PROCESS_VM_READ,
                            false, CurrentProcess.Id);

#if DEBUG
                    Console.ReadLine();
#endif
                }
                catch (AccessViolationException)
                {
                    return ErrorCode.ProcessAccessViolation;
                }
                catch (ArgumentException)
                {
                    return ErrorCode.ProcessNotFound;
                }
                catch (Exception)
                {
                    return ErrorCode.UnknownError;
                }

                PrepareOpen(pBytesRow, pView);
            }

            UserResponse ur = new UserResponse(ErrorCode.Success);

            while (ur.Success)
            {
                ReadUserKey(ref ur);
            }

            return ur.Code;
        }

        internal static ErrorCode OpenMemory(string pBaseAddress, OffsetView pView = OffsetView.Hexadecimal, int pBytesRow = 0)
        {
            return ErrorCode.NotImplemented;

            UserResponse ur = new UserResponse(ErrorCode.Success);
            
            while (ur.Success)
            {
                ReadUserKey(ref ur);
            }

            return ur.Code;
        }

        static void PrepareOpen(int pBytesRow, OffsetView pView)
        {
            AutoSize = pBytesRow <= 0;

            CurrentOffsetView = pView;
            LastWindowHeight = Console.WindowHeight;
            LastWindowWidth = Console.WindowWidth;

            Console.CursorVisible = false;
            Console.Clear();

            PrepareScreen();
        }

        /// <summary>
        /// Read user input.
        /// </summary>
        static void ReadUserKey(ref UserResponse pUserResponse)
        {
            ConsoleKeyInfo k = Console.ReadKey(true);
            
            if (AutoSize)
                if (LastWindowHeight != Console.WindowHeight ||
                    LastWindowWidth != Console.WindowWidth)
                {
                    Console.Clear();
                    PrepareScreen();

                    LastWindowHeight = Console.WindowHeight;
                    LastWindowWidth = Console.WindowWidth;
                }
            
            switch (k.Key)
            {
                // -- Hidden shortcuts --

                case ConsoleKey.F5:
                    {
                        ReadFileAndUpdate(0); //TODO: pos
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
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFileInfo.Length - MainPanel.BytesOnScreen)
                        {
                            Message("Already at the end of the file.");
                            return;
                        }

                        long? t = Utils.GetNumberFromUser("Find byte:",
                            pSuggestion: LastByteSearched.ToString("X2"));

                        if (t == null)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }
                        
                        if (t < 0 || t > byte.MaxValue)
                        {
                            MainPanel.Update();
                            Message("A value between 0 and 255 is required.");
                        }
                        else
                        {
                            LastByteSearched = (byte)t;
                            MainPanel.Update();
                            Message("Searching...");
                            FindResult p = Find((byte)t, CurrentFileStream.Position + 1);

                            switch (p.Error)
                            {
                                case ErrorCode.Success:
                                    {
                                        Goto(--p.Position);
                                        if (p.Position > uint.MaxValue)
                                            Message($"Found {t:X2} at {p.Position:X16}");
                                        else
                                            Message($"Found {t:X2} at {p.Position:X8}");
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
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (CurrentFileStream.Position >= CurrentFileInfo.Length - MainPanel.BytesOnScreen)
                        {
                            Message("Already at the end of the file.");
                            return;
                        }

                        if (MainPanel.BytesOnScreen >= CurrentFileInfo.Length)
                        {
                            Message("Not possible.");
                            return;
                        }

                        string t = Utils.GetUserInput("Find data:", pSuggestion: LastDataSearched);

                        if (t == null || t.Length == 0)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }
                        
                        LastDataSearched = t;
                        MainPanel.Update();
                        Message("Searching...");
                        FindResult p = FindData(t, CurrentFileStream.Position + 1);
                        
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
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        if (MainPanel.BytesOnScreen >= CurrentFileInfo.Length)
                        {
                            Message("Not possible.");
                            return;
                        }

                        if (CurrentFileStream.Position >= CurrentFileInfo.Length)
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

                        if (t >= 0 && t <= CurrentFileInfo.Length - MainPanel.BytesOnScreen)
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
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        string c = Utils.GetUserInput("Hex, dec, oct?");

                        if (c == null || c.Length < 1)
                        {
                            MainPanel.Update();
                            Message("Canceled.");
                            return;
                        }

                        switch (c[0])
                        {
                            case 'H': case 'h':
                                CurrentOffsetView = OffsetView.Hexadecimal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            case 'O': case 'o':
                                CurrentOffsetView = OffsetView.Octal;
                                OffsetPanel.Update();
                                MainPanel.Update();
                                InfoPanel.Update();
                                return;

                            case 'D': case 'd':
                                CurrentOffsetView = OffsetView.Decimal;
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
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Not implemented. Sorry!");
                    }
                    break;

                // Replace
                case ConsoleKey.H:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Not implemented. Sorry!");
                    }
                    return;

                // Info
                case ConsoleKey.I:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Message($"{Utils.GetEntryInfo(CurrentFileInfo)}  {Utils.GetFormattedSize(CurrentFileInfo.Length)}");
                    }
                    return;

                // Exit
                case ConsoleKey.X:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        pUserResponse.Code = ErrorCode.Exit;
                        Exit();
                    }
                    return;

                // Dump
                case ConsoleKey.D:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        Message("Dumping...");
                        Dump();
                        Message("Dumping done!");
                    }
                    return;

                // -- Data nagivation --
                case ConsoleKey.LeftArrow:
                    if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                        if (k.Modifiers == ConsoleModifiers.Control)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position -
                                (CurrentFileStream.Position % MainPanel.BytesInRow));
                        }
                        else if (CurrentFileStream.Position - 1 >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - 1);
                        }
                    return;
                case ConsoleKey.RightArrow:
                    if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                        if (k.Modifiers == ConsoleModifiers.Control)
                        {
                            long NewPos = CurrentFileStream.Position +
                                (MainPanel.BytesInRow - CurrentFileStream.Position % MainPanel.BytesInRow);

                            if (NewPos + MainPanel.BytesOnScreen <= CurrentFileInfo.Length)
                                ReadFileAndUpdate(NewPos);
                            else
                                ReadFileAndUpdate(CurrentFileInfo.Length - MainPanel.BytesOnScreen);
                        }
                        else if (CurrentFileStream.Position + MainPanel.BytesOnScreen + 1 <= CurrentFileInfo.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position + 1);
                        }
                    return;

                case ConsoleKey.UpArrow:
                    if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                        if (CurrentFileStream.Position - MainPanel.BytesInRow >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - MainPanel.BytesInRow);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    return;
                case ConsoleKey.DownArrow:
                    if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                        if (CurrentFileStream.Position + MainPanel.BytesOnScreen + MainPanel.BytesInRow <= CurrentFileInfo.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position + MainPanel.BytesInRow);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFileInfo.Length - MainPanel.BytesOnScreen);
                        }
                    return;

                case ConsoleKey.PageUp:
                    if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                        if (CurrentFileStream.Position - MainPanel.BytesOnScreen >= 0)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position - MainPanel.BytesOnScreen);
                        }
                        else
                        {
                            ReadFileAndUpdate(0);
                        }
                    return;
                case ConsoleKey.PageDown:
                    if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                        if (CurrentFileStream.Position + (MainPanel.BytesOnScreen * 2) <= CurrentFileInfo.Length)
                        {
                            ReadFileAndUpdate(CurrentFileStream.Position += MainPanel.BytesOnScreen);
                        }
                        else
                        {
                            ReadFileAndUpdate(CurrentFileInfo.Length - MainPanel.BytesOnScreen);
                        }
                    return;

                case ConsoleKey.Home:
                    if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                        ReadFileAndUpdate(0);
                    return;
                case ConsoleKey.End:
                    if (MainPanel.BytesOnScreen < CurrentFileInfo.Length)
                        ReadFileAndUpdate(CurrentFileInfo.Length - MainPanel.BytesOnScreen);
                    return;
            }
        }

        /// <summary>
        /// Prepares the screen with the information needed.<para/>
        /// Initialize: AutoSizing, Buffer, TitlePanel, Readfile, MainPanel,
        /// InfoPanel, and ControlPanel.
        /// </summary>
        /// <remarks>
        /// Also used when resizing.
        /// </remarks>
        static void PrepareScreen()
        {
            if (AutoSize)
                MainPanel.BytesInRow = Utils.GetBytesInRow();

            switch (CurrentEntryType)
            {
                case EntryType.File:
                    DisplayBuffer = new byte[CurrentFileInfo.Length < MainPanel.BytesOnScreen ?
                        CurrentFileInfo.Length : MainPanel.BytesOnScreen];
                    break;
                case EntryType.Process:
                    DisplayBuffer = new byte[500]; // temp
                    break;
            }

            TitlePanel.Update();
            switch (CurrentEntryType)
            {
                case EntryType.File:
                    ReadFileAndUpdate(CurrentFileStream.Position);
                    break;
                case EntryType.Process:
                    ReadFileAndUpdate(0); // temp
                    break;
            }
            ControlPanel.Place();
        }

        //temp
        #region Read file
        /// <summary>
        /// Read the current file at a position.
        /// </summary>
        /// <param name="pBasePosition">Position.</param>
        
        static void ReadCurrentFile(long pBasePosition)
        {
            switch (CurrentEntryType)
            {
                case EntryType.File:
                    CurrentFileStream.Position = pBasePosition;
                    CurrentFileStream.Read(DisplayBuffer, 0, DisplayBuffer.Length);
                    CurrentFileStream.Position = pBasePosition;
                    break;
                case EntryType.Process:
                    Debug.WriteLine("handle: 0x" + CurrentProcessHandle.ToInt64().ToString("X8"));
                    Debug.WriteLine("Buffer length: " + DisplayBuffer.Length);

#warning: ReadCurrentFile - Process
                    bool last = false;
                    SYSTEM_INFO s = new SYSTEM_INFO();
                    kernel32.GetSystemInfo(out s);

                    IntPtr minp = s.MinimumApplicationAddress;

                    long min = s.MinimumApplicationAddress.ToInt64();
                    long max = s.MaximumApplicationAddress.ToInt64();

                    MEMORY_BASIC_INFORMATION mem = new MEMORY_BASIC_INFORMATION();
                    
                    int read = 0;

                    /*while (min < max)
                    {
                        // 28 = sizeof(MEMORY_BASIC_INFORMATION)
                        kernel32.VirtualQueryEx(CurrentProcessHandle, minp, out mem, 28);

                        Debug.WriteLine("memprotect: " + mem.Protect + " | memstate: " + mem.State);

                        if (mem.Protect == kernel32.PAGE_READWRITE && mem.State == kernel32.MEM_COMMIT) // or overwrite to true
                        {
                            //byte[] _buffer = new byte[mem.RegionSize]; // OutOfMemoryException

                            //Debug.WriteLine("_buffer size: " + _buffer.Length);
                                                                                                             // mem.RegionSize
                            kernel32.ReadProcessMemory(CurrentProcessHandle.ToInt32(), mem.BaseAddress, Buffer, Buffer.Length, ref read);
                            
                            //for (int i = 0; i < mem.RegionSize && i < Buffer.Length; i++)
                                //Buffer[i] = _buffer[i];
                        }

                        min += mem.RegionSize;
                        minp = new IntPtr(min);
                    }*/

                    /*TODO: Process memory navigation
                     * How it will go down *
                    - Memory regions
                      - Get all pointers in a list? (lpBaseAddress)
                      - make TUI for list?
                    - Put
                      mem.Protect == kernel32.PAGE_READWRITE && mem.State == kernel32.MEM_COMMIT
                      in there?
                    - Fill 0s if read is 0?
                    - VirtualQueryEx: 28 or sizeof(MEMORY_BASIC_INFORMATION)? 28
                     */

                    int regions = 0;
                    while (min < max) // temporary/working
                    {
                        kernel32.VirtualQueryEx(CurrentProcessHandle, minp, out mem, 28);
                            kernel32.ReadProcessMemory(CurrentProcessHandle.ToInt32(),
                            mem.BaseAddress + (int)pBasePosition, DisplayBuffer, DisplayBuffer.Length, ref read);
                        min += mem.RegionSize;
                        minp = new IntPtr(min);
                        Debug.WriteLine($"Region #{++regions}, read: {read} bytes");
                    }

                    Debug.WriteLine($"Regions: {regions}");

                    //kernel32.VirtualQueryEx(CurrentProcessHandle, minp, out mem, 28);
                    //kernel32.ReadProcessMemory(CurrentProcessHandle.ToInt32(), mem.BaseAddress + (int)pBasePosition, Buffer, Buffer.Length, ref read);
                    
                    break;
            }
        }

        /// <summary>
        /// 1. Read file. 2. Update MainPanel. 3. Update InfoPanel
        /// </summary>
        /// <param name="pPosition">New position.</param>
        static void ReadFileAndUpdate(long pPosition)
        {
            ReadCurrentFile(pPosition);
            MainPanel.Update();
            InfoPanel.Update();
        }
        #endregion

        /// <summary>
        /// Go to a specific position in the file.
        /// </summary>
        /// <param name="pPosition">Position</param>
        static void Goto(long pPosition)
        {
            ReadFileAndUpdate(pPosition);
        }
        
        /// <summary>
        /// Displays a message on screen to inform the user.
        /// </summary>
        /// <param name="pMessage">Message to show.</param>
        static void Message(string pMessage)
        {
            Console.SetCursorPosition(0, InfoPanel.StartPosition);
            Console.Write(s(Console.WindowWidth - 1));

            string msg = $"[ {pMessage} ]";
            Console.SetCursorPosition((Console.WindowWidth / 2) - (msg.Length / 2),
                InfoPanel.StartPosition);

            Utils.ToggleColors();
            Console.Write(msg);
            Console.ResetColor();
        }

        /// <summary>
        /// Toggle fullscreen mode.
        /// </summary>
        static void ToggleFullscreenMode()
        {
            FullscreenMode = !FullscreenMode;

            PrepareScreen();

            if (FullscreenMode)
            {
                MainPanel.Update();
                InfoPanel.Update();
            }
        }

        #region Dump
        /// <summary>
        /// File to dump as text data with the app's <see cref="MainPanel.BytesInRow"/>
        /// and <see cref="CurrentOffsetView"/>.
        /// </summary>
        /// <returns><see cref="ErrorCode"/></returns>
        static ErrorCode Dump()
        {
            return Dump(CurrentFileInfo.Name, MainPanel.BytesInRow, CurrentOffsetView);
        }

        /// <summary>
        /// File to dump as text data.
        /// </summary>
        /// <param name="pFileToDump">Output filename.</param>
        /// <param name="pBytesInRow">Number of bytes in a row.</param>
        /// <param name="pViewMode"><see cref="OffsetView"/> to use.</param>
        /// <returns><see cref="ErrorCode"/></returns>
        static public ErrorCode Dump(string pFileToDump, int pBytesInRow = 16, OffsetView pViewMode = OffsetView.Hexadecimal)
        {
            if (!File.Exists(pFileToDump))
                return ErrorCode.FileNotFound;
            
            // These variables are independant because I do
            // not want to mess with the global ones and it
            // may happen the user /d directly.
            FileInfo f = new FileInfo(pFileToDump);
            DisplayBuffer = new byte[pBytesInRow];

            using (StreamWriter sw = new StreamWriter($"{pFileToDump}.{EXTENSION}"))
            {
                if (!sw.BaseStream.CanWrite)
                    return ErrorCode.DumbCannotWrite;

                pBytesInRow = pBytesInRow == 0 ? 16 : pBytesInRow;

                sw.AutoFlush = true;

                sw.WriteLine(f.Name);
                sw.WriteLine();
                sw.WriteLine($"Size: {Utils.GetFormattedSize(f.Length)}");
                sw.WriteLine($"Attributes: {Utils.GetEntryInfo(f)}");
                sw.WriteLine($"File date: {f.CreationTime}");
                sw.WriteLine($"Dump date: {DateTime.Now}");
                sw.WriteLine();

                sw.Write($"Offset {pViewMode.GetChar()}  ");
                for (int i = 0; i < pBytesInRow; i++)
                {
                    sw.Write($"{i:X2} ");
                }
                sw.WriteLine();
                
                if (CurrentFileInfo == null)
                    return DumpFile(f.Open(FileMode.Open), sw, pViewMode, pBytesInRow);
                else
                    return DumpFile(CurrentFileStream, sw, pViewMode, pBytesInRow);
               
            }
        }
        
        /// <remarks>
        /// Only to be used with <see cref="Dump()"/>!
        /// </remarks>
        static ErrorCode DumpFile(FileStream pIn, StreamWriter pOut, OffsetView pViewMode, int pBytesInRow)
        {
            long line = 0;
            int BufferPositionHex = 0;
            int BufferPositionData = 0;
            string t = string.Empty;

            DisplayBuffer = new byte[pBytesInRow];

            long lastpos = CurrentFileStream.Position;

            if (!pIn.CanRead)
                return ErrorCode.DumbCannotRead;

            bool working = true;

            while (working)
            {
                switch (pViewMode)
                {
                    case OffsetView.Hexadecimal:
                        t = $"{line:X8}  ";
                        break;

                    case OffsetView.Decimal:
                        t = $"{line:D8}  ";
                        break;

                    case OffsetView.Octal:
                        t = $"{ToOct(line)}  ";
                        break;
                }

                line += pBytesInRow;

                pIn.Read(DisplayBuffer, 0, pBytesInRow);

                for (int pos = 0; pos < pBytesInRow; pos++)
                {
                    if (BufferPositionHex < pIn.Length)
                        t += $"{DisplayBuffer[pos]:X2} ";
                    else
                        t += "   ";

                    BufferPositionHex++;
                }

                t += " ";

                for (int pos = 0; pos < pBytesInRow; pos++)
                {
                    if (BufferPositionData < pIn.Length)
                        t += DisplayBuffer[pos].ToAscii();
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
        #endregion

        #region Find
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
            if (pPosition < 0 || pPosition > CurrentFileInfo.Length)
                return new FindResult(ErrorCode.PositionOutOfBound);

            if (!CurrentFileInfo.Exists)
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
            return FindData(pData, CurrentFileStream.Position);
        }

        /// <summary>
        /// Find a string of data with a given position.
        /// </summary>
        /// <param name="pData">Data as a string.</param>
        /// <param name="pPosition">Starting position.</param>
        /// <returns><see cref="FindResult"/></returns>
        /// <remarks>
        /// How does this work?
        /// Search every character, if the first one seems to be right,
        /// read the data and compare it.
        /// </remarks>
        static FindResult FindData(string pData, long pPosition)
        {
            if (pPosition < 0 || pPosition > CurrentFileInfo.Length)
                return new FindResult(ErrorCode.PositionOutOfBound);

            if (!CurrentFileInfo.Exists)
                return new FindResult(ErrorCode.FileNotFound);

            if (string.IsNullOrWhiteSpace(pData))
                return new FindResult(ErrorCode.FindEmptyString);

            CurrentFileStream.Position = pPosition;

            byte[] b = new byte[pData.Length];
            bool Continue = true;
            if (pData.StartsWith("/") && pData.EndsWith("/"))
            {
                RegexOptions rf = RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.CultureInvariant;

                Regex r = new Regex(pData.Trim(new char[] { '/' }), rf);

                Message("Searching with regex...");

                while (Continue)
                {
                    if (CurrentFileStream.Position + pData.Length > CurrentFileStream.Length)
                        Continue = false;

                    if (r.IsMatch(char.ToString((char)CurrentFileStream.ReadByte())))
                    {
                        if (pData.Length == 1)
                        {
                            return new FindResult(CurrentFileStream.Position - 1);
                        }
                        else
                        {
                            CurrentFileStream.Position--;
                            CurrentFileStream.Read(b, 0, b.Length);
                            if (r.IsMatch(Encoding.ASCII.GetString(b)))
                            {
                                return new FindResult(CurrentFileStream.Position - pData.Length);
                            }
                        }
                    }
                }
            }
            else // Copying pasting is good
            {
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
                                return new FindResult(CurrentFileStream.Position - pData.Length);
                            }
                        }
                    }
                }
            }

            CurrentFileStream.Position = pPosition - 1;

            return new FindResult(ErrorCode.FindNoResult);
        }
        #endregion

        #region Exit
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

                string name = string.Empty;

                switch (CurrentEntryType)
                {
                    case EntryType.File:
                        name = CurrentFileInfo.Name;
                        break;
                    case EntryType.Process:
                        name = CurrentProcess.ProcessName;
                        break;
                }

                if (name.Length <= Console.WindowWidth)
                {
                    Console.Write(name + s(Console.WindowWidth - name.Length));
                }
                else
                    Console.Write(name.Substring(0, Console.WindowWidth));

                Console.ResetColor();
            }
        }
        #endregion

        #region OffsetPanel
        /// <summary>
        /// Shows offset base view and the offset on each byte.
        /// </summary>
        static class OffsetPanel
        {
            static internal int Position = 1;

            /// <summary>
            /// Update the offset map
            /// </summary>
            static internal void Update()
            {
                StringBuilder t = new StringBuilder($"Offset {CurrentOffsetView.GetChar()}  ");
                
                switch (CurrentEntryType)
                {
                    case EntryType.File:
                        if (CurrentFileStream.Position > uint.MaxValue)
                            t.Append(" ");
                        break;
                    case EntryType.Process:
                        break;
                }

                for (int i = 0; i < MainPanel.BytesInRow;)
                {
                    t.Append($"{++i:X2} ");
                }

                if (LastWindowHeight != Console.WindowHeight ||
                    LastWindowWidth != Console.WindowWidth)
                    t.Append(s(Console.WindowWidth - t.Length - 1)); // Force clean

                Console.SetCursorPosition(0, Position);
                Console.Write(t.ToString());
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
            /// Current cursor position for Edit mode. [x,y]
            /// </summary>
            //TODO: Decide on type for CursorPosition (v0.9)
            //static internal int[,] CursorPosition;

            /// <summary>
            /// Gets the position to start rendering on the console (Y axis).
            /// </summary>
            static internal int StartPosition
            {
                get
                {
                    return FullscreenMode ? 1 : 2;
                }
            }

            /// <summary>
            /// Gets the heigth of the main panel.
            /// </summary>
            static internal int FrameHeight
            {
                get
                {
                    return FullscreenMode ?
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
            static internal int BytesOnScreen
            {
                get
                {
                    return FrameHeight * BytesInRow;
                }
            }

            /// <summary>
            /// Update from Buffer.
            /// </summary>
            static internal void Update()
            {
                int fh = FrameHeight;

                int width = Console.WindowWidth;

                long len = 0; // File/Process size
                long pos = 0; // File/Process position
                switch (CurrentEntryType)
                {
                    case EntryType.File:
                        pos = CurrentFileStream.Position;
                        len = CurrentFileInfo.Length;
                        break;
                    case EntryType.Process:
                        pos = 0; //TODO: Position (process)
                        len = DisplayBuffer.Length;
                        break;
                }
                
                OffsetPanel.Update();

                int d = 0;
                StringBuilder line = new StringBuilder();
                StringBuilder ascii = new StringBuilder();
                Console.SetCursorPosition(0, StartPosition);
                for (int lineIndex = 0; lineIndex < fh; lineIndex++)
                {
                    switch (CurrentOffsetView)
                    {
                        case OffsetView.Hexadecimal:
                            line = new StringBuilder($"{(lineIndex * BytesInRow) + pos:X8}  ", width);
                            break;

                        case OffsetView.Decimal:
                            line = new StringBuilder($"{(lineIndex * BytesInRow) + pos:D8}  ", width);
                            break;

                        case OffsetView.Octal:
                            line = new StringBuilder($"{ToOct((lineIndex * BytesInRow) + pos)}  ", width);
                            break;
                    }

                    ascii = new StringBuilder(BytesInRow);
                    // d = data (hex) index
                    for (int i = 0; i < BytesInRow; ++i, ++d)
                    {
                        if (pos + d < len)
                        {
                            line.Append($"{DisplayBuffer[d]:X2} ");
                            ascii.Append(DisplayBuffer[d].ToAscii());
                        }
                        else
                        {
                            line.Append(ascii.ToString());
                            //TODO: Fill
                            //line.Append(
                            Console.Write(line.ToString());
                            return;
                        }
                    }

                    line.Append(" "); // over 0xFFFFFFFF padding

                    line.Append(ascii.ToString());
                    
                    Console.WriteLine(line.ToString());
                }
            }
        }
        #endregion

        #region InfoPanel
        /// <summary>
        /// Current position information.
        /// </summary>
        static class InfoPanel
        {
            /// <summary>
            /// Starting position to rendering on the console (Y axis).
            /// </summary>
            static internal int StartPosition
            {
                get
                {
                    return FullscreenMode ?
                        Console.WindowHeight - 1 : Console.WindowHeight - 3;
                }
            }

            /// <summary>
            /// Update the offset information
            /// </summary>
            static internal void Update()
            {
                long pos = 0;
                decimal r = 0;

                switch (CurrentEntryType)
                {
                    case EntryType.File:
                        pos = CurrentFileStream.Position;
                        r = CurrentFileInfo.Length > 0 ?
                            Math.Round(
                                ((decimal)(pos + DisplayBuffer.Length) / CurrentFileInfo.Length) * 100) :
                                0;
                        break;
                    case EntryType.Process:

                        break;
                }

                string t =
                    $"  DEC={pos:D8} | HEX={pos:X8} | OCT={ToOct(pos)} | POS={r,3}%";

                Console.SetCursorPosition(0, StartPosition);
                Console.Write(t); // Force-clear any messages
            }
        }
        #endregion

        #region ControlPanel
        /// <summary>
        /// Panel that shows keyboard shortcuts.
        /// </summary>
        static class ControlPanel
        {
            public static int TitleLength = 12;

            /// <summary>
            /// Places the control map on screen (e.g. ^T Try jumping and etc.)
            /// </summary>
            static internal void Place()
            {
                int width = Console.WindowWidth;

                Console.SetCursorPosition(0, Console.WindowHeight - 2);
                if (width >= TitleLength)     PlaceShortcut('W', "Find byte");
                if (width >= TitleLength * 2) PlaceShortcut('J', "Find data");
                if (width >= TitleLength * 3) PlaceShortcut('G', "Goto");
                if (width >= TitleLength * 4) PlaceShortcut('H', "Replace");
                Console.SetCursorPosition(0, Console.WindowHeight - 1);
                if (width >= TitleLength)     PlaceShortcut('X', "Exit");
                if (width >= TitleLength * 2) PlaceShortcut('O', "Offset base");
                if (width >= TitleLength * 3) PlaceShortcut('I', "Info");
                if (width >= TitleLength * 4) PlaceShortcut('D', "Dump");
                if (width >= TitleLength * 5) PlaceShortcut('E', "Edit mode");
            }

            /// <summary>
            /// Write out a shortcut and its short description
            /// </summary>
            /// <param name="pShortcutKey">Shortcut (^D)</param>
            /// <param name="pTitle">Display name.</param>
            static void PlaceShortcut(char pShortcutKey, string pTitle)
            {
                Utils.ToggleColors();
                Console.Write("^" + pShortcutKey);
                Console.ResetColor();

                if (pTitle.Length > TitleLength)
                    Console.Write(" " + pTitle.Substring(0, TitleLength - 1));
                else
                    Console.Write(" " + pTitle.PadRight(TitleLength));
            }
        }
        #endregion
        #endregion

        #region Type extensions
        /// <summary>
        /// Generate a string with spaces with a desired length.
        /// </summary>
        /// <param name="l">Length</param>
        /// <returns>String</returns>
        static string s(this int l) => new string(' ', l);

        /// <summary>
        /// Converts into an octal number.
        /// </summary>
        /// <param name="l">Number.</param>
        /// <returns>String.</returns>
        static string ToOct(this long l) => Convert.ToString(l, 8).PadLeft(8, '0');

        /// <summary>
        /// Returns a printable character if found.<para/>
        /// Between 0x20 (space) to 0x7E (~)
        /// </summary>
        /// <param name="b">Byte to transform.</param>
        /// <returns>ASCII character.</returns>
        static char ToAscii(this byte b) => b < 0x20 || b > 0x7E ? '.' : (char)b;

        /// <summary>
        /// Gets the character for the upper bar depending on the
        /// offset base view.
        /// </summary>
        /// <param name="pView">This <see cref="OffsetView"/></param>
        /// <returns>Character.</returns>
        static char GetChar(this OffsetView pView)
        {
            switch (pView)
            {
                case OffsetView.Hexadecimal:
                    return 'h';
                case OffsetView.Decimal:
                    return 'd';
                case OffsetView.Octal:
                    return 'o';
                default:
                    return '?'; // ??????????
            }
        }

        public static int Code(this ErrorCode pCode) => (int)pCode;
        #endregion
    }
}