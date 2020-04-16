using System.Threading;

namespace Player.Services
{
    public static class SynchronizationContext
    {
        public static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(0, 1);
    }
}
