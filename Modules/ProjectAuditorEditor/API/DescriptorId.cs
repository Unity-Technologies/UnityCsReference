// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// An unique identifier for an Issue Descriptor. IDs must have exactly 3 upper case characters, followed by 4 digits.
    /// </summary>
    /// <remarks>
    /// <see cref="ReportItem"/>s representing Issues (as opposed to Insights: purely informational ProjectIssues) all contain a DescriptorId.
    /// The DescriptorId is used to reference the information about a particular issue (for example, "PAC2002" is the code for "Object allocation"),
    /// and the ReportItem simply contains the file:line location in code where the allocation occurs and a "PAC2002" DescriptorId.
    /// The descriptor can generally be treated as a string, but for efficient descriptor lookups the string is converted to an int in the struct's constructor.
    /// </remarks>
    [Serializable]
    public struct DescriptorId : IEquatable<DescriptorId>, ISerializationCallbackReceiver
    {
        [NonSerialized][HideInInspector]
        int m_AsInt;

        [SerializeField]
        string m_AsString;

        /// <summary>Implicit conversion of DescriptorId to string.</summary>
        /// <param name="d">A DescriptorId to convert</param>
        /// <returns>A string representation of the ID</returns>
        public static implicit operator string(DescriptorId d) => d.m_AsString;

        /// <summary>Implicit conversion of string to DescriptorId.</summary>
        /// <param name="id">A string to convert</param>
        /// <returns>A DescriptorId constructed using the string ID</returns>
        public static implicit operator DescriptorId(string id) => new DescriptorId(id);

        /// <summary>Implicit conversion of DescriptorId to int.</summary>
        /// <param name="d">A DescriptorId to convert</param>
        /// <returns>The integer hash of the ID</returns>
        public static implicit operator int(DescriptorId d) => d.m_AsInt;

        /// <summary>
        /// Returns the string representation of the ID
        /// </summary>
        /// <returns>The DescriptorId in string form.</returns>
        public override string ToString() => m_AsString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">String representing a valid and unique Descriptor ID. IDs must have exactly 3 upper case characters, followed by 4 digits.</param>
        public DescriptorId(string id)
        {
            m_AsString = id;

            if (string.IsNullOrEmpty(id))
            {
                // No error message. This is valid when constructing ReportItems representing Insights.
                m_AsInt = -1;
                return;
            }

            // ID must be exactly 7 characters, beginning with exactly 3 uppercase letters, followed by exactly 4 digits
            if (id.Length != 7 || id[0] < 'A' || id[0] > 'Z' || id[1] < 'A' || id[1] > 'Z' || id[2] < 'A' || id[2] > 'Z' ||
                id[3] < '0' || id[3] > '9' || id[4] < '0' || id[4] > '9' || id[5] < '0' || id[5] > '9' || id[6] < '0' || id[6] > '9')
            {
                Debug.LogError("Invalid ID string supplied to DescriptorId");
                m_AsInt = -1;
                return;
            }

            m_AsInt = HashDescriptorString(id);
        }

        /// <summary>
        /// Checks whether the ID has been successfully constructed from a correctly-formatted string.
        /// </summary>
        /// <returns>True if the ID is valid</returns>
        public bool IsValid()
        {
            return m_AsInt >= 0;
        }

        /// <summary>
        /// Get the hashed integer representation of the ID
        /// </summary>
        /// <returns>The ID as an int</returns>
        public int AsInt()
        {
            return m_AsInt;
        }

        /// <summary>
        /// Get the string representation of the ID
        /// </summary>
        /// <returns>The ID as a string</returns>
        public string AsString()
        {
            return m_AsString;
        }

        /// <summary>
        /// Get the Descriptor which corresponds to this ID.
        /// </summary>
        /// <returns>The Descriptor which corresponds to this ID. If the ID is invalid, throws an exception.</returns>
        public Descriptor GetDescriptor()
        {
            return DescriptorLibrary.GetDescriptor(AsInt());
        }

        /// <summary>
        /// Checks whether two DescriptorIDs contain the same ID.
        /// </summary>
        /// <param name="other">A DescriptorId to compare to.</param>
        /// <returns>True if the descriptors are the same, false if they are different.</returns>
        public bool Equals(DescriptorId other)
        {
            return m_AsInt == other.m_AsInt;
        }

        /// <summary>
        /// Checks whether two DescriptorIDs contain the same ID.
        /// </summary>
        /// <param name="other">A string representing a descriptor ID to compare to.</param>
        /// <returns>True if the descriptors are the same, false if they are different.</returns>
        public bool Equals(string other)
        {
            return m_AsString == other;
        }

        private static int HashDescriptorString(string id)
        {
            var characters = (short)((char)(id[0] - 'A') << 10 | (char)(id[1] - 'A') << 5 | (char)(id[2] - 'A'));
            var numerical = (id[3] - '0') * 1000 + (id[4] - '0') * 100 + (id[5] - '0') * 10 + (id[6] - '0');
            return characters << 16 | numerical;
        }


        /// <summary>
        /// Unity calls this method automatically before serialization.
        /// </summary>
        public void OnBeforeSerialize()
        {
            
        }

        /// <summary>
        /// Unity calls this method automatically after deserialization.
        /// </summary>
        public void OnAfterDeserialize()
        {
            this = new DescriptorId(m_AsString);
        }
    }
}
