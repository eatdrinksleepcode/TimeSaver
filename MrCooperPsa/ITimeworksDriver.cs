using System;
using System.Collections.Generic;
using System.Threading;

namespace MrCooperPsa
{
    public interface ITimeworksDriver : IDriverWrapper {
        void NavigateToTimeworks();
        void SignInToTimeworks();
        void AddExportElementToPage();
        IEnumerable<TimeEntry> WaitForExportedEntries(CancellationToken cancellation);
    }
}