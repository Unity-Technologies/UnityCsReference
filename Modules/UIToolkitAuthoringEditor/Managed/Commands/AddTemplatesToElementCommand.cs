// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddTemplatesToElementCommand
{
    const string CommandUndoName = "Add templates to document";

    readonly VisualElementAsset ParentAsset;
    readonly int Index;
    readonly VisualTreeAsset[] Templates;

    public AddTemplatesToElementCommand(VisualElementAsset parentAsset, int index, VisualTreeAsset[] templates)
    {
        ParentAsset = parentAsset;
        Index = index;
        Templates = templates;
    }

    public void Execute()
    {
        Assert.IsNotNull(ParentAsset);
        Assert.IsNotNull(ParentAsset.visualTreeAsset);
        Assert.IsNotNull(Templates);
        foreach (var template in Templates)
        {
            Assert.IsNotNull(template);
            Assert.IsFalse(string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(template)));
        }
        Assert.IsTrue(Index >= -1 && Index <= ParentAsset.childCount);

        var visualTreeAsset = ParentAsset.visualTreeAsset;
        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

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

        EditorUtility.SetDirty(visualTreeAsset);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(visualTreeAsset);
    }
}
