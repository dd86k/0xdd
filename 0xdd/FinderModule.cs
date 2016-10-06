using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace _0xdd
{
    public static class Finder
    {
        //static string _lastStringData;
        //static byte _lastByteData;

        /// <summary>
        /// Find a byte at a specific position.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="pos">Positon to start searching from.</param>
        /// <returns>Positon, if found.</returns>
        public static long FindByte(byte data, FileStream io, FileInfo file, long pos = 0)
        {
            if (!file.Exists)
                return -3; // File not found

            if (pos < 0 || pos > file.Length)
                return -2; // Out of bound

            io.Position = pos;

            bool Continue = true;
            while (Continue)
            {
                if (data == (byte)io.ReadByte())
                    return io.Position;

                if (io.Position >= io.Length)
                    Continue = false;
            }

            // If not found, place the position back it was before
            io.Position = pos;

            return -1;
        }

        /// <summary>
        /// Find a string of data with a given position.
        /// </summary>
        /// <param name="data">Data as a string.</param>
        /// <param name="pos">Starting position.</param>
        /// <returns><see cref="FindResult"/></returns>
        /// <remarks>
        /// How does this work?
        /// Search every character, if the first one seems to be right,
        /// read the data and compare it.
        /// </remarks>
        public static long FindString(string data, FileStream io, FileInfo file, long pos = 0)
        {
            if (!file.Exists)
                return -3;

            if (pos < 0 || pos > file.Length)
                return -2;

            if (string.IsNullOrWhiteSpace(data))
                return -4;

            io.Position = pos;

            byte[] b = new byte[data.Length];
            bool c = true;
            if (data.StartsWith("/") && data.EndsWith("/"))
            {
                RegexOptions rf = RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.CultureInvariant;

                Regex r = new Regex(data.Trim(new char[] { '/' }), rf);

                InfoPanel.Message("Searching with regex...");

                while (c)
                {
                    if (io.Position + data.Length > io.Length)
                        c = false;

                    if (r.IsMatch(char.ToString((char)io.ReadByte())))
                    {
                        if (data.Length == 1)
                        {
                            return io.Position - 1;
                        }
                        else
                        {
                            io.Position--;
                            io.Read(b, 0, b.Length);
                            if (r.IsMatch(Encoding.ASCII.GetString(b)))
                            {
                                return io.Position - data.Length;
                            }
                        }
                    }
                }
            }
            else // Non-regex
            {
                while (c)
                {
                    if (io.Position + data.Length > io.Length)
                        c = false;

                    if (data[0] == (char)io.ReadByte())
                    {
                        if (data.Length == 1)
                        {
                            return io.Position - 1;
                        }
                        else
                        {
                            io.Position--;
                            io.Read(b, 0, b.Length);
                            if (data == Encoding.ASCII.GetString(b))
                            {
                                return io.Position - data.Length;
                            }
                        }
                    }
                }
            }

            io.Position = pos - 1;

            return -1;
        }
    }
}
