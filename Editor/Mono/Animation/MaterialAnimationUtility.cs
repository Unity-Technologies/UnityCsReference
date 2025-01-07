// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ShaderPropertyType = UnityEngine.Rendering.ShaderPropertyType;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    static internal class MaterialAnimationUtility
    {
        const string kMaterialPrefix = "material.";

        static PropertyModification[] CreatePropertyModifications(int count, Object target)
        {
            PropertyModification[] modifications = new PropertyModification[count];
            for (int i = 0; i < modifications.Length; i++)
            {
                modifications[i] = new PropertyModification();
                modifications[i].target = target;
            }
            return modifications;
        }

        static void SetupPropertyModification(string name, float value, PropertyModification prop)
        {
            prop.propertyPath = kMaterialPrefix + name;
            prop.value = value.ToString();
        }

        static PropertyModification[] MaterialPropertyToPropertyModifications(MaterialProperty materialProp, Object target, float value)
        {
            PropertyModification[] modifications = CreatePropertyModifications(1, target);
            SetupPropertyModification(materialProp.name, value, modifications[0]);
            return modifications;
        }

        static PropertyModification[] MaterialPropertyToPropertyModifications(MaterialProperty materialProp, Object target, Color color)
        {
            PropertyModification[] modifications = CreatePropertyModifications(4, target);
            SetupPropertyModification(materialProp.name + ".r", color.r, modifications[0]);
            SetupPropertyModification(materialProp.name + ".g", color.g, modifications[1]);
            SetupPropertyModification(materialProp.name + ".b", color.b, modifications[2]);
            SetupPropertyModification(materialProp.name + ".a", color.a, modifications[3]);
            return modifications;
        }

        static PropertyModification[] MaterialPropertyToPropertyModifications(MaterialProperty materialProp, Object target, Vector4 vec)
        {
            return MaterialPropertyToPropertyModifications(materialProp.name, target, vec);
        }

        static PropertyModification[] MaterialPropertyToPropertyModifications(string name, Object target, Vector4 vec)
        {
            PropertyModification[] modifications = CreatePropertyModifications(4, target);
            SetupPropertyModification(name + ".x", vec.x, modifications[0]);
            SetupPropertyModification(name + ".y", vec.y, modifications[1]);
            SetupPropertyModification(name + ".z", vec.z, modifications[2]);
            SetupPropertyModification(name + ".w", vec.w, modifications[3]);
            return modifications;
        }

        static bool ApplyMaterialModificationToAnimationRecording(PropertyModification[] previousModifications, PropertyModification[] currentModifications)
        {
            UndoPropertyModification[] undoModifications = new UndoPropertyModification[previousModifications.Length];
            for (int i = 0; i < undoModifications.Length; ++i)
            {
                undoModifications[i].previousValue = previousModifications[i];
                undoModifications[i].currentValue = currentModifications[i];
            }

            UndoPropertyModification[] ret = Undo.InvokePostprocessModifications(undoModifications);
            return ret.Length != undoModifications.Length;
        }

        static public bool OverridePropertyColor(MaterialProperty materialProp, Renderer target, out Color color)
        {
            var propertyPaths = new List<string>();
            string basePropertyPath = kMaterialPrefix + materialProp.name;

            if (materialProp.propertyType == ShaderPropertyType.Texture)
            {
                propertyPaths.Add(basePropertyPath + "_ST.x");
                propertyPaths.Add(basePropertyPath + "_ST.y");
                propertyPaths.Add(basePropertyPath + "_ST.z");
                propertyPaths.Add(basePropertyPath + "_ST.w");
            }
            else if (materialProp.propertyType == ShaderPropertyType.Color)
            {
                propertyPaths.Add(basePropertyPath + ".r");
                propertyPaths.Add(basePropertyPath + ".g");
                propertyPaths.Add(basePropertyPath + ".b");
                propertyPaths.Add(basePropertyPath + ".a");
            }
            else if (materialProp.propertyType == ShaderPropertyType.Vector)
            {
                propertyPaths.Add(basePropertyPath + ".x");
                propertyPaths.Add(basePropertyPath + ".y");
                propertyPaths.Add(basePropertyPath + ".z");
                propertyPaths.Add(basePropertyPath + ".w");
            }
            else
            {
                propertyPaths.Add(basePropertyPath);
            }

            if (propertyPaths.Exists(path => AnimationMode.IsPropertyAnimated(target, path)))
            {
                color = AnimationMode.animatedPropertyColor;
                if (AnimationMode.InAnimationRecording())
                    color = AnimationMode.recordedPropertyColor;
                else if (propertyPaths.Exists(path => AnimationMode.IsPropertyCandidate(target, path)))
                    color = AnimationMode.candidatePropertyColor;

                return true;
            }

            color = Color.white;
            return false;
        }

        static public void SetupMaterialPropertyBlock(MaterialProperty materialProp, int changedMask, Renderer target)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            materialProp.WriteToMaterialPropertyBlock(block, changedMask);
            target.SetPropertyBlock(block);
        }

        static public void TearDownMaterialPropertyBlock(Renderer target)
        {
            target.SetPropertyBlock(null);
        }

        static public bool ApplyMaterialModificationToAnimationRecording(MaterialProperty materialProp, int changedMask, Renderer target, object oldValue)
        {
            bool applied = false;
            switch (materialProp.propertyType)
            {
                case ShaderPropertyType.Color:
                    SetupMaterialPropertyBlock(materialProp, changedMask, target);
                    applied = ApplyMaterialModificationToAnimationRecording(MaterialPropertyToPropertyModifications(materialProp, target, (Color)oldValue), MaterialPropertyToPropertyModifications(materialProp, target, materialProp.colorValue));
                    if (!applied)
                        TearDownMaterialPropertyBlock(target);
                    return applied;

                case ShaderPropertyType.Vector:
                    SetupMaterialPropertyBlock(materialProp, changedMask, target);
                    applied = ApplyMaterialModificationToAnimationRecording(MaterialPropertyToPropertyModifications(materialProp, target, (Vector4)oldValue), MaterialPropertyToPropertyModifications(materialProp, target, materialProp.vectorValue));
                    if (!applied)
                        TearDownMaterialPropertyBlock(target);
                    return applied;

                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    SetupMaterialPropertyBlock(materialProp, changedMask, target);
                    applied = ApplyMaterialModificationToAnimationRecording(MaterialPropertyToPropertyModifications(materialProp, target, (float)oldValue), MaterialPropertyToPropertyModifications(materialProp, target, materialProp.floatValue));
                    if (!applied)
                        TearDownMaterialPropertyBlock(target);
                    return applied;

                case ShaderPropertyType.Texture:
                {
                    if (MaterialProperty.IsTextureOffsetAndScaleChangedMask(changedMask))
                    {
                        string name = materialProp.name + "_ST";
                        SetupMaterialPropertyBlock(materialProp, changedMask, target);
                        applied = ApplyMaterialModificationToAnimationRecording(MaterialPropertyToPropertyModifications(name, target, (Vector4)oldValue), MaterialPropertyToPropertyModifications(name, target, materialProp.textureScaleAndOffset));
                        if (!applied)
                            TearDownMaterialPropertyBlock(target);
                        return applied;
                    }
                    else
                        return false;
                }
            }

            return false;
        }

        static public PropertyModification[] MaterialPropertyToPropertyModifications(MaterialProperty materialProp, Renderer target)
        {
            switch (materialProp.propertyType)
            {
                case ShaderPropertyType.Color:
                    return MaterialPropertyToPropertyModifications(materialProp, target, materialProp.colorValue);
                case ShaderPropertyType.Vector:
                    return MaterialPropertyToPropertyModifications(materialProp, target, materialProp.vectorValue);
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    return MaterialPropertyToPropertyModifications(materialProp, target, materialProp.floatValue);

                case ShaderPropertyType.Texture:
                {
                    string name = materialProp.name + "_ST";
                    return MaterialPropertyToPropertyModifications(name, target, materialProp.vectorValue);
                }
            }

            return null;
        }
    }
}
