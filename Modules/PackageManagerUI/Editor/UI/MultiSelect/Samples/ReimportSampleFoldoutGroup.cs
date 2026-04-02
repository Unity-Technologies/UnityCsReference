// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class ReimportSampleFoldoutGroup : SampleMultiSelectFoldoutGroup
{
    public ReimportSampleFoldoutGroup(IApplicationProxy applicationProxy, IIOProxy ioProxy, ISampleImporter sampleImporter) : base(new ImportSampleAction(applicationProxy, ioProxy, sampleImporter))
    {
    }

    public override void Refresh()
    {
        mainFoldout.headerTextTemplate = L10n.Tr("Reimport {0}");
        base.Refresh();
    }

    public override bool AddItem(Sample item)
    {
        if (!item.isImported)
            return false;
        return base.AddItem(item);
    }
}
