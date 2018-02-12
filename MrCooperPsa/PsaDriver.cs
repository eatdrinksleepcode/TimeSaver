using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MrCooperPsa {
    public class PsaDriver<TDriver> : IDisposable where TDriver : IWebDriver, IJavaScriptExecutor {
        private TDriver dynamicsDriver;

        public PsaDriver(TDriver driver) {
            this.dynamicsDriver = driver;
            this.dynamicsDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
        }

        public void Dispose() {
            if(null != dynamicsDriver) {
                dynamicsDriver.Dispose();
            }
        }

        const string dtoToJsDateFormat = "yyyy, M - 1, d";

        public void ExportEntriesToPSA(IEnumerable<Tuple<DateTimeOffset, TimeSpan>> entries) {
            var newButtonId = "msdyn_timeentry|NoRelationship|HomePageGrid|Mscrm.HomepageGrid.msdyn_timeentry.NewRecord";
            bool refresh = true;

            foreach (var entry in entries) {
                dynamicsDriver.ExecuteScript(@"
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

                dynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_date').setValue(new Date({entry.Item1.ToString(dtoToJsDateFormat)}));
                ");
                Thread.Sleep(1000);
                dynamicsDriver.SwitchTo().Frame(0);
                dynamicsDriver.FindElement(By.Id("msdyn_date_iDateInput")).SendKeys(Keys.Return);
                dynamicsDriver.SwitchTo().DefaultContent();
                Thread.Sleep(1000);
                dynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_type').setValue(171700002);
                ");
                dynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_project').setValue([{{
                        id: ""{{F1FBC909-CC8C-E711-811D-E0071B66DF51}}"",
                        type: ""10114"",
                        name: ""Home Intelligence""
                    }}]);
                ");
                dynamicsDriver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_projecttask').setValue([{{
                        id: ""{{22E5EC7A-86EF-46DF-B392-A97AFD816232}}"",
                        type: ""10119"",
                        name: ""4. Development""
                    }}]);
                ");
                dynamicsDriver.ExecuteScript($@"
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

        public void NavigateToDynamicsTimeEntries() {
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
    }
}
