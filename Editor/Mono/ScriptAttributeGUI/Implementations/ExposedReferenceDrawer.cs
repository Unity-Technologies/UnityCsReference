// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

abstract class BaseExposedPropertyDrawer : UnityEditor.PropertyDrawer
{
    private static float kDriveWidgetWidth = 18.0f;
    private static GUIStyle kDropDownStyle = null;
    private static Color kMissingOverrideColor = new Color(1.0f, 0.11f, 0.11f, 1.0f);

    protected readonly GUIContent ExposePropertyContent = EditorGUIUtility.TextContent("Expose Property");
    protected readonly GUIContent UnexposePropertyContent = EditorGUIUtility.TextContent("Unexpose Property");
    protected readonly GUIContent NotFoundOn = EditorGUIUtility.TextContent("not found on");
    protected readonly GUIContent OverridenByContent = EditorGUIUtility.TextContent("Overriden by ");

    private GUIContent m_ModifiedLabel = new GUIContent();

    protected enum ExposedPropertyMode
    {
        DefaultValue,
        Named,
        NamedGUID
    }

    protected enum OverrideState
    {
        DefaultValue,
        MissingOverride,
        Overridden
    }

    public BaseExposedPropertyDrawer()
    {
        if (kDropDownStyle == null)
            kDropDownStyle = new GUIStyle("ShurikenDropdown");
    }

    static ExposedPropertyMode GetExposedPropertyMode(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return ExposedPropertyMode.DefaultValue;
        else
        {
            GUID guid;
            if (GUID.TryParse(propertyName, out guid))
                return ExposedPropertyMode.NamedGUID;
            else
                return ExposedPropertyMode.Named;
        }
    }

    protected IExposedPropertyTable GetExposedPropertyTable(SerializedProperty property)
    {
        var t = property.serializedObject.context;
        return t as IExposedPropertyTable;
    }

    protected abstract void OnRenderProperty(Rect position,
        PropertyName exposedPropertyNameString,
        Object currentReferenceValue,
        UnityEditor.SerializedProperty exposedPropertyDefault,
        UnityEditor.SerializedProperty exposedPropertyName,
        ExposedPropertyMode mode,
        IExposedPropertyTable exposedProperties);

    public override void OnGUI(Rect position,
        UnityEditor.SerializedProperty prop,
        GUIContent label)
    {
        SerializedProperty defaultValue = prop.FindPropertyRelative("defaultValue");
        SerializedProperty exposedName = prop.FindPropertyRelative("exposedName");
        var exposedNameStr = exposedName.stringValue;
        var propertyMode = GetExposedPropertyMode(exposedNameStr);

        Rect propertyFieldPosition = position;
        propertyFieldPosition.xMax = propertyFieldPosition.xMax - ExposedReferencePropertyDrawer.kDriveWidgetWidth;

        Rect driveFieldPosition = position;
        driveFieldPosition.x = propertyFieldPosition.xMax;
        driveFieldPosition.width = ExposedReferencePropertyDrawer.kDriveWidgetWidth;

        var exposedPropertyTable = GetExposedPropertyTable(prop);

        bool showContextMenu = exposedPropertyTable != null;
        var propertyName = new PropertyName(exposedNameStr);

        OverrideState currentOverrideState = OverrideState.DefaultValue;
        var currentReferenceValue = Resolve(propertyName, exposedPropertyTable, defaultValue.objectReferenceValue, out currentOverrideState);

        var previousColor = GUI.color;
        var wasBoldDefaultFont = EditorGUIUtility.GetBoldDefaultFont();

        var valuePosition = DrawLabel(showContextMenu, currentOverrideState, label, position, exposedPropertyTable, exposedNameStr, exposedName, defaultValue);

        EditorGUI.BeginChangeCheck();
        if (propertyMode == ExposedPropertyMode.DefaultValue || propertyMode == ExposedPropertyMode.NamedGUID)
        {
            OnRenderProperty(valuePosition, propertyName, currentReferenceValue, defaultValue, exposedName, propertyMode, exposedPropertyTable);
        }
        else
        {
            valuePosition.width /= 2;
            EditorGUI.BeginChangeCheck();
            exposedNameStr = EditorGUI.TextField(valuePosition, exposedNameStr);
            if (EditorGUI.EndChangeCheck())
                exposedName.stringValue = exposedNameStr;

            valuePosition.x += valuePosition.width;
            OnRenderProperty(valuePosition, new PropertyName(exposedNameStr), currentReferenceValue, defaultValue, exposedName, propertyMode, exposedPropertyTable);
        }

        EditorGUI.EndDisabledGroup();

        GUI.color = previousColor;
        EditorGUIUtility.SetBoldDefaultFont(wasBoldDefaultFont);


        if (showContextMenu && GUI.Button(driveFieldPosition, GUIContent.none, kDropDownStyle))
        {
            GenericMenu menu = new GenericMenu();
            PopulateContextMenu(menu, currentOverrideState, exposedPropertyTable, exposedName, defaultValue);
            menu.ShowAsContext();
            Event.current.Use();
        }
    }

    Rect DrawLabel(bool showContextMenu, OverrideState currentOverrideState, GUIContent label, Rect position, IExposedPropertyTable exposedPropertyTable, string exposedNameStr, SerializedProperty exposedName, SerializedProperty defaultValue)
    {
        if (showContextMenu)
        {
            position.xMax = position.xMax - ExposedReferencePropertyDrawer.kDriveWidgetWidth;
        }

        EditorGUIUtility.SetBoldDefaultFont(currentOverrideState != OverrideState.DefaultValue);

        m_ModifiedLabel.text = label.text;
        m_ModifiedLabel.tooltip = label.tooltip;
        m_ModifiedLabel.image = label.image;

        if (!string.IsNullOrEmpty(m_ModifiedLabel.tooltip))
        {
            m_ModifiedLabel.tooltip += "\n";
        }
        if (currentOverrideState == OverrideState.MissingOverride)
        {
            GUI.color = kMissingOverrideColor;
            m_ModifiedLabel.tooltip += label.text + " " + NotFoundOn.text + " " + exposedPropertyTable.ToString() + ".";
        }
        else if (currentOverrideState == OverrideState.Overridden && exposedPropertyTable != null)
        {
            m_ModifiedLabel.tooltip += OverridenByContent.text + exposedPropertyTable.ToString() + ".";
        }

        var prefixRect = EditorGUI.PrefixLabel(position, m_ModifiedLabel);

        // Show contextual menu
        if (exposedPropertyTable != null && Event.current.type == EventType.ContextClick)
        {
            if (position.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                PopulateContextMenu(menu, string.IsNullOrEmpty(exposedNameStr) ? OverrideState.DefaultValue : OverrideState.Overridden, exposedPropertyTable, exposedName, defaultValue);
                menu.ShowAsContext();
            }
        }

        return prefixRect;
    }

    protected Object Resolve(PropertyName exposedPropertyName, IExposedPropertyTable exposedPropertyTable, Object defaultValue, out OverrideState currentOverrideState)
    {
        Object objReference = null;
        bool propertyFoundInTable = false;
        var propertyIsNamed = !PropertyName.IsNullOrEmpty(exposedPropertyName);
        currentOverrideState = OverrideState.DefaultValue;

        if (exposedPropertyTable != null)
        {
            objReference = exposedPropertyTable.GetReferenceValue(exposedPropertyName, out propertyFoundInTable);

            if (propertyFoundInTable)
                currentOverrideState = OverrideState.Overridden;
            else if (propertyIsNamed)
                currentOverrideState = OverrideState.MissingOverride;
        }

        return currentOverrideState == OverrideState.Overridden ? objReference : defaultValue;
    }

    protected abstract void PopulateContextMenu(GenericMenu menu, OverrideState overrideState, IExposedPropertyTable exposedPropertyTable, SerializedProperty exposedName, SerializedProperty defaultValue);
}


[CustomPropertyDrawer(typeof(ExposedReference < >))]
class ExposedReferencePropertyDrawer : BaseExposedPropertyDrawer
{
    protected override void OnRenderProperty(Rect position,
        PropertyName exposedPropertyNameString,
        Object currentReferenceValue,
        UnityEditor.SerializedProperty exposedPropertyDefault,
        UnityEditor.SerializedProperty exposedPropertyName,
        ExposedPropertyMode mode,
        IExposedPropertyTable exposedPropertyTable)
    {
        var typeOfExposedReference = fieldInfo.FieldType.GetGenericArguments()[0];

        EditorGUI.BeginChangeCheck();
        var newValue = EditorGUI.ObjectField(position, currentReferenceValue, typeOfExposedReference, exposedPropertyTable != null);

        if (EditorGUI.EndChangeCheck())
        {
            if (mode == ExposedPropertyMode.DefaultValue)
            {
                // We can directly assign to the exposed property default value if
                // * asset we are modifying is in the scene
                // * object we are assigning to the property is also an asset
                if (!EditorUtility.IsPersistent(exposedPropertyDefault.serializedObject.targetObject) || newValue == null || EditorUtility.IsPersistent(newValue))
                {
                    if (!EditorGUI.CheckForCrossSceneReferencing(exposedPropertyDefault.serializedObject.targetObject, newValue))
                    {
                        exposedPropertyDefault.objectReferenceValue = newValue;
                    }
                }
                else
                {
                    var guid = UnityEditor.GUID.Generate();
                    var str = guid.ToString();
                    exposedPropertyNameString = new PropertyName(str);
                    exposedPropertyName.stringValue = str;

                    Undo.RecordObject(exposedPropertyTable as UnityEngine.Object, "Set Exposed Property");
                    exposedPropertyTable.SetReferenceValue(exposedPropertyNameString, newValue);
                }
            }
            else
            {
                Undo.RecordObject(exposedPropertyTable as UnityEngine.Object, "Set Exposed Property");
                exposedPropertyTable.SetReferenceValue(exposedPropertyNameString, newValue);
            }
        }
    }

    protected override void PopulateContextMenu(GenericMenu menu, OverrideState overrideState, IExposedPropertyTable exposedPropertyTable, SerializedProperty exposedName, SerializedProperty defaultValue)
    {
        var propertyName = new PropertyName(exposedName.stringValue);
        OverrideState currentOverrideState;
        UnityEngine.Object currentValue = Resolve(new PropertyName(exposedName.stringValue), exposedPropertyTable, defaultValue.objectReferenceValue, out currentOverrideState);

        if (overrideState == OverrideState.DefaultValue)
        {
            menu.AddItem(new GUIContent(ExposePropertyContent.text), false, (userData) =>
                {
                    var guid = UnityEditor.GUID.Generate();
                    exposedName.stringValue = guid.ToString();
                    exposedName.serializedObject.ApplyModifiedProperties();
                    var newPropertyName = new PropertyName(exposedName.stringValue);

                    Undo.RecordObject(exposedPropertyTable as Object, "Set Exposed Property");
                    exposedPropertyTable.SetReferenceValue(newPropertyName, currentValue);
                } , null);
        }
        else
        {
            menu.AddItem(UnexposePropertyContent, false, (userData) =>
                {
                    exposedName.stringValue = "";
                    exposedName.serializedObject.ApplyModifiedProperties();

                    Undo.RecordObject(exposedPropertyTable as Object, "Clear Exposed Property");
                    exposedPropertyTable.ClearReferenceValue(propertyName);
                }, null);
        }
    }
}
