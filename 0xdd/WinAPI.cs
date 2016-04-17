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
        /*
        public static bool HasDebuggerAttached(Process p)
        {
            bool d = false;
            kernel32.CheckRemoteDebuggerPresent(p.Handle, ref d);
            return d;
        }
        */
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
        public ushort processorArchitecture;
        ushort reserved;
        public uint pageSize;
        public IntPtr minimumApplicationAddress;
        public IntPtr maximumApplicationAddress;
        public IntPtr activeProcessorMask;
        public uint numberOfProcessors;
        public uint processorType;
        public uint allocationGranularity;
        public ushort processorLevel;
        public ushort processorRevision;
    }

    public struct MEMORY_BASIC_INFORMATION
    {
        public int BaseAddress;
        public int AllocationBase;
        public int AllocationProtect;
        /// <summary>
        /// Size of the region allocated by the program.
        /// </summary>
        public int RegionSize;
        /// <summary>
        /// Check if allocated (MEM_COMMIT).
        /// </summary>
        public int State;
        /// <summary>
        /// Page protection (must be PAGE_READWRITE).
        /// </summary>
        public int Protect;
        public int lType;
    }
}