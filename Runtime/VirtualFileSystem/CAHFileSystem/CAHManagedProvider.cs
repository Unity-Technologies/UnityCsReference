// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    [VisibleToOtherModules("UnityEngine.ContentLoadModule", "ContentBuildLoadPreview")]
    internal interface ICAHArtifactHandler
    {
        bool Exists(Hash128 hash);
        bool Open(Hash128 hash, out IManagedVFSFileHandler handler, out int handle);
    }

    [RequiredByNativeCode]
    internal static partial class CAHManagedRouter
    {
        // Cleared on reload by s_CodeReloadCleanup, which also resets the native m_HasManagedHandlers
        // flag; field-level cleanup would leave the flag stuck true against an empty list.
        [NoAutoStaticsCleanup] // reset by s_CodeReloadCleanup
        static readonly List<ICAHArtifactHandler> s_Handlers = new List<ICAHArtifactHandler>();
        [NoAutoStaticsCleanup] // synchronization primitive; no user refs, safe to persist across code reload
        static readonly ReaderWriterLockSlim s_Lock = new ReaderWriterLockSlim();

        [NoAutoStaticsCleanup] // persists; LifecycleController owns the per-reload invocation
        static readonly DelegateAutoCleanup s_CodeReloadCleanup = new DelegateAutoCleanup(
            OnCodeReloadCleanup, typeof(CodeLoadedScope), ScopeTransitionType.Exiting, nameof(CAHManagedRouter));

        // Drop handlers registered by the old ALC and tell native there are none left, keeping
        // m_HasManagedHandlers in sync with the emptied list until handlers re-register.
        static void OnCodeReloadCleanup()
        {
            using (new WriteLockScope(s_Lock))
            {
                s_Handlers.Clear();
                CAHFileSystem.SetHasManagedHandlers(false);
            }

            // Outside the write lock: the async read thread takes the open-file cache mutex and then this
            // lock when it opens through a handler, so flushing while holding it would invert that order.
            CAHFileSystem.InvalidateManagedFileHandles();
        }

        internal static void RegisterHandler(ICAHArtifactHandler handler)
        {
            using (new WriteLockScope(s_Lock))
            {
                if (!s_Handlers.Contains(handler))
                    s_Handlers.Add(handler);
                if (s_Handlers.Count == 1)
                    CAHFileSystem.SetHasManagedHandlers(true);
            }
        }

        internal static void UnregisterHandler(ICAHArtifactHandler handler)
        {
            using (new WriteLockScope(s_Lock))
            {
                s_Handlers.Remove(handler);
                if (s_Handlers.Count == 0)
                    CAHFileSystem.SetHasManagedHandlers(false);
            }

            // Unconditional, and outside the write lock for the lock ordering reason above: this handler's
            CAHFileSystem.InvalidateManagedFileHandles();
        }

        [RequiredByNativeCode]
        internal static bool Exists(Hash128 hash)
        {
            using (new ReadLockScope(s_Lock))
            {
                for (int i = 0; i < s_Handlers.Count; i++)
                {
                    try
                    {
                        if (s_Handlers[i].Exists(hash))
                            return true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"CAHManagedRouter.Exists failed for handler {s_Handlers[i].GetType().Name}: {e.Message}");
                    }
                }
            }
            return false;
        }

        [RequiredByNativeCode]
        internal static bool OpenFile(Hash128 hash, int flags, IntPtr outHandle)
        {
            using (new ReadLockScope(s_Lock))
            {
                for (int i = 0; i < s_Handlers.Count; i++)
                {
                    try
                    {
                        if (s_Handlers[i].Open(hash, out IManagedVFSFileHandler handler, out int handle))
                        {
                            InternalManagedFileHandle internalHandle = ManagedVFSRouter.AllocateHandle(handler, handle);
                            unsafe
                            {
                                *(InternalManagedFileHandle*)outHandle = internalHandle;
                            }
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"CAHManagedRouter.OpenFile failed for handler {s_Handlers[i].GetType().Name}: {e.Message}");
                    }
                }
            }
            return false;
        }
    }
}
