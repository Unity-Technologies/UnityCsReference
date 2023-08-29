// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SignInDetails : VisualElement
    {
        private static readonly string k_Message = L10n.Tr("You must sign in before Unity can display all information about this package.");
        private static readonly string k_ButtonText = L10n.Tr("Sign in");

        private IUnityConnectProxy m_UnityConnect;

        public SignInDetails(IUnityConnectProxy unityConnect)
        {
            m_UnityConnect = unityConnect;

            // This container is needed to center the SignInDetails vertically and horizontally.
            var container = new VisualElement();
            container.AddToClassList("signInDetailsContainer");
            Add(container);

            container.Add(new Label(k_Message));
            container.Add(new DropdownButton(OnSignInButtonClicked) { text = k_ButtonText });
        }

        private void OnSignInButtonClicked()
        {
            m_UnityConnect.ShowLogin();
        }
    }
}
