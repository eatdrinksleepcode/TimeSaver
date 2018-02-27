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
        private IConfigDriver configDriver;

        static void Main(string[] args) {
            using (var p = new Program()) {
                p.Run();
                Console.ReadLine();
            }
        }

        private static Rect? FindScreenSize() {
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

            const int systemBarHeight = 25;
            if(sizes.Length == 2) {
                return new Rect {
                    Position = new Point(0, systemBarHeight),
                    // WTF: screen size is twice as large as actual in both dimensions??
                    Size = new Size(sizes[0] / 2, (sizes[1] / 2) - systemBarHeight)
                };
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
                const int configHeight = 250;
                var (top, bottom) = screenSize.Value.SliceBottom(configHeight);
                var (left, right) = top.SplitVertical();

                timeworksDriver.SetScreenSize(left.Position, left.Size);
                dynamicsDriver.SetScreenSize(right.Position, right.Size);
                configDriver.SetScreenSize(bottom.Position, bottom.Size);
            }
        }

        private void InitializeFirefoxDrivers() {
            var options = CreateFirefoxOptions();
            dynamicsDriver = new PsaDriver<FirefoxDriver>(CreateFirefoxDriver(options));
            timeworksDriver = new TimeworksDriver<FirefoxDriver>(CreateFirefoxDriver(options));
            configDriver = new ConfigDriver<FirefoxDriver>(CreateFirefoxDriver(options));
        }

        private static FirefoxOptions CreateFirefoxOptions() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            System.IO.Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(Program).Assembly.Location));
            var firefoxProfileDir = FirefoxDefaultProfileDirectory;;
            var options = new FirefoxOptions();
            options.LogLevel = FirefoxDriverLogLevel.Warn;
            if (null != firefoxProfileDir) {
                options.Profile = new FirefoxProfile(firefoxProfileDir);
            }

            return options;
        }

        private static FirefoxDriver CreateFirefoxDriver(FirefoxOptions options) {
            try {
                return new FirefoxDriver(options);
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
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
            configDriver = new ConfigDriver<ChromeDriver>(new ChromeDriver(chromeDriverDir, options));
        }

        private void Run() {
            var configTask = configDriver.WaitForSave();

            configDriver.NavigateToConfigPage();
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
