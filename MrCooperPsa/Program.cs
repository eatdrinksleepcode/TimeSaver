using System;
using System.IO;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

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
            dynamicsDriver = new ChromeDriver(chromeDriverDir, options);
            dynamicsDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            //timeworksDriver = new ChromeDriver(chromeDriverDir, options);
        }

        private void DoStuff() {
            dynamicsDriver.Navigate().GoToUrl("https://cooper.crm.dynamics.com/main.aspx");
            //timeworksDriver.Navigate().GoToUrl("https://thoughtworks.lightning.force.com/c/TimecardApp.app");

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
                Console.WriteLine("No Mr Cooper email found. Please log in.");
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
