using System;
using System.Collections.Generic;

namespace MrCooperPsa
{
    public interface ITimeworksDriver : IDriverWrapper {
        void NavigateToTimeworks();
        void SignInToTimeworks();
        void AddExportElementToPage();
        IEnumerable<TimeEntry> WaitForExportedEntries();
    }
}