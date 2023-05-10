// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Structure containing information about individual links contained in the text object.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal struct LinkInfo
    {
        public int hashCode;

        public int linkIdFirstCharacterIndex;
        public int linkIdLength;
        public int linkTextfirstCharacterIndex;
        public int linkTextLength;

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal char[] linkId;

        string m_LinkIdString;
        string m_LinkTextString;

        internal void SetLinkId(char[] text, int startIndex, int length)
        {
            if (linkId == null || linkId.Length < length) linkId = new char[length];

            for (int i = 0; i < length; i++)
                linkId[i] = text[startIndex + i];

            linkIdLength = length;
            m_LinkIdString = null;
            m_LinkTextString = null;
        }

        /// <summary>
        /// Function which returns the text contained in a link.
        /// </summary>
        /// <returns></returns>
        public string GetLinkText(TextInfo textInfo)
        {
            if (string.IsNullOrEmpty(m_LinkTextString))
                for (int i = linkTextfirstCharacterIndex; i < linkTextfirstCharacterIndex + linkTextLength; i++)
                    m_LinkTextString += textInfo.textElementInfo[i].character;

            return m_LinkTextString;
        }

        /// <summary>
        /// Function which returns the link ID as a string.
        /// </summary>
        /// <returns></returns>
        public string GetLinkId()
        {
            if (string.IsNullOrEmpty(m_LinkIdString))
                m_LinkIdString = new string(linkId, 0, linkIdLength);

            return m_LinkIdString;
        }
    }
}
