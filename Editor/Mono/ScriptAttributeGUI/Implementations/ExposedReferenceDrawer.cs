// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;

abstract class BaseExposedPropertyDrawer : UnityEditor.PropertyDrawer
{
    private static float kDriveWidgetWidth = 18.0f;
    private static GUIStyle kDropDownStyle = null;
    private static Color kMissingOverrideColor = new Color(1.0f, 0.11f, 0.11f, 1.0f);
    protected static string kSetExposedPropertyMsg = "Set Exposed Property";
    protected static string kClearExposedPropertyMsg = "Clear Exposed Property";
    private  const string kVisualElementName = "ExposedReference";

    internal readonly GUIContent ExposePropertyContent = EditorGUIUtility.TrTextContent("Expose Property");
    internal readonly GUIContent UnexposePropertyContent = EditorGUIUtility.TrTextContent("Unexpose Property");
    protected readonly GUIContent NotFoundOn = EditorGUIUtility.TrTextContent("not found on");
    protected readonly GUIContent OverridenByContent = EditorGUIUtility.TrTextContent("Overridden by ");

    private GUIContent m_ModifiedLabel = new GUIContent();

    internal enum ExposedPropertyMode
    {
        DefaultValue,
        Named,
        NamedGUID
    }

    internal enum OverrideState
    {
        DefaultValue,
        MissingOverride,
        Overridden
    }

    ExposedReferenceObject m_Item;

    public BaseExposedPropertyDrawer()
    {
        if (kDropDownStyle == null)
            kDropDownStyle = "ShurikenDropdown";
    }

    static internal ExposedPropertyMode GetExposedPropertyMode(string propertyName)
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
        m_Item = new ExposedReferenceObject(prop);

        Rect propertyFieldPosition = position;
        propertyFieldPosition.xMax = propertyFieldPosition.xMax - ExposedReferencePropertyDrawer.kDriveWidgetWidth;

        Rect driveFieldPosition = position;
        driveFieldPosition.x = propertyFieldPosition.xMax;
        driveFieldPosition.width = ExposedReferencePropertyDrawer.kDriveWidgetWidth;

        bool showContextMenu = m_Item.exposedPropertyTable != null;
        var propertyName = new PropertyName(m_Item.exposedPropertyNameString);

        var previousColor = GUI.color;
        var wasBoldDefaultFont = EditorGUIUtility.GetBoldDefaultFont();

        var valuePosition = DrawLabel(showContextMenu, label, position, m_Item);
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        if (m_Item.propertyMode == ExposedPropertyMode.DefaultValue || m_Item.propertyMode == ExposedPropertyMode.NamedGUID)
        {
            OnRenderProperty(valuePosition, propertyName, m_Item.currentReferenceValue, m_Item.exposedPropertyDefault,
                m_Item.exposedPropertyName,
                m_Item.propertyMode, m_Item.exposedPropertyTable);
        }
        else
        {
            valuePosition.width /= 2;
            EditorGUI.BeginChangeCheck();
            m_Item.exposedPropertyNameString = EditorGUI.TextField(valuePosition, m_Item.exposedPropertyNameString);
            if (EditorGUI.EndChangeCheck())
                m_Item.exposedPropertyName.stringValue = m_Item.exposedPropertyNameString;

            valuePosition.x += valuePosition.width;
            OnRenderProperty(valuePosition, new PropertyName(m_Item.exposedPropertyNameString),
                m_Item.currentReferenceValue, m_Item.exposedPropertyDefault,
                m_Item.exposedPropertyName, m_Item.propertyMode, m_Item.exposedPropertyTable);
        }

        GUI.color = previousColor;
        EditorGUIUtility.SetBoldDefaultFont(wasBoldDefaultFont);

        if (showContextMenu && GUI.Button(driveFieldPosition, GUIContent.none, kDropDownStyle))
        {
            GenericMenu menu = new GenericMenu();
            PopulateContextMenu(menu, m_Item);
            menu.ShowAsContext();
            Event.current.Use();
        }

        EditorGUI.indentLevel = indent;
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty prop)
    {
        m_Item = new ExposedReferenceObject(prop);

        var propertyType = fieldInfo.FieldType;

        if (propertyType.IsArrayOrList())
        {
            propertyType = propertyType.GetArrayOrListElementType();
        }

        var typeOfExposedReference = propertyType.GetGenericArguments()[0];

        ObjectField obj = new ObjectField()
        {
            name = kVisualElementName,
            label = preferredLabel,
            objectType = typeOfExposedReference,
            value = m_Item.currentReferenceValue,
            allowSceneObjects = m_Item.exposedPropertyTable != null
        };

        obj.RegisterValueChangedCallback(SetReference);
        obj.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

        return obj;
    }

    void SetReference(ChangeEvent<Object> evt)
    {
        SetReference(evt.newValue);
        if (m_Item.currentReferenceValue != evt.newValue)
        {
            m_Item.currentReferenceValue = evt.newValue;

            //save the modified SerializedObject since we are bypassing the binding system
            m_Item.exposedPropertyName.serializedObject.ApplyModifiedProperties();
        }
    }

    internal void SetReference(Object newValue)
    {
        if (m_Item.exposedPropertyTable == null)
            return;

        if (m_Item.propertyMode == ExposedPropertyMode.DefaultValue)
        {
            // We can directly assign to the exposed property default value if
            // * asset we are modifying is in the scene
            // * object we are assigning to the property is also an asset
            if (!EditorUtility.IsPersistent(m_Item.exposedPropertyDefault.serializedObject.targetObject) ||
                newValue == null || EditorUtility.IsPersistent(newValue))
            {
                if (!EditorGUI.CheckForCrossSceneReferencing(
                        m_Item.exposedPropertyDefault.serializedObject.targetObject, newValue))
                {
                    m_Item.exposedPropertyDefault.objectReferenceValue = newValue;
                }
            }
            else
            {
                var str = UnityEditor.GUID.Generate().ToString();
                m_Item.exposedPropertyNameString = str;
                m_Item.exposedPropertyName.stringValue = str;
                m_Item.propertyMode = ExposedPropertyMode.NamedGUID;

                Undo.RecordObject(m_Item.exposedPropertyTable as UnityEngine.Object, kSetExposedPropertyMsg);
                m_Item.exposedPropertyTable.SetReferenceValue(m_Item.exposedPropertyNameString, newValue);
            }
        }
        else
        {
            Undo.RecordObject(m_Item.exposedPropertyTable as UnityEngine.Object, kSetExposedPropertyMsg);
            m_Item.exposedPropertyTable.SetReferenceValue(m_Item.exposedPropertyNameString, newValue);
        }
    }


    Rect DrawLabel(bool showContextMenu, GUIContent label, Rect position, ExposedReferenceObject item)
    {
        if (showContextMenu)
        {
            position.xMax = position.xMax - ExposedReferencePropertyDrawer.kDriveWidgetWidth;
        }

        EditorGUIUtility.SetBoldDefaultFont(item.currentOverrideState != OverrideState.DefaultValue);

        m_ModifiedLabel.text = label.text;
        m_ModifiedLabel.tooltip = label.tooltip;
        m_ModifiedLabel.image = label.image;

        if (!string.IsNullOrEmpty(m_ModifiedLabel.tooltip))
        {
            m_ModifiedLabel.tooltip += "\n";
        }

        if (item.currentOverrideState == OverrideState.MissingOverride)
        {
            GUI.color = kMissingOverrideColor;
            m_ModifiedLabel.tooltip += label.text + " " + NotFoundOn.text + " " + item.exposedPropertyTable + ".";
        }
        else if (item.currentOverrideState == OverrideState.Overridden && item.exposedPropertyTable != null)
        {
            m_ModifiedLabel.tooltip += OverridenByContent.text + item.exposedPropertyTable + ".";
        }

        var prefixRect = EditorGUI.PrefixLabel(position, m_ModifiedLabel);

        // Show contextual menu
        if (item.exposedPropertyTable != null && Event.current.type == EventType.ContextClick)
        {
            if (position.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                item.currentOverrideState = string.IsNullOrEmpty(item.exposedPropertyNameString) ? OverrideState.DefaultValue : OverrideState.Overridden;
                PopulateContextMenu(menu, item);
                menu.ShowAsContext();
            }
        }

        return prefixRect;
    }

    protected abstract void PopulateContextMenu(GenericMenu menu, ExposedReferenceObject item);

    // UITK context menu
    void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        if (m_Item != null && m_Item.exposedPropertyTable != null)
        {
            OverrideState currentOverrideState;
            var currentValue = m_Item.Resolve(out currentOverrideState);

            if (m_Item.currentOverrideState == OverrideState.DefaultValue)
            {
                evt.menu.AppendAction(ExposePropertyContent.text,
                    (userData) =>
                    {
                        ExposedReferencePropertyDrawer.SetReferenceValueMenuItem(m_Item.exposedPropertyTable,
                            m_Item.exposedPropertyName, currentValue);
                    });
            }
            else
            {
                evt.menu.AppendAction(UnexposePropertyContent.text, (userData) =>
                {
                    ExposedReferencePropertyDrawer.ClearReferenceValueMenuItem(m_Item.exposedPropertyTable,
                        m_Item.exposedPropertyName, new PropertyName(m_Item.exposedPropertyName.stringValue));
                });
            }
        }

        evt.menu.AppendAction("Properties...",
            (userData) =>
            {
                UnityEditor.EditorUtility.OpenPropertyEditor(m_Item.currentReferenceValue);
            });
    }
}

[CustomPropertyDrawer(typeof(ExposedReference<>))]
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
        var propertyType = fieldInfo.FieldType;

        if (propertyType.IsArrayOrList())
        {
            propertyType = propertyType.GetArrayOrListElementType();
        }

        var typeOfExposedReference = propertyType.GetGenericArguments()[0];

        EditorGUI.BeginChangeCheck();
        var newValue = EditorGUI.ObjectField(position, currentReferenceValue, typeOfExposedReference,
            exposedPropertyTable != null);

        if (EditorGUI.EndChangeCheck())
        {
            SetReference(newValue);
        }
    }

    protected override void PopulateContextMenu(GenericMenu menu, ExposedReferenceObject item)
    {
        var propertyName = new PropertyName(item.exposedPropertyName.stringValue);
        OverrideState currentOverrideState;
        UnityEngine.Object currentValue = item.Resolve(out currentOverrideState);

        if (item.currentOverrideState == OverrideState.DefaultValue)
        {
            menu.AddItem(new GUIContent(ExposePropertyContent.text), false,
                (userData) => { SetReferenceValueMenuItem(item.exposedPropertyTable, item.exposedPropertyName, currentValue); }, null);
        }
        else
        {
            menu.AddItem(UnexposePropertyContent, false,
                (userData) => { ClearReferenceValueMenuItem(item.exposedPropertyTable, item.exposedPropertyName, propertyName); }, null);
        }
    }

    internal static void SetReferenceValueMenuItem(IExposedPropertyTable exposedPropertyTable,
        SerializedProperty exposedName, Object currentValue)
    {
        var guid = UnityEditor.GUID.Generate();
        exposedName.stringValue = guid.ToString();
        exposedName.serializedObject.ApplyModifiedProperties();
        var newPropertyName = new PropertyName(exposedName.stringValue);

        Undo.RecordObject(exposedPropertyTable as Object, kSetExposedPropertyMsg);
        exposedPropertyTable.SetReferenceValue(newPropertyName, currentValue);
    }

    internal static void ClearReferenceValueMenuItem(IExposedPropertyTable exposedPropertyTable,
        SerializedProperty exposedName, PropertyName propertyName)
    {
        exposedName.stringValue = "";
        exposedName.serializedObject.ApplyModifiedProperties();

        Undo.RecordObject(exposedPropertyTable as Object, kClearExposedPropertyMsg);
        exposedPropertyTable.ClearReferenceValue(propertyName);
    }
}
