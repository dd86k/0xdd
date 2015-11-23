using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace _0xdd
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
                    Assembly
                    .GetExecutingAssembly().GetName().Version.ToString();
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
                    Assembly
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
                        Process
                        .GetCurrentProcess().MainModule.FileName
                    );
            }
        }

        /// <summary>
        /// Entry point (.ctor)
        /// </summary>
        /// <param name="args">CLI arguments.</param>
        /// <returns>Error.</returns>
        static int Main(string[] args)
        {
#if DEBUG
            /* ~~ Used for debugging within Visual Studio (vshost) ~~ */
            //args = new string[] { ExecutableFilename };
            //args = new string[] { "f" };
            //args = new string[] { "fff" };
            //args = new string[] { "tt" };
            //args = new string[] { "-dump", "tt" };
            //args = new string[] { "gg.txt" };
            //args = new string[] { "/w", "a", "gg.txt" };
            args = new string[] { "0xdd.vshost.exe.config" };
           
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
            int bytesInRow = 16;
            _0xdd.OffsetBaseView ovm = _0xdd.OffsetBaseView.Hexadecimal;
            bool dump = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-v":
                    case "/v":
                    case "-view":
                    case "/view":
                        switch (args[i + 1][0])
                        {
                            case 'h':
                            case 'H':
                                // Don't change, h is default anyway.
                                break;
                            case 'd':
                            case 'D':
                                ovm = _0xdd.OffsetBaseView.Decimal;
                                break;
                            case 'o':
                            case 'O':
                                ovm = _0xdd.OffsetBaseView.Octal;
                                break;
                            default:
                                Console.WriteLine($"Invalid parameter for /v: {args[i + 1]}");
                                return 1;
                        }
                        break;

                    case "-w":
                    case "/w":
                    case "-width":
                    case "/width":
                        // Automatic
                        if (args[i + 1][0] == 'a' || args[i + 1][0] == 'A')
                        {
                            bytesInRow = ((Console.WindowWidth - 10) / 4) - 1;
                        }
                        // User-defined
                        else if (int.TryParse(args[i + 1], out bytesInRow))
                        {
                            if (bytesInRow < 1)
                            {
                                Console.WriteLine($"Invalid parameter for /w: {args[i + 1]} (Too low)");
                                return 1;
                            }
                        }
                        // If parsing failed
                        else
                        {
                            Console.WriteLine($"Invalid parameter for /w: {args[i + 1]} (Invalid format)");
                            return 1;
                        }
                        break;

                    case "-U":
                    case "/U":
                    case "-Update":
                    case "/Update":
                        Update();
                        return 0;

                    case "-dump":
                    case "/dump":
                        dump = true;
                        break;

                        //TODO: Interactive mode

                    case "/?":
                    case "/help":
                    case "-help":
                    case "--help":
                        ShowHelp();
                        return 0;

                    case "/ver":
                    case "-ver":
                    case "/version":
                    case "-version":
                    case "--version":
                        ShowVersion();
                        return 0;
                }
            }

            if (File.Exists(file))
            {
                Console.Clear();

                if (dump)
                {
                    Console.WriteLine("Dumping file...");
                    int err = _0xdd.Dump(file, bytesInRow, ovm);
                    switch (err)
                    {
                        case 1:
                            Console.WriteLine("File not found, aborted.");
                            break;
                        case 0:
                            Console.WriteLine("Dumping done!");
                            break;
                        default:
                            Console.WriteLine("Unknown error, aborted.");
                            return byte.MaxValue;
                    }
                    return err;
                }
                else
                {
                    #if DEBUG
                    // I want Visual Studio to catch the exceptions!
                    _0xdd.Open(file, ovm, bytesInRow);
                    #else
                    try
                    {
                        _0xdd.Open(file, ovm, bytesInRow);
                    }
                    catch (Exception e)
                    {
                        Abort(e);
                    }
                    #endif
                }
            }
            else
            {
                Console.WriteLine("File not found.");
                return 1;
            }

            return 0;
        }

        static void Update()
        {
            if (File.Exists(UPDATER_NAME))
            {
                ProcessStartInfo updater = new ProcessStartInfo(UPDATER_NAME);

                Process updaterProcess = new Process();
                updaterProcess.EnableRaisingEvents = true;
                updaterProcess.StartInfo = updater;
                updaterProcess.Exited += new EventHandler(currentProcess_Exited);

                //  Set the options.
                updater.UseShellExecute = false;
                updater.ErrorDialog = false;
                updater.CreateNoWindow = true;

                //  Specify redirection.
                updater.RedirectStandardError = true;
                updater.RedirectStandardInput = true;
                updater.RedirectStandardOutput = true;

                //updater.ErrorDialog = false;
                Process.Start(updater);
                try
                {
                    updaterProcess.Start();
                }
                catch
                {
                    Console.WriteLine("Error starting the process.");
                }
            }
            else
            {
                Console.WriteLine($"ABORTED: Updater not found. ({UPDATER_NAME})");
            }
        }

        private static void currentProcess_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Done.");
        }

        static void Abort(Exception e)
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Red;

            Console.WriteLine(" !! Fatal error !! ");

            Console.ResetColor();

            Console.WriteLine($"Exception: {e.GetType()}");
            Console.WriteLine($"Message: {e.Message}");
            Console.WriteLine("  -- BEGIN TRACE --");
            Console.WriteLine(e.StackTrace);
            Console.WriteLine("  -- END TRACE --");

            Console.WriteLine();
        }

        static void ShowHelp()
        {
            //                 1       10        20        30        40        50        60        70        80
            //                 |--------|---------|---------|---------|---------|---------|---------|---------|
            Console.WriteLine(" Usage:");
            Console.WriteLine("  0xdd [/v {h|d|o}] [/w {<Number>|auto}] [/U] [/dump] <file>");
            Console.WriteLine();
            Console.WriteLine("  /v      Start with an offset view: Hex, Dec, Oct.        Default: Hex");
            Console.WriteLine("  /w      Start with a number of bytes to show in a row.   Default: 16");
            Console.WriteLine("  /U      Updates if necessary.");
            Console.WriteLine("  /dump   Dumps the data file as plain text.");
            Console.WriteLine();
            Console.WriteLine("  /?         Shows this screen and exits.");
            Console.WriteLine("  /version   Shows version and exits.");
        }

        static void ShowVersion()
        {
            //                 1       10        20        30        40        50        60        70        80
            //                 |--------|---------|---------|---------|---------|---------|---------|---------|
            Console.WriteLine();
            Console.WriteLine($"0xdd - {ProjectVersionString}");
            Console.WriteLine("Copyright (c) 2015 DD~!/guitarxhero");
            Console.WriteLine("License: MIT License <http://opensource.org/licenses/MIT>");
            Console.WriteLine("Project page: <https://github.com/guitarxhero/0xDD>");
            Console.WriteLine();
            Console.WriteLine(" -- Credits --");
            Console.WriteLine("DD~! (guitarxhero) - Original author");
        }
    }
}
