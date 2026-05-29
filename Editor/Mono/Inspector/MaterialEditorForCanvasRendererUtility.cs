// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

using Object = UnityEngine.Object;
using Component = UnityEngine.Component;

[assembly: InternalsVisibleTo("UnityEditor.Shader.Tests")]
namespace UnityEditor
{
    internal static class MaterialEditorForCanvasRendererUtility
    {
        public static readonly Lazy<Type> s_tmpTextType = new(() => Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro"));
        public static readonly Lazy<PropertyInfo> s_tmpTextTypeMaterialProperty = new(() => s_tmpTextType.Value?.GetProperty("fontSharedMaterial"));
        public static readonly Lazy<PropertyInfo> s_tmpTextTypeFontProperty = new(() => s_tmpTextType.Value?.GetProperty("font"));
        public static readonly Lazy<PropertyInfo> s_tmpTextTypeAtlasTextureProperty = new(() => s_tmpTextTypeFontProperty.Value?.PropertyType.GetProperty("atlasTexture"));

        public static readonly Lazy<Type> s_graphicType = new(() => Type.GetType("UnityEngine.UI.Graphic, UnityEngine.UI"));
        public static readonly Lazy<PropertyInfo> s_graphicTypeMaterialProperty = new(() => s_graphicType.Value?.GetProperty("material"));

        public static Component s_previousDraggedUponGraphic;
        public static Material s_previousGraphicMaterial;
        public static bool s_previousGraphicAlreadyHadPrefabModification;

        public static bool IsTMProComponent(Component graphic)
        {
            if (graphic == null)
                return false;
            return s_tmpTextType.Value != null && s_tmpTextType.Value.IsAssignableFrom(graphic.GetType());
        }

        public static PropertyInfo GetGraphicMaterialPropertyInfo(bool isTMPro)
        {
            return isTMPro ? s_tmpTextTypeMaterialProperty.Value : s_graphicTypeMaterialProperty.Value;
        }

        public static string GetMaterialSerializedFieldName(bool isTMPro)
        {
            return isTMPro ? "m_sharedMaterial" : "m_Material";
        }

        /// <summary>
        /// Validates if a material is compatible with a TMPro component.
        /// A material is compatible if its _MainTex matches the TMPro font's atlas texture.
        /// </summary>
        /// <param name="graphic"> Graphic component being dragged upon </param>
        /// <param name="material"> Material object being dragged </param>
        /// <returns> True if the component is not TMPro, or if the material is compatible </returns>
        internal static bool ValidateMaterialForTMPro(Component graphic, Material material)
        {
            if (graphic == null || material == null)
                return false;

            if (!IsTMProComponent(graphic))
                return true;

            var fontAsset = s_tmpTextTypeFontProperty.Value?.GetValue(graphic);
            if (fontAsset == null)
                return true;

            var atlasTexture = s_tmpTextTypeAtlasTextureProperty.Value?.GetValue(fontAsset) as Texture;
            if (atlasTexture == null)
                return true;

            var materialTexture = material.HasProperty("_MainTex") ?  material.GetTexture("_MainTex") : null;
            return materialTexture != null && materialTexture == atlasTexture;
        }

        [RequiredByNativeCode]
        internal static bool ValidateMaterialForCanvasRendererTMPro(CanvasRenderer canvasRenderer, Material material)
        {
            if (canvasRenderer == null || s_graphicType.Value == null)
                return false;

            var go = canvasRenderer.gameObject;
            go.TryGetComponent(s_graphicType.Value, out var graphic);

            return ValidateMaterialForTMPro(graphic, material);
        }

        [RequiredByNativeCode]
        internal static void AssignMaterialToUIGraphic(CanvasRenderer canvasRenderer, Material material)
        {
            if (canvasRenderer == null)
                return;
            var go = canvasRenderer.gameObject;

            if (s_graphicType.Value != null)
            {
                if (go.TryGetComponent(s_graphicType.Value, out var graphic))
                {
                    // Reject non-TMPro materials for TMPro objects
                    if (!ValidateMaterialForTMPro(graphic, material))
                        return;

                    Undo.RecordObject(graphic, "Assign material");

                    // TMPro uses fontSharedMaterial (m_sharedMaterial) instead of Graphic.material (m_Material)
                    bool isTMPro = IsTMProComponent(graphic);
                    var materialProperty = GetGraphicMaterialPropertyInfo(isTMPro);
                    materialProperty?.SetValue(graphic, material);

                    EditorUtility.SetDirty(graphic);
                    return;
                }
            }
        }
        public static void RevertCanvasRendererDragChanges()
        {
            if (s_previousDraggedUponGraphic != null)
            {
                bool isTMPro = IsTMProComponent(s_previousDraggedUponGraphic);
                bool hasRevert = false;
                if (!s_previousGraphicAlreadyHadPrefabModification &&
                    PrefabUtility.GetPrefabInstanceStatus(s_previousDraggedUponGraphic) == PrefabInstanceStatus.Connected)
                {
                    using var matPropSO = new SerializedObject(s_previousDraggedUponGraphic);
                    var matProp = matPropSO.FindProperty(GetMaterialSerializedFieldName(isTMPro));
                    if (matProp != null)
                    {
                        PrefabUtility.RevertPropertyOverride(matProp, InteractionMode.AutomatedAction, false);
                        hasRevert = true;
                    }
                }
                if (!hasRevert)
                {
                    var materialProperty = GetGraphicMaterialPropertyInfo(isTMPro);
                    materialProperty?.SetValue(s_previousDraggedUponGraphic, s_previousGraphicMaterial);
                }
            }
            s_previousDraggedUponGraphic = null;
            s_previousGraphicMaterial = null;
        }

        internal static void HandleCanvasRenderer(CanvasRenderer canvasRenderer, Material material, EventType type, bool alt)
        {
            if (canvasRenderer == null || s_graphicType.Value == null)
                return;

            var go = canvasRenderer.gameObject;

            if (!go.TryGetComponent(s_graphicType.Value, out var graphic))
                return;

            var applyMaterial = false;
            switch (type)
            {
                case EventType.DragUpdated:
                    if (ValidateMaterialForTMPro(graphic, material))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        applyMaterial = true;
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }
                    break;

                case EventType.DragPerform:
                    if (ValidateMaterialForTMPro(graphic, material))
                    {
                        DragAndDrop.AcceptDrag();
                        applyMaterial = true;
                    }
                    RevertCanvasRendererDragChanges();
                    break;

                case EventType.DragExited:
                    RevertCanvasRendererDragChanges();
                    break;
            }

            if (applyMaterial)
            {
                bool isTMPro = IsTMProComponent(graphic);
                var materialProperty = GetGraphicMaterialPropertyInfo(isTMPro);

                if (type != EventType.DragPerform && s_previousDraggedUponGraphic != graphic)
                {
                    RevertCanvasRendererDragChanges();
                    s_previousDraggedUponGraphic = graphic;

                    using var so = new SerializedObject(graphic);
                    var matProp = so.FindProperty(GetMaterialSerializedFieldName(isTMPro));
                    s_previousGraphicMaterial = matProp != null ? matProp.objectReferenceValue as Material : null;

                    // Update prefab modification status cache for graphic
                    s_previousGraphicAlreadyHadPrefabModification = false;
                    if (PrefabUtility.GetPrefabInstanceStatus(s_previousDraggedUponGraphic) == PrefabInstanceStatus.Connected)
                    {
                        s_previousGraphicAlreadyHadPrefabModification = matProp != null && matProp.prefabOverride;
                    }
                    materialProperty?.SetValue(graphic, material);
                }

                else if (type == EventType.DragPerform)
                {
                    Undo.RecordObject(graphic, "Assign Material");
                    materialProperty?.SetValue(graphic, material);
                    EditorUtility.SetDirty(graphic);

                    // Sync material to CanvasRenderer immediately so the change is detected (Graphic's canvas rebuild is deferred) so Inspector updates material preview
                    canvasRenderer.materialCount = Mathf.Max(canvasRenderer.materialCount, 1);
                    canvasRenderer.SetMaterial(material, 0);

                    if (!isTMPro)
                    {
                        var mainTextureProp = s_graphicType.Value.GetProperty("mainTexture");
                        var mainTexture = mainTextureProp?.GetValue(graphic) as Texture;
                        if (mainTexture != null)
                            canvasRenderer.SetTexture(mainTexture);
                    }
                }
            }
        }
    }
} // namespace UnityEditor
