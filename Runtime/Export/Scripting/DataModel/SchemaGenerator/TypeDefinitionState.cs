// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.DataModel;

/// <summary>
/// An enum to describe the state of resolution of a type definition
/// </summary>
internal enum TypeDefinitionState
{
    /// <summary>
    /// No information yet
    /// </summary>
    Empty,
    /// <summary>
    /// Information not available
    /// </summary>
    EmptyCantResolve,
    /// <summary>
    /// It contains an unresolved reference to an assembly
    /// </summary>
    EmptyWithAssembly,
    /// <summary>
    /// It contains a valid handle to a type definition in the reader
    /// </summary>
    EmptyWithHandle,
    /// <summary>
    /// It has all information resolved except fields
    /// </summary>
    CompleteNoFieldInfo,
    /// <summary>
    /// It has all information resolved including fields
    /// </summary>
    Complete
}
