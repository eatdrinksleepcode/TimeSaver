using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace MrCooperPsa {
    class Program : IDisposable {
        private IWebDriver dynamicsDriver;
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
            dynamicsDriver = new ChromeDriver(chromeDriverDir, options);
            dynamicsDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            timeworksDriver = new TimeworksDriver<ChromeDriver>(new ChromeDriver(chromeDriverDir, options));
        }

        private void DoStuff() {
            NavigateToDynamicsTimeEntries();
            ExtractEntriesFromTimeworks();
        }

        private void ExtractEntriesFromTimeworks() {
            timeworksDriver.NavigateToTimeworks();
            timeworksDriver.SignInToTimeworks();
            timeworksDriver.AddExportElementToPage();

            while (true) {
                var entries = timeworksDriver.WaitForExportedEntries();
                ExportEntriesToPSA(entries);
                Console.WriteLine("Done exporting");
            }
        }

        const string dtoToJsDateFormat = "yyyy, M - 1, d";

        private void ExportEntriesToPSA(IEnumerable<Tuple<DateTimeOffset, TimeSpan>> entries) {
            var newButtonId = "msdyn_timeentry|NoRelationship|HomePageGrid|Mscrm.HomepageGrid.msdyn_timeentry.NewRecord";
            bool refresh = true;

            var jsDynamicsDriver = ((IJavaScriptExecutor)dynamicsDriver);

            foreach (var entry in entries) {
                ((IJavaScriptExecutor)dynamicsDriver).ExecuteScript(@"
                    document.getElementById('navBarOverlay').style.display = 'none';
                ");

                var newButton = dynamicsDriver.FindElement(By.Id(newButtonId)).FindElement(By.TagName("a")).FindElement(By.TagName("span"));
                newButton.Click();
                newButtonId = "msdyn_timeentry|NoRelationship|Form|Mscrm.Form.msdyn_timeentry.NewRecord";

                if (refresh) {
                    dynamicsDriver.Navigate().Refresh();
                    refresh = false;
                }
                dynamicsDriver.FindElement(By.Id("msdyn_timeentry|NoRelationship|Form|Mscrm.Form.msdyn_timeentry.Save")).FindElement(By.TagName("a")).FindElement(By.TagName("span"));

                jsDynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_date').setValue(new Date({entry.Item1.ToString(dtoToJsDateFormat)}));
                ");
                Thread.Sleep(1000);
                dynamicsDriver.SwitchTo().Frame(0);
                dynamicsDriver.FindElement(By.Id("msdyn_date_iDateInput")).SendKeys(Keys.Return);
                dynamicsDriver.SwitchTo().DefaultContent();
                Thread.Sleep(1000);
                jsDynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_type').setValue(171700002);
                ");
                jsDynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_project').setValue([{{
                        id: ""{{F1FBC909-CC8C-E711-811D-E0071B66DF51}}"",
                        type: ""10114"",
                        name: ""Home Intelligence""
                    }}]);
                ");
               jsDynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_projecttask').setValue([{{
                        id: ""{{22E5EC7A-86EF-46DF-B392-A97AFD816232}}"",
                        type: ""10119"",
                        name: ""4. Development""
                    }}]);
                ");
                jsDynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_duration').setValue({entry.Item2.TotalMinutes});
                ");

                new WebDriverWait(dynamicsDriver, TimeSpan.FromSeconds(10)).Until(d => {
                    return ((IJavaScriptExecutor)dynamicsDriver).ExecuteScript($@"
                        return frames[0].Xrm.Page.getAttribute('msdyn_resourcecategory').getValue() != null;
                    ");
                });

                var saveButton = dynamicsDriver.FindElement(By.Id("msdyn_timeentry|NoRelationship|Form|Mscrm.Form.msdyn_timeentry.Save")).FindElement(By.TagName("a")).FindElement(By.TagName("span"));
                saveButton.Click();

                dynamicsDriver.FindElement(By.Id("msdyn_timeentry|NoRelationship|Form|msdyn.msdyn_timeentry.Form.Submit"));
            }

            dynamicsDriver.FindElement(By.Id("Tabmsdyn_timeentry-main")).Click();
        }

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
            timeworksDriver.Dispose();
        }
    }
}
