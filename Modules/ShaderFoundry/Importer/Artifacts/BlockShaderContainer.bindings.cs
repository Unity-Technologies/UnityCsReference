// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    namespace ShaderFoundry
    {
        [NativeHeader("Modules/ShaderFoundry/Importer/Artifacts/BlockShaderContainer.h")]
        [NativeClass("ShaderFoundry::BlockShaderContainer")]
        internal sealed partial class BlockShaderContainer : Object
        {
            internal extern ShaderContainer GetContainer();
            internal extern BlockShaderContainer[] GetDependencies();

            internal IEnumerable<BlockShaderErrors.Error> GetErrors()
            {
                return GetErrorsObject()?.Errors ?? new List<BlockShaderErrors.Error>();
            }

            private BlockShaderErrors GetErrorsObject()
            {
                string assetPath = AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(assetPath))
                    return null;

                // We have to use LoadAllAssetsAtPath since the errors asset isn't visible in the project hierarchy
                foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (obj is BlockShaderErrors errorsObject)
                        return errorsObject;
                }
                return null;
            }
        }
    }
}
