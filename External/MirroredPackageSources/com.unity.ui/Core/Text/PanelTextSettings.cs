using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
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
