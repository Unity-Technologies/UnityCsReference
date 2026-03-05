// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ImportSampleAction : SampleAction
    {
        private readonly IApplicationProxy m_Application;
        private readonly IIOProxy m_IOProxy;

        public ImportSampleAction(IApplicationProxy application, IIOProxy ioProxy)
        {
            m_Application = application;
            m_IOProxy = ioProxy;
        }

        public override string GetText(Sample sample, bool isInProgress)
        {
            if (sample.isImported)
                return L10n.Tr("Reimport");

            if (sample.previousImportPaths?.Count > 0)
                return L10n.Tr("Update");

            return L10n.Tr("Import");
        }

        public override string GetTooltip(Sample sample, bool isInProgress)
        {
            if (sample.isImported)
                return L10n.Tr("Click to reimport this sample into your project.");

            if (sample.previousImportPaths?.Count > 0)
                return L10n.Tr("Click to update this sample to the latest version.");

            return L10n.Tr("Click to import this sample into your project.");
        }

        protected override bool TriggerActionImplementation(Sample sample)
        {
            var previousImports = sample.previousImportPaths;
            if (previousImports.Count > 0)
            {
                var previousImportPathsStringBuilder = new StringBuilder();
                foreach (var path in previousImports)
                {
                    previousImportPathsStringBuilder.Append(path.Replace(@"\", "/").Replace(m_Application.dataPath, "Assets"));
                    previousImportPathsStringBuilder.Append('\n');
                }

                string warningMessage;
                if (previousImports.Count > 1)
                {
                    warningMessage = L10n.Tr("Different versions of the sample are already imported at") + "\n\n"
                        + previousImportPathsStringBuilder + "\n" + L10n.Tr("They will be deleted when you update.");
                }
                else
                {
                    if (sample.isImported)
                    {
                        warningMessage = L10n.Tr("The sample is already imported at") + "\n\n"
                            + previousImportPathsStringBuilder + "\n" + L10n.Tr("Importing again will override all changes you have made to it.");
                    }
                    else
                    {
                        warningMessage = L10n.Tr("A different version of the sample is already imported at") + "\n\n"
                            + previousImportPathsStringBuilder + "\n" + L10n.Tr("It will be deleted when you update.");
                    }
                }

                if (!m_Application.DisplayDialog("importPackageSample",
                        L10n.Tr("Importing package sample"),
                        warningMessage + L10n.Tr(" Are you sure you want to continue?"),
                        L10n.Tr("Yes"), L10n.Tr("No")))
                    return false;
            }

            var eventName = previousImports.Count == 0 ? "importSample" : "reimportSample";
            PackageManagerWindowAnalytics.SendEvent(eventName, sample.package?.versions.primary.uniqueId);

            var success = sample.Import(Sample.ImportOptions.OverridePreviousImports);
            if (!success)
                return false;

            PingSampleInProjectBrowser(sample);
            return true;
        }

        private void PingSampleInProjectBrowser(Sample sample)
        {
            if (m_Application.PingObjectInProjectBrowser(IOUtils.GetRelativePath(m_IOProxy.CurrentDirectory, sample.importPath)))
                return;
            if (sample.previousImportPaths?.Count > 0)
                m_Application.PingObjectInProjectBrowser(IOUtils.GetRelativePath(m_IOProxy.CurrentDirectory,sample.previousImportPaths[^1]));
        }
    }
}
