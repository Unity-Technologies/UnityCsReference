// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using UnityEngine.Bindings;

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    internal static class DictionarySerialization
    {
        /// <summary>
        /// One serialized entry for a <c>Dictionary&lt;TKey, TValue&gt;</c>: the key/value pair as stored in the
        /// backing <c>Entry[]</c> array during Unity serialization (YAML/binary). Matches the layout native code uses
        /// when emitting dictionary collection commands.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        internal struct SerializedKeyValue<TKey, TValue>
        {
            [SerializeField] public TKey key;
            [SerializeField] public TValue value;

            public SerializedKeyValue(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        internal const string KeyFieldName = nameof(SerializedKeyValue<int, int>.key);
        internal const string ValueFieldName = nameof(SerializedKeyValue<int, int>.value);

        /// <summary>
        /// Static context for duplicate dictionary entries. Non-null only in the Editor (including play mode in the Editor);
        /// the player leaves this null so duplicate rows are not tracked.
        /// </summary>
        /// <remarks>
        /// Threading: set once per domain on the main thread from <see cref="UnityEditor.DictionarySerializationDuplicateEntriesCleanup.Initialize"/>,
        /// which is invoked under <c>[OnCodeLoaded]</c>. <c>[OnCodeLoaded]</c> is sequenced before
        /// <c>SerializableManagedRefsUtilities::RestoreBackups</c> and before any worker-thread serialization in the new domain,
        /// which provides the happens-before relationship relied on by readers. Duplicate-entry callbacks may read this property
        /// from worker threads during serialization; no explicit memory barrier is required for those reads.
        /// </remarks>
        internal static IDuplicateEntriesForDictionaries s_DuplicateEntriesForDictionaries { get; set; }

        [FreeFunction("DictionaryFieldUniqueIdentifierBindings::FormatDictionaryFieldUniqueIdentifierForActiveContext", IsThreadSafe = true)]
        [NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/DictionaryFieldUniqueIdentifierStack.h")]
        private static extern string Internal_FormatDictionaryFieldUniqueIdentifierForActiveContext(IntPtr dictionaryIdentifierTemplateUtf8);

        // Read-path helper: skips native path formatting unless this host already has stored duplicates.
        // Write path receives the already-formatted identifier from native via SetEntriesFromSerializedData.
        static string ResolveDictionaryFieldUniqueIdentifierForDuplicateLookups(EntityId hostingEntityId, IntPtr dictionaryIdentifierTemplateUtf8)
        {
            if (s_DuplicateEntriesForDictionaries == null || dictionaryIdentifierTemplateUtf8 == IntPtr.Zero)
                return string.Empty;
            if (hostingEntityId == EntityId.None)
                return string.Empty;
            if (!s_DuplicateEntriesForDictionaries.HostHasDuplicateDictionaryEntries(hostingEntityId))
                return string.Empty;
            return Internal_FormatDictionaryFieldUniqueIdentifierForActiveContext(dictionaryIdentifierTemplateUtf8) ?? string.Empty;
        }

        internal static bool HostHasDuplicateDictionaryEntries(EntityId entityId)
        {
            return s_DuplicateEntriesForDictionaries != null && s_DuplicateEntriesForDictionaries.HostHasDuplicateDictionaryEntries(entityId);
        }

        internal static int PruneDuplicateDictionaryEntriesForUnloadedHosts()
        {
            if (s_DuplicateEntriesForDictionaries == null)
                return 0;
            return s_DuplicateEntriesForDictionaries.PruneUnloadedHosts();
        }

        internal static bool HasAnyCachedDuplicateDictionaryHosts()
        {
            return s_DuplicateEntriesForDictionaries != null && s_DuplicateEntriesForDictionaries.HasAnyCachedHosts;
        }

        private delegate bool SetEntriesTypedDelegate(EntityId hostingEntityId, object dictionary, Array array, string dictionaryIdentifier, out bool hadDuplicates);
        private static readonly MethodInfo s_SetEntriesTypedInfo = typeof(DictionarySerialization).GetMethod(nameof(SetEntriesTyped), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly ConcurrentDictionary<Type, SetEntriesTypedDelegate> s_SetEntriesTypedCache = new ConcurrentDictionary<Type, SetEntriesTypedDelegate>();

        private delegate Array GetEntriesTypedDelegate(EntityId hostingEntityId, object dictionary, IntPtr dictionaryIdentifierTemplateUtf8);
        private static readonly MethodInfo s_GetEntriesTypedInfo = typeof(DictionarySerialization).GetMethod(nameof(GetEntriesTyped), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly ConcurrentDictionary<Type, GetEntriesTypedDelegate> s_GetEntriesTypedCache = new ConcurrentDictionary<Type, GetEntriesTypedDelegate>();

        private static bool SetEntriesTyped<TKey, TValue>(EntityId hostingEntityId, object dictionary, Array array, string dictionaryIdentifier, out bool hadDuplicates)
        {
            hadDuplicates = false;

            if (dictionary is not Dictionary<TKey, TValue> dict)
                return false;

            var entries = (SerializedKeyValue<TKey, TValue>[])array;
            dict.Clear();
            dict.EnsureCapacity(entries.Length);

            List<int> duplicateIndices = null;
            List<SerializedKeyValue<TKey, TValue>> duplicateEntries = null;

            // Track entries skipped because TryAdd threw (e.g. a user-defined GetHashCode/Equals on the
            // key type raised). We log a single warning at the end with the first failure's details and
            // the total skip count instead of one warning per entry, to avoid console spam when the key
            // type is broken across many rows.
            int skippedDueToException = 0;
            int firstSkippedIndex = -1;
            Exception firstSkippedException = null;

            for (int i = 0; i < entries.Length; i++)
            {
                TKey key = entries[i].key;
                TValue value = entries[i].value;
                // Intentional: skip (null, null) entries. The inspector inserts a (null, null) placeholder
                // row when the user clicks "+" to add a new dictionary entry but has not yet filled in
                // either field. Such placeholders must not be promoted into the live dictionary because
                // (a) Dictionary<TKey, TValue> with a reference-type TKey rejects null keys via
                // ArgumentNullException at TryAdd time, and (b) preserving placeholders in the live
                // dict would surface them as real entries on the next read pass. A genuine
                // (null, null) row in user data (e.g. Dictionary<string, string>) would also be
                // dropped here without warning, but that shape is indistinguishable from a placeholder
                // and would have failed Dictionary.Add for the same null-key reason anyway.
                if (key is null && value is null)
                    continue;

                bool added;
                try
                {
                    added = dict.TryAdd(key, value);
                }
                catch (Exception ex)
                {
                    if (skippedDueToException == 0)
                    {
                        firstSkippedIndex = i;
                        firstSkippedException = ex;
                    }
                    skippedDueToException++;
                    continue;
                }

                if (!added)
                {
                    duplicateIndices ??= new List<int>();
                    duplicateEntries ??= new List<SerializedKeyValue<TKey, TValue>>();
                    duplicateIndices.Add(i);
                    duplicateEntries.Add(entries[i]);
                }
            }

            if (skippedDueToException > 0)
            {
                string identifierSuffix = string.IsNullOrEmpty(dictionaryIdentifier) ? string.Empty : $" (field '{dictionaryIdentifier}')";
                string firstFailureDetails = $"index {firstSkippedIndex}: {firstSkippedException.GetType().Name}: {firstSkippedException.Message}";
                string countSuffix = skippedDueToException == 1
                    ? "1 entry was skipped"
                    : $"{skippedDueToException} entries were skipped (first failure shown)";
                Debug.LogWarning(
                    $"Dictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>{identifierSuffix} deserialization: {countSuffix}. "
                    + $"TryAdd threw at {firstFailureDetails}. "
                    + "This typically indicates a user-defined GetHashCode or Equals on the key type threw.");
            }

            // Intentional: hadDuplicates is assigned only on the editor duplicate-tracking path. Player
            // builds (s_DuplicateEntriesForDictionaries == null) and editor loads without an active
            // FieldUniqueIdentifierContext (empty dictionaryIdentifier) do not track duplicates and must
            // not surface the Console warning -- the native caller (DictionaryField::SetArray) gates
            // logging on this flag, and the matching DebugAssert there encodes the same invariant.
            if (s_DuplicateEntriesForDictionaries != null && !string.IsNullOrEmpty(dictionaryIdentifier))
            {
                if (duplicateIndices == null)
                    s_DuplicateEntriesForDictionaries.Clear(hostingEntityId, dictionaryIdentifier);
                else
                {
                    s_DuplicateEntriesForDictionaries.Store(hostingEntityId, dictionaryIdentifier, new DuplicateEntriesData(duplicateIndices.ToArray(), duplicateEntries.ToArray(), dict.Count));
                    hadDuplicates = duplicateIndices.Count > 0;
                }
            }

            return true;
        }

        private static SetEntriesTypedDelegate GetSetEntriesTypedDelegate(Type[] dictArgs)
        {
            Type cacheKey = typeof(SerializedKeyValue<,>).MakeGenericType(dictArgs);
            return s_SetEntriesTypedCache.GetOrAdd(cacheKey, _ =>
            {
                var method = s_SetEntriesTypedInfo.MakeGenericMethod(dictArgs);
                return (SetEntriesTypedDelegate)Delegate.CreateDelegate(typeof(SetEntriesTypedDelegate), method);
            });
        }

        private static Array GetEntriesTyped<TKey, TValue>(EntityId hostingEntityId, object dictionary, IntPtr dictionaryIdentifierTemplateUtf8)
        {
            if (dictionary is not Dictionary<TKey, TValue> dict)
                return null;

            string dictionaryPath = ResolveDictionaryFieldUniqueIdentifierForDuplicateLookups(hostingEntityId, dictionaryIdentifierTemplateUtf8);

            int count = dict.Count;
            DuplicateEntriesData storedDuplicates = default;
            if (s_DuplicateEntriesForDictionaries != null && !string.IsNullOrEmpty(dictionaryPath))
                storedDuplicates = s_DuplicateEntriesForDictionaries.Get(hostingEntityId, dictionaryPath);

            int duplicateCount = 0;
            if (storedDuplicates.indices != null && storedDuplicates.entries != null)
            {
                // Per DuplicateEntriesData's contract: when the arrays are non-null they are also same-length
                // (asserted at construction) and indices is strictly ascending (caller-maintained at the sole
                // producer SetEntriesTyped). Both null = default = cache miss.
                duplicateCount = storedDuplicates.indices.Length;
            }

            // Normal fast path. No cached duplicate entries; only live dictionary pairs are serialized.
            if (duplicateCount == 0)
            {
                var fastPathResult = new SerializedKeyValue<TKey, TValue>[count];
                int fastIndex = 0;
                foreach (KeyValuePair<TKey, TValue> kvp in dict)
                    fastPathResult[fastIndex++] = new SerializedKeyValue<TKey, TValue>(kvp.Key, kvp.Value);

                return fastPathResult;
            }
            else
            {
                return GetArrayWithHandledDuplicateEntries(dict, hostingEntityId, dictionaryPath, count, storedDuplicates, duplicateCount);
            }
        }

        private static Array GetArrayWithHandledDuplicateEntries<TKey, TValue>(Dictionary<TKey, TValue> dict, EntityId hostingEntityId, string dictionaryPath, int count, DuplicateEntriesData storedDuplicates, int duplicateCount)
        {
            if (count == storedDuplicates.dictionaryCountWhenRecorded)
            {
                // When the dictionary count still matches the count from when duplicate entries were recorded, preserve original serialized indices so ordering in saved files is unchanged.

                var typedDuplicateEntries = (SerializedKeyValue<TKey, TValue>[])storedDuplicates.entries;
                int totalSize = count + duplicateCount;
                var result = new SerializedKeyValue<TKey, TValue>[totalSize];

                // Single bounds precondition: indices is strictly-ascending by DuplicateEntriesData's
                // caller-maintained contract (upheld at the sole producer SetEntriesTyped), so checking the
                // maximum (last) index covers every entry without a per-iteration assert below.
                Assert.IsTrue(duplicateCount == 0 || storedDuplicates.indices[duplicateCount - 1] < totalSize,
                    "Duplicate entry index out of bounds");

                for (int i = 0; i < duplicateCount; i++)
                    result[storedDuplicates.indices[i]] = typedDuplicateEntries[i];

                // Linear lockstep merge: walk the sorted duplicate-index array and the live dict together,
                // emitting each kvp into the next non-duplicate slot. Replaces the previous per-call
                // HashSet<int> membership lookup with a single int of state, eliminating the only avoidable
                // allocation on this hot inspector-repaint path. Correctness depends on indices being strictly
                // ascending; this is upheld by the sole producer site DictionarySerialization.SetEntriesTyped
                // (see DuplicateEntriesData's caller contract).
                int dupIdxPos = 0;
                int writeSlot = 0;
                foreach (KeyValuePair<TKey, TValue> kvp in dict)
                {
                    while (dupIdxPos < duplicateCount && storedDuplicates.indices[dupIdxPos] == writeSlot)
                    {
                        writeSlot++;
                        dupIdxPos++;
                    }
                    if (writeSlot >= totalSize)
                        break;
                    result[writeSlot] = new SerializedKeyValue<TKey, TValue>(kvp.Key, kvp.Value);
                    writeSlot++;
                }

                return result;
            }
            else
            {
                // Dictionary count changed since duplicates were recorded, meaning the live dictionary was
                // mutated outside of the inspector. Cached duplicate indices and entries can no longer be
                // mapped back into the serialized view in a meaningful way, so drop the cache entirely and
                // serialize only the live dictionary entries. Any duplicate rows previously preserved in
                // the cache are discarded.

                if (s_DuplicateEntriesForDictionaries != null && !string.IsNullOrEmpty(dictionaryPath))
                    s_DuplicateEntriesForDictionaries.Clear(hostingEntityId, dictionaryPath);

                var liveOnlyResult = new SerializedKeyValue<TKey, TValue>[count];
                int writeIndex = 0;
                foreach (KeyValuePair<TKey, TValue> kvp in dict)
                    liveOnlyResult[writeIndex++] = new SerializedKeyValue<TKey, TValue>(kvp.Key, kvp.Value);

                return liveOnlyResult;
            }
        }

        private static GetEntriesTypedDelegate GetGetEntriesTypedDelegate(Type[] dictArgs)
        {
            Type cacheKey = typeof(SerializedKeyValue<,>).MakeGenericType(dictArgs);
            return s_GetEntriesTypedCache.GetOrAdd(cacheKey, _ =>
            {
                var method = s_GetEntriesTypedInfo.MakeGenericMethod(dictArgs);
                return (GetEntriesTypedDelegate)Delegate.CreateDelegate(typeof(GetEntriesTypedDelegate), method);
            });
        }

        /// <summary>
        /// Returns the array indices of duplicate keys that could not be merged
        /// into the live dictionary, for the given host and dictionary field path. Used by the Editor (e.g.
        /// <see cref="UnityEditor.SerializedProperty.GetDictionaryDuplicateEntryIndices"/>) to highlight or preserve duplicate rows.
        /// </summary>
        internal static int[] GetDuplicateIndices(EntityId entityId, string dictionaryPropertyPath)
        {
            if (s_DuplicateEntriesForDictionaries == null)
                return Array.Empty<int>();
            if (entityId == EntityId.None || string.IsNullOrEmpty(dictionaryPropertyPath))
                return Array.Empty<int>();
            var data = s_DuplicateEntriesForDictionaries.Get(entityId, dictionaryPropertyPath);
            return data.indices ?? Array.Empty<int>();
        }

        #region Required by native code

        /// <summary>
        /// Deserializes a dictionary from the native backing <c>Entry[]</c> array: clears the dictionary and repopulates it
        /// from each serialized key/value entry. Duplicate keys are tracked in
        /// <see cref="s_DuplicateEntriesForDictionaries"/> when the Editor context is set so Apply/Update can preserve them.
        /// </summary>
        /// <param name="hadDuplicates">Editor-only signal: set to <c>true</c> only on the duplicate-tracking path
        /// (when <see cref="s_DuplicateEntriesForDictionaries"/> is non-null and <paramref name="dictionaryIdentifier"/>
        /// is non-empty). Always <c>false</c> in player builds (no duplicate tracking) and on Editor loads without an
        /// active FieldUniqueIdentifierContext (no identifier to anchor a warning to). The native caller uses this as
        /// the gate for emitting the Editor-only Console warning in <c>DictionaryField::SetArray</c>. Always set even
        /// when this method returns <c>false</c>.</param>
        [RequiredByNativeCode]
        internal static bool SetEntriesFromSerializedData(EntityId hostingEntityId, object dictionary, object entriesArray, string dictionaryIdentifier, out bool hadDuplicates)
        {
            hadDuplicates = false;

            if (dictionary == null)
                return false;

            if (dictionary is not IDictionary dict)
                return false;

            Array array = entriesArray as Array;
            if (array == null)
                return false;

            Type entryType = array.GetType().GetElementType();
            Type dictType = dict.GetType();
            Type[] dictArgs = dictType.GetGenericArguments();

            if (!IsGenericDictionaryEntryType(entryType, dictArgs))
                return false;

            var setEntries = GetSetEntriesTypedDelegate(dictArgs);
            return setEntries(hostingEntityId, dictionary, array, dictionaryIdentifier, out hadDuplicates);
        }

        /// <summary>
        /// Returns the SerializedKeyValue&lt;TKey, TValue&gt; type for the given generic dictionary type, or null if not a generic dictionary.
        /// Used by the native serialization backend to resolve the element class when building the command queue.
        /// </summary>
        [RequiredByNativeCode]
        internal static Type GetDictionaryEntryTypeForSerialization(Type dictionaryType)
        {
            if (dictionaryType == null || !dictionaryType.IsGenericType)
                return null;
            Type def = dictionaryType.GetGenericTypeDefinition();
            Type[] args = dictionaryType.GetGenericArguments();
            if (args.Length != 2)
                return null;
            return typeof(SerializedKeyValue<,>).MakeGenericType(args);
        }

        /// <summary>
        /// Builds the array of dictionary entries for serialization (write path). Fills SerializedKeyValue&lt;TKey, TValue&gt;[] from the dictionary's
        /// key/value pairs and any duplicate entries stored in the static context at their original indices.
        /// </summary>
        [RequiredByNativeCode]
        internal static Array GetDictionaryEntriesForSerialization(EntityId hostingEntityId, object dictionary, IntPtr dictionaryIdentifierTemplateUtf8)
        {
            if (dictionary == null)
                return null;

            Type dictType = dictionary.GetType();
            if (!dictType.IsGenericType)
                return null;

            Type[] dictArgs = dictType.GetGenericArguments();
            if (dictArgs.Length != 2)
                return null;

            var getEntries = GetGetEntriesTypedDelegate(dictArgs);
            return getEntries(hostingEntityId, dictionary, dictionaryIdentifierTemplateUtf8);
        }

        #endregion

        private static bool IsGenericDictionaryEntryType(Type entryType, Type[] dictGenericArgs)
        {
            if (entryType == null || !entryType.IsGenericType || dictGenericArgs == null || dictGenericArgs.Length != 2)
                return false;
            Type def = entryType.GetGenericTypeDefinition();
            if (def != typeof(SerializedKeyValue<,>))
                return false;
            Type[] entryArgs = entryType.GetGenericArguments();
            return entryArgs.Length == 2 && entryArgs[0] == dictGenericArgs[0] && entryArgs[1] == dictGenericArgs[1];
        }
    }
}
