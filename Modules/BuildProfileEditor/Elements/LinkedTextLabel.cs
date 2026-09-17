// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Application = UnityEngine.Device.Application;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Label whose text carries inline links, for copy that reads as a sentence rather than a row of
    /// buttons. Compose the text with <see cref="Link"/> and assign it to <see cref="TextElement.text"/>.
    /// </summary>
    internal class LinkedTextLabel : Label
    {
        // USS cannot style link tags, so the link colour is embedded in the text.
        static string linkColor => EditorGUIUtility.isProSkin ? "#4f80f8" : "#0808fc";

        readonly List<string> m_Urls = new List<string>();

        /// <summary>
        /// Link targets in the order <see cref="Link"/> registered them.
        /// </summary>
        internal IReadOnlyList<string> urls => m_Urls;

        internal LinkedTextLabel()
        {
            AddToClassList("wrap");
            RegisterCallback<PointerUpLinkTagEvent>(evt =>
            {
                if (int.TryParse(evt.linkID, out var index) && index >= 0 && index < m_Urls.Count)
                    Application.OpenURL(m_Urls[index]);
            });
        }

        /// <summary>
        /// Returns <paramref name="displayText"/> marked up as a link to <paramref name="url"/>, and
        /// registers the URL so a click on it opens the browser.
        /// </summary>
        internal string Link(string displayText, string url)
        {
            m_Urls.Add(url);
            return $"<link=\"{m_Urls.Count - 1}\"><color={linkColor}>{displayText}</color></link>";
        }

        internal void ClearLinks()
        {
            m_Urls.Clear();
            text = string.Empty;
        }
    }
}
