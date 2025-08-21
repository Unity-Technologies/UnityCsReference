// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class NodeScheduler
    {
        private class TurnGroup
        {
            private readonly HashSet<Turn> m_Turns = new();
            private readonly bool m_Single;

            public bool IsSingle => m_Single;
            public int Count => m_Turns.Count;

            private TurnGroup(bool single) => m_Single = single;

            public void Add(Turn turn)
            {
                Assert.IsFalse(m_Single && m_Turns.Count > 0, "Cannot add turn to isolation group");
                Assert.IsFalse(s_TurnToGroup.ContainsKey(turn), "Turn already assigned");

                m_Turns.Add(turn);
                s_TurnToGroup[turn] = this;
            }

            public void Remove(Turn turn)
            {
                Assert.IsTrue(m_Turns.Contains(turn), "Turn not in group");

                m_Turns.Remove(turn);
                s_TurnToGroup.Remove(turn);

                if (m_Turns.Count == 0)
                    s_Queue.Remove(this);
            }

            public static TurnGroup CreateGroup(params Turn[] turns)
            {
                var group = new TurnGroup(false);
                foreach (var turn in turns)
                    group.Add(turn);
                return group;
            }

            public static TurnGroup CreateSingleGroup(Turn turn)
            {
                var group = new TurnGroup(true);
                group.Add(turn);
                return group;
            }
        }

        public class Turn : IDisposable
        {
            public bool IsValid => s_TurnToGroup.ContainsKey(this);

            public void Dispose()
            {
                if (!IsValid)
                    return;
                    
                var group = s_TurnToGroup[this];
                group.Remove(this);
            }

            public async Task UntilActive(CancellationToken cancellationToken = default)
            {
                do
                {
                    if (!IsValid)
                        throw new InvalidOperationException("Trying to wait on disposed turn");

                    var taskGroup = s_TurnToGroup[this];
                    var currentGroup = s_Queue.First.Value;

                    if (taskGroup == currentGroup)
                        return;

                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                } while(true);
            }
        }

        private static readonly LinkedList<TurnGroup> s_Queue = new();
        private static readonly Dictionary<Turn, TurnGroup> s_TurnToGroup = new();

        public static Turn AssignTurn(bool runInIsolation)
        {
            var turn = new Turn();

            if (runInIsolation)
            {
                s_Queue.AddLast(TurnGroup.CreateSingleGroup(turn));
                return turn;
            }

            var lastTurnGroup = s_Queue.Last;
            if (lastTurnGroup != null && !lastTurnGroup.Value.IsSingle)
            {
                lastTurnGroup.Value.Add(turn);
                return turn;
            }

            s_Queue.AddLast(TurnGroup.CreateGroup(turn));
            return turn;
        }

        public static char[] Describe()
        {
            var description = new char[s_Queue.Count];

            int i = 0;
            foreach (var group in s_Queue)
            {
                description[i++] = group.IsSingle ? 'i' : (char)('0' + group.Count);
            }
            
            return description;
        }
    }
}
