// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEditor
{
    internal class NetworkDetailStats
    {
        public enum NetworkDirection
        {
            Incoming,
            Outgoing
        };

        // keep this many ticks of packet history
        const int kPacketHistoryTicks = 20;

        // how often a tick happens
        const float kPacketTickInterval = 0.5f;

        // a sequence of statistics for a particular operation entry
        internal class NetworkStatsSequence
        {
            int[] m_MessagesPerTick = new int[kPacketHistoryTicks];
            public int MessageTotal;

            public void Add(int tick, int amount)
            {
                m_MessagesPerTick[tick] += amount;
                MessageTotal += amount;
            }

            public void NewProfilerTick(int tick)
            {
                MessageTotal -= m_MessagesPerTick[tick];
                m_MessagesPerTick[tick] = 0;
            }

            public int GetFiveTick(int tick)
            {
                // average per ticks over last 5 ticks
                int count = 0;
                for (int i = 0; i < 5; i++)
                {
                    count += m_MessagesPerTick[(tick - i + kPacketHistoryTicks) % kPacketHistoryTicks];
                }
                return (int)(count / 5);
            }

            public int GetTenTick(int tick)
            {
                // average per ticks over last 10 ticks
                int count = 0;
                for (int i = 0; i < 10; i++)
                {
                    count += m_MessagesPerTick[(tick - i + kPacketHistoryTicks) % kPacketHistoryTicks];
                }
                return (int)(count / 10);
            }
        }

        // detailed stats for one entry of a network operation, such as a command or rpc-call
        internal class NetworkOperationEntryDetails
        {
            public string m_EntryName;
            public int m_IncomingTotal;
            public int m_OutgoingTotal;
            public NetworkStatsSequence m_IncomingSequence = new NetworkStatsSequence();
            public NetworkStatsSequence m_OutgoingSequence = new NetworkStatsSequence();

            public void NewProfilerTick(int tickId)
            {
                m_IncomingSequence.NewProfilerTick(tickId);
                m_OutgoingSequence.NewProfilerTick(tickId);
            }

            public void Clear()
            {
                m_IncomingTotal = 0;
                m_OutgoingTotal = 0;
            }

            public void AddStat(NetworkDirection direction, int amount)
            {
                int tickId = ((int)s_LastTickTime) % kPacketHistoryTicks;
                switch (direction)
                {
                    case NetworkDirection.Incoming:
                    {
                        m_IncomingTotal += amount;
                        m_IncomingSequence.Add(tickId, amount);
                    }
                    break;

                    case NetworkDirection.Outgoing:
                    {
                        m_OutgoingTotal += amount;
                        m_OutgoingSequence.Add(tickId, amount);
                    }
                    break;
                }
            }
        }

        // detailed stats for one type of network operation (HLAPI MsgId)
        internal class NetworkOperationDetails
        {
            public short MsgId;
            public float totalIn;
            public float totalOut;
            public Dictionary<string, NetworkOperationEntryDetails> m_Entries = new Dictionary<string, NetworkOperationEntryDetails>();

            public void NewProfilerTick(int tickId)
            {
                foreach (var entry in m_Entries.Values)
                {
                    entry.NewProfilerTick(tickId);
                }
                NetworkTransport.SetPacketStat(0, MsgId, (int)totalIn, 1);
                NetworkTransport.SetPacketStat(1, MsgId, (int)totalOut, 1);
                totalIn = 0;
                totalOut = 0;
            }

            public void Clear()
            {
                foreach (var entry in m_Entries.Values)
                {
                    entry.Clear();
                }
                totalIn = 0;
                totalOut = 0;
            }

            public void SetStat(NetworkDirection direction, string entryName, int amount)
            {
                NetworkOperationEntryDetails entry;
                if (m_Entries.ContainsKey(entryName))
                {
                    entry = m_Entries[entryName];
                }
                else
                {
                    entry = new NetworkOperationEntryDetails();
                    entry.m_EntryName = entryName;
                    m_Entries[entryName] = entry;
                }
                entry.AddStat(direction, amount);

                switch (direction)
                {
                    case NetworkDirection.Incoming:
                    {
                        totalIn = amount;
                    }
                    break;

                    case NetworkDirection.Outgoing:
                    {
                        totalOut = amount;
                    }
                    break;
                }
            }

            public void IncrementStat(NetworkDirection direction, string entryName, int amount)
            {
                NetworkOperationEntryDetails entry;
                if (m_Entries.ContainsKey(entryName))
                {
                    entry = m_Entries[entryName];
                }
                else
                {
                    entry = new NetworkOperationEntryDetails();
                    entry.m_EntryName = entryName;
                    m_Entries[entryName] = entry;
                }
                entry.AddStat(direction, amount);

                switch (direction)
                {
                    case NetworkDirection.Incoming:
                    {
                        totalIn += amount;
                    }
                    break;

                    case NetworkDirection.Outgoing:
                    {
                        totalOut += amount;
                    }
                    break;
                }
            }
        }

        static internal Dictionary<short, NetworkOperationDetails> m_NetworkOperations = new Dictionary<short, NetworkOperationDetails>();

        static float s_LastTickTime;

        static public void NewProfilerTick(float newTime)
        {
            if (newTime - s_LastTickTime > kPacketTickInterval)
            {
                s_LastTickTime = newTime;
                int tickId = ((int)s_LastTickTime) % kPacketHistoryTicks;
                foreach (var op in m_NetworkOperations.Values)
                {
                    op.NewProfilerTick(tickId);
                }
            }
        }

        static public void SetStat(NetworkDirection direction, short msgId, string entryName, int amount)
        {
            NetworkOperationDetails op;
            if (m_NetworkOperations.ContainsKey(msgId))
            {
                op = m_NetworkOperations[msgId];
            }
            else
            {
                op = new NetworkOperationDetails();
                op.MsgId = msgId;
                m_NetworkOperations[msgId] = op;
            }

            op.SetStat(direction, entryName, amount);
        }

        static public void IncrementStat(NetworkDirection direction, short msgId, string entryName, int amount)
        {
            NetworkOperationDetails op;
            if (m_NetworkOperations.ContainsKey(msgId))
            {
                op = m_NetworkOperations[msgId];
            }
            else
            {
                op = new NetworkOperationDetails();
                op.MsgId = msgId;
                m_NetworkOperations[msgId] = op;
            }

            op.IncrementStat(direction, entryName, amount);
        }

        static public void ResetAll()
        {
            foreach (var detail in m_NetworkOperations.Values)
            {
                NetworkTransport.SetPacketStat(0, detail.MsgId, 0, 1);
                NetworkTransport.SetPacketStat(1, detail.MsgId, 0, 1);
            }
            m_NetworkOperations.Clear();
        }
    }
}
