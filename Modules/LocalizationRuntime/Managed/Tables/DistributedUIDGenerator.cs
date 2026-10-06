// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// The default key generator. It produces a distributed unique id, Snowflake-style, that combines a timestamp, a
/// machine id, and a per-millisecond sequence.
/// </summary>
/// <remarks>
/// The generated ids are non-sequential, so several users can add keys to the same table without id collisions. The id
/// packs a per-millisecond sequence (12 bits), a machine id (10 bits), and a timestamp measured from
/// <see cref="CustomEpoch"/> (41 bits). Timestamps count from a fixed default epoch of 2020-01-01T00:00:00Z, and every
/// generator in the process draws from one sequence counter, so two default generators created at the same moment
/// still hand out different ids. The machine id is derived from
/// <see cref="UnityEngine.SystemInfo.deviceUniqueIdentifier"/> so it is stable per machine. Assign an instance to
/// <see cref="SharedTableData.KeyGenerator"/> to control how a collection assigns key ids, or implement
/// <see cref="IKeyGenerator"/> for a different scheme.
/// </remarks>
/// <example>
/// Generate two ids and confirm they differ.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/DistributedUIDGeneratorOverviewExample.cs"/>
/// </example>
/// <seealso cref="IKeyGenerator"/>
/// <seealso cref="SharedTableData"/>
[Serializable]
public class DistributedUIDGenerator : IKeyGenerator, ISerializationCallbackReceiver
{
    const int k_MachineIdBits = 10;
    const int k_SequenceBits = 12;

    const int k_MaxMachineId = (1 << k_MachineIdBits) - 1;
    const int k_MaxSequence = (1 << k_SequenceBits) - 1;
    // What is left of a long once the machine id, the sequence and the sign bit are taken.
    const long k_MaxTimestamp = (1L << (63 - k_MachineIdBits - k_SequenceBits)) - 1;
    // How far behind the recorded timestamp the clock may run before waiting for it stops being reasonable.
    const int k_MaxSpinMilliseconds = 100;

    // 2020-01-01T00:00:00Z. A fixed epoch keeps every default generator on one timeline.
    internal const long k_DefaultEpoch = 1577836800000;

    [NoAutoStaticsCleanup] // a monitor with no state to clear
    static readonly object s_SequenceLock = new();

    // Absolute Unix milliseconds, so instances with different epochs still share one counter.
    [NoAutoStaticsCleanup] // reissuing a timestamp and sequence after a reload would hand out an id already in use
    static long s_LastTimestamp = -1;
    [NoAutoStaticsCleanup] // mirrors s_LastTimestamp
    static long s_Sequence;

    [SerializeField, HideInInspector] long m_CustomEpoch = k_DefaultEpoch;

    int m_MachineId;

    /// <summary>
    /// The epoch, in Unix milliseconds, that timestamps are measured from.
    /// </summary>
    /// <remarks>
    /// Defaults to 2020-01-01T00:00:00Z, which every generator built by the default constructor shares. The 41-bit
    /// timestamp covers 69 years from the epoch. A generator restored from a saved
    /// <see cref="SharedTableData"/> always measures from the default epoch, so an epoch passed to the constructor
    /// lasts only for the life of that instance.
    /// </remarks>
    public long CustomEpoch => m_CustomEpoch;

    /// <summary>
    /// The machine id folded into each id.
    /// </summary>
    /// <remarks>
    /// Clamped to the range 1 to 1023 on assignment. When not set, it is derived from the device identifier so ids from
    /// different machines rarely collide.
    /// </remarks>
    public int MachineId
    {
        get
        {
            if (m_MachineId == 0)
                m_MachineId = DeriveMachineId();
            return m_MachineId;
        }
        set
        {
            if (value < 1 || value > k_MaxMachineId)
                Debug.LogWarning($"Machine id {value} is outside the range 1 to {k_MaxMachineId} and was clamped.");
            m_MachineId = Mathf.Clamp(value, 1, k_MaxMachineId);
        }
    }

    static int DeriveMachineId()
    {
        var id = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(id) || id == SystemInfo.unsupportedIdentifier)
            return new System.Random().Next(1, k_MaxMachineId + 1);

        var hash = 2166136261u;
        foreach (var c in id)
            hash = (hash ^ c) * 16777619u;
        return (int)(hash % (uint)k_MaxMachineId) + 1;
    }

    /// <summary>
    /// Creates a generator that measures timestamps from the default epoch.
    /// </summary>
    /// <remarks>
    /// The <see cref="CustomEpoch"/> is 2020-01-01T00:00:00Z, shared by every generator built this way. Use the
    /// <see cref="DistributedUIDGenerator(long)"/> overload to measure from a different point instead.
    /// </remarks>
    /// <example>
    /// Assign a default generator to a collection's shared data.
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/DistributedUIDGeneratorConstructorExample.cs"/>
    /// </example>
    public DistributedUIDGenerator() {}

    /// <summary>
    /// Creates a generator that measures timestamps from a specific epoch.
    /// </summary>
    /// <remarks>
    /// The timestamp portion of each id counts from <paramref name="customEpoch"/> instead of the default epoch, which
    /// takes the generator off the timeline the default generators share, so its ids can collide with theirs. For that
    /// reason a generator restored from a saved <see cref="SharedTableData"/> reverts to the default epoch, and an
    /// epoch set here lasts only for the life of the instance.
    /// </remarks>
    /// <param name="customEpoch">The epoch, in Unix milliseconds, that timestamps are measured from.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the time elapsed since <paramref name="customEpoch"/> does not fit the 41-bit timestamp: an epoch
    /// later than now would make the id negative, and one more than 69 years ago would overflow into the sign bit.
    /// An epoch before 1970, and so negative, is a valid date and is accepted.
    /// </exception>
    /// <example>
    /// Create a generator pinned to a fixed epoch.
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/DistributedUIDGeneratorCustomEpochExample.cs"/>
    /// </example>
    public DistributedUIDGenerator(long customEpoch)
    {
        var elapsed = UtcMilliseconds() - customEpoch;
        if (elapsed < 0 || elapsed > k_MaxTimestamp)
            throw new ArgumentOutOfRangeException(nameof(customEpoch), customEpoch, "The epoch must be a Unix time in milliseconds that is in the past and within 69 years of now.");
        m_CustomEpoch = customEpoch;
    }

    /// <summary>
    /// Returns the next distributed unique id.
    /// </summary>
    /// <remarks>
    /// Combines the current timestamp, the <see cref="MachineId"/>, and a per-millisecond sequence. Every generator in
    /// the process shares that sequence, so ids stay unique when the clock moves backwards, when several ids are
    /// requested within the same millisecond, and when two generators are asked for a key at the same moment.
    /// </remarks>
    /// <returns>A distributed unique id suitable for a table key.</returns>
    /// <example>
    /// Generate a single id.
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/DistributedUIDGeneratorGetNextKeyExample.cs"/>
    /// </example>
    public long GetNextKey()
    {
        lock (s_SequenceLock)
        {
            var timestamp = UtcMilliseconds();
            if (timestamp <= s_LastTimestamp)
            {
                s_Sequence = (s_Sequence + 1) & k_MaxSequence;
                timestamp = s_Sequence == 0 ? WaitNextMillis() : s_LastTimestamp; // sequence exhausted; wait for the next millisecond
            }
            else
            {
                s_Sequence = 0;
            }
            s_LastTimestamp = timestamp;

            var id = (timestamp - m_CustomEpoch) << (k_MachineIdBits + k_SequenceBits);
            id |= (uint)MachineId << k_SequenceBits;
            id |= s_Sequence;
            return id;
        }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() {}

    // A saved epoch is whatever the generator was built with, which puts it off the timeline the defaults share.
    void ISerializationCallbackReceiver.OnAfterDeserialize() => m_CustomEpoch = k_DefaultEpoch;

    static long UtcMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    static long WaitNextMillis()
    {
        var timestamp = UtcMilliseconds();
        if (timestamp <= s_LastTimestamp - k_MaxSpinMilliseconds)
        {
            return s_LastTimestamp + 1;
        }
        while (timestamp <= s_LastTimestamp)
        {
            timestamp = UtcMilliseconds();
        }
        return timestamp;
    }
}
