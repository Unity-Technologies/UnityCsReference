// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageToolBarError : VisualElement
    {
        private PackageDatabase m_PackageDatabase;
        private void ResolveDependencies(PackageDatabase packageDatabase)
        {
            m_PackageDatabase = packageDatabase;
        }

        private Label m_ErrorMessage;
        private Label m_ErrorStatus;
        private Button m_OkButton;

        private IPackage m_Package;
        private IPackageVersion m_Version;
        public PackageToolBarError(PackageDatabase packageDatabase)
        {
            ResolveDependencies(packageDatabase);

            m_ErrorStatus = new Label { name = "state" };
            m_ErrorStatus.AddToClassList("status");
            Add(m_ErrorStatus);

            m_ErrorMessage = new Label { name = "message" };
            m_ErrorMessage.AddToClassList("errorMessage");
            m_ErrorMessage.ShowTextTooltipOnSizeChange();
            Add(m_ErrorMessage);

            m_OkButton = new Button { name = "okButton", text = L10n.Tr("Ok")};
            m_OkButton.clickable.clicked += OnOkClicked;
            Add(m_OkButton);
        }

        public bool Refresh(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version;

            var operationError = m_Version?.errors?.FirstOrDefault(e => e.HasAttribute(UIError.Attribute.IsClearable))
                ?? m_Package?.errors?.FirstOrDefault(e => e.HasAttribute(UIError.Attribute.IsClearable));
            if (operationError == null)
            {
                ClearError();
                return false;
            }
            SetError(operationError.message, operationError.HasAttribute(UIError.Attribute.IsWarning) ? PackageState.Warning : PackageState.Error);
            return true;
        }

        private void SetError(string message, PackageState state)
        {
            switch (state)
            {
                case PackageState.Error:
                    m_ErrorStatus.AddClasses("error");
                    break;
                case PackageState.Warning:
                    m_ErrorStatus.AddClasses("warning");
                    break;
                default: break;
            }

            m_ErrorMessage.text = message;
            UIUtils.SetElementDisplay(this, true);
        }

        private void ClearError()
        {
            UIUtils.SetElementDisplay(this, false);
            m_ErrorMessage.text = string.Empty;
            m_ErrorStatus.ClearClassList();
        }

        private void OnOkClicked()
        {
            // We use `PackageDatabase.ClearPackageErrors` instead of `IPackage.ClearErrors` to trigger `onPackagesChanged` events.
            m_PackageDatabase.ClearPackageErrors(m_Package, e => e.HasAttribute(UIError.Attribute.IsClearable));
        }
    }
}
