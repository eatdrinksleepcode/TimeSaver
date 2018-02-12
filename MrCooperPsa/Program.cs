using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium.Chrome;

namespace MrCooperPsa {
    class Program : IDisposable {
        private PsaDriver<ChromeDriver> dynamicsDriver;
        private TimeworksDriver<ChromeDriver> timeworksDriver;

        static void Main(string[] args) {
            using (var p = new Program()) {
                p.DoStuff();
                Console.ReadLine();
            }
        }

        private static Size? FindScreenSize() {
            var process = Process.Start(new ProcessStartInfo {
                FileName = "system_profiler",
                Arguments = "SPDisplaysDataType",
                RedirectStandardOutput = true
            });
            process.WaitForExit();
            if(process.ExitCode != 0) {
                return null;
            }
            var sizes = ReadLines(process.StandardOutput)
                            .Where(line => line.Contains("Resolution"))
                            .SelectMany(line => line.Trim().Split(" ")
                                .Select(a => int.TryParse(a, out var result) ? result : (int?)null)
                                .Where(size => null != size)
                                .Select(size => size.Value))
                            .ToArray();

            if(sizes.Length == 2) {
                return new Size(sizes[0], sizes[1]);
            } else {
                return null;
            }
        }

        private static IEnumerable<string> ReadLines(StreamReader reader) {
            string line;
            while((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }

        public Program() {
            var options = new ChromeOptions();
            //options.AddArgument("user-data-dir=/Users/user/Library/Application Support/Google/Chrome");
            //options.AddArgument("--profile-directory=Default");
            var chromeDriverDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            dynamicsDriver = new PsaDriver<ChromeDriver>(new ChromeDriver(chromeDriverDir, options));
            timeworksDriver = new TimeworksDriver<ChromeDriver>(new ChromeDriver(chromeDriverDir, options));

            var screenSize = FindScreenSize();

            if(null != screenSize) {
                var position = new Point(0, 0);
                // WTF: screen size is twice as large as actual in both dimensions??
                var middleWidth = screenSize.Value.Width / 4;
                var size = new Size(middleWidth, screenSize.Value.Height / 2);

                timeworksDriver.SetScreenSize(position, size);
                dynamicsDriver.SetScreenSize(new Point(middleWidth, 0), size);
            }
        }

        private void DoStuff() {
            dynamicsDriver.NavigateToDynamicsTimeEntries();
            ExtractEntriesFromTimeworks();
        }

        private void ExtractEntriesFromTimeworks() {
            timeworksDriver.NavigateToTimeworks();
            timeworksDriver.SignInToTimeworks();
            timeworksDriver.AddExportElementToPage();

            while (true) {
                var entries = timeworksDriver.WaitForExportedEntries();
                dynamicsDriver.ExportEntriesToPSA(entries);
                Console.WriteLine("Done exporting");
            }
        }

        public void Dispose() {
            dynamicsDriver.Dispose();
            timeworksDriver.Dispose();
        }
    }
}
