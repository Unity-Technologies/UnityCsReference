// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    // Interface used for constraining generic functions on Attributes
    // which control system update, creation, or destruction order
    internal interface ISystemOrderAttribute
    {
        Type SystemType { get; }
    }


    /// <summary>
    /// Apply to a system to specify an update ordering constraint with another system in the same <see cref="ComponentSystemGroup"/>.
    /// </summary>
    /// <remarks>Updating before or after a system constrains the scheduler ordering of these systems within a ComponentSystemGroup.
    /// Both the before and after systems must be a members of the same ComponentSystemGroup.</remarks>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = true)]
    public class UpdateBeforeAttribute : Attribute, ISystemOrderAttribute
    {
        /// <summary>
        /// Specify a system which the tagged system must update before.
        /// </summary>
        /// <param name="systemType">The target system which the tagged system must update before. This system must be
        /// a member of the same <see cref="ComponentSystemGroup"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the system type is empty.</exception>
        public UpdateBeforeAttribute(Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            SystemType = systemType;
        }

        /// <summary>
        /// The type of the target system, which the tagged system must update before.
        /// </summary>
        public Type SystemType { get; }
    }

    /// <summary>
    /// Apply to a system to specify an update ordering constraint with another system in the same <see cref="ComponentSystemGroup"/>.
    /// </summary>
    /// <remarks>Updating before or after a system constrains the scheduler ordering of these systems within a ComponentSystemGroup.
    /// Both the before and after systems must be a members of the same ComponentSystemGroup.</remarks>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = true)]
    public class UpdateAfterAttribute : Attribute, ISystemOrderAttribute
    {
        /// <summary>
        /// Specify a system which the tagged system must update after.
        /// </summary>
        /// <param name="systemType">The target system which the tagged system must update after. This system must be
        /// a member of the same <see cref="ComponentSystemGroup"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the system type is empty.</exception>
        public UpdateAfterAttribute(Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            SystemType = systemType;
        }

        /// <summary>
        /// The type of the target system, which the tagged system must update after.
        /// </summary>
        public Type SystemType { get; }
    }

    /// <summary>
    /// Apply to a system to specify a creation ordering constraint with another system in the same <see cref="ComponentSystemGroup"/>.
    /// </summary>
    /// <remarks>Create before or after a system constrains the creation order of these systems when initializing a default world.
    /// System destruction order is defined as the reverse of creation order.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class CreateBeforeAttribute : Attribute, ISystemOrderAttribute
    {
        /// <summary>
        /// Specify a system which the tagged system must be created before.
        /// </summary>
        /// <param name="systemType">The target system which the tagged system must be created before. This system must be
        /// a member of the same <see cref="ComponentSystemGroup"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the system type is empty.</exception>
        public CreateBeforeAttribute(Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            SystemType = systemType;
        }

        /// <summary>
        /// The type of the target system, which the tagged system must be created before.
        /// </summary>
        public Type SystemType { get; }
    }

    /// <summary>
    /// Apply to a system to specify a creation ordering constraint with another system in the same <see cref="ComponentSystemGroup"/>.
    /// </summary>
    /// <remarks>Create before or after a system constrains the creation order of these systems when initializing a default world.
    /// System destruction order is defined as the reverse of creation order.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class CreateAfterAttribute : Attribute, ISystemOrderAttribute
    {
        /// <summary>
        /// Specify a system which the tagged system must be created after.
        /// </summary>
        /// <param name="systemType">The target system which the tagged system must be created after. This system must be
        /// a member of the same <see cref="ComponentSystemGroup"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the system type is empty.</exception>
        public CreateAfterAttribute(Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            SystemType = systemType;
        }

        /// <summary>
        /// The type of the target system, which the tagged system must be created after.
        /// </summary>
        public Type SystemType { get; }
    }

    /// <summary>
    /// The specified Type must be a ComponentSystemGroup.
    /// Updating in a group means this system will be automatically updated by the specified ComponentSystemGroup when the group is updated.
    /// The system may order itself relative to other systems in the group with UpdateBefore and UpdateAfter. This ordering takes
    /// effect when the system group is sorted.
    ///
    /// If the optional OrderFirst parameter is set to true, this system will act as if it has an implicit [UpdateBefore] targeting all other
    /// systems in the group that do *not* have OrderFirst=true, but it may still order itself relative to other systems with OrderFirst=true.
    ///
    /// If the optional OrderLast parameter is set to true, this system will act as if it has an implicit [UpdateAfter] targeting all other
    /// systems in the group that do *not* have OrderLast=true, but it may still order itself relative to other systems with OrderLast=true.
    ///
    /// An UpdateInGroup attribute with both OrderFirst=true and OrderLast=true is invalid, and will throw an exception.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
    public class UpdateInGroupAttribute : Attribute
    {
        /// <summary>
        /// If true, the tagged system will be sorted earlier than all systems in the <see cref="ComponentSystemGroup"/>
        /// which do not have OrderFirst=true.
        /// </summary>
        public bool OrderFirst = false;
        /// <summary>
        /// If true, the tagged system will be sorted later than all systems in the <see cref="ComponentSystemGroup"/>
        /// which do not have OrderLast=true.
        /// </summary>
        public bool OrderLast = false;

        /// <summary>
        /// Specify the <see cref="ComponentSystemGroup"/> which the tagged system should be added to. The tagged system
        /// will be updated as part of this system group's Update() method.
        /// </summary>
        /// <param name="groupType">The <see cref="ComponentSystemGroup"/> type/</param>
        /// <exception cref="ArgumentNullException">Thrown id the group type is empty.</exception>
        public UpdateInGroupAttribute(Type groupType)
        {
            if (groupType == null)
                throw new ArgumentNullException(nameof(groupType));

            GroupType = groupType;
        }

        /// <summary>
        /// Retrieve the <see cref="ComponentSystemGroup"/> type.
        /// </summary>
        public Type GroupType { get; }
    }
}
