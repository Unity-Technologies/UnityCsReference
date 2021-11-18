// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Connect;

namespace UnityEditor
{
    /// <summary>
    /// The enumerated states of the project's COPPA compliance setting, where 1 indicates compliance.
    /// </summary>
    public enum CoppaCompliance
    {
        /// <summary>
        /// The project has not yet defined the COPPA state.
        /// </summary>
        CoppaUndefined = COPPACompliance.COPPAUndefined,

        /// <summary>
        /// The project will adhere to COPPA regulations because it targets an audience under a certain age.
        /// </summary>
        CoppaCompliant = COPPACompliance.COPPACompliant,

        /// <summary>
        /// The project will not adhere to COPPA regulations because it does not target an audience under a specific age.
        /// </summary>
        CoppaNotCompliant = COPPACompliance.COPPANotCompliant,
    }

    static class CoppaComplianceExtensions
    {
        internal static COPPACompliance ToCOPPACompliance(this CoppaCompliance coppaCompliance)
        {
            return (COPPACompliance)coppaCompliance;
        }
    }
}
