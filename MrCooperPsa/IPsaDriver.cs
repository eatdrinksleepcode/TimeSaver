using System;
using System.Collections.Generic;

namespace MrCooperPsa
{
    public interface IPsaDriver : IDriverWrapper {
        void NavigateToDynamicsTimeEntries();
        void ExportEntriesToPSA(IEnumerable<TimeEntry> entries);
    }
}