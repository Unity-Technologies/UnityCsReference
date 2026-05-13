// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [VisibleToOtherModules("UnityEngine.ContentLoadModule", "ContentBuildLoadPreview")]
    internal interface ICAHArtifactHandler
    {
        bool Exists(Hash128 hash);
        bool Open(Hash128 hash, out IManagedVFSFileHandler handler, out int handle);
    }

    [RequiredByNativeCode]
    internal static class CAHManagedRouter
    {
        static readonly List<ICAHArtifactHandler> s_Handlers = new List<ICAHArtifactHandler>();
        static readonly ReaderWriterLockSlim s_Lock = new ReaderWriterLockSlim();

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
