// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class DeselectSampleAction : SampleAction
{
    private readonly IPageManager m_PageManager;
    public DeselectSampleAction(IPageManager pageManager)
    {
        m_PageManager = pageManager;
    }

    protected override bool TriggerActionImplementation(Sample sample)
    {
        m_PageManager.activePage.RemoveSelection(new[] { sample.uniqueId }, false);
        return true;
    }

    protected override bool TriggerActionImplementation(IReadOnlyCollection<Sample> samples)
    {
        var samplesUniqueIds = samples.SelectToNewArray(s => s.uniqueId);
        m_PageManager.activePage.RemoveSelection(samplesUniqueIds, false);
        return true;
    }

    public override string GetText(Sample item, bool isInProgress)
    {
        return L10n.Tr("Deselect");
    }

    public override string GetTooltip(Sample item, bool isInProgress)
    {
        return L10n.Tr("Click to deselect these items from the list.");
    }

    public override ToolbarButtonBase<Sample, Sample> CreateToolbarButton()
    {
        return new SampleToolBarSimpleButton(this);
    }
}
