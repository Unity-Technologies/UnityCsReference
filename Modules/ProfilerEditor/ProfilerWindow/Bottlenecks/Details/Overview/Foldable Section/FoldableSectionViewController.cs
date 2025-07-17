// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // A container view controller that embeds a number of child view controllers in a horizontal foldable section.
    abstract class FoldableSectionViewController : ViewController
    {
        // Model.
        readonly string m_Title;

        // View.
        Foldout m_TitleFoldout;
        VisualElement m_ContentContainer;

        protected FoldableSectionViewController(string title)
        {
            m_Title = title;
        }

        protected abstract int NumberOfSections();

        protected abstract ViewController ViewControllerForSection(int section);

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("FoldableSectionView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "foldable-section-view__dark";
            const string k_UssClass_Light = "foldable-section-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_TitleFoldout.text = m_Title;

            // Build sections.
            var numberOfSections = NumberOfSections();
            for (var i = 0; i < numberOfSections; ++i)
            {
                var viewController = ViewControllerForSection(i);
                var view = viewController.View;
                m_ContentContainer.Add(view);
                AddChild(viewController);

                const float k_SpacingPercentage = 2f;
                view.style.maxWidth = new StyleLength(new Length(((1f / numberOfSections) * 100f) - k_SpacingPercentage, LengthUnit.Percent));
            }
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleFoldout = view.Q<Foldout>("foldable-section-view__title-foldout");
            m_ContentContainer = view.Q<VisualElement>("foldable-section-view__content-container");
        }
    }
}
