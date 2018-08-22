// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/LicensingLegacy/LicenseInfo.h")]
    [StaticAccessor("LicensingLegacy::LicenseInfo::Get()", StaticAccessorType.Arrow)]
    internal partial class LicenseManagementWindow
    {
        [NativeName("QueryLicenseUpdateChecked")]
        public static extern void CheckForUpdates();
        [NativeName("NewActivation")]
        public static extern void ActivateNewLicense();
        public static extern void ManualActivation();
        public static extern void ReturnLicense();
    }
}
