using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;

namespace MrCooperPsa {
    public class TimeworksDriver<TDriver> : DriverWrapper<TDriver> where TDriver : IWebDriver, IJavaScriptExecutor {
        private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        public TimeworksDriver(TDriver driver) : base(driver) {
            this.Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
        }

        public void NavigateToTimeworks() {
            Driver.Navigate().GoToUrl("https://thoughtworks.lightning.force.com/c/TimecardApp.app");
        }

        public void SignInToTimeworks() {
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
        }

        public void AddExportElementToPage() {
            var dateDisplay = Driver.FindElement(By.ClassName("date-display"));
            Driver.ExecuteScript(@"
                const exportDiv = document.createElement(""span"");
                exportDiv.innerText = ""Export To PSA"";
                exportDiv.style.cssText = ""padding-right: 10px"";
                exportDiv.onclick = function() {
                    document.exportToPSA = true;
                };
                const dateDisplayDiv = document.getElementsByClassName(""date-display"")[0];
                dateDisplayDiv.parentNode.insertBefore(exportDiv, dateDisplayDiv);
            ");
        }

        public IEnumerable<Tuple<DateTimeOffset, TimeSpan>> WaitForExportedEntries() {
            Console.WriteLine("Waiting for export...");
            var result = WaitUntil(TimeSpan.FromDays(1), () => {
                Console.WriteLine("Checking for export...");
                return (bool)Driver.ExecuteScript(@"
                    if(document.exportToPSA) {
                        document.exportToPSA = false;
                        return true;
                    }
                    return false;
                ");
            });

            var dateDisplay = Driver.FindElement(By.ClassName("date-display"));

            var startDateParts = dateDisplay.Text.Split(" - ").First().Split(" ");
            var startDate = new DateTimeOffset(2018, 1, int.Parse(startDateParts[0]), 0, 0, 0, TimeSpan.Zero);
            while (startDate.ToString("MMM") != startDateParts[1]) {
                startDate = startDate.AddMonths(1);
            }

            Console.WriteLine($"Exporting starting at {startDate.ToString("d")}");

            var timecards = Driver.FindElements(By.ClassName("timecard-entry"));

            Console.WriteLine($"Found {timecards.Count} timecards");

            var entries = Enumerable.Empty<Tuple<DateTimeOffset, TimeSpan>>();

            foreach (var timecard in timecards) {
                var timecardEntries = Enumerable.Empty<Tuple<DateTimeOffset, TimeSpan>>();
                char[] digits = Enumerable.Range(0, 10).Select(i => (char)('0' + i)).ToArray();
                string timecardIdValue = timecard.GetAttribute("id");
                var id = timecardIdValue.Substring(timecardIdValue.Split(digits)[0].Length);
                Console.WriteLine($"Inspecting timecard {id}");
                var timecardHeader = timecard.FindElement(By.ClassName("header-name-for-timecard-entry"));
                if (timecardHeader.TagName != "span") {
                    var timecardAccount = timecardHeader.FindElement(By.TagName("div")).FindElement(By.ClassName("project-assignment-text")).Text;
                    if (timecardAccount.Contains("Mr. Cooper")) {
                        Console.WriteLine($"Exporting timecard {id} for {timecardAccount}");
                        timecardEntries = Days.Select((d, i) => {
                            var inputId = $"{d}{id}1";
                            Console.WriteLine($"Finding input {inputId}");
                            var input = Driver.FindElement(By.Id(inputId));
                            Console.WriteLine($"Found input {inputId}");
                            var value = input.GetAttribute("value");
                            Console.WriteLine($"{inputId}.value = {value}");
                            if (!string.IsNullOrEmpty(value)) {
                                var hours = double.Parse(value);
                                var day = startDate.AddDays(i);
                                Console.WriteLine($"{day.ToString("d")} = {hours} hours");
                                return Tuple.Create(day, TimeSpan.FromHours(hours));
                            }
                            return null;
                        }).Where(x => null != x).Where(x => x.Item2 != TimeSpan.Zero);
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
