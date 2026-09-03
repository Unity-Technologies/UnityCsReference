// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

sealed class UnpackTemplatesCommand : Command<UnpackTemplatesCommand>
{
    TemplateAsset[] m_TemplatesToUnpack;
    bool m_UnpackCompletely;

    public override string UndoName => m_UnpackCompletely ? "Unpack Template Completely" : "Unpack Template";
    public override CommandCategory Category { get; } = CommandCategory.Hierarchy;

    public static UnpackTemplatesCommand GetPooled(object source, TemplateAsset[] templates, bool unpackCompletely)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.m_TemplatesToUnpack = templates;
        cmd.m_UnpackCompletely = unpackCompletely;
        return cmd;
    }

    public static bool Validate(object source, TemplateAsset[] templates, bool unpackCompletely)
    {
        using var cmd = GetPooled(source, templates, unpackCompletely);
        return cmd.Validate();
    }

    public static bool Validate(ReadOnlySpan<TemplateAsset> templates)
    {
        if (templates.IsEmpty)
            return false;
        foreach (var ta in templates)
        {
            if (ta == null || ta.visualTreeAsset == null)
                return false;
            if (ta.ResolveTemplate() == null)
                return false;
        }
        return true;
    }

    public static CommandExecutionStatus Execute(object source, TemplateAsset[] templates, bool unpackCompletely)
    {
        using var cmd = GetPooled(source, templates, unpackCompletely);
        return UICommandQueue.Execute(cmd).Status;
    }

    protected override void Init()
    {
        base.Init();
        m_TemplatesToUnpack = null;
        m_UnpackCompletely = false;
    }

    public override void Prepare(in PrepareContext context)
    {
        using var _ = HashSetPool<VisualTreeAsset>.Get(out var seen);
        foreach (var ta in m_TemplatesToUnpack)
        {
            var vta = ta?.visualTreeAsset;
            if (vta != null && seen.Add(vta))
            {
                context.RecordUndo(vta);
                context.RecordUndo(vta.inlineSheet);
            }
        }
    }

    public override bool Validate()
        => Validate(m_TemplatesToUnpack.AsSpan());

    public override CommandExecutionStatus Execute()
    {
        using var toSelectHandle = ListPool<VisualElementAsset>.Get(out var toSelect);

        foreach (var templateAsset in m_TemplatesToUnpack)
        {
            var vta = templateAsset?.visualTreeAsset;
            if (vta == null)
                continue;

            var unpacked = UnpackOne(templateAsset, vta);
            if (unpacked != null)
                toSelect.Add(unpacked);
        }

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelect);
        return CommandExecutionStatus.Success;
    }

    VisualElementAsset UnpackOne(TemplateAsset root, VisualTreeAsset vta)
    {
        using var toUnpackHandle = ListPool<TemplateAsset>.Get(out var toUnpack);
        toUnpack.Add(root);

        VisualElementAsset rootUnpacked = null;

        while (toUnpack.Count > 0)
        {
            var templateAsset = toUnpack[0];
            toUnpack.RemoveAt(0);

            var linkedVta = templateAsset.ResolveTemplate();
            if (linkedVta == null)
                continue;

            var parentVea = templateAsset.parentAsset as VisualElementAsset ?? vta.visualTree;
            var index = templateAsset.SiblingIndex();

            var linkedVtaCopy = DeepCopy(linkedVta);

            try
            {
                ApplyAttributeOverrides(templateAsset.attributeOverrides, linkedVtaCopy);
                PropagateAttributeOverridesToNestedTemplates(templateAsset.attributeOverrides, linkedVtaCopy);
                // Converts propagated name-path overrides to integer-ID serializedDataOverrides before Swallow.
                UxmlSerializer.CreateSerializedDataOverrides(linkedVtaCopy);
                ApplyComponentAttributeOverrides(templateAsset.componentAttributeOverrides, linkedVtaCopy);

                var wrapperVea = vta.AddElementOfType(parentVea, typeof(VisualElement).FullName);
                wrapperVea.serializedData = UxmlSerializedDataCreator.CreateUxmlSerializedData(typeof(VisualElement));
                vta.ReparentElementInDocument(wrapperVea, parentVea, index);

                // Keep the legacy attribute bag for UXML round-trip export.
                if (templateAsset.properties != null)
                    foreach (var prop in templateAsset.properties)
                    {
                        // "template" names the linked asset and is meaningful only on TemplateAsset;
                        // transferring it to a plain VisualElement emits a stale attribute in UXML.
                        if (prop.name == "template")
                            continue;
                        wrapperVea.SetAttribute(prop.name, prop.value);
                    }

                if (templateAsset.classes is { Count: > 0 } classes)
                    foreach (var cls in classes)
                        wrapperVea.AddStyleClass(cls);

                // Transfer inline attributes (including name) from the template instance's
                // serializedData directly — the importer already stored them with the
                // OverriddenInUxml flag, so no string re-parsing is needed.
                if (templateAsset.serializedData != null && wrapperVea.serializedData != null)
                {
                    var srcTypeName = templateAsset.serializedData.GetType().DeclaringType.FullName;
                    var srcDesc = UxmlSerializedDataRegistry.GetDescription(srcTypeName);
                    var dstDesc = UxmlSerializedDataRegistry.GetDescription(wrapperVea.fullTypeName);

                    if (srcDesc != null && dstDesc != null)
                    {
                        foreach (var srcAttr in srcDesc.serializedAttributes)
                        {
                            if ((srcAttr.GetSerializedValueAttributeFlags(templateAsset.serializedData) &
                                 UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml) == 0)
                                continue;

                            if (dstDesc.FindAttributeWithUxmlName(srcAttr.name) is not { } dstAttr)
                                continue;

                            dstAttr.SetSerializedValue(wrapperVea.serializedData,
                                srcAttr.GetSerializedValue(templateAsset.serializedData),
                                UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
                        }
                    }
                }

                // Transfer the template instance's own inline style to the wrapper.
                if (templateAsset.ruleIndex >= 0)
                    wrapperVea.ruleIndex = templateAsset.ruleIndex;

                vta.Swallow(wrapperVea, linkedVtaCopy);
                templateAsset.RemoveFromHierarchy();

                rootUnpacked ??= wrapperVea;

                if (m_UnpackCompletely)
                    CollectNestedTemplates(wrapperVea, toUnpack);
            }
            finally
            {
                if (linkedVtaCopy.inlineSheet != null)
                    Object.DestroyImmediate(linkedVtaCopy.inlineSheet);
                Object.DestroyImmediate(linkedVtaCopy);
            }
        }

        return rootUnpacked;
    }

    static void CollectNestedTemplates(UxmlAsset parent, List<TemplateAsset> toUnpack)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent[i];
            if (child is TemplateAsset nested)
                toUnpack.Add(nested);
            else
                CollectNestedTemplates(child, toUnpack);
        }
    }

    static VisualTreeAsset DeepCopy(VisualTreeAsset source)
    {
        var copy = ScriptableObject.CreateInstance<VisualTreeAsset>();
        var originalInlineSheet = copy.inlineSheet;

        var json = JsonUtility.ToJson(source);
        JsonUtility.FromJsonOverwrite(json, copy);
        copy.SetupReferences();

        copy.inlineSheet = originalInlineSheet;
        if (source.inlineSheet != null)
        {
            if (copy.inlineSheet == null)
                copy.inlineSheet = StyleSheetUtility.CreateInstanceWithHideFlags();
            var sheetJson = JsonUtility.ToJson(source.inlineSheet);
            JsonUtility.FromJsonOverwrite(sheetJson, copy.inlineSheet);
            copy.inlineSheet.RequestRebuild();
        }

        return copy;
    }

    static void ApplyAttributeOverrides(List<TemplateAsset.AttributeOverride> overrides, VisualTreeAsset linkedVtaCopy)
    {
        if (overrides == null || overrides.Count == 0)
            return;

        using var pathHandle = ListPool<string>.Get(out var namePath);

        foreach (var asset in linkedVtaCopy.DepthFirstTraversal())
        {
            if (asset is not VisualElementAsset vea)
                continue;

            BuildNamePath(vea, namePath);

            foreach (var attributeOverride in overrides)
            {
                if (!attributeOverride.NamesPathMatchesElementNamesPath(namePath))
                    continue;
                vea.SetAttribute(attributeOverride.m_AttributeName, attributeOverride.m_Value);
                if (vea.serializedData != null)
                    UxmlSerializer.TryParseSerializedAttribute(attributeOverride.m_AttributeName, attributeOverride.m_Value, vea.serializedData, new CreationContext(linkedVtaCopy));
            }
        }
    }

    static void ApplyComponentAttributeOverrides(List<TemplateAsset.ComponentAttributeOverride> overrides, VisualTreeAsset linkedVtaCopy)
    {
        if (overrides == null || overrides.Count == 0)
            return;

        using var pathHandle = ListPool<string>.Get(out var namePath);

        foreach (var asset in linkedVtaCopy.DepthFirstTraversal())
        {
            if (asset is not VisualElementAsset vea || !vea.hasComponentData)
                continue;

            BuildNamePath(vea, namePath);

            foreach (var componentOverride in overrides)
            {
                if (!componentOverride.NamesPathMatchesElementNamesPath(namePath))
                    continue;

                var overrideData = componentOverride.m_ComponentData;
                if (overrideData == null)
                    continue;

                var overrideType = overrideData.GetType();
                var dstData = vea.componentData.Find(c => c.GetType() == overrideType);
                if (dstData == null)
                    continue;

                var typeName = overrideType.DeclaringType?.FullName;
                if (typeName == null)
                    continue;

                var desc = UxmlSerializedDataRegistry.GetDescription(typeName);
                if (desc == null)
                    continue;

                foreach (var attr in desc.serializedAttributes)
                {
                    if ((attr.GetSerializedValueAttributeFlags(overrideData) &
                         UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml) == 0)
                        continue;

                    attr.SetSerializedValue(dstData,
                        attr.GetSerializedValue(overrideData),
                        UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
                }
            }
        }
    }

    // Pushes outer-instance overrides (e.g. ["inner-tc", "deep-label"]) into nested TemplateAsset.attributeOverrides
    // so that CreateSerializedDataOverrides can then convert them to the integer-ID form the runtime expects.
    static void PropagateAttributeOverridesToNestedTemplates(List<TemplateAsset.AttributeOverride> overrides, VisualTreeAsset linkedVtaCopy)
    {
        if (overrides == null || overrides.Count == 0)
            return;

        using var pathHandle = ListPool<string>.Get(out var namePath);

        foreach (var asset in linkedVtaCopy.DepthFirstTraversal())
        {
            if (asset is not TemplateAsset nestedTemplate)
                continue;

            BuildNamePath(nestedTemplate, namePath);
            if (namePath.Count == 0)
                continue;

            foreach (var attributeOverride in overrides)
            {
                var overridePath = attributeOverride.m_NamesPath;
                if (overridePath == null || overridePath.Length <= namePath.Count)
                    continue;

                var isPrefix = true;
                for (var i = 0; i < namePath.Count; i++)
                {
                    if (namePath[i] != overridePath[i])
                    {
                        isPrefix = false;
                        break;
                    }
                }

                if (!isPrefix)
                    continue;

                var subPath = new string[overridePath.Length - namePath.Count];
                for (var i = 0; i < subPath.Length; i++)
                    subPath[i] = overridePath[namePath.Count + i];

                nestedTemplate.SetAttributeOverride(attributeOverride.m_AttributeName, attributeOverride.m_Value, subPath);
            }
        }
    }

    static void BuildNamePath(UxmlAsset asset, List<string> path)
    {
        path.Clear();
        var current = asset;
        while (current != null && current.HasParent())
        {
            if (current is VisualElementAsset vea &&
                vea.TryGetAttributeValue("name", out var n) &&
                !string.IsNullOrEmpty(n))
            {
                path.Insert(0, n);
            }
            current = current.parentAsset;
        }
    }
}
