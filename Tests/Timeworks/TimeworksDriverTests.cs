using System;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace MrCooperPsa.Timeworks {
    public class TimeworksDriverTests {
        [Fact]
        public void StartDateDst() {
            var startDate = TimeworksDriver<ChromeDriver>.FindStartDate("29 Oct");

            Assert.Equal(new DateTimeOffset(2018, 10, 29, 0, 0, 0, TimeSpan.Zero), startDate);
        }
    }
}