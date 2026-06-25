// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal sealed class WriteStyleRulePropertyValueCommand<T> : Command<WriteStyleRulePropertyValueCommand<T>>
{
    const string CommandUndoName = "Write style rule property value";

    public static WriteStyleRulePropertyValueCommand<T> GetPooled(
        object source,
        StyleSheet styleSheet,
        StyleProperty property,
        VariablesInspector.VariableType type,
        T value)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Property = property;
        cmd.Type = type;
        cmd.Value = value;
        return cmd;
    }

    public static void Execute(object source,
        StyleSheet styleSheet,
        StyleProperty property,
        VariablesInspector.VariableType type,
        T value)
    {
        using var command = GetPooled(source, styleSheet, property, type, value);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleProperty Property { get; private set; }
    public VariablesInspector.VariableType Type { get; private set; }
    public T Value { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling | CommandCategory.Variables;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Property = null;
        Type = default;
        Value = default;
    }

    public override bool Validate() => StyleSheet != null && Property != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        switch (Type)
        {
            case VariablesInspector.VariableType.Float:
                StyleSheet.WriteFloat(ref Property.values[0], (float)(object)Value);
                break;
            case VariablesInspector.VariableType.Color:
                StyleSheet.WriteColor(ref Property.values[0], (Color)(object)Value);
                break;
            case VariablesInspector.VariableType.Length:
                StyleSheet.WriteLength(ref Property.values[0], (Length)(object)Value);
                break;
            case VariablesInspector.VariableType.Angle:
                StyleSheet.WriteAngle(ref Property.values[0], (Angle)(object)Value);
                break;
            case VariablesInspector.VariableType.Time:
                StyleSheet.WriteTimeValue(ref Property.values[0], (TimeValue)(object)Value);
                break;
            case VariablesInspector.VariableType.AssetReference:
                StyleSheet.WriteAssetReference(ref Property.values[0], (UnityEngine.Object)(object)Value);
                break;
            case VariablesInspector.VariableType.Keyword:
                StyleSheet.WriteKeyword(ref Property.values[0], (StyleValueKeyword)(object)Value);
                break;
            case VariablesInspector.VariableType.String:
                StyleSheet.WriteString(ref Property.values[0], (string)(object)Value);
                break;
            case VariablesInspector.VariableType.Enum:
                StyleSheet.WriteEnumAsString(ref Property.values[0], (string)(object)Value);
                break;
        }

        return CommandExecutionStatus.Success;
    }
}
