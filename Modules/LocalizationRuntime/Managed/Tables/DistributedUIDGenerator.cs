// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// The default key generator. It produces a distributed unique id, Snowflake-style, that combines a timestamp, a
/// machine id, and a per-millisecond sequence.
/// </summary>
/// <remarks>
/// The generated ids are non-sequential, so several users can add keys to the same table without id collisions. The id
/// packs a per-millisecond sequence (12 bits), a machine id (10 bits), and a timestamp measured from
/// <see cref="CustomEpoch"/> (41 bits). The machine id is derived from <see cref="UnityEngine.SystemInfo.deviceUniqueIdentifier"/>
/// so it is stable per machine. Assign an instance to <see cref="SharedTableData.KeyGenerator"/> to control how a
/// collection assigns key ids, or implement <see cref="IKeyGenerator"/> for a different scheme.
/// </remarks>
/// <example>
/// <para>Generate two ids and confirm they differ.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/DistributedUIDGeneratorOverviewExample.cs"/>
/// </example>
/// <seealso cref="IKeyGenerator"/>
/// <seealso cref="SharedTableData"/>
[Serializable]
public class DistributedUIDGenerator : IKeyGenerator
{
    const int k_MachineIdBits = 10;
    const int k_SequenceBits = 12;

    const int k_MaxMachineId = (1 << k_MachineIdBits) - 1;
    const int k_MaxSequence = (1 << k_SequenceBits) - 1;
    // How far behind the recorded timestamp the clock may run before waiting for it stops being reasonable.
    const int k_MaxSpinMilliseconds = 100;

    [SerializeField, HideInInspector] long m_CustomEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    long m_LastTimestamp = -1;
    long m_Sequence;
    int m_MachineId;

    /// <summary>
    /// The epoch, in Unix milliseconds, that timestamps are measured from.
    /// </summary>
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
        set => m_MachineId = Mathf.Clamp(value, 1, k_MaxMachineId);
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
    /// Creates a generator that measures timestamps from the current time.
    /// </summary>
    /// <remarks>
    /// The current UTC time becomes the <see cref="CustomEpoch"/>. Use the <see cref="DistributedUIDGenerator(long)"/>
    /// overload to pin the epoch to a fixed point instead.
    /// </remarks>
    /// <example>
    /// <para>Assign a default generator to a collection's shared data.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/DistributedUIDGeneratorConstructorExample.cs"/>
    /// </example>
    public DistributedUIDGenerator() {}

    /// <summary>
    /// Creates a generator that measures timestamps from a specific epoch.
    /// </summary>
    /// <remarks>
    /// Because the timestamp portion of each id counts from <paramref name="customEpoch"/>, a later epoch keeps ids
    /// smaller for longer before the 41-bit timestamp range is exhausted.
    /// </remarks>
    /// <param name="customEpoch">The epoch, in Unix milliseconds, that timestamps are measured from.</param>
    /// <example>
    /// <para>Create a generator pinned to a fixed epoch.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/DistributedUIDGeneratorCustomEpochExample.cs"/>
    /// </example>
    public DistributedUIDGenerator(long customEpoch) => m_CustomEpoch = customEpoch;

    /// <summary>
    /// Returns the next distributed unique id.
    /// </summary>
    /// <remarks>
    /// Combines the current timestamp, the <see cref="MachineId"/>, and a per-millisecond sequence. The sequence keeps
    /// ids unique and increasing even when the clock moves backwards or several ids are requested within the same
    /// millisecond.
    /// </remarks>
    /// <returns>A distributed unique id suitable for a table key.</returns>
    /// <example>
    /// <para>Generate a single id.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/DistributedUIDGeneratorGetNextKeyExample.cs"/>
    /// </example>
    public long GetNextKey()
    {
        var timestamp = TimeStamp();
        if (timestamp <= m_LastTimestamp)
        {
            m_Sequence = (m_Sequence + 1) & k_MaxSequence;
            timestamp = m_Sequence == 0 ? WaitNextMillis() : m_LastTimestamp; // sequence exhausted; wait for the next millisecond
        }
        else
        {
            m_Sequence = 0;
        }
        m_LastTimestamp = timestamp;

        var id = timestamp << (k_MachineIdBits + k_SequenceBits);
        id |= (uint)MachineId << k_SequenceBits;
        id |= m_Sequence;
        return id;
    }

    long TimeStamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - m_CustomEpoch;

    long WaitNextMillis()
    {
        var timestamp = TimeStamp();
        if (timestamp <= m_LastTimestamp - k_MaxSpinMilliseconds)
        {
            return m_LastTimestamp + 1;
        }
        while (timestamp <= m_LastTimestamp)
            timestamp = TimeStamp();
        return timestamp;
    }
}
