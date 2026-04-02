// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageDetailsTabElement : BaseTabElement
    {
        protected virtual bool requiresUserSignIn => false;
        // used to determine if the tab should be shown at all
        public virtual bool IsValid(IPackageVersion version) => true;

        protected readonly VisualElement m_ContentContainer;
        private readonly VisualElement m_SignInDetails;

        private readonly IUnityConnectProxy m_UnityConnect;

        protected PackageDetailsTabElement(IUnityConnectProxy unityConnect)
        {
            m_UnityConnect = unityConnect;

            m_SignInDetails = new SignInDetails(unityConnect);
            Add(m_SignInDetails);

            m_ContentContainer = new VisualElement();
            m_ContentContainer.AddToClassList("detailsTabContentContainer");
            Add(m_ContentContainer);

            AddToClassList("packageDetailsTabElement");
        }

        // We are using the Template Method design pattern.
        // This function is the public entry point. It contains a generic handling for refreshing a tab.
        // The tab specific refresh logic needs to be implement using the abstract RefreshContent function.
        public void Refresh(IPackageVersion version)
        {
            var isSignInDetailsVisible = requiresUserSignIn && !m_UnityConnect.isUserLoggedIn;
            UIUtils.SetElementDisplay(m_SignInDetails, isSignInDetailsVisible);
            UIUtils.SetElementDisplay(m_ContentContainer, !isSignInDetailsVisible);

            if (isSignInDetailsVisible)
                return;

            RefreshContent(version);
        }

        // This is not meant to be called directly. It is used in the Refresh template method.
        // We override this function to implement specific refresh logic for each tab.
        protected abstract void RefreshContent(IPackageVersion version);

        // We are using the Template Method design pattern.
        // For tab specific handling, we need to override the DerivedRefreshHeight function.
        public void RefreshHeight(float detailHeight, float scrollViewHeight, float detailsHeaderHeight,
            float tabViewHeaderContainerHeight, float customContainerHeight, float extensionContainerHeight)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            if (UIUtils.IsElementVisible(m_SignInDetails))
            {
                var headerTotalHeight = detailsHeaderHeight + tabViewHeaderContainerHeight + customContainerHeight + extensionContainerHeight;
                var leftOverHeight = detailHeight - headerTotalHeight - layout.height;
                style.height = scrollViewHeight -  headerTotalHeight - leftOverHeight;
                return;
            }

            DerivedRefreshHeight(detailHeight, scrollViewHeight, detailsHeaderHeight, tabViewHeaderContainerHeight, customContainerHeight, extensionContainerHeight);
        }

        protected virtual void DerivedRefreshHeight(float detailHeight, float scrollViewHeight, float detailsHeaderHeight,
            float tabViewHeaderContainerHeight, float customContainerHeight, float extensionContainerHeight)
        {
            // This function can be overridden by the derived class to implement special height refresh logic.
        }
    }
}
