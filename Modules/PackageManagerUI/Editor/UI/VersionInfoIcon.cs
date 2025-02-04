// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class VersionInfoIcon : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new VersionInfoIcon();
        }

        private IApplicationProxy m_Application;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<IApplicationProxy>();
        }

        public VersionInfoIcon()
        {
            ResolveDependencies();
        }

        public void Refresh(IPackage package)
        {
            UIUtils.SetElementDisplay(this, false);
            var version = package?.versions?.primary;
            if (version is not { isInstalled: true })
                return;

            if (version.IsDifferentVersionThanRequested && !version.isInvalidSemVerInManifest)
            {
                UIUtils.SetElementDisplay(this, true);
                tooltip = string.Format(
                    L10n.Tr("Unity installed version {0} instead of the requested version {1} because other packages or features might depend on it, or because of an editor's requirements."),
                    version.versionString, version.versionInManifest);
                return;
            }

            if (!version.HasTag(PackageTag.Unity) || version.hasEntitlements)
                return;

            var recommended = package.versions.recommended;
            // If a Unity package doesn't have a recommended version (decided by versions set in the editor manifest or remote manifest override),
            // then that package is not considered part of the Unity Editor "product" and we need to let users know.
            if (!version.HasTag(PackageTag.BuiltIn) && recommended == null)
            {
                UIUtils.SetElementDisplay(this, true);
                tooltip = string.Format(L10n.Tr("This package is not officially supported for Unity {0}."), m_Application.unityVersion);
                return;
            }

            // We want to let users know when they are using a version different than the recommended.
            // However, we don't want to show the info icon if the version currently installed
            // is a higher patch version of the one in the editor manifest (still considered recommended).
            if (package.state != PackageState.InstalledAsDependency
                && recommended != null
                && version.version?.IsEqualOrPatchOf(recommended.version) != true)
            {
                UIUtils.SetElementDisplay(this, true);
                tooltip = string.Format(
                    L10n.Tr("This version is not the recommended for Unity {0}. The recommended version is {1}."),
                    m_Application.unityVersion, recommended.versionString);
            }
        }
    }
}
