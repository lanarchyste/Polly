using System;
using System.Threading;

namespace Polly.Utilities
{
    // Adapted from the link below, with slight modifications.

    // http://www.interact-sw.co.uk/iangblog/2004/04/26/yetmoretimedlocking
    // Ian Griffiths (original TimedLock author) wrote:
    // Thanks to Eric Gunnerson for recommending this be a struct rather
    // than a class - avoids a heap allocation.
    // Thanks to Change Gillespie and Jocelyn Coulmance for pointing out
    // the bugs that then crept in when I changed it to use struct...
    // Thanks to John Sands for providing the necessary incentive to make
    // me invent a way of using a struct in both release and debug builds
    // without losing the debug leak tracking.
    internal struct TimedLock : IDisposable
    {
        // The TimedLock class throws a LockTimeoutException if a lock cannot be obtained within the LockTimeout.  This allows the easier discovery and debugging of deadlocks during Polly development, than if using a pure lock.
        // We do not however ever want to throw a LockTimeoutException in production - hence the forked LockTimeout value below for DEBUG versus RELEASE builds.  
        // This applies particularly because CircuitBreakerPolicy runs state-change delegates during the lock, in order that the state change holds true (cannot be superseded by activity on other threads) while the delegate runs.  
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        public static TimedLock Lock(object o)
        {
            return Lock(o, LockTimeout);
        }

        private static TimedLock Lock(object o, TimeSpan timeout)
        {
            TimedLock tl = new TimedLock(o);
            if (!Monitor.TryEnter(o, timeout))
            {
                throw new LockTimeoutException();
            }

            return tl;
        }

        private TimedLock(object o)
        {
            target = o;
        }
        private object target;

        public void Dispose()
        {
            Monitor.Exit(target);

            // It's a bad error if someone forgets to call Dispose,
            // so in Debug builds, we put a finalizer in to detect
            // the error. If Dispose is called, we suppress the
            // finalizer.
        }
    }

    internal class LockTimeoutException : Exception
    {
        public LockTimeoutException() : base("Timeout waiting for lock")
        {
        }
    }
}