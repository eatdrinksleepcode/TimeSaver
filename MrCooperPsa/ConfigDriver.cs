using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;

namespace MrCooperPsa {
    public class ConfigDriver<T> : DriverWrapper<T>, IConfigDriver where T : IWebDriver, IJavaScriptExecutor {
        private readonly ConfigRepository configRepo;

        public ConfigDriver(T driver, ConfigRepository configRepo) : base(driver) {
            this.configRepo = configRepo;
        }

        public void NavigateToConfigPage() {
            var configFile = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "Config.html");
            Driver.Navigate().GoToUrl($"file://{configFile}");
            Driver.FindElement(By.TagName("body"));
            SetConfigOnPage();
        }

        private void SetConfigOnPage() {
            var config = configRepo.LoadConfig();
            Driver.ExecuteJavaScript($"setConfig({JsonConvert.SerializeObject(config)})");
        }

        public System.Threading.Tasks.Task WaitForSave(CancellationToken cancelled = default(CancellationToken)) {
            return System.Threading.Tasks.Task.Run(() => {
                    while (!cancelled.IsCancellationRequested) {
                        WaitUntil(TimeSpan.FromDays(1), () => (bool) Driver.ExecuteScript(@"
                            return document.readyToSave || false;
                        "), cancelled);

                        if (!cancelled.IsCancellationRequested) {
                            Console.WriteLine("Saving config");
                            var oldConfig = configRepo.LoadConfig();
                            var configFromBrowser = Driver.ExecuteJavaScript<IDictionary<string, object>>("return getConfig()");
                            var newConfig = new Config {
                                Browser = (string)configFromBrowser["browser"],
                                Role = (string)configFromBrowser["role"],
                            };
                            configRepo.SaveConfig(newConfig);
                            Driver.ExecuteJavaScript("saveComplete()");
                            if (oldConfig.Browser != newConfig.Browser) {
                                return;
                            }
                        }
                    }
                }, cancelled
            );
        }
    }
}
