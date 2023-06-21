// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class BaseVersionList : IVersionList
    {
        public virtual IEnumerable<IPackageVersion> key => this;

        public virtual IPackageVersion installed => null;

        public abstract IPackageVersion latest { get; }

        public virtual IPackageVersion importAvailable => null;

        public virtual IPackageVersion imported => null;

        public abstract IPackageVersion recommended { get; }

        public abstract IPackageVersion primary { get; }

        public virtual IPackageVersion lifecycleVersion => null;

        public virtual bool isNonLifecycleVersionInstalled => false;

        public virtual bool hasLifecycleVersion => false;

        public virtual int numUnloadedVersions => 0;

        public virtual IPackageVersion GetUpdateTarget(IPackageVersion version) => recommended;

        public abstract IEnumerator<IPackageVersion> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
