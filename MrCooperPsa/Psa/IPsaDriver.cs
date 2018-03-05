using System.Collections.Generic;
using System.Threading;

namespace MrCooperPsa.Psa
{
    public interface IPsaDriver : IDriverWrapper {
        System.Threading.Tasks.Task NavigateToDynamicsTimeEntries(CancellationToken cancellation);
        void ExportEntriesToPSA(IEnumerable<TimeEntry> entries);
    }
}