using System;
using System.IO;
using System.Net;
using static System.Console;
using static System.IO.File;

namespace ConHexView
{
    class Program
    {
        const string UPDATE_URL = "http://didi.wilomgfx.net/";
        const string UPDATE_FILENAME = "0xdd";
        const string UPDATE_VERSIONFILE = "0xdd_ver";

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
        /// Get the current executable's filename without extension.
        /// </summary>
        static string ExecutableFilenameWithoutExtension
        {
            get
            {
                return
                    Path.GetFileNameWithoutExtension(
                        System.Diagnostics.Process
                        .GetCurrentProcess().MainModule.FileName
                    );
            }
        }

        static int Main(string[] args)
        {
#if DEBUG
            //args = new string[] { ExecutableFilenameWithoutExtension + ".exe" };
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
                        return Update();

                    case "-dump":
                    case "/dump":
                        WriteLine("Dumping file...");
                        HexView.Dump(file, ovm);
                        WriteLine("Dumping done!");
                        return 0;
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
                HexView.Open(file, ovm);
#endif
            }
            else
            {
                WriteLine("File not found.");
                return 1;
            }

            return 0;
        }

        static int Update()
        {
            try
            {
                WriteLine();
                WriteLine("Checking version...");
                WebRequest ver_wr = WebRequest.Create($"{UPDATE_URL}{UPDATE_VERSIONFILE}");
                WebResponse ver_wb = ver_wr.GetResponse();
                Stream ver_str = ver_wb.GetResponseStream();
                string ver;
                using (StreamReader sr = new StreamReader(ver_str))
                {
                    ver = sr.ReadToEnd();
                }
                Version version = new Version(ver);

                //TODO: Revise the update checking method
                if (!(version.Minor > ProjectVersion.Minor || version.Major > ProjectVersion.Major))
                {
                    WriteLine("You already have the latest version.");
                    return 0;
                }

                WriteLine($"An update is available: {ver}");
                Write("Would you like to update now? [Yes, No] ");

                string answer = ReadLine().ToLower();

                switch (answer)
                { // I'm lazy
                    case "y":
                    case "ye":
                    case "yes": break;
                    default: return 0;
                }

                Write("Creating request...");
                WebRequest wr = WebRequest.Create($"{UPDATE_URL}{UPDATE_FILENAME}.exe");
                WriteLine(" Done.");

                Write("Getting response...");
                WebResponse wres = wr.GetResponse();
                Stream str = wres.GetResponseStream();
                if (str == null)
                    throw new NullReferenceException();
                WriteLine(" Done.");

                Write("Saving file...");
                string newname = $"{UPDATE_FILENAME}.exe";

                int i = 1;
                bool foundname = false;
                while (!foundname)
                {
                    if (Exists(newname))
                    {
                        newname = $"{newname}-{i}.exe";
                        i++;
                    }
                    else
                        foundname = true;
                }

                using (var sw = Create(newname))
                {
                    str.CopyTo(sw);
                }
                WriteLine(" Done.");

                //TODO: Find a way to replace the file.

                //Write("...");
                //
                //WriteLine(" Done.");

                WriteLine($"");
            }
            catch (Exception e)
            {
                WriteLine();
                WriteLine("Aborting -- Couldn't update.");

                // Can't connect or get file
                if (e is WebException)
                {
                    WriteLine("Unable to retrieve the file.");
                }
                // Can't overwrite file
                else if (e is System.IO.IOException)
                {
                    WriteLine("Unable to write in the current directory.");
                }
                else
                {
                    WriteLine("No idea what happened.");
                }

                WriteLine();

                return 1;
            }

            return 0;
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
            WriteLine($"  {ExecutableFilenameWithoutExtension} [-v {{h|d|o}}] [-U] [-dump] <file>");
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
