// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ICustomDisplayDialog : IService
    {
        DialogResult Show(CustomDialogArgsBase args);
    }

    [ExcludeFromCodeCoverage]
    internal class CustomDisplayDialog : BaseService<ICustomDisplayDialog>, ICustomDisplayDialog
    {
        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IResourceLoader m_ResourceLoader;

        public CustomDisplayDialog(IApplicationProxy applicationProxy, IResourceLoader resourceLoader)
        {
            m_ApplicationProxy = RegisterDependency(applicationProxy);
            m_ResourceLoader = RegisterDependency(resourceLoader);
        }

        public DialogResult Show(CustomDialogArgsBase args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            var content = new CustomDisplayDialogContent(m_ApplicationProxy, m_ResourceLoader, args);
            if (ModalWindowContainer.ShowModal(content))
                PackageManagerDialogAnalytics.SendEvent(content.args.idForAnalytics, content.windowTitle, content.args.bodyText, content.result.ToString());
            return content.result;
        }
    }
}
