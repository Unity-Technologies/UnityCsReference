// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A class containing the information needed to create a variable.
    /// </summary>
    [UnityRestricted]
    internal class VariableCreationInfos
    {
        /// <summary>
        /// The type of the variable.
        /// </summary>
        public Type VariableType { get; set; }

        /// <summary>
        /// The type handle of the variable.
        /// </summary>
        public TypeHandle TypeHandle { get; set; } = TypeHandle.Float;

        /// <summary>
        /// The scope of the variable.
        /// </summary>
        public VariableScope Scope { get; set; } = VariableScope.Local;

        /// <summary>
        /// The modifiers of the variable.
        /// </summary>
        public ModifierFlags ModifierFlags { get; set; } = ModifierFlags.None;

        /// <summary>
        /// The name of the variable.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The group to insert the variable in.
        /// </summary>
        public GroupModel Group { get; set; } = null;

        /// <summary>
        /// The index in the group where the variable will be inserted.
        /// </summary>
        public int IndexInGroup { get; set; } = 0;
    }
}
