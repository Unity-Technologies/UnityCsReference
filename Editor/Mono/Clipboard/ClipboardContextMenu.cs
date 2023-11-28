// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

// ReSharper disable once CheckNamespace - we explicitly want UnityEditor namespace
namespace UnityEditor
{
    internal static class ClipboardContextMenu
    {
        // Adds Copy/Paste entries into the menu if not null,
        // or handles Copy/Paste commands in the given event if not null.
        internal static void SetupPropertyCopyPaste(SerializedProperty property, GenericMenu menu, Event evt)
        {
            var type = property.propertyType;
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault -- we don't support all the types
            switch (type)
            {
                case SerializedPropertyType.Vector2:
                    SetupAction(property, menu, evt,
                        p => Clipboard.vector2Value = p.vector2Value,
                        p => Clipboard.hasVector2,
                        p => p.vector2Value = Clipboard.vector2Value);
                    break;
                case SerializedPropertyType.Vector3:
                    SetupAction(property, menu, evt,
                        p => Clipboard.vector3Value = p.vector3Value,
                        p => Clipboard.hasVector3,
                        p => p.vector3Value = Clipboard.vector3Value);
                    break;
                case SerializedPropertyType.Vector4:
                    SetupAction(property, menu, evt,
                        p => Clipboard.vector4Value = p.vector4Value,
                        p => Clipboard.hasVector4,
                        p => p.vector4Value = Clipboard.vector4Value);
                    break;
                case SerializedPropertyType.Boolean:
                    SetupAction(property, menu, evt,
                        p => Clipboard.boolValue = p.boolValue,
                        p => Clipboard.hasBool,
                        p => p.boolValue = Clipboard.boolValue);
                    break;
                case SerializedPropertyType.Quaternion: SetupQuaternion(property, menu, evt); break;
                case SerializedPropertyType.ObjectReference: SetupObjectReference(property, menu, evt); break;
                case SerializedPropertyType.Color:
                    SetupAction(property, menu, evt,
                        p => Clipboard.colorValue = p.colorValue,
                        p => Clipboard.hasColor,
                        p => p.colorValue = Clipboard.colorValue);
                    break;
                case SerializedPropertyType.Gradient:
                    SetupAction(property, menu, evt,
                        p => Clipboard.gradientValue = p.gradientValue,
                        p => Clipboard.hasGradient,
                        delegate(SerializedProperty p) {
                            p.gradientValue = Clipboard.gradientValue;
                            UnityEditorInternal.GradientPreviewCache.ClearCache();
                        });
                    break;
                case SerializedPropertyType.AnimationCurve:
                    SetupAction(property, menu, evt,
                        p => Clipboard.animationCurveValue = p.animationCurveValue,
                        p => Clipboard.hasAnimationCurve,
                        p => p.animationCurveValue = Clipboard.animationCurveValue);
                    break;
                case SerializedPropertyType.Vector2Int:
                    SetupAction(property, menu, evt,
                        p => Clipboard.vector2Value = p.vector2IntValue,
                        p => Clipboard.hasVector2,
                        delegate(SerializedProperty p)
                        {
                            var vi = Clipboard.vector2Value;
                            p.vector2IntValue = new Vector2Int((int)vi.x, (int)vi.y);
                        });
                    break;
                case SerializedPropertyType.Vector3Int:
                    SetupAction(property, menu, evt,
                        p => Clipboard.vector3Value = p.vector3IntValue,
                        p => Clipboard.hasVector3,
                        delegate(SerializedProperty p)
                        {
                            var vi = Clipboard.vector3Value;
                            p.vector3IntValue = new Vector3Int((int)vi.x, (int)vi.y, (int)vi.z);
                        });
                    break;
                case SerializedPropertyType.Rect:
                    SetupAction(property, menu, evt,
                        p => Clipboard.rectValue = p.rectValue,
                        p => Clipboard.hasRect,
                        p => p.rectValue = Clipboard.rectValue);
                    break;
                case SerializedPropertyType.RectInt:
                    SetupAction(property, menu, evt,
                        delegate(SerializedProperty p)
                        {
                            var ri = p.rectIntValue;
                            Clipboard.rectValue = new Rect(ri.x, ri.y, ri.width, ri.height);
                        },
                        p => Clipboard.hasRect,
                        delegate(SerializedProperty p)
                        {
                            var r = Clipboard.rectValue;
                            p.rectIntValue = new RectInt((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                        });
                    break;
                case SerializedPropertyType.Bounds:
                    SetupAction(property, menu, evt,
                        p => Clipboard.boundsValue = p.boundsValue,
                        p => Clipboard.hasBounds,
                        p => p.boundsValue = Clipboard.boundsValue);
                    break;
                case SerializedPropertyType.BoundsInt:
                    SetupAction(property, menu, evt,
                        p =>
                        {
                            var b = p.boundsIntValue;
                            Clipboard.boundsValue = new Bounds(b.position, b.size * 2);
                        },
                        p => Clipboard.hasBounds,
                        p =>
                        {
                            var b = Clipboard.boundsValue;
                            p.boundsIntValue = new BoundsInt(new Vector3Int((int)b.center.x, (int)b.center.y, (int)b.center.z),
                                new Vector3Int((int)(b.size.x / 2), (int)(b.size.y / 2), (int)(b.size.z / 2)));
                        });
                    break;
                case SerializedPropertyType.LayerMask:
                    SetupAction(property, menu, evt,
                        p => Clipboard.layerMaskValue = p.intValue,
                        p => Clipboard.hasLayerMask,
                        p => p.intValue = Clipboard.layerMaskValue);
                    break;
                case SerializedPropertyType.RenderingLayerMask:
                    SetupAction(property, menu, evt,
                        p => Clipboard.renderingLayerMaskValue = p.uintValue,
                        p => Clipboard.hasRenderingLayerMask,
                        p => p.uintValue = Clipboard.renderingLayerMaskValue);
                    break;
                case SerializedPropertyType.Enum:
                    SetupAction(property, menu, evt,
                        Clipboard.SetEnumProperty,
                        Clipboard.HasEnumProperty,
                        Clipboard.GetEnumProperty);
                    break;
                case SerializedPropertyType.Hash128:
                    SetupAction(property, menu, evt,
                        p => Clipboard.hash128Value = p.hash128Value,
                        p => Clipboard.hasHash128,
                        p => p.hash128Value = Clipboard.hash128Value);
                    break;
                case SerializedPropertyType.Generic:
                    if (property.type == "MinMaxGradient")
                    {
                        SetupMinMaxGradient(property, menu, evt);
                    }
                    else if (property.type == "MinMaxCurve")
                    {
                        SetupMinMaxCurve(property, menu, evt);
                    }
                    else
                    {
                        if (property.isArray && property.arrayElementType == "managedReference<>") break;

                        SetupAction(property, menu, evt,
                            Clipboard.SetSerializedProperty,
                            p => Clipboard.HasSerializedProperty(),
                            Clipboard.GetSerializedProperty);
                    }
                    break;
                case SerializedPropertyType.Integer:
                    {
                        switch (property.numericType)
                        {
                            case SerializedPropertyNumericType.Int64:
                                SetupAction(property, menu, evt,
                                            p => Clipboard.longValue = p.longValue,
                                            p => Clipboard.hasLong,
                                            p => p.longValue = Clipboard.longValue);
                                break;

                            case SerializedPropertyNumericType.UInt64:
                                SetupAction(property, menu, evt,
                                            p => Clipboard.uLongValue = p.ulongValue,
                                            p => Clipboard.hasUlong,
                                            p => p.ulongValue = Clipboard.uLongValue);
                                break;

                            case SerializedPropertyNumericType.UInt32:
                                SetupAction(property, menu, evt,
                                            p => Clipboard.uIntValue = p.uintValue,
                                            p => Clipboard.hasUint,
                                            p => p.uintValue = Clipboard.uIntValue);
                                break;

                            case SerializedPropertyNumericType.UInt16:
                                SetupAction(property, menu, evt,
                                            p => Clipboard.uIntValue = (System.UInt16)p.uintValue,
                                            p => Clipboard.hasUint,
                                            p => p.uintValue = (System.UInt16)Clipboard.uIntValue);
                                break;

                            case SerializedPropertyNumericType.Int16:
                                SetupAction(property, menu, evt,
                                            p => Clipboard.integerValue = (System.Int16)p.intValue,
                                            p => Clipboard.hasInteger,
                                            p => p.intValue = (System.UInt16)Clipboard.integerValue);
                                break;
                            case SerializedPropertyNumericType.UInt8:
                                SetupAction(property, menu, evt,
                                            p => Clipboard.uIntValue = (System.Byte)p.uintValue,
                                            p => Clipboard.hasUint,
                                            p => p.uintValue = (System.Byte)Clipboard.uIntValue);
                                break;
                            case SerializedPropertyNumericType.Int8:
                                SetupAction(property, menu, evt,
                                            p => Clipboard.integerValue = (System.Byte)p.intValue,
                                            p => Clipboard.hasInteger,
                                            p => p.intValue = (System.Byte)Clipboard.integerValue);
                                break;

                            default:
                                SetupAction(property, menu, evt,
                                    p => Clipboard.integerValue = p.intValue,
                                    p => Clipboard.hasInteger,
                                    p => p.intValue = Clipboard.integerValue);
                                break;
                        }
                    }
                    break;
                case SerializedPropertyType.Float:
                    SetupAction(property, menu, evt,
                        p => Clipboard.floatValue = p.floatValue,
                        p => Clipboard.hasFloat,
                        p => p.floatValue = Clipboard.floatValue);
                    break;
                case SerializedPropertyType.String:
                    SetupAction(property, menu, evt,
                        p => Clipboard.stringValue = p.stringValue,
                        p => Clipboard.hasString,
                        p => p.stringValue = Clipboard.stringValue);
                    break;
                case SerializedPropertyType.Character:
                    SetupAction(property, menu, evt,
                        p => Clipboard.stringValue = ((char)p.intValue).ToString(),
                        p => Clipboard.hasString && Clipboard.stringValue.Length == 1,
                        p => p.intValue = Convert.ToChar(Clipboard.stringValue[0]));
                    break;
            }
        }

        static readonly GUIContent kCopyContent = EditorGUIUtility.TrTextContent("Copy");
        static readonly GUIContent kPasteContent = EditorGUIUtility.TrTextContent("Paste");
        static readonly GUIContent kCopyEulerContent = EditorGUIUtility.TrTextContent("Copy Euler Angles");
        static readonly GUIContent kCopyQuatContent = EditorGUIUtility.TrTextContent("Copy Quaternion");
        static readonly GUIContent kCopyPathContent = EditorGUIUtility.TrTextContent("Copy Path");
        static readonly GUIContent kCopyGuidContent = EditorGUIUtility.TrTextContent("Copy GUID");
        static readonly GUIContent kPasteMinColorContent = EditorGUIUtility.TrTextContent("Paste Min Color");
        static readonly GUIContent kPasteMaxColorContent = EditorGUIUtility.TrTextContent("Paste Max Color");
        static readonly GUIContent kPasteMinGradientContent = EditorGUIUtility.TrTextContent("Paste Min Gradient");
        static readonly GUIContent kPasteMaxGradientContent = EditorGUIUtility.TrTextContent("Paste Max Gradient");
        static readonly GUIContent kPasteMinScalarContent = EditorGUIUtility.TrTextContent("Paste Min Scalar");
        static readonly GUIContent kPasteMaxScalarContent = EditorGUIUtility.TrTextContent("Paste Max Scalar");
        static readonly GUIContent kPasteMinCurveContent = EditorGUIUtility.TrTextContent("Paste Min Curve");
        static readonly GUIContent kPasteMaxCurveContent = EditorGUIUtility.TrTextContent("Paste Max Curve");

        static void AddSeparator(GenericMenu menu)
        {
            if (menu.GetItemCount() > 0)
                menu.AddSeparator("");
        }

        internal static GUIContent overrideCopyContent { set; get; }
        internal static GUIContent overridePasteContent { set; get; }

        static void SetupAction(SerializedProperty property, GenericMenu menu, Event evt,
            Action<SerializedProperty> copyFunc,
            Func<SerializedProperty, bool> canPasteFunc,
            Action<SerializedProperty> pasteFunc)
        {
            var canCopy = !property.hasMultipleDifferentValues;
            var canPaste = GUI.enabled && canPasteFunc(property);

            if (menu != null)
            {
                AddSeparator(menu);

                var copyContent = overrideCopyContent ?? kCopyContent;
                if (canCopy)
                    menu.AddItem(copyContent, false, o => copyFunc((SerializedProperty)o), property);
                else
                    menu.AddDisabledItem(copyContent);

                var pasteContent = overridePasteContent ?? kPasteContent;
                if (canPaste)
                {
                    menu.AddItem(pasteContent, false,
                        delegate(object o)
                        {
                            var prop = (SerializedProperty)o;
                            pasteFunc(prop);
                            prop.serializedObject.ApplyModifiedProperties();
                            // Constrain proportions scale widget might need extra recalculation, notify if a paste
                            ConstrainProportionsTransformScale.NotifyPropertyPasted(prop.propertyPath);
                        }, property);
                }
                else
                {
                    menu.AddDisabledItem(pasteContent);
                }
            }

            if (evt != null)
            {
                if (canCopy && evt.commandName == EventCommandNames.Copy)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        copyFunc(property);
                        evt.Use();
                    }
                }
                if (canPaste && evt.commandName == EventCommandNames.Paste)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        pasteFunc(property);
                        property.serializedObject.ApplyModifiedProperties();
                        evt.Use();
                    }
                }
            }
        }

        static void PasteQuaternion(SerializedProperty p)
        {
            p.quaternionValue = Clipboard.hasQuaternion ? Clipboard.quaternionValue : Quaternion.Euler(Clipboard.vector3Value);
            p.serializedObject.ApplyModifiedProperties();
        }

        static void SetupQuaternion(SerializedProperty property, GenericMenu menu, Event evt)
        {
            var canCopy = !property.hasMultipleDifferentValues;
            var canPaste = GUI.enabled && (Clipboard.hasQuaternion || Clipboard.hasVector3);

            if (menu != null)
            {
                AddSeparator(menu);
                if (canCopy)
                {
                    menu.AddItem(kCopyEulerContent, false, o => Clipboard.vector3Value = ((SerializedProperty)o).quaternionValue.eulerAngles, property);
                    menu.AddItem(kCopyQuatContent, false, o => Clipboard.quaternionValue = ((SerializedProperty)o).quaternionValue, property);
                }
                else
                {
                    menu.AddDisabledItem(kCopyEulerContent);
                    menu.AddDisabledItem(kCopyQuatContent);
                }
                if (canPaste)
                    menu.AddItem(kPasteContent, false, o => PasteQuaternion((SerializedProperty)o), property);
                else
                    menu.AddDisabledItem(kPasteContent);
            }
            if (evt != null)
            {
                if (canCopy && evt.commandName == EventCommandNames.Copy)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        Clipboard.quaternionValue = property.quaternionValue;
                        evt.Use();
                    }
                }
                if (canPaste && evt.commandName == EventCommandNames.Paste)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        PasteQuaternion(property);
                        evt.Use();
                    }
                }
            }
        }

        static void SetupMinMaxGradient(SerializedProperty property, GenericMenu menu, Event evt)
        {
            var canCopy = !property.hasMultipleDifferentValues;
            var canPasteWhole = GUI.enabled && Clipboard.HasSerializedProperty();

            if (menu != null)
            {
                AddSeparator(menu);

                var copyContent = overrideCopyContent ?? kCopyContent;
                if (canCopy)
                    menu.AddItem(copyContent, false, o => Clipboard.SetSerializedProperty((SerializedProperty)o), property);
                else
                    menu.AddDisabledItem(copyContent);

                var pasteContent = overridePasteContent ?? kPasteContent;

                if (canPasteWhole)
                {
                    menu.AddItem(pasteContent, false,
                        delegate (object o)
                        {
                            var prop = (SerializedProperty)o;
                            Clipboard.GetSerializedProperty(prop);
                            prop.serializedObject.ApplyModifiedProperties();
                        }, property);
                }
                else if (GUI.enabled && Clipboard.hasColor)
                {
                    MinMaxGradientState state = (MinMaxGradientState)property.FindPropertyRelative("minMaxState").intValue;
                    if (state == MinMaxGradientState.k_Color)
                    {
                        AddPasteColorItem(property.FindPropertyRelative("maxColor"), menu, pasteContent);
                    }
                    else if (state == MinMaxGradientState.k_RandomBetweenTwoColors)
                    {
                        // Allow the user to choose whether to paste the color on their clipboard to either the max or min
                        AddPasteColorItem(property.FindPropertyRelative("maxColor"), menu, kPasteMaxColorContent);
                        AddPasteColorItem(property.FindPropertyRelative("minColor"), menu, kPasteMinColorContent);
                    }
                    else
                    {
                        menu.AddDisabledItem(pasteContent);
                    }
                }
                else if (GUI.enabled && Clipboard.hasGradient)
                {
                    MinMaxGradientState state = (MinMaxGradientState)property.FindPropertyRelative("minMaxState").intValue;
                    if (state == MinMaxGradientState.k_Gradient || state == MinMaxGradientState.k_RandomColor)
                    {
                        AddPasteGradientItem(property.FindPropertyRelative("maxGradient"), menu, pasteContent);
                    }
                    else if (state == MinMaxGradientState.k_RandomBetweenTwoGradients)
                    {
                        // Allow the user to choose whether to paste the gradient on their clipboard to either the max or min
                        AddPasteGradientItem(property.FindPropertyRelative("maxGradient"), menu, kPasteMaxGradientContent);
                        AddPasteGradientItem(property.FindPropertyRelative("minGradient"), menu, kPasteMinGradientContent);
                    }
                    else
                    {
                        menu.AddDisabledItem(pasteContent);
                    }
                }
                else
                {
                    menu.AddDisabledItem(pasteContent);
                }
            }
            if (evt != null)
            {
                if (canCopy && evt.commandName == EventCommandNames.Copy)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        Clipboard.SetSerializedProperty(property);
                        evt.Use();
                    }
                }
                if (canPasteWhole && evt.commandName == EventCommandNames.Paste)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        Clipboard.GetSerializedProperty(property);
                        property.serializedObject.ApplyModifiedProperties();
                        evt.Use();
                    }
                }
            }
        }
        static void SetupMinMaxCurve(SerializedProperty property, GenericMenu menu, Event evt)
        {
            var canCopy = !property.hasMultipleDifferentValues;
            var canPasteWhole = GUI.enabled && Clipboard.HasSerializedProperty();

            if (menu != null)
            {
                AddSeparator(menu);

                var copyContent = overrideCopyContent ?? kCopyContent;
                if (canCopy)
                    menu.AddItem(copyContent, false, o => Clipboard.SetSerializedProperty((SerializedProperty)o), property);
                else
                    menu.AddDisabledItem(copyContent);

                var pasteContent = overridePasteContent ?? kPasteContent;

                if (canPasteWhole)
                {
                    menu.AddItem(pasteContent, false,
                        delegate (object o)
                        {
                            var prop = (SerializedProperty)o;
                            Clipboard.GetSerializedProperty(prop);
                            prop.serializedObject.ApplyModifiedProperties();
                        }, property);
                }
                else if (GUI.enabled && Clipboard.hasFloat)
                {
                    MinMaxCurveState state = (MinMaxCurveState)property.FindPropertyRelative("minMaxState").intValue;
                    if (state == MinMaxCurveState.k_Scalar)
                    {
                        AddPasteFloatItem(property.FindPropertyRelative("scalar"), menu, pasteContent);
                    }
                    else if (state == MinMaxCurveState.k_TwoScalars)
                    {
                        // Allow the user to choose whether to paste the float on their clipboard to either the min or max
                        AddPasteFloatItem(property.FindPropertyRelative("minScalar"), menu, kPasteMinScalarContent);
                        AddPasteFloatItem(property.FindPropertyRelative("scalar"), menu, kPasteMaxScalarContent);
                    }
                    else
                    {
                        menu.AddDisabledItem(pasteContent);
                    }
                }
                else if (GUI.enabled && Clipboard.hasAnimationCurve)
                {
                    MinMaxCurveState state = (MinMaxCurveState)property.FindPropertyRelative("minMaxState").intValue;
                    if (state == MinMaxCurveState.k_Curve)
                    {
                        AddPasteCurveItem(property.FindPropertyRelative("maxCurve"), menu, pasteContent);
                    }
                    else if (state == MinMaxCurveState.k_TwoCurves)
                    {
                        // Allow the user to choose whether to paste the color on their clipboard to either the max or min
                        AddPasteCurveItem(property.FindPropertyRelative("maxCurve"), menu, kPasteMaxCurveContent);
                        AddPasteCurveItem(property.FindPropertyRelative("minCurve"), menu, kPasteMinCurveContent);
                    }
                    else
                    {
                        menu.AddDisabledItem(pasteContent);
                    }
                }
                else
                {
                    menu.AddDisabledItem(pasteContent);
                }
            }
            if (evt != null)
            {
                if (canCopy && evt.commandName == EventCommandNames.Copy)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        Clipboard.SetSerializedProperty(property);
                        evt.Use();
                    }
                }
                if (canPasteWhole && evt.commandName == EventCommandNames.Paste)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        Clipboard.GetSerializedProperty(property);
                        property.serializedObject.ApplyModifiedProperties();
                        evt.Use();
                    }
                }
            }
        }

        static void AddPasteColorItem(SerializedProperty property, GenericMenu menu, GUIContent menuItemContent)
        {
            menu.AddItem(menuItemContent, false,
            delegate (object o)
            {
                var prop = (SerializedProperty)o;
                prop.colorValue = Clipboard.colorValue;
                prop.serializedObject.ApplyModifiedProperties();
            }, property);
        }

        static void AddPasteGradientItem(SerializedProperty property, GenericMenu menu, GUIContent menuItemContent)
        {
            menu.AddItem(menuItemContent, false,
            delegate (object o)
            {
                var prop = (SerializedProperty)o;
                prop.gradientValue = Clipboard.gradientValue;
                prop.serializedObject.ApplyModifiedProperties();
                UnityEditorInternal.GradientPreviewCache.ClearCache();
            }, property);
        }
        static void AddPasteFloatItem(SerializedProperty property, GenericMenu menu, GUIContent menuItemContent)
        {
            menu.AddItem(menuItemContent, false,
            delegate (object o)
            {
                var prop = (SerializedProperty)o;
                prop.floatValue = Clipboard.floatValue;
                prop.serializedObject.ApplyModifiedProperties();
            }, property);
        }
        static void AddPasteCurveItem(SerializedProperty property, GenericMenu menu, GUIContent menuItemContent)
        {
            menu.AddItem(menuItemContent, false,
            delegate (object o)
            {
                var prop = (SerializedProperty)o;
                prop.animationCurveValue = Clipboard.animationCurveValue;
                prop.serializedObject.ApplyModifiedProperties();
                UnityEditorInternal.AnimationCurvePreviewCache.ClearCache();
            }, property);
        }

        static void PasteObjectReference(SerializedProperty p)
        {
            if (Clipboard.hasObject)
                p.objectReferenceValue = Clipboard.objectValue;
            else if (Clipboard.hasGuid)
                p.objectReferenceValue = AssetDatabase.LoadMainAssetAtGUID(Clipboard.guidValue);
            else if (!new GUID(AssetDatabase.AssetPathToGUID(Clipboard.stringValue)).Empty())
                p.objectReferenceValue = AssetDatabase.LoadMainAssetAtPath(Clipboard.stringValue);
            p.serializedObject.ApplyModifiedProperties();
        }

        static void SetupObjectReference(SerializedProperty property, GenericMenu menu, Event evt)
        {
            var hasPasteObject = Clipboard.hasObject;
            var hasPasteGuid = Clipboard.hasGuid;
            var hasPastePath = !new GUID(AssetDatabase.AssetPathToGUID(Clipboard.stringValue)).Empty();
            var obj = property.objectReferenceValue;

            var canCopy = !property.hasMultipleDifferentValues && obj != null;
            var canPaste = GUI.enabled && (hasPasteGuid || hasPastePath || hasPasteObject);

            if (menu != null)
            {
                AddSeparator(menu);
                var hasAsset = canCopy && AssetDatabase.Contains(obj);
                if (canCopy)
                    menu.AddItem(kCopyContent, false, o => Clipboard.objectValue = (UnityEngine.Object)o, obj);
                else
                    menu.AddDisabledItem(kCopyContent);
                if (hasAsset)
                {
                    menu.AddItem(kCopyPathContent, false, o => Clipboard.stringValue = AssetDatabase.GetAssetPath((UnityEngine.Object)o).ToString(), obj);
                    menu.AddItem(kCopyGuidContent, false, o => Clipboard.guidValue = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath((UnityEngine.Object)o))), obj);
                }
                else
                {
                    menu.AddDisabledItem(kCopyPathContent);
                    menu.AddDisabledItem(kCopyGuidContent);
                }

                if (canPaste)
                    menu.AddItem(kPasteContent, false, o => PasteObjectReference((SerializedProperty)o), property);
                else
                    menu.AddDisabledItem(kPasteContent);
            }

            if (evt != null)
            {
                if (canCopy && evt.commandName == EventCommandNames.Copy)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        Clipboard.objectValue = obj;
                        evt.Use();
                    }
                }
                if (canPaste && evt.commandName == EventCommandNames.Paste)
                {
                    if (evt.type == EventType.ValidateCommand)
                        evt.Use();
                    if (evt.type == EventType.ExecuteCommand)
                    {
                        PasteObjectReference(property);
                        evt.Use();
                    }
                }
            }
        }

        //
        // Clipboard related Transform component context menus
        //

        [MenuItem("CONTEXT/Transform/Copy/Position", false, 100100)] // ReSharper disable once UnusedMember.Local
        static void CopyTransformPositionMenu(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null)
                return;
            Clipboard.vector3Value = tr.localPosition;
        }

        [MenuItem("CONTEXT/Transform/Paste/Position", true)] // ReSharper disable once UnusedMember.Local
        static bool PasteTransformPositionMenuValidate(MenuCommand command)
        {
            var tr = command.context as Transform;
            return tr != null && Clipboard.hasVector3 && IsUserModifiable(tr);
        }

        [MenuItem("CONTEXT/Transform/Paste/Position", false, 100100)] // ReSharper disable once UnusedMember.Local
        static void PasteTransformPositionMenu(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null)
                return;
            Undo.RecordObject(tr, $"Paste {tr.gameObject.name} Position");
            tr.localPosition = Clipboard.vector3Value;
        }

        [MenuItem("CONTEXT/Transform/Copy/Rotation", false, 100102)] // ReSharper disable once UnusedMember.Local
        static void CopyTransformRotationMenu(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null)
                return;
            Clipboard.vector3Value = tr.localEulerAngles;
        }

        [MenuItem("CONTEXT/Transform/Paste/Rotation", true)] // ReSharper disable once UnusedMember.Local
        static bool PasteTransformRotationMenuValidate(MenuCommand command)
        {
            var tr = command.context as Transform;
            return tr != null && (Clipboard.hasVector3 || Clipboard.hasQuaternion) && IsUserModifiable(tr);
        }

        [MenuItem("CONTEXT/Transform/Paste/Rotation", false, 100102)] // ReSharper disable once UnusedMember.Local
        static void PasteTransformRotationMenu(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null)
                return;
            Undo.RecordObject(tr, $"Paste {tr.gameObject.name} Rotation");
            if (Clipboard.hasQuaternion)
                tr.localRotation = Clipboard.quaternionValue;
            else
                tr.localEulerAngles = Clipboard.vector3Value;
        }

        [MenuItem("CONTEXT/Transform/Copy/Scale", false, 100103)] // ReSharper disable once UnusedMember.Local
        static void CopyTransformScaleMenu(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null)
                return;
            Clipboard.vector3Value = tr.localScale;
        }

        [MenuItem("CONTEXT/Transform/Paste/Scale", true)] // ReSharper disable once UnusedMember.Local
        static bool PasteTransformScaleMenuValidate(MenuCommand command)
        {
            var tr = command.context as Transform;
            return tr != null && Clipboard.hasVector3 && IsUserModifiable(tr);
        }

        [MenuItem("CONTEXT/Transform/Paste/Scale", false, 100103)] // ReSharper disable once UnusedMember.Local
        static void PasteTransformScaleMenu(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null)
                return;
            Undo.RecordObject(tr, $"Paste {tr.gameObject.name} Scale");
            tr.localScale = Clipboard.vector3Value;
        }

        [MenuItem("CONTEXT/Transform/Copy/World Transform", false, 100150)] // ReSharper disable once UnusedMember.Local
        static void CopyTransformWorldPlacementMenu(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null)
                return;
            Clipboard.SetCustomValue(new TransformWorldPlacement(tr));
        }

        [MenuItem("CONTEXT/Transform/Paste/World Transform", true)] // ReSharper disable once UnusedMember.Local
        static bool PasteTransformWorldPlacementMenuValidate(MenuCommand command)
        {
            var tr = command.context as Transform;
            return tr != null && IsUserModifiable(tr) && Clipboard.HasCustomValue<TransformWorldPlacement>();
        }

        [MenuItem("CONTEXT/Transform/Paste/World Transform", false, 100150)] // ReSharper disable once UnusedMember.Local
        static void PasteTransformWorldPlacementMenu(MenuCommand command)
        {
            var tr = command.context as Transform;
            if (tr == null)
                return;
            Undo.RecordObject(tr, $"Paste {tr.gameObject.name} World Placement");
            var placement = Clipboard.GetCustomValue<TransformWorldPlacement>();
            tr.position = placement.position;
            tr.rotation = placement.rotation;
            tr.localScale = placement.scale;
        }

        //@TODO This really should be a helper utility somewhere else
        static bool IsUserModifiable(UnityEngine.Object o)
        {
            if (o == null)
                return false;
            if ((o.hideFlags & HideFlags.NotEditable) != 0)
                return false;
            if (EditorUtility.IsPersistent(o))
            {
                if (PrefabUtility.IsPartOfImmutablePrefab(o))
                    return false;
                if (!Editor.IsAppropriateFileOpenForEdit(o))
                    return false;
            }
            return true;
        }
    }

    [Serializable]
    internal class TransformWorldPlacement
    {
        public TransformWorldPlacement()
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
        }

        public TransformWorldPlacement(Transform t)
        {
            position = t.position;
            rotation = t.rotation;
            scale = t.localScale;
        }

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
}
