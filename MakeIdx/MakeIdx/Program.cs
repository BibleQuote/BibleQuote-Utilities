using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MakeIdx
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length < 2)
            {
                string appName = Assembly.GetExecutingAssembly().GetName().Name;
                Console.WriteLine($"Usage: {appName} htm_file idx_file [idx_encoding]");
            }

            string htmFile = args[0];
            string idxFile = args[1];

            string idxEncoding = args.Length > 2 ? args[2] : "utf-16";

            try
            {
                MakeIndexFile(htmFile, idxFile, idxEncoding);

                Console.WriteLine("Done.");
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
            }

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        private static void MakeIndexFile(string htmFile, string idxFile, string idxEncoding)
        {
            Regex rItem = new Regex("<h4>(?<header>.*?)</h4>");
            Regex rTitle = new Regex("<title>(?<title>.*?)</title>");

            string htmContent;

            long offsetBytes;
            int prevIndex = 0;

            Stream fs = File.OpenRead(htmFile);
            string detectedEncoding = DetectFileEncoding(fs);

            Console.WriteLine("htm file encoding: " + detectedEncoding);
            Encoding htmEncoding = Encoding.GetEncoding(detectedEncoding);
            using (StreamReader htmReader = new StreamReader(fs, htmEncoding))
            {
                offsetBytes = htmEncoding.GetPreamble().Length;
                htmContent = htmReader.ReadToEnd();
            }

            Match titleMatch = rTitle.Match(htmContent);
            MatchCollection itemsMatches = rItem.Matches(htmContent);

            using (StreamWriter idxWriter = new StreamWriter(idxFile, false, Encoding.GetEncoding(idxEncoding)))
            {
                idxWriter.WriteLine(titleMatch.Success ? titleMatch.Groups[1].Value : "");

                foreach (Match itemMatch in itemsMatches)
                {
                    int charIndex = itemMatch.Index - 1;
                    string text = htmContent.Substring(prevIndex, charIndex - prevIndex);

                    string itemText = itemMatch.Groups[1].Value;

                    offsetBytes += htmEncoding.GetBytes(text).Length;

                    idxWriter.WriteLine(itemText);
                    idxWriter.WriteLine($"{offsetBytes}");

                    prevIndex = charIndex;
                }
            }
        }

        private static string DetectFileEncoding(Stream fileStream)
        {
            Encoding utf8Verifier = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            using (StreamReader reader = new StreamReader(fileStream, utf8Verifier, true, leaveOpen: true, bufferSize: 1024))
            {
                string detectedEncoding;
                try
                {
                    while (!reader.EndOfStream)
                    {
                        reader.ReadLine();
                    }
                    detectedEncoding = reader.CurrentEncoding.BodyName;
                }
                catch (Exception)
                {
                    // failed to decode the file using the BOM/UT8.
                    // assume it's windows-1251
                    detectedEncoding = "windows-1251";
                }

                // Rewind the stream
                fileStream.Seek(0, SeekOrigin.Begin);
                return detectedEncoding;
            }
        }
    }
}
