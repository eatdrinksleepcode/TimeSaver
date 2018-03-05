using System;
using System.Drawing;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MrCooperPsa {
    public class DriverWrapper<TDriver> : IDisposable where TDriver : IWebDriver, IJavaScriptExecutor {

        public DriverWrapper(TDriver driver) {
            this.Driver = driver;
            this.Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromDays(1);
        }

        public void Dispose() {
            if (null != Driver) {
                Driver.Dispose();
            }
        }

        public TDriver Driver { get; }

        public void SetScreenSize(Point position, Size size) {
            var window = Driver.Manage().Window;
            window.Position = position;
            window.Size = size;
        }

        protected T WaitUntil<T>(TimeSpan timeout, Func<T> until, CancellationToken cancellation = default(CancellationToken)) {
            return new DefaultWait<TDriver>(Driver) {
                Timeout = timeout
            }.Until(d => {
                var result = until();
                cancellation.ThrowIfCancellationRequested();
                return result;
            });
        }
    }
}
