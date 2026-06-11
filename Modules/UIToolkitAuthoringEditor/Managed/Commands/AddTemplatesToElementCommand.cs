// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class AddTemplatesToElementCommand : Command<AddTemplatesToElementCommand>
{
    const string CommandUndoName = "Add templates to document";

    public static AddTemplatesToElementCommand GetPooled(object source, VisualElementAsset parentAsset, int index, VisualTreeAsset[] templates)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ParentAsset = parentAsset;
        cmd.Index = index;
        cmd.Templates = templates;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset parentAsset, int index, VisualTreeAsset[] templates)
    {
        using var command = GetPooled(source, parentAsset, index, templates);
        UICommandQueue.Execute(command);
    }

    public VisualElementAsset ParentAsset { get; private set; }
    public int Index { get; private set; }
    public VisualTreeAsset[] Templates { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        ParentAsset = null;
        Index = -1;
        Templates = null;
    }

    public override bool Validate()
    {
        if (ParentAsset == null || ParentAsset.visualTreeAsset == null || Templates == null)
            return false;
        if (Index < -1 || Index > ParentAsset.childCount)
            return false;
        foreach (var template in Templates)
        {
            if (template == null)
                return false;
            if (string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(template)))
                return false;
        }
        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(ParentAsset.visualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        var visualTreeAsset = ParentAsset.visualTreeAsset;
        for (var i = 0; i < Templates.Length; ++i)
        {
            var index = Index + i;
            var template = Templates[i];
            var assetPath = AssetDatabase.GetAssetPath(template);
            var templateAsset = visualTreeAsset.AddTemplateInstance(ParentAsset, assetPath);

            templateAsset.serializedData = new TemplateContainer.UxmlSerializedData();
            var uxmlTypeDescription = UxmlSerializedDataRegistry.GetDescription(typeof(TemplateContainer).FullName);
            var attribute = uxmlTypeDescription.FindAttributeWithPropertyName(nameof(TemplateContainer.templateUXML));

            var uxmlValue = new TemplateContainer.TemplateUXML { templateId = ParentAsset.visualTreeAsset.GetTemplateNameFromPath(assetPath) };
            attribute.SetSerializedValue(templateAsset.serializedData, uxmlValue, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
            visualTreeAsset.ReparentElementInDocument(templateAsset, ParentAsset, index);
        }

        return CommandExecutionStatus.Success;
    }
}
