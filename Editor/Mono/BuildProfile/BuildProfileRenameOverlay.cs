// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Handles displaying tooltip view for renaming build profile
    /// items with text field
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal class BuildProfileRenameOverlay
    {
        static readonly string k_InvalidChars = BuildProfileModuleUtil.GetFilenameInvalidCharactersStr();
        static readonly string k_ErrorMessage = string.Format(L10n.Tr("A file name can't contain any of the following characters:\t{0}"), k_InvalidChars);

        TextField m_TextField;
        Rect? m_ErrorRect = null;

        Rect errorRect
        {
            get
            {
                if (m_ErrorRect == null)
                    m_ErrorRect = GUIUtility.GUIToScreenRect(m_TextField.worldBound);

                return m_ErrorRect.Value;
            }
        }

        public BuildProfileRenameOverlay(TextField textField)
        {
            m_TextField = textField;
        }

        public void OnNameChanged(string previousValue, string newValue)
        {
            // It's fine to call show and close tooltip on multiple frames
            // since it only opens if not opened before. Also it only closes if
            // not closed before
            if (HasInvalidCharacterIndex(newValue))
            {
                // We can't use the text field's tooltip property since it's displayed
                // automatically on hover. So we use the TooltipView to display the
                // error message (same way as the RenameOverlay used for assets)
                TooltipView.Show(k_ErrorMessage, errorRect);
                m_TextField.SetValueWithoutNotify(previousValue);

                // The cursor should be kept in place when adding an invalid character
                var targetIndex = Mathf.Max(m_TextField.cursorIndex - 1, 0);
                m_TextField.cursorIndex = targetIndex;
                m_TextField.selectIndex = targetIndex;
            }
            else
            {
                TooltipView.ForceClose();
            }
        }

        public void OnRenameEnd()
        {
            // Make sure tooltip is closed
            TooltipView.ForceClose();
        }

        bool HasInvalidCharacterIndex(string value)
        {
            return value.IndexOfAny(k_InvalidChars.ToCharArray()) >= 0;
        }
    }
}
