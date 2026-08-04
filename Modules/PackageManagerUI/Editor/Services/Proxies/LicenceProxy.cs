// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics.CodeAnalysis;
using UnityEditor.Experimental.Licensing;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ILicenceProxy : IService
    {
        bool UpdateLicense();
    }

    [ExcludeFromCodeCoverage]
    internal class LicenceProxy : BaseService<ILicenceProxy>, ILicenceProxy
    {
        public bool UpdateLicense()
        {
            var updated = LicensingUtility.UpdateLicense();
            if (!updated)
                Debug.LogError(L10n.Tr("[Package Manager Window] Failed to update licenses."));

            return updated;
        }
    }
}
