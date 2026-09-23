// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class CreateTemplateFromElementCommand : Command<CreateTemplateFromElementCommand>
{
    VisualElementAsset m_ElementToTemplatize;
    VisualElementAsset m_ParentAsset;
    VisualTreeAsset m_Template;

    public override string UndoName => "Create Template";
    public override CommandCategory Category { get; } = CommandCategory.Hierarchy;

    public static CreateTemplateFromElementCommand GetPooled(object source, VisualElementAsset elementToTemplatize, VisualElementAsset parentAsset, VisualTreeAsset template)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.m_ElementToTemplatize = elementToTemplatize;
        cmd.m_ParentAsset = parentAsset;
        cmd.m_Template = template;
        return cmd;
    }

    public static bool Validate(object source, VisualElementAsset elementToTemplatize, VisualElementAsset parentAsset, VisualTreeAsset template)
    {
        using var cmd = GetPooled(source, elementToTemplatize, parentAsset, template);
        return cmd.Validate();
    }

    public static CommandExecutionStatus Execute(object source, VisualElementAsset elementToTemplatize, VisualElementAsset parentAsset, VisualTreeAsset template)
    {
        using var cmd = GetPooled(source, elementToTemplatize, parentAsset, template);
        return UICommandQueue.Execute(cmd).Status;
    }

    protected override void Init()
    {
        base.Init();
        m_ElementToTemplatize = null;
        m_ParentAsset = null;
        m_Template = null;
    }

    public override void Prepare(in PrepareContext context)
    {
        var vta = m_ParentAsset?.visualTreeAsset;
        context.RecordUndo(vta);
        context.RecordUndo(vta?.inlineSheet);
    }

    public override bool Validate()
    {
        if (m_ElementToTemplatize == null || m_ParentAsset == null || m_Template == null)
            return false;
        if (m_ParentAsset.visualTreeAsset == null)
            return false;
        return !string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(m_Template));
    }

    public override CommandExecutionStatus Execute()
    {
        var vta = m_ParentAsset.visualTreeAsset;
        var assetPath = AssetDatabase.GetAssetPath(m_Template);
        var insertionIndex = m_ElementToTemplatize.SiblingIndex();

        m_ElementToTemplatize.RemoveFromHierarchy();

        var templateAsset = vta.AddTemplateInstance(m_ParentAsset, assetPath);
        templateAsset.serializedData = new TemplateContainer.UxmlSerializedData();
        var typeDesc = UxmlSerializedDataRegistry.GetDescription(typeof(TemplateContainer).FullName);
        var attr = typeDesc.FindAttributeWithPropertyName(nameof(TemplateContainer.templateUXML));
        var uxmlValue = new TemplateContainer.TemplateUXML
        {
            templateAsset = m_Template,
            templateId = vta.GetTemplateNameFromPath(assetPath)
        };
        attr.SetSerializedValue(templateAsset.serializedData, uxmlValue, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

        // The instance takes over the extracted element's name so USS and UQuery written
        // against that name keep targeting the same position in the document.
        if (m_ElementToTemplatize.TryGetAttributeValue("name", out var elementName) && !string.IsNullOrEmpty(elementName))
            UxmlAssetUtilities.SetAttributeAndSyncSerializedData(templateAsset, "name", elementName);

        vta.ReparentElementInDocument(templateAsset, m_ParentAsset, insertionIndex);

        using var toSelectHandle = ListPool<VisualElementAsset>.Get(out var toSelect);
        toSelect.Add(templateAsset);
        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelect);

        return CommandExecutionStatus.Success;
    }
}
