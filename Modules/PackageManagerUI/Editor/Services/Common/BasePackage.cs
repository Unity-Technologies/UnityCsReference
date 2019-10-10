// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal abstract class BasePackage : IPackage
    {
        [SerializeField]
        protected string m_Name;
        public string name => m_Name;

        public string displayName => versions.FirstOrDefault()?.displayName ?? string.Empty;
        public PackageState state
        {
            get
            {
                if (progress != PackageProgress.None)
                    return PackageState.InProgress;

                if (errors.Any())
                    return PackageState.Error;

                var primary = versions.primary;
                if (primary.HasTag(PackageTag.InDevelopment))
                    return PackageState.InDevelopment;

                if (primary.isInstalled && !primary.isDirectDependency)
                    return PackageState.InstalledAsDependency;

                if (primary != versions.recommended)
                    return PackageState.UpdateAvailable;

                if (versions.importAvailable != null)
                    return PackageState.ImportAvailable;

                if (versions.installed != null)
                    return PackageState.Installed;

                return PackageState.None;
            }
        }

        [SerializeField]
        protected PackageProgress m_Progress;
        public PackageProgress progress
        {
            get { return m_Progress; }
            set { m_Progress = value; }
        }

        // errors on the package level (not just about a particular version)
        [SerializeField]
        protected List<Error> m_Errors;

        // Combined errors for this package or any version.
        // Stop lookup after first error encountered on a version to save time not looking up redundant errors.
        public IEnumerable<Error> errors
        {
            get
            {
                var versionErrors = versions.Select(v => v.errors).FirstOrDefault(e => e?.Any() ?? false) ?? Enumerable.Empty<Error>();
                return versionErrors.Concat(m_Errors ?? Enumerable.Empty<Error>());
            }
        }

        public void AddError(Error error)
        {
            m_Errors.Add(error);
        }

        public void ClearErrors()
        {
            m_Errors.Clear();
        }

        [SerializeField]
        protected PackageType m_Type;
        public bool Is(PackageType type)
        {
            return (m_Type & type) != 0;
        }

        public virtual bool isDiscoverable => true;

        public virtual IEnumerable<PackageImage> images => Enumerable.Empty<PackageImage>();
        public virtual IEnumerable<PackageLink> links => Enumerable.Empty<PackageLink>();

        public abstract string uniqueId { get; }
        public abstract IVersionList versions { get; }

        public abstract IPackage Clone();
    }
}
