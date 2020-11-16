using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class LibraryFoldout : PersistedFoldout
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<LibraryFoldout, UxmlTraits> { }
        public new class UxmlTraits : PersistedFoldout.UxmlTraits { }

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
