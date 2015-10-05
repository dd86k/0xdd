using System;
using static System.Console;
using static System.IO.File;

namespace ConHexView
{
    class Program
    {
        /// <summary>
        /// Get the current version of the console oriented solution.
        /// </summary>
        static string ProjectVersion
        {
            get
            {
                return
                    System.Reflection.Assembly
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
                    System.Reflection.Assembly
                    .GetExecutingAssembly().GetName().Name;
            }
        }

        /// <summary>
        /// Get the current filename without extension of the executable.
        /// </summary>
        static string CurrentFilenameWithoutExtension
        {
            get
            {
                return
                    System.IO.Path.GetFileNameWithoutExtension(
                        System.Diagnostics.Process
                        .GetCurrentProcess().MainModule.FileName
                    );
            }
        }

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return 0;
            }

            //TODO: Treat arguments
            // -V/--offsetview [h]exa/[d]ecimal/[o]ctal
            for (int i = 0; i < args.Length; i++)
            {

            }
            
            string file = args[args.Length - 1];

            if (Exists(file))
            {
                Clear();
                try
                {
                    HexView.Open(file);
                }
                catch (Exception e)
                {
                    WriteLine();
                    WriteLine(" !! Fatal error !!");
                    WriteLine($"Exception: {e.GetType()}");
                    WriteLine($"Message: {e.Message}");
                    WriteLine($"Stack: {e.StackTrace}");
                    WriteLine();
                }
            }
            else
                return 1;

            return 0;
        }

        static void ShowHelp()
        {
            //         1       10        20        30        40        50        60        70        80
            //         |--------|---------|---------|---------|---------|---------|---------|---------|
            WriteLine(" Usage:");
            WriteLine($"  {CurrentFilenameWithoutExtension} [options] <file>");
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
