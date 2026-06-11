// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class AddElementCommand : Command<AddElementCommand>
{
    public static AddElementCommand GetPooled(object source, Type elementType, VisualTreeAsset visualTreeAsset, VisualElementAsset parentVea, int index = -1)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.m_ElementType = elementType;
        cmd.m_VisualTreeAsset = visualTreeAsset;
        cmd.m_ParentVea = parentVea ?? visualTreeAsset.visualTree;
        cmd.m_Index = index;
        return cmd;
    }

    public static void Execute(object source, Type elementType, VisualTreeAsset visualTreeAsset, VisualElementAsset parentVea, int index = -1)
    {
        using var command = GetPooled(source, elementType, visualTreeAsset, parentVea, index);
        UICommandQueue.Execute(command);
    }

    Type m_ElementType;
    VisualTreeAsset m_VisualTreeAsset;
    VisualElementAsset m_ParentVea;
    int m_Index;

    public override string UndoName => "Add element";
    public override CommandCategory Category { get; } = CommandCategory.Hierarchy;

    public Type ElementType => m_ElementType;
    public VisualTreeAsset VisualTreeAsset => m_VisualTreeAsset;
    public VisualElementAsset ParentVea => m_ParentVea;
    public int Index => m_Index;

    protected override void Init()
    {
        base.Init();
        m_ElementType = null;
        m_VisualTreeAsset = null;
        m_ParentVea = null;
        m_Index = -1;
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(m_VisualTreeAsset);
    }

    public override bool Validate() => m_ElementType != null && m_VisualTreeAsset != null;

    public override CommandExecutionStatus Execute()
    {
        var vea = m_VisualTreeAsset.AddElementOfType(m_ParentVea, m_ElementType.FullName);
        vea.serializedData = UxmlSerializedDataCreator.CreateUxmlSerializedData(m_ElementType);

        if (vea is VisualElementAsset newVea)
            HandlePositioning(newVea);

        using var toSelectNodesHandle = ListPool<VisualElementAsset>.Get(out var toSelectAssets);
        toSelectAssets.Add(vea);
        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);

        return CommandExecutionStatus.Success;
    }

    void HandlePositioning(VisualElementAsset newVea)
    {
        if (m_Index < 0 || m_Index >= m_ParentVea.childCount) return;

        m_VisualTreeAsset.ReparentElementInDocument(newVea, m_ParentVea, m_Index);
    }
}
