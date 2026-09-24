// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;
using Object = UnityEngine.Object;

abstract class BaseExposedPropertyDrawer : UnityEditor.PropertyDrawer
{
    private static readonly float kDriveWidgetWidth = 18.0f;
    private static readonly GUIStyle kDropDownStyle = "ShurikenDropdown";
    private static readonly Color kMissingOverrideColor = new Color(1.0f, 0.11f, 0.11f, 1.0f);
    protected static readonly string kSetExposedPropertyMsg = "Set Exposed Property";
    protected static readonly string kClearExposedPropertyMsg = "Clear Exposed Property";
    internal  const string kVisualElementName = "ExposedReference";

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

    public BaseExposedPropertyDrawer()
    {
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

    protected abstract void OnRenderProperty(Rect position, ExposedReferenceObject item);

    public override void OnGUI(Rect position,
        UnityEditor.SerializedProperty prop,
        GUIContent label)
    {
        // The item is local to this call: a single drawer instance is shared by all elements of an
        // array (see PropertyHandlerCache.GetPropertyHash), so it must not outlive the property being drawn.
        var item = new ExposedReferenceObject(prop);

        Rect propertyFieldPosition = position;
        propertyFieldPosition.xMax = propertyFieldPosition.xMax - ExposedReferencePropertyDrawer.kDriveWidgetWidth;

        Rect driveFieldPosition = position;
        driveFieldPosition.x = propertyFieldPosition.xMax;
        driveFieldPosition.width = ExposedReferencePropertyDrawer.kDriveWidgetWidth;

        bool showContextMenu = item.exposedPropertyTable != null;

        var previousColor = GUI.color;
        var wasBoldDefaultFont = EditorGUIUtility.GetBoldDefaultFont();

        var valuePosition = DrawLabel(showContextMenu, label, position, item);
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        if (item.propertyMode == ExposedPropertyMode.DefaultValue || item.propertyMode == ExposedPropertyMode.NamedGUID)
        {
            OnRenderProperty(valuePosition, item);
        }
        else
        {
            valuePosition.width /= 2;
            EditorGUI.BeginChangeCheck();
            item.exposedPropertyNameString = EditorGUI.TextField(valuePosition, item.exposedPropertyNameString);
            if (EditorGUI.EndChangeCheck())
                item.exposedPropertyName.stringValue = item.exposedPropertyNameString;

            valuePosition.x += valuePosition.width;
            OnRenderProperty(valuePosition, item);
        }

        GUI.color = previousColor;
        EditorGUIUtility.SetBoldDefaultFont(wasBoldDefaultFont);

        if (showContextMenu && GUI.Button(driveFieldPosition, GUIContent.none, kDropDownStyle))
        {
            GenericMenu menu = new GenericMenu();
            PopulateContextMenu(menu, item);
            menu.ShowAsContext();
            Event.current.Use();
        }

        EditorGUI.indentLevel = indent;
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty prop)
    {
        // The item belongs to the field we are about to create, not to the drawer: all elements of an array
        // share one drawer instance, so storing it on the drawer would let each CreatePropertyGUI call for a
        // sibling element overwrite it, and every field would then write through the last element's exposed
        // name. PropertyField always rebuilds a custom drawer's GUI on rebind, so this can never go stale.
        var item = new ExposedReferenceObject(prop);

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
            value = item.currentReferenceValue,
            allowSceneObjects = item.exposedPropertyTable != null
        };

        obj.RegisterValueChangedCallback(evt => SetReference(item, obj, evt.newValue));
        obj.AddManipulator(new ContextualMenuManipulator(evt => BuildContextualMenu(evt, item)));
        obj.AddToClassList(ObjectField.alignedFieldUssClassName);

        // Track for Undo/Redo changes which can come from exposedPropertyTable
        Undo.UndoRedoCallback undoRedoCallback = () =>
        {
            item.UpdateValue();
            obj.SetValueWithoutNotify(item.currentReferenceValue);
            UpdateObjectField(item, obj);
        };

        // Track the property for external changed including Undo/Redo
        obj.TrackPropertyValue(prop, _ => undoRedoCallback());
        obj.RegisterCallback<AttachToPanelEvent>(evt => Undo.undoRedoPerformed += undoRedoCallback);
        obj.RegisterCallback<DetachFromPanelEvent>(evt => Undo.undoRedoPerformed -= undoRedoCallback);

        UpdateObjectField(item, obj);

        return obj;
    }

    void SetReference(ExposedReferenceObject item, ObjectField objectField, Object newValue)
    {
        SetReference(item, newValue);
        if (item.currentReferenceValue != newValue)
        {
            item.currentReferenceValue = newValue;

            //save the modified SerializedObject since we are bypassing the binding system
            item.exposedPropertyName.serializedObject.ApplyModifiedProperties();
            UpdateObjectField(item, objectField);
        }
    }

    static void UpdateObjectField(ExposedReferenceObject item, ObjectField objectField)
    {
        if (item.propertyMode == ExposedPropertyMode.DefaultValue)
        {
            // Set the serialized property so we can support drag and drop for the default value.
            objectField?.SetProperty(ObjectField.serializedPropertyKey, item.exposedPropertyDefault);
        }
        else
        {
            objectField?.ClearProperty(ObjectField.serializedPropertyKey);
        }
    }

    internal void SetReference(ExposedReferenceObject item, Object newValue)
    {
        bool isDefaultValueMode = item.propertyMode == ExposedPropertyMode.DefaultValue;
        if (isDefaultValueMode || item.propertyMode == ExposedPropertyMode.NamedGUID)
        {
            // We can directly assign to the exposed property default value if
            // * asset we are modifying is in the scene
            // * object we are assigning to the property is also an asset
            if (isDefaultValueMode && (!EditorUtility.IsPersistent(item.exposedPropertyDefault.serializedObject.targetObject) ||
                newValue == null || EditorUtility.IsPersistent(newValue)))
            {
                if (!EditorGUI.CheckForCrossSceneReferencing(
                        item.exposedPropertyDefault.serializedObject.targetObject, newValue))
                {
                    item.exposedPropertyDefault.objectReferenceValue = newValue;
                }
            }
            else
            {
                // If PropertyName already exists, re-use it UUM-25160
                if (String.IsNullOrEmpty(item.exposedPropertyNameString) || String.IsNullOrEmpty(item.exposedPropertyName.stringValue))
                {
                    var str = UnityEngine.GUID.Generate().ToString();
                    item.exposedPropertyNameString = str;
                    item.exposedPropertyName.stringValue = str;
                    item.propertyMode = ExposedPropertyMode.NamedGUID;
                }

                // Timeline uses ExposedReference to hold both exposed and regular references, make sure we handle them differently
                if (item.isExposedReference)
                    SetAsExposedReference(item, newValue);
                else
                    SetAsRegularReference(item, newValue);
            }
        }
        else
        {
            if (item.isExposedReference)
                SetAsExposedReference(item, newValue);
            else
                SetAsRegularReference(item, newValue);
        }
    }

    static void SetAsExposedReference(ExposedReferenceObject item, Object value)
    {
        Undo.RecordObject(item.exposedPropertyTable as UnityEngine.Object, kSetExposedPropertyMsg);
        item.exposedPropertyTable.SetReferenceValue(item.exposedPropertyNameString, value);
    }

    static void SetAsRegularReference(ExposedReferenceObject item, Object value)
    {
        if (item.currentReferenceValue)
            Undo.RecordObject(item.exposedPropertyDefault.serializedObject.targetObject, kSetExposedPropertyMsg);

        item.exposedPropertyDefault.objectReferenceValue = value;
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
    void BuildContextualMenu(ContextualMenuPopulateEvent evt, ExposedReferenceObject item)
    {
        if (item != null && item.exposedPropertyTable != null)
        {
            OverrideState currentOverrideState;
            var currentValue = item.Resolve(out currentOverrideState);

            if (item.currentOverrideState == OverrideState.DefaultValue)
            {
                evt.menu.AppendAction(ExposePropertyContent.text,
                    (userData) =>
                    {
                        ExposedReferencePropertyDrawer.SetReferenceValueMenuItem(item.exposedPropertyTable,
                            item.exposedPropertyName, currentValue);
                    });
            }
            else
            {
                evt.menu.AppendAction(UnexposePropertyContent.text, (userData) =>
                {
                    ExposedReferencePropertyDrawer.ClearReferenceValueMenuItem(item.exposedPropertyTable,
                        item.exposedPropertyName, new PropertyName(item.exposedPropertyName.stringValue));
                });
            }
        }

        evt.menu.AppendAction("Properties...",
            (userData) =>
            {
                UnityEditor.EditorUtility.OpenPropertyEditor(item.currentReferenceValue);
            });
    }
}

[CustomPropertyDrawer(typeof(ExposedReference<>))]
class ExposedReferencePropertyDrawer : BaseExposedPropertyDrawer
{
    protected override void OnRenderProperty(Rect position, ExposedReferenceObject item)
    {
        var propertyType = fieldInfo.FieldType;

        if (propertyType.IsArrayOrList())
        {
            propertyType = propertyType.GetArrayOrListElementType();
        }

        var typeOfExposedReference = propertyType.GetGenericArguments()[0];

        EditorGUI.BeginChangeCheck();
        var newValue = EditorGUI.ObjectField(position, item.currentReferenceValue, typeOfExposedReference,
            item.exposedPropertyTable != null);

        if (EditorGUI.EndChangeCheck())
        {
            SetReference(item, newValue);
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
        var guid = UnityEngine.GUID.Generate();
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
