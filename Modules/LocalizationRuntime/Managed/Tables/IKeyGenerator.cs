// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Localization;

/// <summary>
/// Generates the unique ids assigned to table keys.
/// </summary>
/// <remarks>
/// A collection's <see cref="SharedTableData"/> asks its generator for a new id whenever a key is added, and the id
/// stays with that key for its lifetime. The default implementation is <see cref="DistributedUIDGenerator"/>, which
/// produces non-sequential ids so several users can add keys without collisions. Implement this interface to supply a
/// different scheme, then assign it through <see cref="SharedTableData.KeyGenerator"/>.
/// </remarks>
/// <example>
/// <para>A simple generator that hands out increasing ids.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IKeyGeneratorExample.cs"/>
/// </example>
/// <seealso cref="DistributedUIDGenerator"/>
/// <seealso cref="SharedTableData"/>
public interface IKeyGenerator
{
    /// <summary>
    /// Returns the next id to assign to a key.
    /// </summary>
    /// <remarks>
    /// The returned id should be non-zero, because a <see cref="SharedTableData"/> treats an id of <c>0</c> as "no key".
    /// The caller retries when a returned id is already in use, so an implementation does not have to guarantee
    /// uniqueness on its own.
    /// </remarks>
    /// <returns>The id to assign to the next key.</returns>
    /// <example>
    /// <para>Request an id from the default generator.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IKeyGeneratorGetNextKeyExample.cs"/>
    /// </example>
    long GetNextKey();
}
