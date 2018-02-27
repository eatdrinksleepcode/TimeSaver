using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;

namespace MrCooperPsa {
    public class ConfigDriver<T> : DriverWrapper<T>, IConfigDriver where T : IWebDriver, IJavaScriptExecutor {

        private Config config;

        public ConfigDriver(T driver) : base(driver) {
            LoadConfig();
        }

        private void LoadConfig() {
            var configFile = GetConfigFilePath();
            if (File.Exists(configFile)) {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFile));
                Console.WriteLine($"Config file loaded from {GetConfigFilePath()}");
            }
        }

        private void SaveConfig() {
            var configFilePath = GetConfigFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config));
            Console.WriteLine($"Config file saved to {configFilePath}");
        }

        private static string GetConfigFilePath() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimeSaver", "config");
        }

        public void NavigateToConfigPage() {
            var configFile = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "Config.html");
            Driver.Navigate().GoToUrl($"file://{configFile}");
            Driver.FindElement(By.TagName("body"));
            SetConfigOnPage();
        }

        private void SetConfigOnPage() {
            Driver.ExecuteJavaScript($"setConfig({JsonConvert.SerializeObject(config)})");
        }

        public System.Threading.Tasks.Task WaitForSave(CancellationToken cancelled = default(CancellationToken)) {
            return System.Threading.Tasks.Task.Run(() => {
                    while (!cancelled.IsCancellationRequested) {
                        WaitUntil(TimeSpan.FromDays(1), () => (bool) Driver.ExecuteScript(@"
                            return document.readyToSave || false;
                        ") || cancelled.IsCancellationRequested);

                        if (!cancelled.IsCancellationRequested) {
                            Console.WriteLine("Saving config");
                            var newConfig = Driver.ExecuteJavaScript<IDictionary<string, object>>("return getConfig()");
                            config = new Config {
                                Browser = (string)newConfig["browser"],
                            };
                            SaveConfig();
                            Driver.ExecuteJavaScript("saveComplete()");
                        }
                    }
                }, cancelled
            );
        }

        struct Config {
            public string Browser { get; set; }
        }
    }
}
