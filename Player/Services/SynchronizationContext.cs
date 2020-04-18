using System.Threading;

namespace Player.Services
{
    public class SynchronizationContext
    {
        public SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(0, 1);
    }
}
