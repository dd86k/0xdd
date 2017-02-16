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
            MainApp.BytesPerRow = 0;
            MainApp.OffsetView = OffsetView.Hex;
            bool dump = false;

            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-o": case "/o":
                        switch (args[i + 1][0])
                        {
                            case 'h': case 'H':
                                MainApp.OffsetView = OffsetView.Hex;
                                break;
                            case 'd': case 'D':
                                MainApp.OffsetView = OffsetView.Dec;
                                break;
                            case 'o': case 'O':
                                MainApp.OffsetView = OffsetView.Oct;
                                break;
                            default:
                                Console.WriteLine(
                                    ErrorCode.CLI_InvalidOffsetView
                                    .GetMessage(args[i + 1])
                                );
#if DEBUG
                                Console.ReadLine();
#endif
                                return ErrorCode.CLI_InvalidOffsetView.ToInt();
                        }
                        break;

                    case "-w": case "/w":
                        {
                            int b = MainApp.BytesPerRow;
                            if (char.ToLower(args[i + 1][0]) != 'a') // Automatic, in case to overwrite settings
                            {
                                MainApp.BytesPerRow = 0;
                            }
                            else if (!int.TryParse(args[i + 1], out b))
                            {
                                Console.WriteLine(
                                    ErrorCode.CLI_InvalidWidth
                                    .GetMessage(args[i + 1])
                                );
#if DEBUG
                                Console.ReadLine();
#endif
                                return ErrorCode.CLI_InvalidWidth.ToInt();
                            }
                            MainApp.BytesPerRow = b;
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
                ErrorCode err = Dumper.Dump(entry, MainApp.BytesPerRow, MainApp.OffsetView);
                
                Console.WriteLine(err.GetMessage());

                return err.ToInt();
            }
            else
            {
#if DEBUG
                // I want Visual Studio to catch the exceptions!
                MainApp.Open(entry);

                ErrorCode e = MainApp.LastError;

                Console.Clear();
                Console.WriteLine(
                    $"ERROR: {e} - {e.GetMessage()} (0x{e.ToInt():X8})"
                );
                Console.ReadKey();
                return e.ToInt();
#else
                try
                {
                    MainApp.Open(entry);

                    if (MainApp.LastError != ErrorCode.Success)
                        Console.WriteLine(MainApp.LastError.GetMessage());

                    return MainApp.LastError.ToInt();
                }
                catch (Exception e)
                {
                    Abort(e);
                }

                return 0;
#endif
            }
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
            Console.WriteLine("Project page: <https://github.com/dd86k/0xdd>");
            Console.WriteLine();
            Console.WriteLine(" -- Credits --");
            Console.WriteLine("DD~! (dd86k) - Original author");
        }
    }
}
