// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IModalManager : IService
    {
        public bool ShowModal(ModalContent content);
        public bool ShowExportModal(IPackageVersion version, OrganizationInfo[] organizationInfos);
    }

    internal class ModalManager : BaseService<IModalManager>, IModalManager
    {
        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IIOProxy m_IOProxy;
        private readonly IResourceLoader m_ResourceLoader;
        private readonly IUnityConnectProxy m_UnityConnectProxy;
        private readonly IUpmClient m_UpmClient;

        public ModalManager(IApplicationProxy applicationProxy, IIOProxy ioProxy, IResourceLoader resourceLoader, IUnityConnectProxy unityConnectProxy, IUpmClient upmClient)
        {
            m_ApplicationProxy = RegisterDependency(applicationProxy);
            m_IOProxy = RegisterDependency(ioProxy);
            m_ResourceLoader = RegisterDependency(resourceLoader);
            m_UnityConnectProxy = RegisterDependency(unityConnectProxy);
            m_UpmClient = RegisterDependency(upmClient);
        }

        public bool ShowModal(ModalContent content)
        {
            return ModalWindowContainer.ShowModal(content);
        }

        public bool ShowExportModal(IPackageVersion version, OrganizationInfo[] organizationInfos)
        {
            var content = new ExportWindowContent(m_ApplicationProxy, m_IOProxy, m_ResourceLoader, m_UnityConnectProxy, m_UpmClient, version);
            content.SetOrganizationInfos(organizationInfos);
            return ModalWindowContainer.ShowModal(content);
        }
    }
}
