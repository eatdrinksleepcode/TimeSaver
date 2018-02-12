using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium.Chrome;

namespace MrCooperPsa {
    class Program : IDisposable {
        private PsaDriver<ChromeDriver> dynamicsDriver;
        private TimeworksDriver<ChromeDriver> timeworksDriver;

        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
            using (var p = new Program()) {
                p.DoStuff();
                Console.ReadLine();
            }
        }

        public Program() {
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
