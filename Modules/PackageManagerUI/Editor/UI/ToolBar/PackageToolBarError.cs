// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageToolBarError : VisualElement
    {
        public PackageToolBarError()
        {
        }

        public bool Refresh(IPackage package, IPackageVersion version)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var operationError = version?.errors?.FirstOrDefault(e => e.HasAttribute(UIError.Attribute.Clearable))
#pragma warning restore RS0030
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                ?? package?.errors?.FirstOrDefault(e => e.HasAttribute(UIError.Attribute.Clearable));
#pragma warning restore RS0030
            if (operationError == null)
            {
                ClearError();
                return false;
            }
            SetError(operationError.message, operationError.HasAttribute(UIError.Attribute.Warning) ? PackageState.Warning : PackageState.Error);
            return true;
        }

        private void SetError(string message, PackageState state)
        {
            switch (state)
            {
                case PackageState.Error:
                    AddToClassList("error");
                    break;
                case PackageState.Warning:
                    AddToClassList("warning");
                    break;
                default: break;
            }

            tooltip = message;
            UIUtils.SetElementDisplay(this, true);
        }

        private void ClearError()
        {
            UIUtils.SetElementDisplay(this, false);
            tooltip = string.Empty;
            ClearClassList();
        }
    }
}
