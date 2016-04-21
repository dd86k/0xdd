using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace _0xdd
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    static class WindowsUtilities
    {
        public static bool HasDebuggerAttached()
        {
            bool d = false;
            kernel32.CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref d);
            return d;
        }
    }

    static class Win32Types
    {
        public const string SE_DEBUG_NAME = "SeDebugPrivilege";

        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        public const int MEM_COMMIT = 0x00001000;
        public const int PAGE_READWRITE = 0x04;

        public static uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public static uint STANDARD_RIGHTS_READ = 0x00020000;
        public static uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        public static uint TOKEN_DUPLICATE = 0x0002;
        public static uint TOKEN_IMPERSONATE = 0x0004;
        public static uint TOKEN_QUERY = 0x0008;
        public static uint TOKEN_QUERY_SOURCE = 0x0010;
        public static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public static uint TOKEN_ADJUST_GROUPS = 0x0040;
        public static uint TOKEN_ADJUST_DEFAULT = 0x0080;
        public static uint TOKEN_ADJUST_SESSIONID = 0x0100;
        public static uint TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY;
        public static uint TOKEN_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
            TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
            TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
            TOKEN_ADJUST_SESSIONID;
    }

    static class kernel32
    {
        /// <summary>
        /// Required to read memory in a process using ReadProcessMemory.
        /// </summary>
        public const ushort PROCESS_VM_READ = 0x0010;
        /// <summary>
        /// Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken).
        /// </summary>
        public const ushort PROCESS_QUERY_INFORMATION = 0x0400;
        /// <summary>
        /// Required to retrieve certain information about a process (see GetExitCodeProcess, GetPriorityClass, IsProcessInJob, QueryFullProcessImageName).
        /// A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION.
        /// </summary>
        public const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern int VirtualAllocEx(int hProcess, int lpAddress, int dwSize, int flAllocationType, int flProtect);
        [DllImport("kernel32.dll")]
        public static extern ulong ReadMemory(ulong offset, byte[] lpBuffer, ulong cb, ulong lpcbBytesRead);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        //[DllImport("kernel32.dll")]
        //public static extern bool ReadProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int nSize, out uint lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        public static extern int GetProcAddress(int hModule, string lpProcName);
        [DllImport("kernel32.dll")]
        public static extern int GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll")]
        public static extern int CreateRemoteThread(int hProcess, int lpThreadAttributes, int dwStackSize, int lpStartAddress, int lpParameter, int dwCreationFlags, int lpThreadId);
        [DllImport("kernel32.dll")]
        public static extern int WaitForSingleObject(int hHandle, int dwMilliseconds);
        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(int hObject);
        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle,
            uint DesiredAccess, out IntPtr TokenHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();
    }

    public static class advapi32
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName,
               out LUID lpLuid);
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
           [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
           ref TOKEN_PRIVILEGES NewState,
           uint Zero,
           IntPtr Null1,
           IntPtr Null2);
    }

    public struct SYSTEM_INFO
    {
        /// <summary>
        /// The processor architecture of the installed operating system.
        /// </summary>
        public ushort processorArchitecture;
        ushort reserved;
        /// <summary>
        /// The page size and the granularity of page protection and commitment.
        /// </summary>
        public uint PageSize;
        /// <summary>
        /// A pointer to the lowest memory address accessible to applications and dynamic-link libraries (DLLs).
        /// </summary>
        public IntPtr MinimumApplicationAddress;
        /// <summary>
        /// A pointer to the highest memory address accessible to applications and DLLs.
        /// </summary>
        public IntPtr MaximumApplicationAddress;
        /// <summary>
        /// A mask representing the set of processors configured into the system. Bit 0 is processor 0; bit 31 is processor 31.
        /// </summary>
        public IntPtr ActiveProcessorMask;
        /// <summary>
        /// The number of logical processors in the current group.
        /// To retrieve this value, use the GetLogicalProcessorInformation function.
        /// </summary>
        public uint numberOfProcessors;
        /// <summary>
        /// An obsolete member that is retained for compatibility.
        /// Use the wProcessorArchitecture, wProcessorLevel, and wProcessorRevision members to determine the type of processor.
        /// </summary>
        public uint processorType;
        /// <summary>
        /// The granularity for the starting address at which virtual memory can be allocated.
        /// For more information, see VirtualAlloc.
        /// </summary>
        public uint allocationGranularity;
        /// <summary>
        /// The architecture-dependent processor level.
        /// It should be used only for display purposes.
        /// To determine the feature set of a processor, use the IsProcessorFeaturePresent function.
        /// </summary>
        public ushort processorLevel;
        /// <summary>
        /// The architecture-dependent processor revision.
        /// </summary>
        public ushort processorRevision;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct MEMORY_BASIC_INFORMATION
    {
        /// <summary>
        /// A pointer to the base address of the region of pages.
        /// </summary>
        public int BaseAddress;
        /// <summary>
        /// A pointer to the base address of a range of pages allocated by the VirtualAllocEx function.
        /// The page pointed to by the BaseAddress member is contained within this allocation range.
        /// </summary>
        public int AllocationBase;
        /// <summary>
        /// The memory protection option when the region was initially allocated.
        /// This member can be one of the memory protection constants or 0 if the caller does not have access.
        /// </summary>
        public int AllocationProtect;
        /// <summary>
        /// The size of the region beginning at the base address in which all pages have identical attributes, in bytes.
        /// </summary>
        public int RegionSize;
        /// <summary>
        /// The state of the pages in the region.
        /// </summary>
        public int State;
        /// <summary>
        /// The access protection of the pages in the region.
        /// This member is one of the values listed for the AllocationProtect member.
        /// </summary>
        public int Protect;
        /// <summary>
        /// The type of pages in the region. The following types are defined. 
        /// </summary>
        public int Type;
    }
}