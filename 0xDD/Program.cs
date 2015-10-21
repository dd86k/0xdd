using System;
using System.IO;
using static System.Console;
using static System.IO.File;

namespace ConHexView
{
    class Program
    {
        const string UPDATER_NAME = "0xdd_updater.exe";

        /// <summary>
        /// Get the current version of the project as a string object.
        /// </summary>
        static string ProjectVersionString
        {
            get
            {
                return
                    System.Reflection.Assembly
                    .GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        /// <summary>
        /// Gets the current version of the project as a <see cref="Version"/> object.
        /// </summary>
        static Version ProjectVersion
        {
            get
            {
                return
                    System.Reflection.Assembly
                    .GetExecutingAssembly().GetName().Version;
            }
        }

        /// <summary>
        /// Get the project's name.
        /// </summary>
        static string ProjectName
        {
            get
            {
                return
                    System.Reflection.Assembly
                    .GetExecutingAssembly().GetName().Name;
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
                        System.Diagnostics.Process
                        .GetCurrentProcess().MainModule.FileName
                    );
            }
        }

        static int Main(string[] args)
        {
#if DEBUG
            //args = new string[] { ExecutableFilename };
            //args = new string[] { "f" };
            //args = new string[] { "tt" };
            //args = new string[] { "-dump", "tt" };
            args = new string[] { "gg.txt" };
#endif

            if (args.Length == 0)
            {
                // Future reminder:
                // New buffer in editing mode if no arguments
                ShowHelp();
                return 0;
            }
            
            string file = args[args.Length - 1];

            int bytesRow = 16;
            HexView.OffsetViewMode ovm = HexView.OffsetViewMode.Hexadecimal;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-v":
                    case "/v":
                        switch (args[i + 1])
                        {
                            case "h":
                                // Default, so leave it as it is.
                                break;
                            case "d":
                                ovm = HexView.OffsetViewMode.Decimal;
                                break;
                            case "o":
                                ovm = HexView.OffsetViewMode.Octal;
                                break;
                            default:
                                WriteLine($"Aborted: {args[i + 1]} is invalid for -v");
                                return 1;
                        }
                        break;

                    case "-U":
                    case "/U":
                        Update();
                        break;

                    case "-dump":
                    case "/dump":
                        WriteLine("Dumping file...");
                        int err = HexView.Dump(file, ovm);
                        switch (err)
                        {
                            case 1:
                                WriteLine("File not found, aborted.");
                                break;
                            case 0:
                                WriteLine("Dumping done!");
                                break;
                            default:
                                WriteLine("Unknown error, aborted.");
                                break;
                        }
                        return err;
                }
            }

            if (Exists(file))
            {
                Clear();

#if RELEASE
                try
                {
                    HexView.Open(file, ovm);
                }
                catch (Exception e)
                {
                    Abort(e);
                }
#elif DEBUG
                // I want Visual Studio to catch the exceptions!
                HexView.Open(file, ovm, bytesRow);
#endif
            }
            else
            {
                WriteLine("File not found.");
                return 1;
            }

            return 0;
        }

        static void Update()
        {
            //TODO: Extract the updater

            if (Exists(UPDATER_NAME))
                System.Diagnostics.Process.Start(UPDATER_NAME);
            else
                WriteLine("ABORTED: Updater not found.");
        }
        
        static void Abort(Exception e)
        {
            WriteLine();
            ForegroundColor = ConsoleColor.White;
            BackgroundColor = ConsoleColor.Red;
            WriteLine(" !! Fatal error !! ");
            ResetColor();
            WriteLine($"Exception: {e.GetType()}");
            WriteLine($"Message: {e.Message}");
            WriteLine($"Stack: {e.StackTrace}");
            WriteLine();
        }

        static void ShowHelp()
        {
            //         1       10        20        30        40        50        60        70        80
            //         |--------|---------|---------|---------|---------|---------|---------|---------|
            WriteLine(" Usage:");
            WriteLine($"  0xdd [-v {{h|d|o}}] [-U] [-dump] <file>");
            WriteLine();
            WriteLine("  -v       Start with an offset view: Hex, Dec, Oct. Default: Hex");
            /*
            WriteLine("  -d       Start with a predefined width. Default: 16");
            */
            WriteLine("  -U       Updates if necessary.");
            WriteLine("  -dump    Dumps a data file as plain text.");
            WriteLine();
            WriteLine("  /help, /?   Shows this screen and exits.");
            WriteLine("  /version    Shows version and exits.");
        }

        static void ShowVersion()
        {
            //         1       10        20        30        40        50        60        70        80
            //         |--------|---------|---------|---------|---------|---------|---------|---------|
            WriteLine();
            WriteLine($"0xDD - {ProjectVersion}");
            WriteLine("Copyright (c) 2015 DD~!/guitarxhero");
            WriteLine("License: MIT License <http://opensource.org/licenses/MIT>");
            WriteLine("Project page: <https://github.com/guitarxhero/0xDD>");
            WriteLine();
            WriteLine(" -- Credits --");
            WriteLine("DD~! (guitarxhero) - Original author");
        }
    }
}
