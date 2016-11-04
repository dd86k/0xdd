using System;
using System.IO;
using static System.Reflection.Assembly;

namespace _0xdd
{
    static class Program
    {
        /// <summary>
        /// Get the current version of the project as a string object.
        /// </summary>
        public static readonly string Version =
            GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Get the project's name.
        /// </summary>
        public static readonly string Name =
            GetExecutingAssembly().GetName().Name;
        
        static int Main(string[] args)
        {
#if DEBUG
            // Used for debugging within Visual Studio (vshost)
            args = new string[] { "image.jpg" };
            //args = new string[] { "/o", "J", "image.jpg" };
            //args = new string[] { "test.txt" };
            //args = new string[] { "zero" };
#endif

            if (args.Length == 0)
            {
                // Future reminder:
                // New buffer in editing mode if no arguments
                ShowHelp();
                return 0;
            }
            
            // Defaults
            string entry = args[args.Length - 1];
            _0xdd.BytesPerRow = 0;
            _0xdd.OffsetView = OffsetView.Hex;
            bool dump = false;

            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-o": case "/o":
                        switch (args[i + 1][0])
                        {
                            case 'h': case 'H':
                                _0xdd.OffsetView = OffsetView.Hex;
                                break;
                            case 'd': case 'D':
                                _0xdd.OffsetView = OffsetView.Dec;
                                break;
                            case 'o': case 'O':
                                _0xdd.OffsetView = OffsetView.Oct;
                                break;
                            default:
                                Console.WriteLine(GetMessage(ErrorCode.CLI_InvalidOffsetView, args[i + 1]));
#if DEBUG
                                Console.ReadLine();
#endif
                                return ErrorCode.CLI_InvalidOffsetView.Code();
                        }
                        break;

                    case "-w": case "/w":
                        {
                            int b = _0xdd.BytesPerRow;
                            if (char.ToLower(args[i + 1][0]) != 'a') // Automatic, in case to overwrite settings
                            {
                                _0xdd.BytesPerRow = 0;
                            }
                            else if (!int.TryParse(args[i + 1], out b))
                            {
                                Console.WriteLine(GetMessage(ErrorCode.CLI_InvalidWidth, args[i + 1]));
#if DEBUG
                                Console.ReadLine();
#endif
                                return ErrorCode.CLI_InvalidWidth.Code();
                            }
                            _0xdd.BytesPerRow = b;
                        }
                        break;

                    case "-dump": case "/dump":
                        dump = true;
                        break;

                    case "/?":
                    case "-h":
                    case "-help":
                    case "--help":
                        ShowHelp();
                        return 0;

                    case "-v":
                    case "/ver":
                    case "--version":
                        ShowVersion();
                        return 0;
                }
            }
            
            if (dump)
            {
                Console.Write("Dumping file... ");
                ErrorCode err = Dumper.Dump(entry, _0xdd.BytesPerRow, _0xdd.OffsetView);
                
                Console.WriteLine(GetMessage(err));

                return err.Code();
            }
            else
            {
#if DEBUG
                // I want Visual Studio to catch the exceptions!
                ErrorCode r = ErrorCode.Success;

                r = _0xdd.Open(entry);

                Console.Clear();
                Console.WriteLine($"\nERRORCODE: {r} - 0x{r.Code():X2}");
                Console.ReadKey();
                return r.Code();
#else
                try
                {
                    _0xdd.Open(entry);

                    if (_0xdd.LastError != ErrorCode.Success)
                        Console.WriteLine(_0xdd.LastError.GetMessage());

                    return _0xdd.LastError.Code();
                }
                catch (Exception e)
                {
                    Abort(e);
                }

                return 0;
#endif
            }
        }

        /// <summary>
        /// Generate a line about the <see cref="ErrorCode"/>
        /// </summary>
        /// <param name="code"><see cref="ErrorCode"/></param>
        /// <returns><see cref="string"/></returns>
        /// <remarks> C syntax </remarks>
        static string GetMessage(this ErrorCode code, string arg = null)
        {
            string m = null;

            switch (code)
            {
                case ErrorCode.Success: return m = "OK!";

                case ErrorCode.FileNotFound:
                    m += "Error: File not found.";
                    break;
                case ErrorCode.FileUnreadable:
                    m += "Error: File not readable.";
                    break;
                case ErrorCode.FileAlreadyOpen:
                    m += "Error: File already open.";
                    break;
                case ErrorCode.FileUnauthorized:
                    m += "Error: Unauthorized to open file.";
                    break;
                case ErrorCode.FileZero:
                    m += "File is of zero length.";
                    break;

                case ErrorCode.PositionOutOfBound:
                    m += "Error: Position out of bound.";
                    break;

                case ErrorCode.DumberCannotWrite:
                    m += "Error: Could not write to output.";
                    break;
                case ErrorCode.DumberCannotRead:
                    m += "Error: Could not read from input.";
                    break;

                case ErrorCode.CLI_InvalidOffsetView:
                    m += $"Invalid parameter for /v : {arg}";
                    break;
                case ErrorCode.CLI_InvalidWidth:
                    m += $"Invalid parameter for /w : {arg}";
                    break;

                case ErrorCode.UnknownError:
                    m += "Error: Unknown error.";
                    break;
                default:
                    m += "Error: Unknown error. [default]";
                    break;

                // The "should not be an app return code" club
                case ErrorCode.FinderNoResult:
                case ErrorCode.FinderEmptyString:
                    break;
            }

            return m += $"\n ERROR: {code} - 0x{code.Code():X2}";
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

            using (StreamWriter o = File.CreateText("0xdd_oops.txt"))
            {
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
                o.WriteLine();
            }
        }

        static void ShowHelp()
        {
            //                 1       10        20        30        40        50        60        70        80
            //                 |--------|---------|---------|---------|---------|---------|---------|---------|
            Console.WriteLine(" Usage:");
            Console.WriteLine("  0xdd [<Options>] <File>");
            Console.WriteLine();
            Console.WriteLine("  /v      Specify starting offset view.        Default: Hex");
            //Console.WriteLine("  /w      Specify bytes per row.   Default: Auto");
            Console.WriteLine($"  /dump   Dump to an {Dumper.EXTENSION} file as plain text.");
            Console.WriteLine();
            Console.WriteLine("  /?         Shows this screen and exits.");
            Console.WriteLine("  /version   Shows version and exits.");
        }

        static void ShowVersion()
        {
            //                 1       10        20        30        40        50        60        70        80
            //                 |--------|---------|---------|---------|---------|---------|---------|---------|
            Console.WriteLine();
            Console.WriteLine($"{Name} - {Version}");
            Console.WriteLine("Copyright (c) 2015 guitarxhero");
            Console.WriteLine("License: MIT License <http://opensource.org/licenses/MIT>");
            Console.WriteLine("Project page: <https://github.com/guitarxhero/0xdd>");
            Console.WriteLine();
            Console.WriteLine(" -- Credits --");
            Console.WriteLine("DD~! (guitarxhero) - Original author");
        }
    }
}
