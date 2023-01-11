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
                var packageInfo = PackageInfo.FindForPackageName("com.unity.2d.sprite");
                if (packageInfo != null)
                    return packageInfo.version == "1.0.0";
                return false;
            }
        }

        public static void Open2DSpriteEditor(Object value)
        {
            SpriteUtilityWindow.ShowSpriteEditorWindow(value);
        }
    }
}
