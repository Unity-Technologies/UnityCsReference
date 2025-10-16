// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Describes an issue that ProjectAuditor reports in the Unity project.
    /// </summary>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{description}")]
    public class ReportItem
    {
        [SerializeField]
        DescriptorId descriptorId;

        [SerializeField]
        SerializableEnum<IssueCategory> category;

        [SerializeField]
        string description;

        [SerializeField]
        SerializableEnum<Severity> severity;

        [SerializeField]
        DependencyNode m_Dependencies;

        [SerializeField]
        Location location;

        [SerializeField]
        string[] properties;

        /// <summary>
        /// Determines whether the Issue was fixed. Only used if the ReportItem represents an Issue.
        /// </summary>
        [NonSerialized]
        internal bool WasFixed = false;

        [SerializeField]
        private bool ignored = false;
        internal bool IsIgnored { get { return ignored; } set { ignored = value; } }

        /// <summary>
        /// An unique identifier for the issue descriptor (read-only).
        /// </summary>
        /// <remarks>
        /// Reports can contain two different types of ReportItem:
        /// - Issues, which indicate a potential problem which should be investigated and possibly fixed: for example, a texture with its Read/Write Enabled checkbox ticked.
        /// - Insights, for informational purposes: for example, general information about a texture in the project.
        ///
        /// Issues can be identified by having a valid <see cref="DescriptorId"/>. See also: the <see cref="IsIssue"/> method.
        /// </remarks>
        public DescriptorId Id => descriptorId;

        internal string DescriptorIdAsString => descriptorId.IsValid() ? descriptorId.AsString() : null;

        /// <summary>
        /// This issue's category (read-only).
        /// </summary>
        public IssueCategory Category => category;

        /// <summary>
        /// Custom properties.
        /// See the "moduleMetadata" section of an exported Report JSON file for information on the formats and
        /// meanings of the custom properties for each IssueCategory.
        /// </summary>
        public string[] CustomProperties
        {
            get => properties;
            internal set => properties = value;
        }

        /// <summary>
        /// Project issue Description (read-only).
        /// </summary>
        public string Description
        {
            get
            {
                // If we don't return a valid string here, the UI can silently break for some reason.
                if (description == null)
                {
                    Debug.LogError("Description for ReportItem is unset.");
                    return "UNSET DESCRIPTION";
                }
                return description;
            }
            internal set => description = value;
        }

        /// <summary>
        /// Dependencies of this project issue.
        /// </summary>
        internal DependencyNode Dependencies
        {
            get => m_Dependencies;
            set => m_Dependencies = value;
        }

        /// <summary>
        /// Name of the file that contains this issue.
        /// </summary>
        public string Filename
        {
            get { return location == null ? string.Empty : location.Filename; }
        }

        /// <summary>
        /// Relative path of the file that contains this issue.
        /// </summary>
        public string RelativePath
        {
            get { return location == null ? string.Empty : location.Path; }
        }

        private string cachedExtension = null;

        /// <summary>
        /// File extension of the file that contains this issue.
        /// </summary>
        public string FileExtension
        {
            get
            {
                if (cachedExtension == null)
                {
                    string rp = RelativePath;
                    var extAIndex = PathUtils.GetExtensionIndexFromPath(rp);
                    if (extAIndex >= (rp.Length - 1))
                        cachedExtension = "";
                    else
                        cachedExtension = rp.Substring(extAIndex + 1);
                }

                return cachedExtension;
            }
        }

        /// <summary>
        /// Line in the file that contains this issue.
        /// </summary>
        public int Line => location == null ? 0 : location.Line;

        /// <summary>
        /// Location of the Insight or Issue (read-only).
        /// </summary>
        public Location Location
        {
            get => location;
            internal set => location = value;
        }

        /// <summary>
        /// Log level.
        /// </summary>
        public LogLevel LogLevel
        {
            get
            {
                switch (Severity)
                {
                    case Severity.Error:
                        return LogLevel.Error;
                    case Severity.Warning:
                        return LogLevel.Warning;
                    case Severity.Info:
                    default:
                        return LogLevel.Info;
                }
            }
        }

        /// <summary>
        /// Issue-specific Severity (read-only).
        /// </summary>
        public Severity Severity
        {
            get
            {
                if (severity == Severity.Default && descriptorId.IsValid())
                    return descriptorId.GetDescriptor().DefaultSeverity;
                return severity.Value;
            }

            internal set => severity = value;
        }

        internal ReportItem()
        {
            // only for json serialization purposes
            descriptorId = new DescriptorId(string.Empty);
            IsIgnored = false;
        }

        /// <summary>
        /// Constructs and returns an instance of ReportItem.
        /// </summary>
        /// <param name="cat">Issue category</param>
        /// <param name="id">Descriptor ID</param>
        /// <param name="args">Arguments to be used in the message formatting</param>
        internal ReportItem(IssueCategory cat, string id, params object[] args)
        {
            descriptorId = new DescriptorId(id);
            var descriptor = descriptorId.GetDescriptor();

            category = cat;
            IsIgnored = false;

            try
            {
                description = string.IsNullOrEmpty(descriptor.MessageFormat) ? descriptor.Title : string.Format(descriptor.MessageFormat, args);
            }
            catch (Exception e)
            {
                Debug.LogError("Error formatting message: " + descriptor.MessageFormat + " with args: " + string.Join(", ", args) + " - " + e.Message);
                description = descriptor.Title;
            }
            severity = descriptor.DefaultSeverity;
        }

        /// <summary>
        /// Constructs and returns an instance of ReportItem.
        /// </summary>
        /// <param name="cat">Issue category</param>
        /// <param name="desc">Issue description</param>
        internal ReportItem(IssueCategory cat, string desc)
        {
            descriptorId = new DescriptorId(null);  // Empty, invalid descriptor
            category = cat;
            description = desc;
            severity = Severity.Default;
            IsIgnored = false;
        }

        /// <summary>
        /// Checks whether this ReportItem represents an Issue.
        /// </summary>
        /// <returns>True if the issue's descriptor ID is valid. Otherwise, returns false.</returns>
        public bool IsIssue()
        {
            return Id.IsValid();
        }

        /// <summary>
        /// Checks whether this issue is major or critical.
        /// </summary>
        /// <returns>True if the issue's Severity is Major or Critical. Otherwise, returns false.</returns>
        internal bool IsMajorOrCritical()
        {
            return Severity == Severity.Critical || Severity == Severity.Major;
        }

        /// <summary>
        /// Checks whether this issue is valid.
        /// </summary>
        /// <returns>True if the issue has a valid description string. Otherwise, returns false.</returns>
        public bool IsValid()
        {
            return Description != null;
        }

        /// <summary>
        /// Gets the number of custom properties this issue has.
        /// </summary>
        /// <returns>The number of custom property strings</returns>
        internal int GetNumCustomProperties()
        {
            return properties != null ? properties.Length : 0;
        }

        // stephenm TODO: The Get/SetCustomProperty methods need more explanation - like how do you find out what enum is used for a given ReportItem.
        /// <summary>
        /// Get a custom property string given an enum.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Property name string</returns>
        internal string GetCustomProperty<T>(T propertyEnum) where T : struct
        {
            return GetCustomProperty(Convert.ToInt32(propertyEnum));
        }

        /// <summary>
        /// Get a custom property string given an index into the custom properties array.
        /// </summary>
        /// <param name="index">Custom property index</param>
        /// <returns>Property name string. Returns empty string if the custom properties array is null or empty or if the index is out of range.</returns>
        internal string GetCustomProperty(int index)
        {
            if (properties == null ||
                properties.Length == 0 ||
                index < 0 ||
                index >= properties.Length)
                return string.Empty; // fail gracefully if layout changed
            return properties[index];
        }

        /// <summary>
        /// Check whether a custom property is a boolean type and whether its value is true.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value type is boolean. Otherwise, returns false.</returns>
        internal bool GetCustomPropertyBool<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = false;
            if (!bool.TryParse(valueAsString, out value))
                return false;
            return value;
        }

        /// <summary>
        /// Check whether a custom property is an integer type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is an integer type. Otherwise, returns 0.</returns>
        internal int GetCustomPropertyInt32<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = 0;
            if (!int.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        /// <summary>
        /// Check whether a custom property is a long type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is a long type. Otherwise, returns 0.</returns>
        internal long GetCustomPropertyInt64<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = (long)0;
            if (!long.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        /// <summary>
        /// Check whether a custom property is a ulong type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is a ulong type. Otherwise, returns 0.</returns>
        internal ulong GetCustomPropertyUInt64<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = (ulong)0;
            if (!ulong.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        /// <summary>
        /// Check whether a custom property is a float type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is a float type. Otherwise, returns 0.0f.</returns>
        internal float GetCustomPropertyFloat<T>(T propertyEnum) where T : struct
        {
            float value;
            return float.TryParse(GetCustomProperty(propertyEnum), out value) ? value : 0.0f;
        }

        /// <summary>
        /// Check whether a custom property is a double type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is a double type. Otherwise, returns 0.0.</returns>
        internal double GetCustomPropertyDouble<T>(T propertyEnum) where T : struct
        {
            double value;
            return double.TryParse(GetCustomProperty(propertyEnum), out value) ? value : 0.0;
        }

        /// <summary>
        /// Set a custom property.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <param name="property">An object containing a value for the property</param>
        internal void SetCustomProperty<T>(T propertyEnum, object property) where T : struct
        {
            properties[Convert.ToUInt32(propertyEnum)] = property.ToString();
        }
    }
}
