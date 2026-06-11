// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class AddClassCommand : Command<AddClassCommand>
{
    public static AddClassCommand GetPooled(object source, VisualElementAsset vea, string className)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.m_ElementAsset = vea;
        cmd.m_ClassName = className;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset vea, string className)
    {
        using var command = GetPooled(source, vea, className);
        UICommandQueue.Execute(command);
    }

    VisualElementAsset m_ElementAsset;
    string m_ClassName;

    public override string UndoName { get; } = "Add class to element";
    public override CommandCategory Category { get; } = CommandCategory.StylingContext;

    public VisualElementAsset ElementAsset => m_ElementAsset;
    public string ClassName => m_ClassName;

    protected override void Init()
    {
        base.Init();
        m_ElementAsset = null;
        m_ClassName = null;
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(m_ElementAsset.visualTreeAsset);
    }

    public override bool Validate() => m_ElementAsset != null && !string.IsNullOrWhiteSpace(m_ClassName);

    public override CommandExecutionStatus Execute()
    {
        m_ElementAsset.AddStyleClass(m_ClassName);
        return CommandExecutionStatus.Success;
    }
}
