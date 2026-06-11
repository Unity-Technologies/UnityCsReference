// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal sealed class ResetStyleRulePropertyValueCommand : Command<ResetStyleRulePropertyValueCommand>
{
    const string CommandUndoName = "Reset style rule property value";

    public static ResetStyleRulePropertyValueCommand GetPooled(
        object source,
        StyleSheet styleSheet,
        StyleProperty property,
        VariablesInspector.VariableType type)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Property = property;
        cmd.Type = type;
        return cmd;
    }

    public static void Execute(object source,
        StyleSheet styleSheet,
        StyleProperty property,
        VariablesInspector.VariableType type)
    {
        using var command = GetPooled(source, styleSheet, property, type);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleProperty Property { get; private set; }
    public VariablesInspector.VariableType Type { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Property = null;
        Type = default;
    }

    public override bool Validate() => StyleSheet != null && Property != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        SetPropertyToDefaultValue(StyleSheet, Property, Type);
        return CommandExecutionStatus.Success;
    }

    internal static void SetPropertyToDefaultValue(StyleSheet styleSheet, StyleProperty property, VariablesInspector.VariableType type)
    {
        switch (type)
        {
            case VariablesInspector.VariableType.Float:
                property.SetFloat(styleSheet, float.Parse(GetTextFieldDefaultValue(type)));
                break;
            case VariablesInspector.VariableType.Color:
                property.SetColor(styleSheet, Color.black);
                break;
            case VariablesInspector.VariableType.AssetReference:
                property.SetAssetReference(styleSheet, new UnityEngine.Object());
                break;
            case VariablesInspector.VariableType.Length:
                property.SetLength(styleSheet, new Length(0, LengthUnit.Pixel));
                break;
            case VariablesInspector.VariableType.Angle:
                property.SetRotate(styleSheet, new Angle(0, AngleUnit.Degree));
                break;
            case VariablesInspector.VariableType.Time:
                property.SetDimension(styleSheet, new Dimension(0, Dimension.Unit.Second));
                break;
            case VariablesInspector.VariableType.Keyword:
                property.SetKeyword(styleSheet, StyleValueKeyword.Auto);
                break;
            case VariablesInspector.VariableType.String:
                property.SetString(styleSheet, GetTextFieldDefaultValue(type));
                break;
            case VariablesInspector.VariableType.Enum:
                property.SetEnum(styleSheet, (Enum)StyleValueKeyword.Auto);
                break;
        }
    }

    static string GetTextFieldDefaultValue(VariablesInspector.VariableType type)
    {
        return type switch
        {
            VariablesInspector.VariableType.Float => "0",
            VariablesInspector.VariableType.String => "String",
            _ => ""
        };
    }
}
