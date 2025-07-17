// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ICustomDisplayDialog : IService
    {
        void Show(CustomDisplayDialogArgs args);
    }

    [ExcludeFromCodeCoverage]
    internal class CustomDisplayDialog : BaseService<ICustomDisplayDialog>, ICustomDisplayDialog
    {
        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IResourceLoader m_ResourceLoader;

        public CustomDisplayDialog(IApplicationProxy applicationProxy, IResourceLoader resourceLoader)
        {
            m_ApplicationProxy = applicationProxy;
            m_ResourceLoader = resourceLoader;
        }

        public void Show(CustomDisplayDialogArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            var content = new CustomDisplayDialogContent(m_ApplicationProxy, m_ResourceLoader, args);
            if (ModalWindowContainer.ShowModal(content))
                PackageManagerDialogAnalytics.SendEvent(content.args.idForAnalytics, content.windowTitle, content.args.bodyText, content.result.ToString());
        }
    }
}
