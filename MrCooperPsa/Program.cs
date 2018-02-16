using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace MrCooperPsa {
    class Program : IDisposable {
        private IPsaDriver dynamicsDriver;
        private ITimeworksDriver timeworksDriver;

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

        private Program() {

            var browserPreference = System.Environment.GetEnvironmentVariable("TW_MRC_BROWSER");

            if ("FIREFOX".Equals(browserPreference, StringComparison.OrdinalIgnoreCase)) {
                InitializeFirefoxDrivers();
            } else if("CHROME".Equals(browserPreference, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(browserPreference)) {
                InitializeChromeDrivers();
            } else {
                throw new Exception($"Invalid environment variable 'TW_MRC_BROWSER': '{browserPreference}'");
            }

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

        private void InitializeFirefoxDrivers() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            System.IO.Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(Program).Assembly.Location));
            var firefoxProfileDir = FirefoxDefaultProfileDirectory;;
            dynamicsDriver = new PsaDriver<FirefoxDriver>(CreateFirefoxDriver(firefoxProfileDir));
            timeworksDriver = new TimeworksDriver<FirefoxDriver>(CreateFirefoxDriver(firefoxProfileDir));
        }

        private static FirefoxDriver CreateFirefoxDriver(string firefoxProfileDir) {
            var options = new FirefoxOptions();
            options.LogLevel = FirefoxDriverLogLevel.Warn;
            if (null != firefoxProfileDir) {
                options.Profile = new FirefoxProfile(firefoxProfileDir);
            }

            try {
                return new FirefoxDriver(options);
            }
            catch (Exception ex) {
                Console.Error.WriteLine("Failed to create driver with default profile; is Firefox already running?");
                Console.Error.WriteLine("Attempting to start driver with temp profile");
                return new FirefoxDriver();
            }
        }

        private static string FirefoxDefaultProfileDirectory {
            get {
                var profilesDir = Path.Combine(UserLibAppSupportDirectory, "Firefox", "Profiles");
                var firefoxProfileDir = Directory.EnumerateDirectories(profilesDir).FirstOrDefault(p => p.EndsWith(".default"));
                return firefoxProfileDir;
            }
        }

        private static string UserLibAppSupportDirectory => Path.Combine(UserLibraryDirectory, "Application Support");

        private static string UserLibraryDirectory => Path.Combine(UserProfileDirectory, "Library");

        private static string UserProfileDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        private void InitializeChromeDrivers() {
            var options = new ChromeOptions();
            //options.AddArgument("user-data-dir=/Users/user/Library/Application Support/Google/Chrome");
            //options.AddArgument("--profile-directory=Default");

            var chromeDriverDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            dynamicsDriver = new PsaDriver<ChromeDriver>(new ChromeDriver(chromeDriverDir, options));
            timeworksDriver = new TimeworksDriver<ChromeDriver>(new ChromeDriver(chromeDriverDir, options));
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
                try {
                    dynamicsDriver.ExportEntriesToPSA(entries);
                    Console.WriteLine("Done exporting");
                } catch(Exception ex) {
                    Console.Error.WriteLine(ex);
                    Console.WriteLine("Error occurred during export. Please return to the time entry screen in PSA before trying again.");
                }
            }
        }

        public void Dispose() {
            dynamicsDriver.Dispose();
            timeworksDriver.Dispose();
        }
    }
}
