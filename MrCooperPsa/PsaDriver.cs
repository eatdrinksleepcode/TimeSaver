using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MrCooperPsa {
    public class PsaDriver<TDriver> : DriverWrapper<TDriver>, IPsaDriver where TDriver : IWebDriver, IJavaScriptExecutor {

        public PsaDriver(TDriver driver) : base(driver) {
        }

        const string dtoToJsDateFormat = "yyyy, M - 1, d";

        public void ExportEntriesToPSA(IEnumerable<Tuple<DateTimeOffset, TimeSpan>> entries) {
            var newButtonId = "msdyn_timeentry|NoRelationship|HomePageGrid|Mscrm.HomepageGrid.msdyn_timeentry.NewRecord";
            bool refresh = true;

            foreach (var entry in entries) {
                Driver.ExecuteScript(@"
                    document.getElementById('navBarOverlay').style.display = 'none';
                ");

                var newButton = Driver.FindElement(By.Id(newButtonId)).FindElement(By.TagName("a")).FindElement(By.TagName("span"));
                newButton.Click();
                newButtonId = "msdyn_timeentry|NoRelationship|Form|Mscrm.Form.msdyn_timeentry.NewRecord";

                if (refresh) {
                    Driver.Navigate().Refresh();
                    refresh = false;
                }
                Driver.FindElement(By.Id("msdyn_timeentry|NoRelationship|Form|Mscrm.Form.msdyn_timeentry.Save")).FindElement(By.TagName("a")).FindElement(By.TagName("span"));

                Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_date').setValue(new Date({entry.Item1.ToString(dtoToJsDateFormat)}));
                ");
                Thread.Sleep(1000);
                Driver.SwitchTo().Frame(0);
                Driver.FindElement(By.Id("msdyn_date_iDateInput")).SendKeys(Keys.Return);
                Driver.SwitchTo().DefaultContent();
                Thread.Sleep(1000);
                Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_type').setValue(171700002);
                ");
                Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_project').setValue([{{
                        id: ""{{F1FBC909-CC8C-E711-811D-E0071B66DF51}}"",
                        type: ""10114"",
                        name: ""Home Intelligence""
                    }}]);
                ");
                Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_projecttask').setValue([{{
                        id: ""{{22E5EC7A-86EF-46DF-B392-A97AFD816232}}"",
                        type: ""10119"",
                        name: ""4. Development""
                    }}]);
                ");
                Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_duration').setValue({entry.Item2.TotalMinutes});
                ");

                WaitUntil(TimeSpan.FromSeconds(10), () => {
                    return Driver.ExecuteScript($@"
                        return frames[0].Xrm.Page.getAttribute('msdyn_resourcecategory').getValue() != null;
                    ");
                });

                var saveButton = Driver.FindElement(By.Id("msdyn_timeentry|NoRelationship|Form|Mscrm.Form.msdyn_timeentry.Save")).FindElement(By.TagName("a")).FindElement(By.TagName("span"));
                saveButton.Click();

                Driver.FindElement(By.Id("msdyn_timeentry|NoRelationship|Form|msdyn.msdyn_timeentry.Form.Submit"));
            }

            Driver.FindElement(By.Id("Tabmsdyn_timeentry-main")).Click();
        }

        public void NavigateToDynamicsTimeEntries() {
            Driver.Navigate().GoToUrl("https://cooper.crm.dynamics.com/main.aspx");

            var mrCooperEmail = System.Environment.GetEnvironmentVariable("MRCOOPER_EMAIL");
            var mrCooperPassword = System.Environment.GetEnvironmentVariable("MRCOOPER_PASSWORD");

            if (!string.IsNullOrEmpty(mrCooperEmail)) {
                Console.WriteLine($"Mr Cooper email found ({mrCooperEmail}). Logging in...");

                var emailInput = Driver.FindElement(By.Name("loginfmt"));
                emailInput.SendKeys(mrCooperEmail);

                var nextButton = Driver.FindElement(By.Id("idSIButton9"));
                nextButton.Click();

                var passwordInput = Driver.FindElement(By.Id("passwordInput"));
                passwordInput.SendKeys(mrCooperPassword);

                var submitButton = Driver.FindElement(By.Id("submitButton"));
                submitButton.Click();

                var dontSaveId = Driver.FindElement(By.Id("idBtn_Back"));
                dontSaveId.Click();
            } else {
                Console.WriteLine("Mr Cooper email not found. Please log in.");
            }

            var projectServiceArrow = Driver.FindElement(By.Id("TabSI")).FindElement(By.TagName("a"));
            projectServiceArrow.Click();

            var timeEntriesLink = Driver.FindElement(By.Id("msdyn_timeentry"));
            timeEntriesLink.Click();
        }
    }
}
