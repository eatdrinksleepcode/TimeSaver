using System;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace MrCooperPsa {
    class Program : IDisposable {
        private IWebDriver dynamicsDriver;
        private IWebDriver timeworksDriver;

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
            //dynamicsDriver = new ChromeDriver(chromeDriverDir, options);
            //dynamicsDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            timeworksDriver = new ChromeDriver(chromeDriverDir, options);
            timeworksDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
        }

        private void DoStuff() {
            timeworksDriver.Navigate().GoToUrl("https://thoughtworks.lightning.force.com/c/TimecardApp.app");

            var twUsername = System.Environment.GetEnvironmentVariable("TW_USERNAME");
            var twPassword = System.Environment.GetEnvironmentVariable("TW_PASSWORD");

            if (!string.IsNullOrEmpty(twUsername)) {
                Console.WriteLine($"TW username found ({twUsername}). Logging in...");

                var usernameInput = timeworksDriver.FindElement(By.Id("okta-signin-username"));
                usernameInput.SendKeys(twUsername);

                var passwordInput = timeworksDriver.FindElement(By.Id("okta-signin-password"));
                passwordInput.SendKeys(twPassword);

                var signInButton = timeworksDriver.FindElement(By.Id("okta-signin-submit"));
                signInButton.Click();
            } else {
                Console.WriteLine($"TW username not found. Please log in.");
            }

            timeworksDriver.FindElement(By.Id("timecardInfo0"));

            var dateDisplay = timeworksDriver.FindElement(By.ClassName("date-display"));
            var timeworksJS = (IJavaScriptExecutor)timeworksDriver;
            timeworksJS.ExecuteScript(@"
                const exportDiv = document.createElement(""span"");
                exportDiv.innerText = ""Export To PSA"";
                exportDiv.style.cssText = ""padding-right: 10px"";
                exportDiv.onclick = function() {
                    document.exportToPSA = true;
                };
                const dateDisplayDiv = document.getElementsByClassName(""date-display"")[0];
                dateDisplayDiv.parentNode.insertBefore(exportDiv, dateDisplayDiv);
            ");

            while (true) {
                WaitForExport();
            }

            //NavigateToDynamicsTimeEntries();
        }

        private void WaitForExport() {
            Console.WriteLine("Waiting for export...");
            var result = new WebDriverWait(timeworksDriver, TimeSpan.FromDays(1)).Until(driver => {
                Console.WriteLine("Checking for export...");
                var jsDriver = (IJavaScriptExecutor)driver;
                return (bool)jsDriver.ExecuteScript(@"
                    if(document.exportToPSA) {
                        document.exportToPSA = false;
                        return true;
                    }
                    return false;
                ");
            });

            var dateDisplay = timeworksDriver.FindElement(By.ClassName("date-display"));

            var startDateParts = dateDisplay.Text.Split(" - ").First().Split(" ");
            var startDate = new DateTimeOffset(2018, 1, int.Parse(startDateParts[0]), 0, 0, 0, TimeSpan.Zero);
            while (startDate.ToString("MMM") != startDateParts[1]) {
                startDate = startDate.AddMonths(1);
            }

            Console.WriteLine($"Exporting starting at {startDate.ToString("d")}");

            var timecards = timeworksDriver.FindElements(By.ClassName("timecard-entry"));

            Console.WriteLine($"Found {timecards.Count} timecards");

            foreach (var timecard in timecards) {
                char[] digits = Enumerable.Range(0, 10).Select(i => (char)('0' + i)).ToArray();
                string timecardIdValue = timecard.GetAttribute("id");
                var id = timecardIdValue.Substring(timecardIdValue.Split(digits)[0].Length);
                Console.WriteLine($"Inspecting timecard {id}");
                var timecardHeader = timecard.FindElement(By.ClassName("header-name-for-timecard-entry"));
                if(timecardHeader.TagName == "span") {
                    continue;
                }
                var timecardAccount = timecardHeader.FindElement(By.TagName("div")).FindElement(By.ClassName("project-assignment-text")).Text;
                if (timecardAccount.Contains("Mr. Cooper")) {
                    Console.WriteLine($"Exporting timecard {id} for {timecardAccount}");
                    var entries = Days.Select((d, i) => {
                        var inputId = $"{d}{id}1";
                        Console.WriteLine($"Finding input {inputId}");
                        var input = timeworksDriver.FindElement(By.Id(inputId));
                        Console.WriteLine($"Found input {inputId}");
                        var value = input.GetAttribute("value");
                        Console.WriteLine($"{inputId}.value = {value}");
                        if (!string.IsNullOrEmpty(value)) {
                            var hours = decimal.Parse(value);
                            var day = startDate.AddDays(i);
                            Console.WriteLine($"{day.ToString("d")} = {hours} hours");
                            return Tuple.Create(day, hours);
                        }
                        return null;
                    }).Where(x => null != x).Where(x => x.Item2 > 0);
                    foreach (var entry in entries) {
                        Console.WriteLine($"{entry.Item1.ToString("d")} = {entry.Item2} hours");
                    }
                } else {
                    Console.WriteLine($"Skipping timecard {id}");
                }
            }

            Console.WriteLine("Done exporting");
        }

        static string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        private void NavigateToDynamicsTimeEntries() {
            dynamicsDriver.Navigate().GoToUrl("https://cooper.crm.dynamics.com/main.aspx");

            var mrCooperEmail = System.Environment.GetEnvironmentVariable("MRCOOPER_EMAIL");
            var mrCooperPassword = System.Environment.GetEnvironmentVariable("MRCOOPER_PASSWORD");

            if (!string.IsNullOrEmpty(mrCooperEmail)) {
                Console.WriteLine($"Mr Cooper email found ({mrCooperEmail}). Logging in...");

                var emailInput = dynamicsDriver.FindElement(By.Name("loginfmt"));
                emailInput.SendKeys(mrCooperEmail);

                var nextButton = dynamicsDriver.FindElement(By.Id("idSIButton9"));
                nextButton.Click();

                var passwordInput = dynamicsDriver.FindElement(By.Id("passwordInput"));
                passwordInput.SendKeys(mrCooperPassword);

                var submitButton = dynamicsDriver.FindElement(By.Id("submitButton"));
                submitButton.Click();

                var dontSaveId = dynamicsDriver.FindElement(By.Id("idBtn_Back"));
                dontSaveId.Click();
            } else {
                Console.WriteLine("Mr Cooper email not found. Please log in.");
            }

            var projectServiceArrow = dynamicsDriver.FindElement(By.Id("TabSI")).FindElement(By.TagName("a"));
            projectServiceArrow.Click();

            var timeEntriesLink = dynamicsDriver.FindElement(By.Id("msdyn_timeentry"));
            timeEntriesLink.Click();
        }

        public void Dispose() {
            dynamicsDriver.Dispose();
            //timeworksDriver.Dispose();
        }
    }
}
