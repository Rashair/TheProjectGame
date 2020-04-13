using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Player.Services
{
    public static class SynchronizationContext
    {
        public static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(0, 1);
    }
}
