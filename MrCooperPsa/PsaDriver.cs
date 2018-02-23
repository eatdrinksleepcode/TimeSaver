using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium;

namespace MrCooperPsa {
    public class PsaDriver<TDriver> : DriverWrapper<TDriver>, IPsaDriver where TDriver : IWebDriver, IJavaScriptExecutor {

        public PsaDriver(TDriver driver) : base(driver) {
        }

        const string dtoToJsDateFormat = "yyyy, M - 1, d";

        public void ExportEntriesToPSA(IEnumerable<TimeEntry> entries) {
            var newButtonId = "msdyn_timeentry|NoRelationship|HomePageGrid|Mscrm.HomepageGrid.msdyn_timeentry.NewRecord";
            bool refresh = true;

            foreach (var entry in entries) {
                var project = DeterminePsaProject(entry);
                if (null == project) {
                    continue;
                }

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
                    frames[0].Xrm.Page.getAttribute('msdyn_date').setValue(new Date({entry.Date.ToString(dtoToJsDateFormat)}));
                ");
                Thread.Sleep(1000);
                Driver.SwitchTo().Frame(0);
                Driver.FindElement(By.Id("msdyn_date_iDateInput")).SendKeys(Keys.Return);
                Driver.SwitchTo().DefaultContent();
                Thread.Sleep(1000);
                Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_type').setValue(171700002);
                ");
                SetProject(project.Value);
                SetTask(project.Value);
                Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_duration').setValue({entry.Duration.TotalMinutes});
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

        private void SetTask(Project project)
        {
            var task = project.DevelopmentTask;
            Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_projecttask').setValue([{{
                        id: ""{{{task.Id}}}"",
                        type: ""10119"",
                        name: ""{task.Name}""
                    }}]);
                ");
        }

        private void SetProject(Project project)
        {
            Driver.ExecuteScript($@"
                    frames[0].Xrm.Page.getAttribute('msdyn_project').setValue([{{
                        id: ""{{{project.Id}}}"",
                        type: ""{project.Type}"",
                        name: ""{project.Name}""
                    }}]);
                ");
        }

        private Project? DeterminePsaProject(TimeEntry entry)
        {
            return entry.Project.StartsWith("Home Intelligence") ? Project.HomeIntelligence 
                : entry.Project.StartsWith("MyWay Digital Experience") ? Project.MyWay
                : (Project?)null;
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
