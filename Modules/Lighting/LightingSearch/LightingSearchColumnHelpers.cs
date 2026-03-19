// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchColumnHelpers
    {
        internal static SearchColumn.GetterEntry CreateGameObjectGetter(Func<GameObject, object> getValue)
        {
            return args =>
            {
                var go = GetGameObject(args);
                return go == null ? null : getValue(go);
            };
        }

        internal static SearchColumn.GetterEntry CreateGameObjectGetter<TComponent>(Func<GameObject, object> getValue)
            where TComponent : Component
        {
            return args =>
            {
                var go = GetGameObject(args);
                if (go == null || !go.TryGetComponent<TComponent>(out _))
                    return null;
                return getValue(go);
            };
        }

        internal static SearchColumn.SetterEntry CreateGameObjectSetter(Action<GameObject, object> setValue, Func<object, bool> isValid)
        {
            return args =>
            {
                if (args.value == null || !isValid(args.value))
                    return;

                var go = GetGameObject(args);
                if (go == null)
                    return;

                setValue(go, args.value);
            };
        }

        internal static SearchColumn.GetterEntry CreateMaterialGetter(Func<Material, object> getValue)
        {
            return args =>
            {
                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                return material == null ? null : getValue(material);
            };
        }

        internal static SearchColumn.SetterEntry CreateMaterialSetter(Action<Material, object> setValue, Func<object, bool> isValid)
        {
            return args =>
            {
                if (args.value == null || !isValid(args.value))
                    return;

                var material = args.item.data as Material ?? args.item.ToObject<Material>();
                if (material == null)
                    return;

                setValue(material, args.value);
            };
        }

        internal static SearchColumn.GetterEntry CreateLightingSettingsGetter(Func<LightingSettings, object> getValue)
        {
            return args =>
            {
                var lightingSettings = args.item.data as LightingSettings ?? args.item.ToObject() as LightingSettings;
                return lightingSettings == null ? null : getValue(lightingSettings);
            };
        }

        internal static SearchColumn.SetterEntry CreateLightingSettingsSetter(Action<LightingSettings, object> setValue, Func<object, bool> isValid)
        {
            return args =>
            {
                if (args.value == null || !isValid(args.value))
                    return;

                var lightingSettings = args.item.data as LightingSettings ?? args.item.ToObject() as LightingSettings;
                if (lightingSettings == null)
                    return;

                setValue(lightingSettings, args.value);
            };
        }

        internal static SearchColumn.BindEntry CreateBinder<TVisual>(Action<TVisual, object> bind)
            where TVisual : VisualElement
        {
            return (args, ve) =>
            {
                if (args.value != null && ve is TVisual visual)
                {
                    visual.visible = true;
                    bind(visual, args.value);
                }
                else
                {
                    ve.visible = false;
                }
            };
        }

        internal static SearchColumn.BindEntry CreateBinderWithComponentCheck<TVisual, TComponent>(Action<TVisual, object> bind)
            where TVisual : VisualElement
            where TComponent : Component
        {
            return (args, ve) =>
            {
                var go = GetGameObject(args);
                if (go == null || !go.TryGetComponent<TComponent>(out _))
                {
                    ve.visible = false;
                    return;
                }

                if (args.value != null && ve is TVisual visual)
                {
                    visual.visible = true;
                    bind(visual, args.value);
                }
                else
                {
                    ve.visible = false;
                }
            };
        }

        internal static bool IsValidBool(object v) => v != null && (v is bool || v is int || v is long);
        internal static bool IsValidEnum(object v) => v != null && v.GetType().IsEnum;
        internal static bool IsValidInt(object v) => v is int || (v != null && (v is long || v is float));

        internal static GameObject GetGameObject(SearchColumnEventArgs args)
        {
            var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
            if (go != null)
                return go;
            var obj = args.item.ToObject();
            return (obj as Component)?.gameObject;
        }

        internal static object SelectFromGameObject(SearchSelectorArgs args, Func<GameObject, object> selector)
        {
            var go = args.current.ToObject<GameObject>();
            return go == null ? null : selector(go);
        }
    }
}
