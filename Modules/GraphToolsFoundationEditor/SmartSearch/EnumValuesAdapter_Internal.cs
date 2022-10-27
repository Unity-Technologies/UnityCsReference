// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// <see cref="ItemLibraryAdapter"/> to search for enum values.
    /// </summary>
    class EnumValuesAdapter_Internal : SimpleLibraryAdapter
    {
        /// <summary>
        /// <see cref="ItemLibraryItem"/> class to use when creating searchable enum values.
        /// </summary>
        public class EnumValueItem : ItemLibraryItem
        {
            public EnumValueItem(Enum value)
                :base(value.ToString())
            {
                this.value = value;
            }

            public readonly Enum value;
        }

        /// <summary>
        /// Initializes a new instance of the EnumValuesAdapter class.
        /// </summary>
        /// <param name="title">The title to display when prompting for search.</param>
        public EnumValuesAdapter_Internal(string title)
            : base(title) {}
    }
}
