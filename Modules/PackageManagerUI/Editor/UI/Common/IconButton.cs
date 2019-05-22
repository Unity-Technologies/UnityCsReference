// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    class IconButton : Button
    {
        public IconButton(string iconPath, int iconHeight = 12)
        {
            int paddingSize = 4;
            int verticalMargin = 3;

            var icon = new VisualElement();
            icon.name = "icon";

            var iconTexture = (Texture2D)EditorGUIUtility.LoadRequired(iconPath);
            icon.style.backgroundImage = new StyleBackground(iconTexture);

            var iconTextureRatio = iconHeight;
            if (iconTexture.width != 0)
                iconTextureRatio = iconTexture.width / iconTexture.height;

            icon.style.minHeight = iconHeight;
            icon.style.minWidth = iconHeight * iconTextureRatio;

            style.paddingTop = paddingSize;
            style.paddingBottom = paddingSize;
            style.paddingLeft = paddingSize;
            style.paddingRight = paddingSize;
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;
            style.marginTop = verticalMargin;
            style.marginBottom = verticalMargin;

            Add(icon);
        }
    }
}
