using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;

namespace MrCooperPsa {
    public class TimeworksDriver<TDriver> : DriverWrapper<TDriver>, ITimeworksDriver where TDriver : IWebDriver, IJavaScriptExecutor {
        private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        public TimeworksDriver(TDriver driver) : base(driver) {
        }

        public System.Threading.Tasks.Task NavigateToTimeworks(CancellationToken cancellation) {
            return System.Threading.Tasks.Task.Run(() => {
                Driver.Navigate().GoToUrl("https://thoughtworks.lightning.force.com/c/TimecardApp.app");
            }, cancellation);
        }

        public System.Threading.Tasks.Task SignInToTimeworks(CancellationToken cancellation) {
            return System.Threading.Tasks.Task.Run(() => {
                var twUsername = System.Environment.GetEnvironmentVariable("TW_USERNAME");
                var twPassword = System.Environment.GetEnvironmentVariable("TW_PASSWORD");

                if (!string.IsNullOrEmpty(twUsername)) {
                    Console.WriteLine($"TW username found ({twUsername}). Logging in...");

                    var usernameInput = Driver.FindElement(By.Id("okta-signin-username"));
                    usernameInput.SendKeys(twUsername);

                    var passwordInput = Driver.FindElement(By.Id("okta-signin-password"));
                    passwordInput.SendKeys(twPassword);

                    var signInButton = Driver.FindElement(By.Id("okta-signin-submit"));
                    signInButton.Click();
            } else {
                    Console.WriteLine($"TW username not found. Please log in.");
                }
            }, cancellation);
        }

        public System.Threading.Tasks.Task AddExportElementToPage(CancellationToken cancellation) {
            return System.Threading.Tasks.Task.Run(() => {
                var dateDisplay = Driver.FindElement(By.ClassName("date-display"));
                Driver.ExecuteScript(@"
                    const dateDisplayDiv = document.getElementsByClassName(""date-display"")[0];
                    dateDisplayDiv.style.cssText = dateDisplayDiv.style.cssText + ""; position: relative"";
    
                    const exportButton = document.createElement(""img"");
                    exportButton.id = ""export-to-psa"";
                    exportButton.src = ""https://media.glassdoor.com/sql/1748022/mr-cooper-squarelogo-1503336704472.png"";
                    exportButton.alt = ""Export To PSA"";
                    exportButton.style.cssText = ""position: absolute; left: -45px; width: 40px"";
                    exportButton.onclick = function() {
                        document.exportToPSA = true;
                        return false;
                    };
                    dateDisplayDiv.insertBefore(exportButton);
                ");
            }, cancellation);
        }

        public IEnumerable<TimeEntry> WaitForExportedEntries(CancellationToken cancellation) {
            Console.WriteLine("Waiting for export...");
            WaitUntil(TimeSpan.FromDays(1), () =>
                (bool)Driver.ExecuteScript(@"
                    if(document.exportToPSA) {
                        document.exportToPSA = false;
                        return true;
                    }
                    return false;
                "), cancellation);

            var dateDisplay = Driver.FindElement(By.ClassName("date-display"));

            var startDateParts = dateDisplay.Text.Split(" - ").First().Split(" ");
            var startDate = new DateTimeOffset(2018, 1, int.Parse(startDateParts[0]), 0, 0, 0, TimeSpan.Zero);
            while (startDate.ToString("MMM") != startDateParts[1]) {
                startDate = startDate.AddMonths(1);
            }

            Console.WriteLine($"Exporting starting at {startDate.ToString("d")}");

            var timecards = Driver.FindElements(By.ClassName("timecard-entry"));

            Console.WriteLine($"Found {timecards.Count} timecards");

            var entries = Enumerable.Empty<TimeEntry>();

            foreach (var timecard in timecards) {
                var timecardEntries = Enumerable.Empty<TimeEntry>();
                char[] digits = Enumerable.Range(0, 10).Select(i => (char)('0' + i)).ToArray();
                string timecardIdValue = timecard.GetAttribute("id");
                var id = timecardIdValue.Substring(timecardIdValue.Split(digits)[0].Length);
                Console.WriteLine($"Inspecting timecard {id}");
                var timecardHeader = timecard.FindElement(By.ClassName("header-name-for-timecard-entry"));
                if (timecardHeader.TagName != "span") {
                    var timecardAccount = timecardHeader.FindElement(By.ClassName("project-assignment-text")).Text;
                    var timecardProject = timecardHeader.FindElements(By.ClassName("project-assignment-text"))[1].Text;
                    if (timecardAccount.Contains("Mr. Cooper")) {
                        Console.WriteLine($"Exporting timecard {id} for {timecardAccount}");
                        timecardEntries =
                            from day in Days
                            let inputId = $"{day}{id}1"
                            let input = Driver.FindElement(By.Id(inputId))
                            let value = input.GetAttribute("value")
                            where !string.IsNullOrEmpty(value)
                            let duration = TimeSpan.FromHours(double.Parse(value))
                            where duration != TimeSpan.Zero
                            select new TimeEntry
                            {
                                Date = startDate.AddDays(Array.IndexOf(Days, day)),
                                Duration = duration,
                                Account = timecardAccount,
                                Project = timecardProject,
                            };
                    } else {
                        Console.WriteLine($"Skipping timecard {id}");
                    }
                }
                entries = entries.Concat(timecardEntries);
            }

            return entries;
        }
    }
}
