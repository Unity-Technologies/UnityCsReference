// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.Json;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class SessionStateJsonRepository<TKey, TState> where TState : class, new()
    {
        readonly StateRepositoryDelegates m_StateRepository;
        readonly string m_Key;
        Dictionary<TKey, TState> m_States;

        // Because this class serializes, it can destroy object references. So extra care must be done when using
        // this class with serializable object references
        SessionStateJsonRepository(StateRepositoryDelegates stateRepository, string key)
        {
            m_StateRepository = stateRepository;
            m_Key = key;
        }

        bool CanDeserialize()
        {
            var serializedData = m_StateRepository.GetStringOrDefaultFunc(m_Key);
            if (!TryDeserialize(serializedData, out m_States))
            {
                m_StateRepository.ClearFunc(m_Key);
                return false;
            }

            return true;
        }

        public static SessionStateJsonRepository<TKey, TState> GetMain(StateRepositoryDelegates stateRepository, string key, out bool hasClearedState)
        {
            DuplicateKeyChecker.CheckDuplicateKey(key);
            var r = new SessionStateJsonRepository<TKey, TState>(stateRepository, key);
            hasClearedState = !r.CanDeserialize();  // Not really an error but we want to test for this situation
            return r;
        }
        public static SessionStateJsonRepository<TKey, TState> GetTest(StateRepositoryDelegates stateRepository, string key)
        {
            var r = new SessionStateJsonRepository<TKey, TState>(stateRepository, key);
            r.CanDeserialize();
            return r;
        }

        public IReadOnlyCollection<KeyValuePair<TKey, TState>> GetAll()
        {
            return new List<KeyValuePair<TKey, TState>>(m_States);
        }

        public int Count => m_States.Count;

        public bool ContainsKey(TKey identifier) => m_States.ContainsKey(identifier);

        public bool TryGetValue(TKey identifier, out TState value) => m_States.TryGetValue(identifier, out value);

        public bool Create(TKey identifier, TState state)
        {
            if (m_States.ContainsKey(identifier))
            {
                return false;
            }
            m_States[identifier] = state;
            m_StateRepository.SaveFunc(m_Key, Serialize(m_States));
            return true;
        }

        public bool Update(TKey identifier, Action<TState> update, out TState state)
        {
            if (m_States.TryGetValue(identifier, out state))
            {
                update.Invoke(state);
                m_StateRepository.SaveFunc(m_Key, Serialize(m_States));
                return true;
            }

            return false;
        }

        public bool Delete(TKey identifier)
        {
            if (m_States.ContainsKey(identifier))
            {
                if (m_States.Remove(identifier))
                {
                    m_StateRepository.SaveFunc(m_Key, Serialize(m_States));
                    return true;
                }
                return false;
            }

            return false;
        }

        bool TryDeserialize(string serializedData, out Dictionary<TKey, TState> states)
        {
            if (string.IsNullOrEmpty(serializedData))
            {
                states = new Dictionary<TKey, TState>();
                return true;
            }

            try
            {
                // We have to use lists because complex Dictionary keys are not serialized properly.
                var list = JsonSerializer.Deserialize<List<KeyValuePair<TKey, TState>>>(serializedData, SessionStateJsonSerialization.SerializerOptions);
                if (list == null)
                {
                    states = new Dictionary<TKey, TState>();
                    return false;
                }

                states = new Dictionary<TKey, TState>();
                foreach (var (key, value) in list)
                {
                    states.Add(key, value);
                }

                return true;
            }
            catch (Exception)
            {
                states = new Dictionary<TKey, TState>();
                return false;
            }
        }

        string Serialize(Dictionary<TKey, TState> states)
        {
            var list = new List<KeyValuePair<TKey, TState>>(states);  // This is because we can serialize lists (not dictionaries)
            return JsonSerializer.Serialize(list, SessionStateJsonSerialization.SerializerOptions);
        }

        public static void DeleteAll(SessionStateJsonRepository<TKey, TState> sessionStateJsonRepository)
        {
            sessionStateJsonRepository.m_States = new Dictionary<TKey, TState>();
            sessionStateJsonRepository.m_StateRepository.ClearFunc(sessionStateJsonRepository.m_Key);
        }
    }

    // This has to be a separate class since SessionStateJsonRepository is a Generic static class and we want to
    // make sure the key is not duplicated regardless of each generated class from the generic.
    // https://stackoverflow.com/questions/3037203/are-static-members-of-a-generic-class-tied-to-the-specific-instance
    static partial class DuplicateKeyChecker
    {
        [AutoStaticsCleanupOnCodeReload] // keys re-registered on reload; old keys would cause false-positive duplicate exceptions
        // Duplicate-key guard set: keys are added as repositories register them, so the set cleared on reload
        // refills as those registrations happen again (and stale keys would report false duplicates).
        [IgnoreForUAL0015("Duplicate-key guard set refilled as repository keys are registered again")]
        static HashSet<string> RuntimeCheckOfNonDuplicateKeys = new HashSet<string>();

        public static void Clear()
        {
            RuntimeCheckOfNonDuplicateKeys.Clear();
        }

        public static void CheckDuplicateKey(string key)
        {
            if (RuntimeCheckOfNonDuplicateKeys.Contains(key))
                throw new ArgumentException(key);
            RuntimeCheckOfNonDuplicateKeys.Add(key);
        }
    }

    static class SessionStateJsonSerialization
    {
        // The stored states keep their payload in public fields, which System.Text.Json omits by default
        [NoAutoStaticsCleanup] // immutable serializer options; safe to persist across reload
        public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions { IncludeFields = true };
    }
}
