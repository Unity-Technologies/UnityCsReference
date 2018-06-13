// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    public partial class ShaderKeyword
    {
        [Obsolete("GetName() has been deprecated. Use GetKeywordName() instead (UnityUpgradable) -> GetKeywordName()")]
        public string GetName()
        {
            return GetKeywordName();
        }
    }
}
