// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class LocateSampleAction: SampleAction
    {
        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IIOProxy m_IOProxy;

        public LocateSampleAction(IApplicationProxy applicationProxy, IIOProxy ioProxy)
        {
            m_ApplicationProxy = applicationProxy;
            m_IOProxy = ioProxy;
        }

        protected override bool TriggerActionImplementation(Sample sample)
        {
            PingSampleInProjectBrowser(sample);
            PackageManagerWindowAnalytics.SendEvent("locateSample", sample.uniqueId);

            return true;
        }

        private bool PingSampleInProjectBrowser(Sample sample)
        {
            if (m_ApplicationProxy.PingObjectInProjectBrowser(m_IOProxy.GetProjectRelativePath(sample.importPath)))
                return true;
            if (sample.previousImportPaths?.Count > 0)
                return m_ApplicationProxy.PingObjectInProjectBrowser(m_IOProxy.GetProjectRelativePath(sample.previousImportPaths[^1]));
            return false;
        }

        public override bool IsVisible(Sample sample)
        {
            return sample.isImported || sample.previousImportPaths?.Count > 0;
        }

        public override string GetText(Sample item, bool isInProgress) => L10n.Tr("Locate");

        public override string GetTooltip(Sample item, bool isInProgress) => L10n.Tr("Click to locate the sample in your project.");

        protected override IEnumerable<DisableCondition> GetAllDisableConditions(Sample sample)
        {
            yield return new DisableIfEntitlementsError(sample);
            yield return new DisableIfPackageIsNotLoaded(sample);
            yield return new DisableIfPackageIsInInvalidLocation(sample);
        }
    }
}
