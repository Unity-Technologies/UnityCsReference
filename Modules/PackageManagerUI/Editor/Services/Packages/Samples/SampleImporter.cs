// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ISampleImporter : IService
    {
        bool Import(Sample sample, Sample.ImportOptions options = Sample.ImportOptions.None);
    }

    internal class SampleImporter : BaseService<ISampleImporter>, ISampleImporter
    {
        private static readonly string k_CopySamplesFilesTitle = L10n.Tr("Copying samples files");

        private readonly IIOProxy m_IOProxy;
        private readonly IAssetDatabaseProxy m_AssetDatabase;
        public SampleImporter(IIOProxy ioProxy, IAssetDatabaseProxy assetDatabase)
        {
            m_IOProxy = RegisterDependency(ioProxy);
            m_AssetDatabase = RegisterDependency(assetDatabase);
        }

        public bool Import(Sample sample, Sample.ImportOptions options = Sample.ImportOptions.None)
        {
            try
            {
                var interactive = (options & Sample.ImportOptions.HideImportWindow) == Sample.ImportOptions.None && sample.interactiveImport;
                var unityPackages = m_IOProxy.GetFiles(sample.resolvedPath, "*.unitypackage");
                if (unityPackages.Length > 0)
                    m_AssetDatabase.ImportPackage(unityPackages[0], interactive);
                else
                {
                    var prevImports = sample.previousImportPaths;
                    if (prevImports.Count > 0 && (options & Sample.ImportOptions.OverridePreviousImports) == Sample.ImportOptions.None)
                        return false;
                    foreach (var v in prevImports)
                    {
                        EditorUtility.DisplayProgressBar(k_CopySamplesFilesTitle, L10n.Tr("Cleaning previous import..."), 0);
                        m_IOProxy.RemovePathAndMeta(v, true);
                    }

                    var sourcePath = sample.resolvedPath;
                    m_IOProxy.DirectoryCopy(sourcePath, sample.importPath, true,
                        (fileName, progress) =>
                        {
                            var name = fileName.Replace(sourcePath + Path.DirectorySeparatorChar, "");
                            EditorUtility.DisplayProgressBar(k_CopySamplesFilesTitle, name, progress);
                        }
                    );
                    EditorUtility.ClearProgressBar();
                    m_AssetDatabase.Refresh();
                }

                return true;
            }
            catch (IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot import sample {sample.displayName}: {e.Message}");
                return false;
            }
        }
    }
}
