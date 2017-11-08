// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine;

namespace UnityEditor
{
    public partial class EditorGUIUtility : GUIUtility
    {
        internal static GUIContent TrTextContent(string text, string tooltip = null, Texture icon = null)
        {
            string text_k = text != null ? text : "";
            string tooltip_k = tooltip != null ? tooltip : "";
            string key = string.Format("{0}|{1}", text_k, tooltip_k);

            GUIContent gc = (GUIContent)s_GUIContents[key];
            if (gc == null)
            {
                gc = new GUIContent(L10n.Tr(text));
                if (tooltip != null)
                {
                    gc.tooltip = L10n.Tr(tooltip);
                }
                if (icon != null)
                {
                    gc.image = icon;
                }
                s_GUIContents[key] = gc;
            }
            return gc;
        }

        internal static GUIContent TrTextContent(string text, string tooltip, string iconName)
        {
            string text_k = text != null ? text : "";
            string tooltip_k = tooltip != null ? tooltip : "";
            string key = string.Format("{0}|{1}", text_k, tooltip_k);

            GUIContent gc = (GUIContent)s_GUIContents[key];
            if (gc == null)
            {
                gc = new GUIContent(L10n.Tr(text));
                if (tooltip != null)
                {
                    gc.tooltip = L10n.Tr(tooltip);
                }
                if (iconName != null)
                {
                    Texture icon = LoadIconRequired(iconName);
                    gc.image = icon;
                }
                s_GUIContents[key] = gc;
            }
            return gc;
        }

        internal static GUIContent TrTextContent(string text, Texture icon)
        {
            return TrTextContent(text, null, icon);
        }

        internal static GUIContent TrTextContentWithIcon(string text, Texture icon)
        {
            return TrTextContent(text, null, icon);
        }

        internal static GUIContent TrTextContentWithIcon(string text, string iconName)
        {
            return TrTextContent(text, null, iconName);
        }

        internal static GUIContent TrTextContentWithIcon(string text, string tooltip, string iconName)
        {
            return TrTextContent(text, tooltip, iconName);
        }

        internal static GUIContent TrTextContentWithIcon(string text, string tooltip, Texture icon)
        {
            return TrTextContent(text, tooltip, icon);
        }

        internal static GUIContent TrTextContentWithIcon(string text, string tooltip, MessageType messageType)
        {
            return TrTextContent(text, tooltip, GetHelpIcon(messageType));
        }

        internal static GUIContent TrTextContentWithIcon(string text, MessageType messageType)
        {
            return TrTextContentWithIcon(text, null, messageType);
        }

        internal static GUIContent TrIconContent(string name, string tooltip = null)
        {
            GUIContent gc = (GUIContent)s_IconGUIContents[name];
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent();

            if (tooltip != null)
            {
                gc.tooltip = L10n.Tr(tooltip);
            }
            gc.image = LoadIconRequired(name);
            s_IconGUIContents[name] = gc;
            return gc;
        }

        internal static GUIContent TrIconContent(Texture icon, string tooltip = null)
        {
            GUIContent gc = (GUIContent)s_IconGUIContents[tooltip];
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent();

            if (tooltip != null)
            {
                gc.tooltip = L10n.Tr(tooltip);
            }
            gc.image = icon;
            s_IconGUIContents[tooltip] = gc;
            return gc;
        }

        internal static GUIContent TrTempContent(string t)
        {
            return TempContent(L10n.Tr(t));
        }

        internal static GUIContent[] TrTempContent(string[] texts)
        {
            GUIContent[] retval = new GUIContent[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                retval[i] = new GUIContent(L10n.Tr(texts[i]));
            return retval;
        }
    }
}
