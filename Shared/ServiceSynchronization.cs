using System.Threading;

namespace Shared
{
    public class ServiceSynchronization
    {
        public readonly SemaphoreSlim SemaphoreSlim;

        public ServiceSynchronization(int init, int max)
        {
            SemaphoreSlim = new SemaphoreSlim(init, max);
        }
    }
}
