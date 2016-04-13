using System;
using System.IO;
using static System.Diagnostics.Process;
using static System.Reflection.Assembly;

namespace _0xdd
{
    class Program
    {
        /// <summary>
        /// Get the current version of the project as a string object.
        /// </summary>
        static string Version
        {
            get
            {
                return $"{GetExecutingAssembly().GetName().Version}";
            }
        }

        /// <summary>
        /// Get the project's name.
        /// </summary>
        static string ProjectName
        {
            get
            {
                return GetExecutingAssembly().GetName().Name;
            }
        }
        
        /// <summary>
        /// Get the current executable's filename.
        /// </summary>
        static string ExecutableFilename
        {
            get
            {
                return
                    Path.GetFileName(
                        GetCurrentProcess().MainModule.FileName
                    );
            }
        }
        
        static int Main(string[] args)
        {
#if DEBUG
#warning Reminder: Re-comment
            // Used for debugging within Visual Studio (vshost)
            //args = new string[] { ExecutableFilename };
            //args = new string[] { "f" };
            //args = new string[] { "fff" };
            //args = new string[] { "b" };
            //args = new string[] { "tt" };
            //args = new string[] { "/dump", "tt" };
            //args = new string[] { "hf.iso" };
            //args = new string[] { "/w", "16", "hf.iso" };
            //args = new string[] { "-dump", "tt" };
            //args = new string[] { "gg.txt" };
            //args = new string[] { "/w", "a", "gg.txt" };
            args = new string[] { "zero" };
#endif

            if (args.Length == 0)
            {
                // Future reminder:
                // New buffer in editing mode if no arguments
                ShowHelp();
                return 0;
            }
            
            // Defaults
            string file = args[args.Length - 1];
            int row = 0; // 0 - Auto, past default: 16
            OffsetView ovm = OffsetView.Hexadecimal;
            bool dump = false;

            //TODO: Settings! (v0.8)

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-v":
                    case "/v":
                        switch (args[i + 1][0])
                        {
                            case 'h': case 'H':
                                ovm = OffsetView.Hexadecimal;
                                break;
                            case 'd': case 'D':
                                ovm = OffsetView.Decimal;
                                break;
                            case 'o': case 'O':
                                ovm = OffsetView.Octal;
                                break;
                            default:
                                Console.WriteLine(gerrcs(ErrorCode.CLI_InvalidOffsetView, args[i + 1]));
#if DEBUG
                                Console.ReadLine();
#endif
                                return ErrorCode.CLI_InvalidOffsetView.Int();
                        }
                        break;

                    case "-w":
                    case "/w":
                        if (char.ToLower(args[i + 1][0]) != 'a') // Automatic
                        {
                            row = 0;
                        }
                        else if (int.TryParse(args[i + 1], out row)) // User-defined
                        {
                            row = Math.Abs(row);
                        }
                        else // If parsing failed
                        {
                            Console.WriteLine(gerrcs(ErrorCode.CLI_InvalidWidth, args[i + 1]));
#if DEBUG
                            Console.ReadLine();
#endif
                            return ErrorCode.CLI_InvalidWidth.Int();
                        }
                        break;

                    case "-dump":
                    case "/dump":
                        dump = true;
                        break;

                    case "/?":
                    case "/help":
                    case "-help":
                    case "--help":
                        ShowHelp();
                        return 0;

                    case "/ver":
                    case "-ver":
                    case "/version":
                    case "--version":
                        ShowVersion();
                        return 0;
                }
            }
            
            if (dump)
            {
                Console.Write("Dumping file... ");
                ErrorCode err = _0xdd.Dump(file, row, ovm);
                
                Console.WriteLine(gerrcs(err));

                return err.Int();
            }
            else
            {
#if DEBUG // I want Visual Studio to catch the exceptions!
                ErrorCode r = _0xdd.Open(file, ovm, row);
                Console.Clear();
                Console.WriteLine($"\nERRORCODE: {r} - 0x{r.Int():X2}");
                Console.ReadLine();
                return r.Int();
#else
                try
                {
                    ErrorCode err = _0xdd.Open(file, ovm, row);

                    if (err != ErrorCode.Success && err != ErrorCode.Exit)
                        Console.WriteLine(gerrcs(err));

                    return err.Int();
                }
                catch (Exception e)
                {
                    Abort(e);
                }
#endif

#if !DEBUG // To supress an error
                return 0;
#endif
            }
        }

        /// <summary>
        /// Generate a line about the <see cref="ErrorCode"/>
        /// </summary>
        /// <param name="pCode"><see cref="ErrorCode"/></param>
        /// <returns><see cref="string"/></returns>
        /// <remarks> C syntax </remarks>
        static string gerrcs(ErrorCode pCode, string pArgument = null)
        {
            string m = " ";

            switch (pCode)
            {
                case ErrorCode.Success: return m = "OK!";

                case ErrorCode.FileNotFound:
                    m += "Error: File not found.";
                    break;
                case ErrorCode.FileUnreadable:
                    m += "Error: File not readable.";
                    break; ;
                case ErrorCode.PositionOutOfBound:
                    m += "Error: Position out of bound.";
                    break;
                case ErrorCode.DumbCannotWrite:
                    m += "Error: Could not write to output.";
                    break;
                case ErrorCode.DumbCannotRead:
                    m += "Error: Could not read from input.";
                    break;

                case ErrorCode.CLI_InvalidOffsetView:
                    m += $"Invalid parameter for /v : {pArgument}";
                    break;
                case ErrorCode.CLI_InvalidWidth:
                    m += $"Invalid parameter for /w : {pArgument}";
                    break;

                // The "should not be an app return code" club
                case ErrorCode.FindNoResult: break;
                case ErrorCode.FindEmptyString: break;

                case ErrorCode.UnknownError:
                    m += "Error: Unknown error.";
                    break;
                default:
                    m += "Error: Unknown error. [default]";
                    break;
            }

            return m += $"\n ERROR: {pCode} - 0x{pCode.Int():X2}";
        }

        static void Abort(Exception e)
        {
            Console.Clear();
            Console.CursorVisible = true;

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("  !! FATAL ERROR !!  ");
            Console.ResetColor();

            Console.WriteLine($"Exception: {e.GetType()}");
            Console.WriteLine($"Message: {e.Message}");
            Console.WriteLine();
            Console.WriteLine("More details had been appended to 0xdd_oops.txt");

            StreamWriter o = File.CreateText("0xdd_oops.txt");
            o.WriteLine("  -- App crash --");
            o.WriteLine($"Time - {DateTime.Now}");
            o.WriteLine();
            o.WriteLine($"Exception: {e.GetType()}");
            o.WriteLine($"Message: {e.Message}");
            o.WriteLine();
            o.WriteLine("    -- BEGIN STACK --");
            o.WriteLine(e.StackTrace);
            o.WriteLine("    -- END STACK --");
            o.WriteLine();
            o.Flush();
            o.Close();
        }

        static void ShowHelp()
        {
            //                 1       10        20        30        40        50        60        70        80
            //                 |--------|---------|---------|---------|---------|---------|---------|---------|
            Console.WriteLine(" Usage:");
            Console.WriteLine("  0xdd [/v {h|d|o}] [/w {<Number>|auto}] [/dump] <File>");
            Console.WriteLine();
            Console.WriteLine("  /v      Start with an offset view: Hex, Dec, Oct.        Default: Hex");
            Console.WriteLine("  /w      Start with a number of bytes to show in a row.   Default: Auto");
            Console.WriteLine("  /dump   Dumps the data as <File>.hexdmp as plain text.");
            Console.WriteLine();
            Console.WriteLine("  /?         Shows this screen and exits.");
            Console.WriteLine("  /version   Shows version and exits.");
        }

        static void ShowVersion()
        {
            //                 1       10        20        30        40        50        60        70        80
            //                 |--------|---------|---------|---------|---------|---------|---------|---------|
            Console.WriteLine();
            Console.WriteLine($"0xdd - {Version}");
            Console.WriteLine("Copyright (c) 2015-2016 DD~!/guitarxhero");
            Console.WriteLine("License: MIT License <http://opensource.org/licenses/MIT>");
            Console.WriteLine("Project page: <https://github.com/guitarxhero/0xdd>");
            Console.WriteLine();
            Console.WriteLine(" -- Credits --");
            Console.WriteLine("DD~! (guitarxhero) - Original author");
        }
    }
}
