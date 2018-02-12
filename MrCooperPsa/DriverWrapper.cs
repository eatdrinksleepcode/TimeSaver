using System;
using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MrCooperPsa {
    public class DriverWrapper<TDriver> : IDisposable where TDriver : IWebDriver, IJavaScriptExecutor {

        public DriverWrapper(TDriver driver) {
            this.Driver = driver;
        }

        public void Dispose() {
            if (null != Driver) {
                Driver.Dispose();
            }
        }

        public TDriver Driver { get; }

        public void SetScreenSize(Point position, Size size) {
            Driver.Manage().Window.Position = position;
            Driver.Manage().Window.Size = size;
        }

        protected T WaitUntil<T>(TimeSpan timeout, Func<T> until) {
            return new DefaultWait<TDriver>(Driver) {
                Timeout = timeout
            }.Until(d => until());
        }
    }
}
