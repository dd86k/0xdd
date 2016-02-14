using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using static System.Console;

namespace _0xdd_Updater
{
    class Program
    {
        const string UPDATE_URL = "http://didi.wilomgfx.net/";
        const string UPDATE_FILENAME = "0xdd.exe";
        const string UPDATE_VERSIONFILE = "0xdd_ver";

        static int Main(string[] args)
        {
            return Update();
        }

        static int Update()
        {
            try
            {
                WriteLine();

                WriteLine("Getting local version... ");
                Version localVersion;
                if (!File.Exists(UPDATE_FILENAME))
                {
                    WriteLine("0xdd couldn't be found, assuming version 0.0");
                    localVersion = new Version(0, 0);
                }
                else
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(UPDATE_FILENAME);
                    localVersion = new Version(fvi.FileVersion);
                }

                WriteLine("Getting online version...");
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
                    Version onlineVersion = new Version(ver);

                    WriteLine("Checking versions...");
                    if (!(onlineVersion.Minor > localVersion.Minor || onlineVersion.Major > localVersion.Major))
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
                if (str == null || !str.CanRead)
                {
                    WriteLine();
                    WriteLine("Unable to read the response.");
                    return 1;
                }
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
                    WriteLine("Additional message: " + e.Message);
                }
                else
                {
                    WriteLine("Unknown error.");
                }

                WriteLine();

                return 1;
            }

            return 0;
        }
    }
}
