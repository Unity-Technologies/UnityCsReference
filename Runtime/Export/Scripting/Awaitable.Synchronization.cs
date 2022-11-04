// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;

namespace UnityEngine
{
    public partial class Awaitable
    {
        private struct ManagedLockWithSingleThreadBias
        {
            // Some scenarios require that we are threadsafe, while it is
            // very highly likely that all interactions will come from the same thread.
            // That is likely to be the case for awaitable coroutines where all current constructs are running
            // their continuations on the main thread (and are likely started from the main thread as well)

            // This custom lock is specifically tailoired towards that, and takes a lot of shortcuts that need to be understood (this is why it is internal):
            // - It is not reentrant
            // - Calling Release while not previously having taken the lock is undefined behaviour
            // - The whole implementation is made to be:
            //   - A valid lock in all single or multi-threaded scenarios
            //   - As fast as possible when all calls are made within the same thread
            volatile int _taken;
            public void Acquire()
            {
                SpinWait w = default;
                while (Interlocked.CompareExchange(ref _taken, 1, 0) != 0)
                {
                    w.SpinOnce();
                }
            }

            public void Release()
            {
                _taken = 0; // no barrier here, as we are super-optimistic in this implementation
                // most of the time this lock is used in a MainThread-only context
            }
        }
    }
}
