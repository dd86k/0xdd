using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace _0xdd
{
    static class WindowsUtilities
    {
        public static bool HasDebuggerAttached()
        {
            bool d = false;
            kernel32.CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref d);
            return d;
        }
    }

    static class kernel32
    {
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int MEM_COMMIT = 0x00001000;
        public const int PAGE_READWRITE = 0x04;
        public const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern int VirtualAllocEx(int hProcess, int lpAddress, int dwSize, int flAllocationType, int flProtect);
        [DllImport("kernel32.dll")]
        public static extern ulong ReadMemory(ulong offset, byte[] lpBuffer, ulong cb, ulong lpcbBytesRead);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
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
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);
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