// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
            if (m_ApplicationProxy.PingObjectInProjectBrowser(IOUtils.GetRelativePath(m_IOProxy.CurrentDirectory, sample.importPath)))
                return true;
            if (sample.previousImportPaths?.Count > 0)
                return m_ApplicationProxy.PingObjectInProjectBrowser(IOUtils.GetRelativePath(m_IOProxy.CurrentDirectory, sample.previousImportPaths[^1]));
            return false;
        }

        public override ActionState GetActionState(Sample sample, out string text, out string tooltip)
        {
            if (sample.isImported || sample.previousImportPaths?.Count > 0)
            {
                text = GetText(sample, false);
                tooltip = GetTooltip(sample, false);
                return ActionState.Visible;
            }

            text = string.Empty;
            tooltip = string.Empty;
            return ActionState.None;
        }

        public override string GetText(Sample item, bool isInProgress) => L10n.Tr("Locate");

        public override string GetTooltip(Sample item, bool isInProgress) => L10n.Tr("Click to locate the sample in your project.");
    }
}
