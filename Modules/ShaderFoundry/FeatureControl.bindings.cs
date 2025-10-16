// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/FeatureControl.h")]
    internal static class FeatureControl
    {
        [FreeFunction("ShaderFoundry::FeatureControl::IsFoundryEnabled")]
        public extern static bool IsFoundryEnabled();

        [FreeFunction("ShaderFoundry::FeatureControl::SetFoundryEnabled")]
        public extern static void SetFoundryEnabled(bool value);

        // A convenience function exposed for use by managed test projects such that they can easily enable and disable
        // the feature.
        public static void SetEnableFoundryForTests(bool value)
        {
            EditorSettings.unlockBlockShaders = value;
            EditorSettings.blockShaders = value;
        }
    }
} // namespace ShaderFoundry::FeatureControl
