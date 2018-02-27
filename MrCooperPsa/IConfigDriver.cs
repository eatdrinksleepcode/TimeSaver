using System.Threading;

namespace MrCooperPsa {
    public interface IConfigDriver : IDriverWrapper {
        void NavigateToConfigPage();
        System.Threading.Tasks.Task WaitForSave(CancellationToken cancelToken = default(CancellationToken));
    }
}