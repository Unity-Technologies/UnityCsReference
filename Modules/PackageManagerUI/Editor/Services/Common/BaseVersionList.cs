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
        public virtual IPackageVersion installed => null;

        public abstract IPackageVersion latest { get; }

        public virtual IPackageVersion importAvailable => null;

        public virtual IPackageVersion imported => null;

        public virtual IPackageVersion recommended => null;

        public virtual IPackageVersion suggestedUpdate => null;

        public abstract IPackageVersion primary { get; }

        public virtual int numUnloadedVersions => 0;

        public abstract IEnumerator<IPackageVersion> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
