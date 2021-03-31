using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents text rendering settings for a specific UI panel.
    /// <seealso cref="PanelSettings.textSettings"/>
    /// </summary>
    public class PanelTextSettings : TextSettings
    {
        private static PanelTextSettings s_DefaultPanelTextSettings;

        internal static PanelTextSettings defaultPanelTextSettings
        {
            get
            {
                if (s_DefaultPanelTextSettings == null)
                    s_DefaultPanelTextSettings = GetDefaultPanelTextSettings?.Invoke();
                if (s_DefaultPanelTextSettings == null)
                    s_DefaultPanelTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                return s_DefaultPanelTextSettings;
            }
        }

        internal FontAsset GetCachedFontAsset(Font font)
        {
            return GetCachedFontAssetInternal(font);
        }

        internal static Func<PanelTextSettings> GetDefaultPanelTextSettings;
    }
}
