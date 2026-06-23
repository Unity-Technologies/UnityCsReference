// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.UIR;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements.Experimental;

// This bindings.cs file will be processed by the script binding generator
// which will generate c++ code to bind this c# class with the c++ one.
namespace UnityEngine.UIElements
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UIAnimationBoundProperty
    {
        public int elementIndex;
        public int propertyId;
        public int channel;
        public float currentValue;
        public EntityId currentObjectValue;
        private uint hash; // Do not use on the managed side
        // 0 after InvalidateBoundValueCaches; 1 after the next sample.
        internal int currentValueUpToDate;
    };

    [NativeHeader("Modules/UIElements/Core/Native/UIAnimationBinder.h")]
    [NativeHeader("Modules/UIElements/Core/Native/UIAnimationClip.h")]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal sealed partial class UIAnimationBinder : Object, IValueAnimationUpdate
    {
        [FreeFunction("UIAnimationBinder::Create")]
        internal static extern UIAnimationBinder Create();

        extern private void Internal_AssignKnownElementNames(string[] names, PropertyName[] propertyHashes);
        extern private void Internal_ApplyBoundValues();
        extern private void Internal_InvalidateBoundValueCaches();

        /// <summary>
        /// Clears native binder value caches so the next read through the animation binding path
        /// refetches from the VisualElement (used when the inspector updates style without going through the binder).
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void InvalidateBoundValueCaches()
        {
            Internal_InvalidateBoundValueCaches();
        }

        /// <summary>
        /// Directly evaluates a UIAnimationClip's float and PPtr curves at the given time
        /// and applies the results to the bound visual elements, bypassing the Animator/PlayableGraph.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal extern void SampleClip(UIAnimationClip clip, float time);

        /// <summary>
        /// Editor-only post-sample notification. Fired after every <see cref="SampleClipForEditor"/>
        /// call, exclusively from editor-side preview/seek paths (the runtime
        /// <see cref="VisualTreeAnimationUpdater"/> playback path keeps using the bare
        /// <see cref="SampleClip"/> extern, so players incur zero overhead).
        /// <para>
        /// Used by <c>VisualElementAnimationWindowController</c> to register every per-element
        /// clip's bindings with <see cref="DrivenPropertyManager"/> on each sample and pairs
        /// naturally with the idempotent <c>TryRegisterProperty</c>.
        /// </para>
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static event Action<UIAnimationBinder, UIAnimationClip, float> editorPostSample;

        /// <summary>
        /// Wrapper around <see cref="SampleClip"/> for editor-only callers that want to
        /// participate in the <see cref="editorPostSample"/> notification. Runtime callers
        /// (e.g. <c>VisualTreeAnimationUpdater</c>) continue to call <see cref="SampleClip"/>
        /// directly so the editor event has no effect outside the editor.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void SampleClipForEditor(UIAnimationClip clip, float time)
        {
            SampleClip(clip, time);
            editorPostSample?.Invoke(this, clip, time);
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal extern bool IsBound(string elementPath, int propertyId);

        internal extern void DeactivateAnimation();


        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal extern void ClearBindings();

        VisualElement rootVisualElement { get; set; }

        // Event invoked when element caches are cleared (for external selection cache management)
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal event Action ElementCachesClearedEvent;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal uint lastKnownHierarchyVersion { get; private set; }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal uint lastKnownNameVersion { get; private set; }

        /// <summary>
        /// Refreshes the element cache only when the visual tree structure or element
        /// names have changed since the last refresh, or when the cache has never been
        /// populated. Avoids the full tree traversal and allocation cost of
        /// <see cref="UpdateElementNames"/> on hot paths that probe repeatedly without
        /// changes (e.g. slider scrubbing during animation recording).
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void UpdateElementNamesIfNeeded()
        {
            if (m_Elements == null)
            {
                UpdateElementNames();
                return;
            }

            if (rootVisualElement?.panel is Panel p
                && (p.hierarchyVersion != lastKnownHierarchyVersion
                    || p.nameVersion != lastKnownNameVersion))
                UpdateElementNames();
        }

        private bool exposeRootElement;
        internal void RegisterRootDocument(VisualElement element, bool exposeRootElement)
        {
            if (rootVisualElement != null)
            {
                rootVisualElement.UnregisterAnimation(this);
            }
            this.exposeRootElement = exposeRootElement;
            rootVisualElement = element;

            rootVisualElement.RegisterAnimation(this);

            ClearElementCaches();
        }

        void OnEnable()
        {
            if(m_ElementsMap != null)
            {
                ClearElementNames();
            }
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        void ClearElementCaches()
        {
            ClearElementNames();
            m_Elements?.Clear();
            m_ElementsMap?.Clear();
            ElementCachesClearedEvent?.Invoke();
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        void UnregisterRootElement()
        {
            if(rootVisualElement != null)
            {
                rootVisualElement.UnregisterAnimation(this);
                rootVisualElement= null;
                ClearElementCaches();
            }
        }

        void ClearElementNames()
        {
            var names = Array.Empty<string>();
            var propertyNames = Array.Empty<PropertyName>();

            Internal_AssignKnownElementNames(names, propertyNames);
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void UpdateElementNames()
        {
            if (m_Elements == null)
            {
                m_Elements = new();
                m_ElementsMap = new();
            }
            else
            {
                m_Elements.Clear();
                m_ElementsMap.Clear();
                ElementCachesClearedEvent?.Invoke();

            }

            var animationRoot = rootVisualElement;

            if (animationRoot != null)
            {
                if (exposeRootElement)
                {
                    var rootPropName = new PropertyName(string.Empty);
                    m_Elements.Add(new KeyValuePair<string, VisualElement>(string.Empty, animationRoot));
                    m_ElementsMap[rootPropName] = animationRoot;
                }

                // Root is the path origin: skip its own name so children are "#a"/"#b", not "#root/#a".
                GatherAnimatableElements(string.Empty, animationRoot, skipElement: true);
            }

            var names = new string[m_Elements.Count]; //TODO: Don't allocate each time
            var propertyNames = new PropertyName[m_Elements.Count]; //TODO: Don't allocate each time

            for (int i = 0; i < m_Elements.Count; i++)
            {
                var name = m_Elements[i].Key;
                names[i] = name;
                propertyNames[i] = new PropertyName(name);
            }


            Internal_AssignKnownElementNames(names, propertyNames);

            if (rootVisualElement?.panel is Panel p)
            {
                lastKnownHierarchyVersion = p.hierarchyVersion;
                lastKnownNameVersion = p.nameVersion;
            }
        }


        static internal Object ReadCurrentObjectReferenceValue(VisualElement element, StylePropertyId propertyID, int channel)
        {
            const Object invalid = null;
            switch (propertyID)
            {
                //Background (Image/Sprite/VectorImage/etc
                case StylePropertyId.BackgroundImage:
                    return element.resolvedStyle.backgroundImage.GetSelectedImage();
                case StylePropertyId.UnityFont:
                    return element.resolvedStyle.unityFont;
                // FontDefinition holds either a Font or a FontAsset; surface whichever is set.
                case StylePropertyId.UnityFontDefinition:
                    return element.resolvedStyle.unityFontDefinition.GetSelectedFont();
                // Cursor is hidden from resolvedStyle; read its texture from computedStyle.
                case StylePropertyId.Cursor:
                    return element.computedStyle.ReadPropertyAnimationCursor(StylePropertyId.Cursor).texture;

                default:
                    return invalid;
            }
        }

        void GatherAnimatableElements(string path, VisualElement element, bool skipElement)
        {
            string subPath = path;

            if (!string.IsNullOrEmpty(element.name) && !skipElement)
            {
                if (string.IsNullOrEmpty(path))
                {
                    subPath = $"#{element.name}";
                }
                else
                {
                    subPath = $"{path}/#{element.name}";
                }

                var propName = new PropertyName(subPath);

                if(!m_ElementsMap.ContainsKey(propName))
                {
                    m_Elements.Add(new KeyValuePair<string, VisualElement>(subPath, element));
                    m_ElementsMap[propName] = element;
                }
            }

            for (int i = 0; i < element.childCount; i++)
            {
                var child = element[i];
                GatherAnimatableElements(subPath, child, false);
            }
        }


        internal void ApplyAnimatedValues()
        {
            // We shouldn't need this
            if (this == null)
            {
                UnregisterRootElement();
                return;
            }

            Internal_ApplyBoundValues();
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        unsafe void IterateOnBoundValues(IntPtr values, int count)
        {
            var boundValues = new ReadOnlySpan<UIAnimationBoundProperty>(values.ToPointer(), count);

            // Batch filter writes: WriteFilter* mutate a per-(element, id) cached list, and we
            // call ApplyPropertyAnimation once per pair in FlushPendingFilterWrites.
            m_BatchingFilterWrites = true;
            try
            {
                foreach (ref readonly var boundValue in boundValues)
                {
                    // Skip stale entries; surviving curves re-flip the flag on the next sample.
                    if (boundValue.currentValueUpToDate == 0)
                        continue;

                    if (GetChannelKind((StylePropertyId)boundValue.propertyId, boundValue.channel) == AnimationChannelKind.PPtr)
                        SetObjectValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel, boundValue.currentObjectValue);
                    else
                        SetFloatValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel, boundValue.currentValue);
                }
            }
            finally
            {
                m_BatchingFilterWrites = false;
                FlushPendingFilterWrites();
            }
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        unsafe void FetchCurrentValue(IntPtr values, int count)
        {
            var boundValues = new Span<UIAnimationBoundProperty>(values.ToPointer(), count);

            foreach (ref var boundValue in boundValues)
            {
                if (GetChannelKind((StylePropertyId)boundValue.propertyId, boundValue.channel) == AnimationChannelKind.PPtr)
                    boundValue.currentObjectValue = GetObjectValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel);
                else
                    boundValue.currentValue = GetFloatValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel);
            }
        }

        void IValueAnimationUpdate.Tick(long currentTimeMs)
        {
            ApplyAnimatedValues();
        }

        /// <summary>
        /// Gets the VisualElement corresponding to the given property name.
        /// </summary>
        /// <param name="propertyName">The property name (e.g., "#Container/#Button/translate.x")</param>
        /// <returns>The VisualElement, or null if not found</returns>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal VisualElement GetVisualElementFromPropertyName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            string elementPath = ExtractElementPathFromPropertyName(propertyName);
            if (string.IsNullOrEmpty(elementPath))
                return null;

            return GetVisualElementFromPath(elementPath);
        }

        private string ExtractElementPathFromPropertyName(string propertyName)
        {
            // Find the last '/' that separates element path from property name
            // Example: "#Container/#Button/translate.x" -> "#Container/#Button"
            int lastSlashIndex = propertyName.LastIndexOf('/');
            if (lastSlashIndex < 0)
                return null; // No separator found

            return propertyName.Substring(0, lastSlashIndex);
        }

        private VisualElement GetVisualElementFromPath(string elementPath)
        {
            if (m_ElementsMap == null)
                return null;

            // Try direct lookup using PropertyName
            var propName = new PropertyName(elementPath);
            if (m_ElementsMap.TryGetValue(propName, out var element))
                return element;

            return null;
        }

        /// <summary>
        /// Returns the path the binder uses to address <paramref name="element"/> when
        /// sampling - e.g. "#parent/#child", or the empty string for a root that was
        /// registered with <c>exposeRootElement = true</c>. Reads from the binder's
        /// existing element cache without triggering a refresh: callers that need
        /// up-to-date results should call <see cref="UpdateElementNames"/> first.
        /// Returns false when <paramref name="element"/> is not in the cache, which
        /// happens when the element has no name, when one of its named ancestors
        /// shadows it via the binder's flatten-unnamed-ancestors logic, or when a
        /// sibling with the same name was registered first.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal bool TryGetPathForElement(VisualElement element, out string path)
        {
            path = null;
            if (element == null || m_Elements == null)
                return false;

            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (ReferenceEquals(m_Elements[i].Value, element))
                {
                    path = m_Elements[i].Key;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the (path, element) pairs the binder currently has registered. Reads
        /// from the binder's existing element cache without triggering a refresh.
        /// Empty until <see cref="UpdateElementNames"/> has been called at least once.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal IReadOnlyList<KeyValuePair<string, VisualElement>> GetRegisteredElements()
        {
            if (m_Elements == null)
                return Array.Empty<KeyValuePair<string, VisualElement>>();
            return m_Elements;
        }

        /// <summary>
        /// Static callback for getting selection EntityId.
        /// Set by editor-only code (VisualElementObjectTypeCustomizer) to provide custom selection handling.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static Func<VisualElement, EntityId> s_GetSelectionEntityIdCallback;

        private VisualElement GetVisualElementForElementIndex(int elementIndex)
        {
            if (m_Elements != null && elementIndex >= 0 && elementIndex < m_Elements.Count)
            {
                return m_Elements[elementIndex].Value;
            }
            return null;
        }

        [RequiredByNativeCode(Optional = true)]
        internal void GetSelectionEntityIdForBindingIndex(int elementIndex, ref EntityId result)
        {
            VisualElement element = GetVisualElementForElementIndex(elementIndex);
            result = s_GetSelectionEntityIdCallback?.Invoke(element) ?? EntityId.None;
        }

    }
}
