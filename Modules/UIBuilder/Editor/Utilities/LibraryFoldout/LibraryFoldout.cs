// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class LibraryFoldout : PersistedFoldout
    {
        [Serializable]
        public new class UxmlSerializedData : PersistedFoldout.UxmlSerializedData
        {
            public override object CreateInstance() => new LibraryFoldout();
        }

        public const string TagLabelName = "tag";
        const string k_TagPillClassName = "builder-library-foldout__tag-pill";

        Label m_Tag;

        public LibraryFoldout()
        {
            // Load styles.
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UtilitiesPath + "/LibraryFoldout/LibraryFoldout.uss"));
            styleSheets.Add(EditorGUIUtility.isProSkin
                ? BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UtilitiesPath + "/LibraryFoldout/LibraryFoldoutDark.uss")
                : BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UtilitiesPath + "/LibraryFoldout/LibraryFoldoutLight.uss"));
        }

        // Should be defined after Toggle text.
        // Known Issue: If label text will be defined before Toggle text, it could be placed before Toggle default label.
        public string tag
        {
            get => m_Tag?.text;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Lazy allocation of label if needed...
                    if (m_Tag == null)
                    {
                        m_Tag = new Label
                        {
                            name = TagLabelName,
                            pickingMode = PickingMode.Ignore
                        };
                        m_Tag.AddToClassList(k_TagPillClassName);
                        toggle.visualInput.Add(m_Tag);
                    }

                    m_Tag.text = value;
                }
                else if (m_Tag != null)
                {
                    Remove(m_Tag);
                    m_Tag = null;
                }
            }
        }
    }
}
