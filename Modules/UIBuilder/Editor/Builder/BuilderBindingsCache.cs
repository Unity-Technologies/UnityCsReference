// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BindingCacheData
    {
        public BindingStatus status;
        public bool isStatusResultValid;
        public bool wasFound;
    }

    internal class BuilderBindingsCache
    {
        private Dictionary<VisualElement, Dictionary<string, BindingCacheData>> m_CachedData = new();
        private List<BindingInfo> m_CurrentElementBindings = new();
        private List<VisualElement> m_BoundElements = new();

        /// <summary>
        /// This event gets invoked when a binding changes its status during an update pass.
        /// It gets called for every binding that changed in this pass
        /// </summary>
        public event Action<VisualElement, string> onBindingStatusChanged;

        /// <summary>
        /// This event gets invoked when a binding becomes unresolved during an update pass.
        /// It only gets called once by pass.
        /// </summary>
        public event Action onBindingBecameUnresolved;

        /// <summary>
        /// This event gets invoked when a binding is removed
        /// It gets called for every removed binding
        /// </summary>
        public event Action<VisualElement, string> onBindingRemoved;

        public bool HasResolvedBinding(VisualElement visualElement, string dataSourcePath)
        {
            if (TryGetCachedData(visualElement, dataSourcePath, out var cacheData))
            {
                return cacheData.isStatusResultValid && cacheData.status == BindingStatus.Success;
            }
            return false;
        }

        public bool TryGetCachedData(VisualElement visualElement, string dataSourcePath, out BindingCacheData cacheData)
        {
            if (m_CachedData.TryGetValue(visualElement, out var pathsDictionary))
            {
                return pathsDictionary.TryGetValue(dataSourcePath, out cacheData);
            }

            cacheData = null;
            return false;
        }

        /// <summary>
        /// Updates the bindings cache and notifies status changes in bindings, it also includes binding removals
        /// </summary>
        public void UpdateCache(Panel panel)
        {
            var bindingBecameUnresolved = false;

            m_BoundElements.Clear();
            DataBindingUtility.GetBoundElements(panel, m_BoundElements);

            foreach (var boundElement in m_BoundElements)
            {
                m_CurrentElementBindings.Clear();
                DataBindingUtility.GetBindingsForElement(boundElement, m_CurrentElementBindings);

                foreach (var bindingInfo in m_CurrentElementBindings)
                {
                    if (bindingInfo.binding == null)
                    {
                        continue;
                    }

                    var isStatusResultValid = DataBindingUtility.TryGetLastUIBindingResult(bindingInfo.bindingId, boundElement, out var bindingResult);
                    var propertyPath = bindingInfo.bindingId;

                    if (!m_CachedData.TryGetValue(boundElement, out var pathsDictionary))
                    {
                        // New binding
                        var resultDictionary = new Dictionary<string, BindingCacheData>();
                        m_CachedData[boundElement] = resultDictionary;
                        resultDictionary[propertyPath] = new BindingCacheData()
                        {
                            isStatusResultValid = isStatusResultValid,
                            status = bindingResult.status,
                        };
                        onBindingStatusChanged?.Invoke(boundElement, propertyPath);
                    }
                    else
                    {
                        if (!pathsDictionary.TryGetValue(propertyPath, out var bindingCacheData))
                        {
                            // New binding
                            m_CachedData[boundElement][propertyPath] = new BindingCacheData()
                            {
                                isStatusResultValid = isStatusResultValid,
                                status = bindingResult.status,
                            };

                            onBindingStatusChanged?.Invoke(boundElement, propertyPath);
                        }
                        else if (bindingCacheData.status != bindingResult.status || bindingCacheData.isStatusResultValid != isStatusResultValid)
                        {
                            // Binding change
                            bindingCacheData.status = bindingResult.status;
                            bindingCacheData.isStatusResultValid = isStatusResultValid;

                            if (bindingResult.status == BindingStatus.Failure || !isStatusResultValid)
                            {
                                bindingBecameUnresolved = true;
                            }

                            onBindingStatusChanged?.Invoke(boundElement, propertyPath);
                        }
                    }

                    m_CachedData[boundElement][propertyPath].wasFound = true;
                }
            }

            // Second pass to find deleted bindings
            foreach (var dataPair in m_CachedData)
            {
                var pathsDictionary = dataPair.Value;
                var keysToRemove = new List<string>();

                foreach (var pathDataPair in pathsDictionary)
                {
                    if (!pathDataPair.Value.wasFound)
                    {
                        var dataSourcePath = pathDataPair.Key;
                        keysToRemove.Add(dataSourcePath);
                    }
                    else
                    {
                        pathDataPair.Value.wasFound = false;
                    }
                }

                foreach (var key in keysToRemove)
                {
                    pathsDictionary.Remove(key);
                    onBindingRemoved?.Invoke(dataPair.Key, key);
                }
            }

            if (bindingBecameUnresolved)
            {
                onBindingBecameUnresolved?.Invoke();
            }
        }

        public void Clear()
        {
            m_CachedData.Clear();
        }
    }

    class BuilderBindingsCacheSubscriber
    {
        private BuilderBindingsCache m_Cache;
        private Action<VisualElement, string> m_OnBindingsChanged;

        public HashSet<string> filteredProperties { get; } = new();

        public BuilderBindingsCache cache
        {
            get => m_Cache;
            set
            {
                if (m_Cache != value)
                {
                    if (m_Cache != null)
                    {
                        m_Cache.onBindingStatusChanged -= OnBindingStatusChanged;
                        m_Cache.onBindingRemoved -= OnBindingStatusChanged;
                    }

                    m_Cache = value;

                    if (m_Cache != null)
                    {
                        m_Cache.onBindingStatusChanged += OnBindingStatusChanged;
                        m_Cache.onBindingRemoved += OnBindingStatusChanged;
                    }
                }
            }
        }

        public BuilderBindingsCacheSubscriber(Action<VisualElement, string> onBindingsChanged)
        {
            m_OnBindingsChanged = onBindingsChanged;
        }

        private void OnBindingStatusChanged(VisualElement target, string bindingPath)
        {
            if (filteredProperties is {Count: > 0})
            {
                if (!filteredProperties.Contains(bindingPath))
                    return;
            }
            m_OnBindingsChanged?.Invoke(target, bindingPath);
        }
    }
}
