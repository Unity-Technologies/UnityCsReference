// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor.Experimental.GraphView
{
    [Serializable]
    internal struct DataBag
    {
        [SerializeField] private List<string> customDataKeys;
        [SerializeField] private List<string> customDataValues;
        [SerializeField] private List<int> customDataKeyIndex;

        public DataBag(ref DataBag other)
        {
            customDataKeys = other.customDataKeys != null ? new List<string>(other.customDataKeys) : null;
            customDataValues = other.customDataValues != null ? new List<string>(other.customDataValues) : null;
            customDataKeyIndex = other.customDataKeyIndex != null ? new List<int>(other.customDataKeyIndex) : null;
        }

        /// <summary>
        /// Add a custom data to the template.
        /// </summary>
        /// <param name="key">Key identifier for the data</param>
        /// <param name="value">Value of the data</param>
        public void AddCustomData(string key, string value)
        {
            customDataKeys ??= new List<string>();
            customDataValues ??= new List<string>();
            customDataKeyIndex ??= new List<int>();

            var keyIndex = customDataKeys.IndexOf(key);
            if (keyIndex < 0)
            {
                keyIndex = customDataKeys.Count;
                customDataKeys.Add(key);
            }
            customDataValues.Add(value);
            customDataKeyIndex.Add(keyIndex);
        }

        /// <summary>
        /// Remove all custom data associated with the specified key.
        /// </summary>
        /// <param name="key">Key of the custom data</param>
        public void RemoveCustomData(string key)
        {
            var keyIndex = customDataKeys?.IndexOf(key) ?? -1;
            if (keyIndex >= 0)
            {
                var indexToRemove = new List<int>();
                for (var i = 0; i < customDataValues.Count; ++i)
                {
                    if (customDataKeyIndex[i] == keyIndex)
                    {
                        indexToRemove.Add(i);
                    }
                }

                foreach (var index in indexToRemove)
                {
                    customDataValues.RemoveAt(index);
                    customDataKeyIndex.RemoveAt(index);
                }
                customDataKeys.RemoveAt(keyIndex);
            }
        }

        /// <summary>
        /// Remove all custom data associated with the specified key.
        /// </summary>
        /// <param name="key">Key of the custom data</param>
        /// <param name="value">Value of the custom data</param>
        public void RemoveCustomData(string key, string value)
        {
            var keyIndex = customDataKeys?.IndexOf(key) ?? -1;
            if (keyIndex >= 0)
            {
                var indexToRemove = new List<int>();
                for (var i = 0; i < customDataValues.Count; ++i)
                {
                    if (customDataKeyIndex[i] == keyIndex && customDataValues[i] == value)
                    {
                        indexToRemove.Add(i);
                    }
                }

                foreach (var index in indexToRemove)
                {
                    customDataValues.RemoveAt(index);
                    customDataKeyIndex.RemoveAt(index);
                }

                // If there are no more values for this key, remove the key itself
                if (customDataKeyIndex.IndexOf(keyIndex) < 0)
                {
                    customDataKeys.RemoveAt(keyIndex);
                }
            }
        }

        public Dictionary<string, List<string>> GetCustomData()
        {
            var dictionary = new Dictionary<string, List<string>>();
            var count = customDataValues?.Count ?? 0;
            for (var i = 0; i < count; i++)
            {
                var keyIndex = customDataKeyIndex[i];
                var key = customDataKeys[keyIndex];
                var value = customDataValues[i];
                if (!dictionary.TryGetValue(key, out var values))
                {
                    dictionary[key] = new List<string> { value };
                }
                else
                {
                    values.Add(value);
                }
            }

            return dictionary;
        }
    }

    /// <summary>
    /// This is a very basic interface to have a common type for template descriptors and template sections
    /// </summary>
    interface ITemplateDescriptor
    {
        string header { get; }
    }

    /// <summary>
    /// Template descriptor
    /// </summary>
    [Serializable]
    internal struct GraphViewTemplateDescriptor : ITemplateDescriptor, IComparable<GraphViewTemplateDescriptor>
    {
        /// <summary>
        /// Name of the template which will be displayed in the template window
        /// </summary>
        public string name = null;
        /// <summary>
        /// Category is used to group templates together in the template window
        /// </summary>
        public string category = null;
        /// <summary>
        /// Give some description to your template so that we know what it's doing
        /// </summary>
        public string description = null;
        /// <summary>
        /// This icon is displayed next to the name in the template window
        /// </summary>
        public Texture2D icon = null;
        /// <summary>
        /// Thumbnail is displayed with the description in the details panel of the template window
        /// </summary>
        public Texture2D thumbnail = null;
        /// <summary>
        /// Allow to sort templates in its category
        /// </summary>
        public int order = 0;
        /// <summary>
        /// Store custom data associated with the template.
        /// </summary>
        public DataBag customData = default;

        [SerializeField] private string toolKey;

        /// <summary>
        /// Internal use only: make the bound with the asset
        /// </summary>
        [NonSerialized]
        internal string assetGuid = null;
        /// <summary>
        /// Internal use only: allow to sort built-in template category first
        /// </summary>
        [NonSerialized]
        internal int internalOrder = 0;

        /// <summary>
        /// Same as the name, inherited from the interface ITemplateDescriptor
        /// </summary>
        public string header => name;
        /// <summary>
        /// Lowercase tool key, used to prefix indexed data
        /// </summary>
        public string ToolKey => toolKey;

        /// <summary>
        /// Create a new GraphViewTemplateDescriptor with the specified tool key.
        /// </summary>
        /// <param name="toolKey">Toolkey allows to identify to which tool this template belongs to</param>
        public GraphViewTemplateDescriptor(string toolKey)
        {
            this.toolKey = toolKey.ToLowerInvariant();
        }



        public int CompareTo(GraphViewTemplateDescriptor other)
        {
            return order.CompareTo(other.order);
        }

        internal DateTime GetModificationDate()
        {
            var filePath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (!string.IsNullOrEmpty(filePath))
            {
                var fullPath = System.IO.Path.GetFullPath(filePath);
                var assetModificationTime = System.IO.File.GetLastWriteTimeUtc(fullPath);
                var metaModificationTime = System.IO.File.GetLastWriteTimeUtc(fullPath + ".meta");
                return assetModificationTime > metaModificationTime ? assetModificationTime : metaModificationTime;
            }
            return DateTime.MinValue;
        }
    }
}
