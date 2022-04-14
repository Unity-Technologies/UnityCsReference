// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Object = UnityEngine.Object;
using ShaderPropertyType = UnityEngine.Rendering.ShaderPropertyType;
using ShaderPropertyFlags = UnityEngine.Rendering.ShaderPropertyFlags;

namespace UnityEditor
{
    // match MonoMaterialProperty layout!
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class MaterialProperty
    {
        public delegate bool ApplyPropertyCallback(MaterialProperty prop, int changeMask, object previousValue);

        private Object[] m_Targets;
        private ApplyPropertyCallback m_ApplyPropertyCallback;
        private string m_Name;
        private string m_DisplayName;
        private System.Object m_Value;
        private Vector4 m_TextureScaleAndOffset;
        private Vector2 m_RangeLimits;
        private ShaderPropertyType m_Type;
        private ShaderPropertyFlags m_Flags;
        private UnityEngine.Rendering.TextureDimension m_TextureDimension;
        private int m_MixedValueMask;


        public Object[] targets { get { return m_Targets; } }
        public PropType type { get { return (PropType)m_Type; } }
        public string name { get { return m_Name; } }
        public string displayName { get { return m_DisplayName; } }
        public PropFlags flags { get { return (PropFlags)m_Flags; } }
        public UnityEngine.Rendering.TextureDimension textureDimension { get { return m_TextureDimension; } }
        public Vector2 rangeLimits { get { return m_RangeLimits; } }
        public bool hasMixedValue { get { return (m_MixedValueMask & 1) != 0; } }
        public ApplyPropertyCallback applyPropertyCallback { get { return m_ApplyPropertyCallback; }  set { m_ApplyPropertyCallback = value; } }

        // Textures have 5 different mixed values for texture + UV scale/offset
        internal int mixedValueMask { get { return m_MixedValueMask; } }

        public void ReadFromMaterialPropertyBlock(MaterialPropertyBlock block)
        {
            ShaderUtil.ApplyMaterialPropertyBlockToMaterialProperty(block, this);
        }

        public void WriteToMaterialPropertyBlock(MaterialPropertyBlock materialblock, int changedPropertyMask)
        {
            ShaderUtil.ApplyMaterialPropertyToMaterialPropertyBlock(this, changedPropertyMask, materialblock);
        }

        public Color colorValue
        {
            get
            {
                if (m_Type == ShaderPropertyType.Color)
                    return (Color)m_Value;
                return Color.black;
            }
            set
            {
                if (m_Type != ShaderPropertyType.Color)
                    return;
                if (!hasMixedValue && value == (Color)m_Value)
                    return;

                ApplyProperty(value);
            }
        }

        public Vector4 vectorValue
        {
            get
            {
                if (m_Type == ShaderPropertyType.Vector)
                    return (Vector4)m_Value;
                return Vector4.zero;
            }
            set
            {
                if (m_Type != ShaderPropertyType.Vector)
                    return;
                if (!hasMixedValue && value == (Vector4)m_Value)
                    return;

                ApplyProperty(value);
            }
        }

        internal static bool IsTextureOffsetAndScaleChangedMask(int changedMask)
        {
            changedMask >>= 1;
            return changedMask != 0;
        }

        public float floatValue
        {
            get
            {
                if (m_Type == ShaderPropertyType.Float || m_Type == ShaderPropertyType.Range)
                    return (float)m_Value;
                return 0.0f;
            }
            set
            {
                if (m_Type != ShaderPropertyType.Float && m_Type != ShaderPropertyType.Range)
                    return;
                if (!hasMixedValue && value == (float)m_Value)
                    return;

                ApplyProperty(value);
            }
        }

        public int intValue
        {
            get
            {
                if (m_Type == ShaderPropertyType.Int)
                    return (int)m_Value;
                return 0;
            }
            set
            {
                if (m_Type != ShaderPropertyType.Int)
                    return;
                if (!hasMixedValue && value == (int)m_Value)
                    return;

                ApplyProperty(value);
            }
        }

        public Texture textureValue
        {
            get
            {
                if (m_Type == ShaderPropertyType.Texture)
                    return (Texture)m_Value;
                return null;
            }
            set
            {
                if (m_Type != ShaderPropertyType.Texture)
                    return;
                if (!hasMixedValue && value == (Texture)m_Value)
                    return;

                m_MixedValueMask &= ~1;
                object previousValue = m_Value;
                m_Value = value;

                ApplyProperty(previousValue, 1);
            }
        }

        public Vector4 textureScaleAndOffset
        {
            get
            {
                if (m_Type == ShaderPropertyType.Texture)
                    return m_TextureScaleAndOffset;
                return Vector4.zero;
            }
            set
            {
                if (m_Type != ShaderPropertyType.Texture)
                    return;
                if (!hasMixedValue && value == m_TextureScaleAndOffset)
                    return;

                m_MixedValueMask &= 1;
                int changedMask = 0;
                for (int c = 1; c < 5; c++)
                    changedMask |= 1 << c;

                object previousValue = m_TextureScaleAndOffset;
                m_TextureScaleAndOffset = value;
                ApplyProperty(previousValue, changedMask);
            }
        }

        private void ApplyProperty(object newValue)
        {
            m_MixedValueMask = 0;
            object previousValue = m_Value;
            m_Value = newValue;
            ApplyProperty(previousValue, 1);
        }

        private void ApplyProperty(object previousValue, int changedPropertyMask)
        {
            if (targets == null || targets.Length == 0)
                throw new ArgumentException("No material targets provided");

            Object[] mats = targets;
            string targetTitle;
            if (mats.Length == 1)
                targetTitle = mats[0].name;
            else
                targetTitle = mats.Length + " " + ObjectNames.NicifyVariableName(ObjectNames.GetClassName(mats[0])) + "s";

            //@TODO: Maybe all this logic should be moved to C++
            // reduces api surface...
            bool didApply = false;
            if (m_ApplyPropertyCallback != null)
                didApply = m_ApplyPropertyCallback(this, changedPropertyMask, previousValue);

            if (!didApply)
                ShaderUtil.ApplyProperty(this, changedPropertyMask, "Modify " + displayName + " of " + targetTitle);
        }

        // -------- helper functions to handle material variant overrides
        // It displays the override bar on the left, the lock icon, and the bold font
        // It also creates the context menu when left clicking a property

        private static class Styles
        {
            public static string revertMultiText = L10n.Tr("Revert on {0} Material(s)");
            public static string applyToMaterialText = L10n.Tr("Apply to Material '{0}'");
            public static string applyToVariantText = L10n.Tr("Apply as Override in Variant '{0}'");

            static Color overrideLineColor_l = new Color32(0x09, 0x09, 0x09, 0xFF);
            static Color overrideLineColor_d = new Color32(0xC4, 0xC4, 0xC4, 0xFF);
            public static Color overrideLineColor { get { return EditorGUIUtility.isProSkin ? overrideLineColor_d : overrideLineColor_l; } }

            public static readonly GUIContent revertContent = EditorGUIUtility.TrTextContent("Revert");
            public static readonly GUIContent revertAllContent = EditorGUIUtility.TrTextContent("Revert all Overrides");
            public static readonly GUIContent lockContent = EditorGUIUtility.TrTextContent("Lock in children");
            public static readonly GUIContent lockOriginContent = EditorGUIUtility.TrTextContent("See lock origin");

            public static readonly GUIContent resetContent = EditorGUIUtility.TrTextContent("Reset");
            public static readonly GUIContent copyContent = EditorGUIUtility.TrTextContent("Copy");
            public static readonly GUIContent pasteContent = EditorGUIUtility.TrTextContent("Paste");

            static readonly Texture lockInChildrenIcon = EditorGUIUtility.IconContent("HierarchyLock").image;
            public static readonly GUIContent lockInChildrenContent = EditorGUIUtility.TrTextContent(string.Empty, "Locked properties cannot be overriden by a child.", lockInChildrenIcon);

            static readonly Texture lockedByAncestorIcon = EditorGUIUtility.IconContent("IN LockButton on").image;
            public static readonly GUIContent lockedByAncestorContent = EditorGUIUtility.TrTextContent(string.Empty, "This property is set and locked by an ancestor.", lockedByAncestorIcon);

            public static readonly GUIStyle centered = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
        }

        struct PropertyData
        {
            public MaterialProperty property;
            public MaterialSerializedProperty serializedProperty;
            public Object[] targets;

            public bool wasBoldDefaultFont;
            public bool isLockedInChildren, isLockedByAncestor, isOverriden;

            public float startY;
            public Rect position;

            private static List<MaterialProperty> capturedProperties = new List<MaterialProperty>();
            private static List<MaterialSerializedProperty> capturedSerializedProperties = new List<MaterialSerializedProperty>();

            private bool HasMixedValues<T>(Func<Material, T> getter)
            {
                T value = getter(targets[0] as Material);
                for (int i = 1; i < targets.Length; ++i)
                {
                    if (!EqualityComparer<T>.Default.Equals(value, getter(targets[i] as Material)))
                        return true;
                }
                return false;
            }

            public bool hasMixedValue
            {
                get
                {
                    if (property != null)
                        return property.hasMixedValue;

                    if (serializedProperty == MaterialSerializedProperty.EnableInstancingVariants)
                        return HasMixedValues((mat) => mat.enableInstancing);
                    else if (serializedProperty == MaterialSerializedProperty.LightmapFlags)
                        return HasMixedValues((mat) => mat.globalIlluminationFlags);
                    else if (serializedProperty == MaterialSerializedProperty.DoubleSidedGI)
                        return HasMixedValues((mat) => mat.doubleSidedGI);
                    else if (serializedProperty == MaterialSerializedProperty.CustomRenderQueue)
                        return HasMixedValues((mat) => mat.rawRenderQueue);
                    return false;
                }
            }

            public void Init()
            {
                isLockedInChildren = false;
                isLockedByAncestor = false;
                isOverriden = true;
                int nameId = property != null ? Shader.PropertyToID(property.name) : -1;
                foreach (Material target in targets)
                {
                    bool l, b, o;
                    if (property != null)
                        target.GetPropertyState(nameId, out o, out l, out b);
                    else
                        target.GetPropertyState(serializedProperty, out o, out l, out b);
                    // When multi editing:
                    // 1. Show property as locked if any target is locked, to prevent bypassing the lock
                    // 2. Show property as overriden if all targets override it, to not show overrides on materials
                    isLockedInChildren |= l;
                    isLockedByAncestor |= b;
                    isOverriden &= o;
                }
            }

            static void MergeStack(out bool lockedInChildren, out bool lockedByAncestor, out bool overriden)
            {
                // We have to copy the property stack, because access from the Menu callbacks is delayed
                capturedProperties.Clear();
                capturedSerializedProperties.Clear();

                lockedInChildren = false;
                lockedByAncestor = false;
                overriden = false;
                for (int i = 0; i < s_PropertyStack.Count; i++)
                {
                    // When multiple properties are displayed on the same line, we *or* everything otherwise it gets confusing.
                    if (s_PropertyStack[i].targets == null) continue;
                    lockedInChildren |= s_PropertyStack[i].isLockedInChildren;
                    lockedByAncestor |= s_PropertyStack[i].isLockedByAncestor;
                    overriden        |= s_PropertyStack[i].isOverriden;

                    if (s_PropertyStack[i].property != null)
                        capturedProperties.Add(s_PropertyStack[i].property);
                    else
                        capturedSerializedProperties.Add(s_PropertyStack[i].serializedProperty);
                }
            }

            static string GetMultiEditingDisplayName(string multiEditSuffix)
            {
                int nonEmptyCount = capturedProperties.Count + capturedSerializedProperties.Count;
                if (nonEmptyCount != 1)
                    return nonEmptyCount + " " + multiEditSuffix;
                else if (capturedProperties.Count != 0)
                    return capturedProperties[0].displayName;
                else
                    return capturedSerializedProperties[0].ToString();
            }

            public static void DoPropertyContextMenu(bool lockMenusOnly, Object[] targets)
            {
                MergeStack(out bool lockedInChildren, out bool lockedByAncestor, out bool overriden);

                GenericMenu menu = new GenericMenu();

                if (lockedByAncestor)
                {
                    if (targets.Length != 1)
                        return;

                    menu.AddItem(Styles.lockOriginContent, false, () => GotoLockOriginAction(targets));
                }
                else if (GUI.enabled)
                {
                    if (!lockMenusOnly)
                        DoRegularMenu(menu, overriden, targets);
                    DoLockPropertiesMenu(menu, !lockedInChildren, targets);
                }

                if (Event.current.shift && capturedProperties.Count == 1)
                {
                    if (menu.GetItemCount() != 0)
                        menu.AddSeparator("");
                    menu.AddItem(EditorGUIUtility.TrTextContent("Copy Property Name"), false, () => EditorGUIUtility.systemCopyBuffer = capturedProperties[0].name);
                }

                if (menu.GetItemCount() == 0)
                    return;

                Event.current.Use();
                menu.ShowAsContext();
            }

            enum DisplayMode { Material, Variant, Mixed };
            static DisplayMode GetDisplayMode(Object[] targets)
            {
                int variantCount = MaterialEditor.GetVariantCount(targets);
                if (variantCount == 0)
                    return DisplayMode.Material;
                if (variantCount == targets.Length)
                    return DisplayMode.Variant;
                return DisplayMode.Mixed;
            }

            static void ResetMaterialProperties()
            {
                foreach (var property in capturedProperties)
                {
                    // fetch default value from shader
                    var shader = (property.targets[0] as Material).shader;
                    int nameId = shader.FindPropertyIndex(property.name);
                    switch (property.type)
                    {
                        case PropType.Float:
                        case PropType.Range:
                            property.floatValue = shader.GetPropertyDefaultFloatValue(nameId);
                            break;
                        case PropType.Vector:
                            property.vectorValue = shader.GetPropertyDefaultVectorValue(nameId);
                            break;
                        case PropType.Color:
                            property.colorValue = shader.GetPropertyDefaultVectorValue(nameId);
                            break;
                        case PropType.Int:
                            property.intValue = shader.GetPropertyDefaultIntValue(nameId);
                            break;
                        case PropType.Texture:
                            Texture texture = null;
                            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)) as ShaderImporter;
                            if (importer != null)
                                texture = importer.GetDefaultTexture(property.name);
                            if (texture == null)
                                texture = EditorMaterialUtility.GetShaderDefaultTexture(shader, property.name);
                            property.textureValue = texture;
                            property.textureScaleAndOffset = new Vector4(1, 1, 0, 0);
                            break;
                    }
                }
            }

            static void HandleApplyRevert(GenericMenu menu, bool singleEditing, Object[] targets)
            {
                // Apply
                if (singleEditing)
                {
                    Material source = (Material)targets[0];
                    Material destination = (Material)targets[0];
                    while (destination = destination.parent as Material)
                    {
                        if (AssetDatabase.IsForeignAsset(destination))
                            continue;

                        var text = destination.isVariant ? Styles.applyToVariantText : Styles.applyToMaterialText;
                        var applyContent = new GUIContent(string.Format(text, destination.name));

                        menu.AddItem(applyContent, false, (object dest) => {
                            foreach (var prop in capturedProperties)
                                source.ApplyPropertyOverride((Material)dest, prop.name);
                            foreach (var prop in capturedSerializedProperties)
                                source.ApplyPropertyOverride((Material)dest, prop);
                        }, destination);
                    }
                }

                // Revert
                var content = singleEditing ? Styles.revertContent :
                    EditorGUIUtility.TempContent(string.Format(Styles.revertMultiText, targets.Length));
                menu.AddItem(content, false, () => {
                    string displayName = GetMultiEditingDisplayName("overrides");
                    string targetName = singleEditing ? targets[0].name : targets.Length + " Materials";
                    Undo.RecordObjects(targets, "Revert " + displayName + " of " + targetName);

                    foreach (Material target in targets)
                    {
                        foreach (var prop in capturedProperties)
                            target.RevertPropertyOverride(prop.name);
                        foreach (var prop in capturedSerializedProperties)
                            target.RevertPropertyOverride(prop);
                    }
                });
            }

            static void HandleCopyPaste(GenericMenu menu)
            {
                GetCopyPasteAction(capturedProperties[0], out var copyAction, out var pasteAction);

                if (menu.GetItemCount() != 0)
                    menu.AddSeparator("");

                if (copyAction != null)
                    menu.AddItem(Styles.copyContent, false, copyAction);
                else
                    menu.AddDisabledItem(Styles.copyContent);
                if (pasteAction != null)
                    menu.AddItem(Styles.pasteContent, false, pasteAction);
                else
                    menu.AddDisabledItem(Styles.pasteContent);
            }

            static void HandleRevertAll(GenericMenu menu, bool singleEditing, Object[] targets)
            {
                foreach (Material target in targets)
                {
                    if (target.overrideCount != 0)
                    {
                        if (menu.GetItemCount() != 0)
                            menu.AddSeparator("");

                        menu.AddItem(Styles.revertAllContent, false, () => {
                                string targetName = singleEditing ? targets[0].name : targets.Length + " Materials";
                                Undo.RecordObjects(targets, "Revert all overrides of " + targetName);

                                foreach (Material target in targets)
                                target.RevertAllPropertyOverrides();
                                });
                        break;
                    }
                }
            }

            static void DoRegularMenu(GenericMenu menu, bool isOverriden, Object[] targets)
            {
                var singleEditing = targets.Length == 1;

                if (isOverriden)
                    HandleApplyRevert(menu, singleEditing, targets);

                if (singleEditing && capturedProperties.Count == 1)
                    HandleCopyPaste(menu);

                DisplayMode displayMode = GetDisplayMode(targets);
                if (displayMode == DisplayMode.Material)
                {
                    if (menu.GetItemCount() != 0)
                        menu.AddSeparator("");

                    menu.AddItem(Styles.resetContent, false, ResetMaterialProperties);
                }
                else if (displayMode == DisplayMode.Variant)
                    HandleRevertAll(menu, singleEditing, targets);
            }

            static void GetCopyPasteAction(MaterialProperty prop, out GenericMenu.MenuFunction copyAction, out GenericMenu.MenuFunction pasteAction)
            {
                bool canCopy = !capturedProperties[0].hasMixedValue;
                bool canPaste = GUI.enabled;

                copyAction = null;
                pasteAction = null;
                switch (prop.type)
                {
                    case PropType.Float:
                    case PropType.Range:
                        if (canCopy) copyAction = () => Clipboard.floatValue = prop.floatValue;
                        if (canPaste && Clipboard.hasFloat) pasteAction = () => prop.floatValue = Clipboard.floatValue;
                        break;
                    case PropType.Int:
                        if (canCopy) copyAction = () => Clipboard.integerValue = prop.intValue;
                        if (canPaste && Clipboard.hasInteger) pasteAction = () => prop.intValue = Clipboard.integerValue;
                        break;
                    case PropType.Color:
                        if (canCopy) copyAction = () => Clipboard.colorValue = prop.colorValue;
                        if (canPaste && Clipboard.hasColor) pasteAction = () => prop.colorValue = Clipboard.colorValue;
                        break;
                    case PropType.Vector:
                        if (canCopy) copyAction = () => Clipboard.vector4Value = prop.vectorValue;
                        if (canPaste && Clipboard.hasVector4) pasteAction = () => prop.vectorValue = Clipboard.vector4Value;
                        break;
                    case PropType.Texture:
                        if (canCopy) copyAction = () => Clipboard.guidValue = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(prop.textureValue));
                        if (canPaste && Clipboard.hasGuid) pasteAction = () => prop.textureValue = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(Clipboard.guidValue)) as Texture;
                        break;
                }
            }

            static void DoLockPropertiesMenu(GenericMenu menu, bool lockValue, Object[] targets)
            {
                if (menu.GetItemCount() != 0)
                    menu.AddSeparator("");

                // Lock
                menu.AddItem(Styles.lockContent, !lockValue, () => {
                    LockProperties(lockValue, targets);
                });
            }

            static void LockProperties(bool lockValue, Object[] targets)
            {
                string actionName = lockValue ? "locking" : "unlocking";
                string displayName = GetMultiEditingDisplayName("properties");
                string targetName = targets.Length == 1 ? targets[0].name : targets.Length + " Materials";
                Undo.RecordObjects(targets, string.Format("{0} {1} of {2}", actionName, displayName, targetName));

                foreach (Material target in targets)
                {
                    foreach (var prop in capturedProperties)
                        target.SetPropertyLock(prop.name, lockValue);
                    foreach (var prop in capturedSerializedProperties)
                        target.SetPropertyLock(prop, lockValue);
                }
            }

            static public void DoLockAction(Object[] targets)
            {
                MergeStack(out bool lockedInChildren, out bool lockedByAncestor, out bool _);

                if (lockedByAncestor)
                    GotoLockOriginAction(targets);
                else
                    LockProperties(!lockedInChildren, targets);

                Event.current.Use();
            }

            static void GotoLockOriginAction(Object[] targets)
            {
                // Find lock origin
                Material origin = targets[0] as Material;
                while ((origin = origin.parent))
                {
                    bool isLocked = false;
                    foreach (var prop in capturedProperties)
                    {
                        origin.GetPropertyState(Shader.PropertyToID(prop.name), out _, out isLocked, out _);
                        if (isLocked) break;
                    }
                    if (isLocked) break;

                    foreach (var prop in capturedSerializedProperties)
                    {
                        origin.GetPropertyState(prop, out _, out isLocked, out _);
                        if (isLocked) break;
                    }
                    if (isLocked) break;
                }

                if (origin)
                {
                    int clickCount = 1;
                    if (Event.current != null)
                    {
                        clickCount = Event.current.clickCount;
                        Event.current.Use();
                    }
                    if (clickCount == 1)
                        EditorGUIUtility.PingObject(origin);
                    else
                    {
                        Selection.SetActiveObjectWithContext(origin, null);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
        static List<PropertyData> s_PropertyStack = new List<PropertyData>();
        internal static void ClearStack() => s_PropertyStack.Clear();

        internal static void BeginProperty(MaterialProperty prop, Object[] targets)
        {
            // Get the current Y coordinate before drawing the property
            // We define a new empty rect in order to grab the current height even if there was nothing drawn in the block
            // (GetLastRect cause issue if it was first element of block)
            MaterialProperty.BeginProperty(Rect.zero, prop, 0, targets, GUILayoutUtility.GetRect(0, 0).yMax);
        }

        internal static void BeginProperty(MaterialSerializedProperty prop, Object[] targets)
        {
            // Get the current Y coordinate before drawing the property
            // We define a new empty rect in order to grab the current height even if there was nothing drawn in the block
            // (GetLastRect cause issue if it was first element of block)
            MaterialProperty.BeginProperty(Rect.zero, null, prop, targets, GUILayoutUtility.GetRect(0, 0).yMax);
        }

        internal static void BeginProperty(Rect totalRect, MaterialProperty prop, MaterialSerializedProperty serializedProp, Object[] targets, float startY = -1)
        {
            if (targets == null || IsRegistered(prop, serializedProp))
            {
                s_PropertyStack.Add(new PropertyData() { targets = null });
                return;
            }

            PropertyData data = new PropertyData()
            {
                property = prop,
                serializedProperty = serializedProp,
                targets = targets,

                startY = startY,
                position = totalRect,
                wasBoldDefaultFont = EditorGUIUtility.GetBoldDefaultFont()
            };
            data.Init();
            s_PropertyStack.Add(data);

            if (data.isOverriden)
                EditorGUIUtility.SetBoldDefaultFont(true);

            if (data.isLockedByAncestor)
                EditorGUI.BeginDisabledGroup(true);

            EditorGUI.showMixedValue = data.hasMixedValue;
        }

        internal static void EndProperty()
        {
            if (s_PropertyStack.Count == 0)
            {
                Debug.LogError("MaterialProperty stack is empty");
                return;
            }
            var data = s_PropertyStack[s_PropertyStack.Count - 1];
            if (data.targets == null)
            {
                s_PropertyStack.RemoveAt(s_PropertyStack.Count - 1);
                return;
            }

            Rect position = data.position;
            if (data.startY != -1)
            {
                position = GUILayoutUtility.GetLastRect();
                position.yMin = data.startY;
                position.x = 1;
                position.width = EditorGUIUtility.labelWidth;
            }

            bool mouseOnLock = false;
            if (position != Rect.zero)
            {
                // Display override rect
                if (data.isOverriden)
                    EditorGUI.DrawMarginLineForRect(position, Styles.overrideLineColor);

                Rect lockRegion = position;
                lockRegion.width = 14;
                lockRegion.height = 14;
                lockRegion.x = 11;
                lockRegion.y += (position.height - lockRegion.height) * 0.5f;
                mouseOnLock = lockRegion.Contains(Event.current.mousePosition);

                // Display lock icon
                Rect lockRect = position;
                lockRect.width = 32;
                lockRect.height = Mathf.Max(lockRect.height, 20.0f);
                lockRect.x = 8;
                lockRect.y += (position.height - lockRect.height) * 0.5f;

                if (data.isLockedByAncestor)
                {
                    // Make sure we draw the lock only once
                    bool isLastLockInStack = true;
                    for (int i = 0; i < s_PropertyStack.Count - 1; i++)
                    {
                        if (s_PropertyStack[i].isLockedByAncestor)
                        {
                            isLastLockInStack = false;
                            break;
                        }
                    }

                    if (isLastLockInStack)
                        GUI.Label(lockRect, Styles.lockedByAncestorContent, Styles.centered);
                }
                else if (data.isLockedInChildren)
                    GUI.Label(lockRect, Styles.lockInChildrenContent, Styles.centered);
                else if (GUI.enabled)
                {
                    GUIView.current?.MarkHotRegion(GUIClip.UnclipToWindow(lockRegion));
                    if (mouseOnLock)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        GUI.Label(lockRect, Styles.lockInChildrenContent, Styles.centered);
                        EditorGUI.EndDisabledGroup();
                    }
                }
            }

            // Restore state
            EditorGUI.showMixedValue = false;

            EditorGUIUtility.SetBoldDefaultFont(data.wasBoldDefaultFont);

            if (data.isLockedByAncestor)
                EditorGUI.EndDisabledGroup();

            // Context menu
            if (Event.current.rawType == EventType.ContextClick && (position.Contains(Event.current.mousePosition) || mouseOnLock))
                PropertyData.DoPropertyContextMenu(mouseOnLock, data.targets);
            else if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && mouseOnLock)
                PropertyData.DoLockAction(data.targets);

            s_PropertyStack.RemoveAt(s_PropertyStack.Count - 1);
        }

        static bool IsRegistered(MaterialProperty prop, MaterialSerializedProperty serializedProp)
        {
            // [PerRendererData] material properties are read-only as they are meant to be set in code on a per-renderer basis.
            // Don't show override UI for them
            if (prop != null && (prop.flags & PropFlags.PerRendererData) != 0)
                return true;
            for (int i = 0; i < s_PropertyStack.Count; i++)
            {
                if (s_PropertyStack[i].property == prop && s_PropertyStack[i].serializedProperty == serializedProp)
                    return true;
            }
            return false;
        }
    }
} // namespace UnityEngine.Rendering
