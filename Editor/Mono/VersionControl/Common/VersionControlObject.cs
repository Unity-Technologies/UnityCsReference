// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.VersionControl
{
    public abstract class VersionControlObject : ScriptableObject
    {
        public virtual bool isConnected => true;

        public virtual void OnActivate()
        {
        }

        public virtual void OnDeactivate()
        {
        }

        public virtual void Refresh()
        {
        }

        public virtual T GetExtension<T>() where T : class
        {
            return this as T;
        }
    }
}
