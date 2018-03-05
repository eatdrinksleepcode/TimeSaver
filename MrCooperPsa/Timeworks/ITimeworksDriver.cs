using System.Collections.Generic;
using System.Threading;

namespace MrCooperPsa.Timeworks
{
    public interface ITimeworksDriver : IDriverWrapper {
        System.Threading.Tasks.Task NavigateToTimeworks(CancellationToken cancellation);
        System.Threading.Tasks.Task SignInToTimeworks(CancellationToken cancellation);
        System.Threading.Tasks.Task AddExportElementToPage(CancellationToken cancellation);
        IEnumerable<TimeEntry> WaitForExportedEntries(CancellationToken cancellation);
    }
}