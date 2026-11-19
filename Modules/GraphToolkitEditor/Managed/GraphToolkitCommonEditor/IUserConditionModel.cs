// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for condition models that wrap a user-authored condition, so that internal code can expose the
    /// wrapped condition through the public condition access interfaces.
    /// </summary>
    interface IUserConditionModel
    {
        /// <summary>
        /// The user-authored condition wrapped by this model.
        /// </summary>
        ICondition UserCondition { get; }
    }
}
