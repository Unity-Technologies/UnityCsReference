// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor
{
    // ColorClipboard supports colors copied to the system copy buffer (as a hex string) and as a Color value to the Pasteboard (in c++)
    static internal class ColorClipboard
    {
        public static void SetColor(Color color)
        {
            EditorGUIUtility.systemCopyBuffer = "";
            EditorGUIUtility.SetPasteboardColor(color);
        }

        public static bool HasColor()
        {
            Color dummy;
            return TryGetColor(false, out dummy);
        }

        public static bool TryGetColor(bool allowHDR, out Color color)
        {
            bool validColor = false;
            if (ColorUtility.TryParseHtmlString(EditorGUIUtility.systemCopyBuffer, out color))
            {
                validColor = true;
            }
            else if (EditorGUIUtility.HasPasteboardColor())
            {
                color = EditorGUIUtility.GetPasteboardColor();
                validColor = true;
            }

            if (validColor)
            {
                // Ensure HDR colors are normalized for LDR color fields
                if (!allowHDR && color.maxColorComponent > 1f)
                    color = color.RGBMultiplied(1f / color.maxColorComponent);
                return true;
            }

            return false;
        }
    }
} // namespace
