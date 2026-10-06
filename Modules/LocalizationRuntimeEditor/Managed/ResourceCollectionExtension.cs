// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Localization.Editor;

/// <summary>
/// Marks a serializable class that attaches tool-specific data to a <see cref="ResourceTableCollection"/>.
/// </summary>
/// <remarks>
/// An extension travels with the collection asset through <see cref="ResourceTableCollection.AddExtension"/>
/// and is stored by reference, so any serializable implementation works, for example the connection settings
/// for an external translation service. Read one back with
/// <see cref="ResourceTableCollection.GetExtension{T}"/>.
/// </remarks>
public interface IResourceCollectionExtension
{
}
