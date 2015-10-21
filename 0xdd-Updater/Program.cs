using System;
using System.IO;
using System.Net;
using static System.Console;

// NOTE: The updater's version __must__ be par with 0xdd's.

namespace _0xdd_Updater
{
    class Program
    {
        const string UPDATE_URL = "http://didi.wilomgfx.net/";
        const string UPDATE_FILENAME = "0xdd.exe";
        const string UPDATE_VERSIONFILE = "0xdd_ver";

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

        static void Main(string[] args)
        {
            Update();
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
                try
                {
                    Version version = new Version(ver);

                    if (!(version.Minor > ProjectVersion.Minor || version.Major > ProjectVersion.Major))
                    {
                        WriteLine("You already have the latest version.");
                        return 0;
                    }
                }
                catch (Exception)
                {
                    WriteLine("Couldn't check the version, aborted.");
                    return 1;
                }

                WriteLine($"An update is available: {ver}");
                Write("Would you like to update now? ([Yes, No]): ");

                string answer = ReadLine().ToLower();

                switch (answer)
                { // I'm lazy
                    case "y":
                    case "ye":
                    case "yes":
                        break;
                    default:
                        return 0;
                }

                Write("Creating request...");
                WebRequest wr = WebRequest.Create($"{UPDATE_URL}{UPDATE_FILENAME}");
                WriteLine(" Done.");

                Write("Getting response...");
                WebResponse wres = wr.GetResponse();
                Stream str = wres.GetResponseStream();
                if (str == null)
                    throw new NullReferenceException("The response stream is null");
                if (str.CanRead)
                    throw new IOException("Can't read the response stream.");
                WriteLine(" Done.");

                Write("Saving file...");
                bool canWrite = false;
                while (!canWrite)
                {
                    try
                    {
                        using (FileStream sw = File.Create(UPDATE_FILENAME))
                        {
                            str.CopyTo(sw);
                        }

                        canWrite = true;
                    }
                    catch (Exception)
                    {
                        //TODO: Ask for user if they want to cancel instead.
                        Write(".");
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                WriteLine(" Done.");

                WriteLine("All ready captain!");
            }
            catch (Exception e)
            {
                WriteLine();
                Write("ABORTED: Couldn't update. ");

                // Can't connect or get file
                if (e is WebException)
                {
                    WriteLine("Unable to retrieve the file.");
                }
                // Can't overwrite file
                else if (e is IOException)
                {
                    WriteLine("Unable to write in the current directory.");
                }
                else
                {
                    WriteLine("Unknown error");
                }

                WriteLine();

                return 1;
            }

            return 0;
        }
    }
}
