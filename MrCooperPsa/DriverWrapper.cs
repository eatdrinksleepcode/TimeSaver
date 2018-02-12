using System;
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

        protected T WaitUntil<T>(TimeSpan timeout, Func<T> until) {
            return new DefaultWait<TDriver>(Driver) {
                Timeout = timeout
            }.Until(d => until());
        }
    }
}
