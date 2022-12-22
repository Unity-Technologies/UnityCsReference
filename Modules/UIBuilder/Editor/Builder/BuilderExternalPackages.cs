// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    static class BuilderExternalPackages
    {
        public static bool is2DSpriteEditorInstalled
        {
            get
            {
                return PackageInfo.GetAllRegisteredPackages().Any(x => x.name == "com.unity.2d.sprite" && x.version == "1.0.0");
            }
        }

        public static void Open2DSpriteEditor(Object value)
        {
            SpriteUtilityWindow.ShowSpriteEditorWindow(value);
        }
    }
}
