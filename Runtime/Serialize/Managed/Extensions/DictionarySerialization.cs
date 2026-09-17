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
using Unity.Scripting.LifecycleManagement;

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    /// <summary>
    /// The rows a serialized dictionary preserves outside its live runtime map, returned by
    /// <c>SerializedProperty.GetDictionaryIgnoredEntries</c>. Both arrays are non-null and disjoint.
    /// </summary>
    public readonly struct DictionaryIgnoredEntries
    {
        /// <summary>Array indices of genuine duplicate-key rows that could not be merged into the live dictionary.</summary>
        public int[] duplicateEntryIndices { get; }

        /// <summary>Array indices of null-key placeholder rows shown in the inspector but excluded from the live dictionary.</summary>
        public int[] nullKeyEntryIndices { get; }

        /// <summary>A result with no ignored rows: both index arrays are empty (never null).</summary>
        internal static DictionaryIgnoredEntries Empty => new DictionaryIgnoredEntries(Array.Empty<int>(), Array.Empty<int>());

        internal DictionaryIgnoredEntries(int[] duplicateEntryIndices, int[] nullKeyEntryIndices)
        {
            this.duplicateEntryIndices = duplicateEntryIndices;
            this.nullKeyEntryIndices = nullKeyEntryIndices;
        }
    }

    internal static partial class DictionarySerialization
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
        /// Static context for ignored dictionary entries (duplicate-key and null-key placeholder rows). Non-null only in the Editor (including play mode in the Editor);
        /// the player leaves this null so ignored rows are not tracked.
        /// </summary>
        /// <remarks>
        /// Threading: set once per domain on the main thread from <see cref="UnityEditor.DictionarySerializationIgnoredEntriesCleanup.Initialize"/>,
        /// which is invoked under <c>[OnCodeLoaded]</c>. <c>[OnCodeLoaded]</c> is sequenced before
        /// <c>SerializableManagedRefsUtilities::RestoreBackups</c> and before any worker-thread serialization in the new domain,
        /// which provides the happens-before relationship relied on by readers. Ignored-entry callbacks may read this property
        /// from worker threads during serialization; no explicit memory barrier is required for those reads.
        /// </remarks>
        [AutoStaticsCleanupOnCodeReload]
        internal static IDictionaryIgnoredEntriesCache s_IgnoredEntriesForDictionaries { get; set; }

        [FreeFunction("DictionaryFieldUniqueIdentifierBindings::FormatDictionaryFieldUniqueIdentifierForActiveContext", IsThreadSafe = true)]
        [NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/DictionaryFieldUniqueIdentifierStack.h")]
        private static extern string Internal_FormatDictionaryFieldUniqueIdentifierForActiveContext(IntPtr dictionaryIdentifierTemplateUtf8);

        // Read-path helper: skips native path formatting unless this host already has stored ignored entries.
        // Write path receives the already-formatted identifier from native via SetEntriesFromSerializedData.
        static string ResolveDictionaryFieldUniqueIdentifierForIgnoredLookups(EntityId hostingEntityId, IntPtr dictionaryIdentifierTemplateUtf8)
        {
            if (s_IgnoredEntriesForDictionaries == null || dictionaryIdentifierTemplateUtf8 == IntPtr.Zero)
                return string.Empty;
            if (hostingEntityId == EntityId.None)
                return string.Empty;
            if (!s_IgnoredEntriesForDictionaries.HostHasIgnoredDictionaryEntries(hostingEntityId))
                return string.Empty;
            return Internal_FormatDictionaryFieldUniqueIdentifierForActiveContext(dictionaryIdentifierTemplateUtf8) ?? string.Empty;
        }

        internal static bool HostHasIgnoredDictionaryEntries(EntityId entityId)
        {
            return s_IgnoredEntriesForDictionaries != null && s_IgnoredEntriesForDictionaries.HostHasIgnoredDictionaryEntries(entityId);
        }

        internal static int PruneIgnoredDictionaryEntriesForUnloadedHosts()
        {
            if (s_IgnoredEntriesForDictionaries == null)
                return 0;
            return s_IgnoredEntriesForDictionaries.PruneUnloadedHosts();
        }

        internal static bool HasAnyCachedIgnoredDictionaryHosts()
        {
            return s_IgnoredEntriesForDictionaries != null && s_IgnoredEntriesForDictionaries.HasAnyCachedHosts;
        }

        [AutoStaticsCleanupOnCodeReload] // editor-set delegate; clear on reload so the old-ALC target is not pinned (the editor re-wires it in its [OnCodeLoaded])
        internal static Action<EntityId, string, bool, bool> s_PostDictionaryKeyWarning { get; set; }

        private delegate bool SetEntriesTypedDelegate(EntityId hostingEntityId, object dictionary, Array array, string dictionaryIdentifier, bool warnAboutIgnoredEntries);
        [NoAutoStaticsCleanup] // reflection handle to a method of this CoreModule type, stable across code reload
        private static readonly MethodInfo s_SetEntriesTypedInfo = typeof(DictionarySerialization).GetMethod(nameof(SetEntriesTyped), BindingFlags.NonPublic | BindingFlags.Static);
        [AutoStaticsCleanupOnCodeReload] // keyed by (possibly user) Type, values close over user generic args — clear so old-ALC types are not pinned
        private static readonly ConcurrentDictionary<Type, SetEntriesTypedDelegate> s_SetEntriesTypedCache = new ConcurrentDictionary<Type, SetEntriesTypedDelegate>();

        private delegate Array GetEntriesTypedDelegate(EntityId hostingEntityId, object dictionary, IntPtr dictionaryIdentifierTemplateUtf8);
        [NoAutoStaticsCleanup] // reflection handle to a method of this CoreModule type, stable across code reload
        private static readonly MethodInfo s_GetEntriesTypedInfo = typeof(DictionarySerialization).GetMethod(nameof(GetEntriesTyped), BindingFlags.NonPublic | BindingFlags.Static);
        [AutoStaticsCleanupOnCodeReload] // keyed by (possibly user) Type, values close over user generic args — clear so old-ALC types are not pinned
        private static readonly ConcurrentDictionary<Type, GetEntriesTypedDelegate> s_GetEntriesTypedCache = new ConcurrentDictionary<Type, GetEntriesTypedDelegate>();

        private static bool SetEntriesTyped<TKey, TValue>(EntityId hostingEntityId, object dictionary, Array array, string dictionaryIdentifier, bool warnAboutIgnoredEntries)
        {
            if (dictionary is not Dictionary<TKey, TValue> dict)
                return false;

            var entries = (SerializedKeyValue<TKey, TValue>[])array;
            dict.Clear();
            dict.EnsureCapacity(entries.Length);

            // Rows that can't live in the Dictionary itself but must be preserved for a lossless inspector
            // round-trip: duplicate-key and null-key placeholder rows. Ascending-index (loop appends in ascending i).
            List<int> ignoredIndices = null;
            List<SerializedKeyValue<TKey, TValue>> ignoredEntries = null;
            // The two disjoint subsets of ignoredIndices, tracked separately so the UI can query both in one pass (GetIgnoredEntryIndices).
            List<int> duplicateIndices = null; // rows where TryAdd returned false (duplicate key)
            List<int> nullKeyIndices = null;   // placeholder rows with a null key

            // Entries skipped because TryAdd threw (e.g. a user-defined GetHashCode/Equals raised). We log a
            // single warning at the end with the first failure's details, to avoid per-entry console spam.
            int skippedDueToException = 0;
            int firstSkippedIndex = -1;
            Exception firstSkippedException = null;

            // Hoisted out of the loop: whether TKey is a UnityEngine.Object subtype is fixed per closed generic,
            // so the fake-null lifetime check below is skipped entirely -- no per-entry isinst / boxing -- for the
            // common value-type and non-Object reference key types.
            bool keyIsUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(typeof(TKey));

            for (int i = 0; i < entries.Length; i++)
            {
                TKey key = entries[i].key;
                TValue value = entries[i].value;
                // Null-key placeholder row: the inspector inserts one when the user adds an entry but hasn't filled
                // in the key. It can't go in the live dict, so preserve it (editor-only) without flagging a duplicate.
                // A UnityEngine.Object key is a real managed "fake-null" wrapper when unassigned/missing, so `is null`
                // (CLR reference-null) misses it. We can't use the overloaded ==/bool operator here: this runs on a
                // deserialization worker thread, and their editor path resolves the EntityId (EnsureRunningOnMainThread
                // -> throws). GetCachedPtr() reads the native pointer directly (thread-safe) and is zero exactly when
                // the key references no live native object -- the placeholder case we want to route here. The
                // keyIsUnityObject guard (hoisted above) short-circuits before the cast, so value-type keys never box.
                // `is null` stays first so its short-circuit guards the cast: a genuine CLR-null key (a true-null
                // UnityObject in players, or when no fake-null wrapper was built) must not be dereferenced below.
                bool keyIsNull = key is null || (keyIsUnityObject && ((UnityEngine.Object)(object)key).GetCachedPtr() == IntPtr.Zero);
                if (keyIsNull)
                {
                    ignoredIndices ??= new List<int>();
                    ignoredEntries ??= new List<SerializedKeyValue<TKey, TValue>>();
                    ignoredIndices.Add(i);
                    ignoredEntries.Add(entries[i]);
                    nullKeyIndices ??= new List<int>();
                    nullKeyIndices.Add(i);
                    continue;
                }

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
                    ignoredIndices ??= new List<int>();
                    ignoredEntries ??= new List<SerializedKeyValue<TKey, TValue>>();
                    ignoredIndices.Add(i);
                    ignoredEntries.Add(entries[i]);
                    duplicateIndices ??= new List<int>();
                    duplicateIndices.Add(i);
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

            // hadDuplicates/hadNullKeys are assigned only on the editor ignored-tracking path; player builds and editor
            // loads without an active FieldUniqueIdentifierContext don't track ignored rows and must not surface a warning.
            if (s_IgnoredEntriesForDictionaries != null && !string.IsNullOrEmpty(dictionaryIdentifier))
            {
                if (ignoredIndices == null)
                    s_IgnoredEntriesForDictionaries.Clear(hostingEntityId, dictionaryIdentifier);
                else
                {
                    int[] duplicateKeyIndices = duplicateIndices?.ToArray() ?? Array.Empty<int>();
                    int[] nullKeyIndicesArray = nullKeyIndices?.ToArray() ?? Array.Empty<int>();
                    s_IgnoredEntriesForDictionaries.Store(hostingEntityId, dictionaryIdentifier, new IgnoredEntriesData(ignoredIndices.ToArray(), ignoredEntries.ToArray(), dict.Count, duplicateKeyIndices, nullKeyIndicesArray));
                    // Both duplicate-key rows and null-key placeholder rows are excluded from the live dictionary and
                    // drive a single combined Console warning on load/instantiate.
                    bool hadDuplicates = duplicateKeyIndices.Length > 0;
                    bool hadNullKeys = nullKeyIndicesArray.Length > 0;

                    if (warnAboutIgnoredEntries && (hadDuplicates || hadNullKeys) && hostingEntityId != EntityId.None)
                        s_PostDictionaryKeyWarning?.Invoke(hostingEntityId, dictionaryIdentifier, hadDuplicates, hadNullKeys);
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

            string dictionaryPath = ResolveDictionaryFieldUniqueIdentifierForIgnoredLookups(hostingEntityId, dictionaryIdentifierTemplateUtf8);

            int count = dict.Count;
            IgnoredEntriesData storedIgnored = default;
            if (s_IgnoredEntriesForDictionaries != null && !string.IsNullOrEmpty(dictionaryPath))
                storedIgnored = s_IgnoredEntriesForDictionaries.Get(hostingEntityId, dictionaryPath);
            int ignoredCount = 0;
            if (storedIgnored.indices != null && storedIgnored.entries != null)
            {
                // Per IgnoredEntriesData's contract: non-null arrays are same-length and indices strictly
                // ascending. Both null = default = cache miss.
                ignoredCount = storedIgnored.indices.Length;
            }

            // Normal fast path. No cached ignored entries; only live dictionary pairs are serialized.
            if (ignoredCount == 0)
            {
                var fastPathResult = new SerializedKeyValue<TKey, TValue>[count];
                int fastIndex = 0;
                foreach (KeyValuePair<TKey, TValue> kvp in dict)
                    fastPathResult[fastIndex++] = new SerializedKeyValue<TKey, TValue>(kvp.Key, kvp.Value);

                return fastPathResult;
            }
            else
            {
                return GetArrayWithHandledIgnoredEntries(dict, hostingEntityId, dictionaryPath, count, storedIgnored, ignoredCount);
            }
        }

        private static Array GetArrayWithHandledIgnoredEntries<TKey, TValue>(Dictionary<TKey, TValue> dict, EntityId hostingEntityId, string dictionaryPath, int count, IgnoredEntriesData storedIgnored, int ignoredCount)
        {
            if (count == storedIgnored.dictionaryCountWhenRecorded)
            {
                // Count still matches when the ignored rows were recorded: preserve original serialized indices so ordering in saved files is unchanged.

                var typedIgnoredEntries = (SerializedKeyValue<TKey, TValue>[])storedIgnored.entries;
                int totalSize = count + ignoredCount;
                var result = new SerializedKeyValue<TKey, TValue>[totalSize];

                // indices is strictly-ascending (IgnoredEntriesData's contract), so checking the maximum
                // (last) index covers every entry without a per-iteration assert.
                Assert.IsTrue(ignoredCount == 0 || storedIgnored.indices[ignoredCount - 1] < totalSize,
                    "Ignored entry index out of bounds");

                for (int i = 0; i < ignoredCount; i++)
                    result[storedIgnored.indices[i]] = typedIgnoredEntries[i];

                // Linear lockstep merge: walk the sorted ignored-index array and the live dict together,
                // emitting each kvp into the next non-ignored slot. Depends on indices being strictly ascending.
                int ignoredIdxPos = 0;
                int writeSlot = 0;
                foreach (KeyValuePair<TKey, TValue> kvp in dict)
                {
                    while (ignoredIdxPos < ignoredCount && storedIgnored.indices[ignoredIdxPos] == writeSlot)
                    {
                        writeSlot++;
                        ignoredIdxPos++;
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
                // Dictionary count changed since the ignored rows were recorded (mutated outside the inspector), so the
                // cached indices can't be mapped back. Drop the cache and serialize only the live entries.

                if (s_IgnoredEntriesForDictionaries != null && !string.IsNullOrEmpty(dictionaryPath))
                    s_IgnoredEntriesForDictionaries.Clear(hostingEntityId, dictionaryPath);

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

        // Reflection handle for the open-generic empty-dictionary factory. The
        // dispatcher closes it over (TKey, TValue) at build time when interning the
        // factory into SerializationCommandObjectTable; see InternDictionaryDefaultAllocateFactory below.
        [NoAutoStaticsCleanup] // reflection handle to a method of this CoreModule type, stable across code reload
        private static readonly MethodInfo s_CreateEmptyDictionaryTypedInfo =
            typeof(DictionarySerialization).GetMethod(nameof(CreateEmptyDictionaryTyped),
                BindingFlags.NonPublic | BindingFlags.Static);

        // Worker for the default-allocate factory: closed over (TKey, TValue) and
        // bound to a Func<object> delegate that the managed-block read dispatcher
        // pulls out of the SerializationCommandObjectTable by integer index.
        private static object CreateEmptyDictionaryTyped<TKey, TValue>()
        {
            return new Dictionary<TKey, TValue>();
        }

        // Helper for the three Intern* methods below. Closes the open-generic
        // method over the dict's (TKey, TValue) and interns the resulting
        // delegate into SerializationCommandObjectTable, returning the int
        // index. Returns -1 when the type can't be resolved.
        private static int InternClosedDelegateForDictionaryType(
            IntPtr dictTypeRaw, MethodInfo openMethod, Type delegateType)
        {
            if (dictTypeRaw == IntPtr.Zero)
                return -1;

            Type dictType = UnityEngine.Serialization.SerializationBackendManagedCommands
                .UnmarshalSystemType(dictTypeRaw);
            if (dictType == null || !dictType.IsGenericType)
                return -1;

            Type[] args = dictType.GetGenericArguments();
            if (args.Length != 2)
                return -1;

            MethodInfo closed = openMethod.MakeGenericMethod(args);
            Delegate del = Delegate.CreateDelegate(delegateType, closed);
            return SerializationCommandObjectTable.Intern(del);
        }

        /// <summary>
        /// Build-time helper: closes <see cref="GetEntriesTyped{TKey,TValue}"/> over the dict's
        /// generic args, interns the typed delegate into <see cref="SerializationCommandObjectTable"/>,
        /// and returns the int index. The managed-block dict command stores this index; the write
        /// dispatcher calls <see cref="InvokeGetEntriesTyped"/> with it to skip per-call
        /// <c>dict.GetType() + GetGenericArguments() + ConcurrentDictionary</c> lookup.
        /// </summary>
        [RequiredByNativeCode]
        internal static int InternGetEntriesTypedDelegate(IntPtr dictTypeRaw)
        {
            return InternClosedDelegateForDictionaryType(
                dictTypeRaw, s_GetEntriesTypedInfo, typeof(GetEntriesTypedDelegate));
        }

        /// <summary>
        /// Build-time helper: closes <see cref="SetEntriesTyped{TKey,TValue}"/> over the dict's
        /// generic args, interns the typed delegate, and returns the int index. The read
        /// dispatcher calls <see cref="InvokeSetEntriesTyped"/> with it.
        /// </summary>
        [RequiredByNativeCode]
        internal static int InternSetEntriesTypedDelegate(IntPtr dictTypeRaw)
        {
            return InternClosedDelegateForDictionaryType(
                dictTypeRaw, s_SetEntriesTypedInfo, typeof(SetEntriesTypedDelegate));
        }

        /// <summary>
        /// Execute-time wrapper for the interned <see cref="GetEntriesTyped{TKey,TValue}"/> delegate.
        /// Falls back to the non-typed entry point (<see cref="GetDictionaryEntriesForSerialization"/>)
        /// when the index is -1, so a build that couldn't intern (e.g. open generic, type lookup
        /// failed) still works correctly — just at the cost of the per-call cache lookup the
        /// non-typed path does.
        /// </summary>
        internal static Array InvokeGetEntriesTyped(
            int idx, EntityId hostingEntityId, object dictionary, IntPtr dictionaryIdentifierTemplate)
        {
            if (idx < 0)
                return GetDictionaryEntriesForSerialization(hostingEntityId, dictionary, dictionaryIdentifierTemplate);
            var del = (GetEntriesTypedDelegate)SerializationCommandObjectTable.Get(idx);
            return del(hostingEntityId, dictionary, dictionaryIdentifierTemplate);
        }

        /// <summary>
        /// Execute-time wrapper for the interned <see cref="SetEntriesTyped{TKey,TValue}"/> delegate.
        /// Falls back to <see cref="SetEntriesFromSerializedData"/> on index -1.
        /// </summary>
        internal static bool InvokeSetEntriesTyped(
            int idx, EntityId hostingEntityId, object dictionary, Array entries,
            string dictionaryIdentifier, bool warnAboutIgnoredEntries)
        {
            if (idx < 0)
                return SetEntriesFromSerializedData(hostingEntityId, dictionary, entries, dictionaryIdentifier, warnAboutIgnoredEntries);
            var del = (SetEntriesTypedDelegate)SerializationCommandObjectTable.Get(idx);
            return del(hostingEntityId, dictionary, entries, dictionaryIdentifier, warnAboutIgnoredEntries);
        }

        /// <summary>
        /// Build-time helper: closes the <see cref="CreateEmptyDictionaryTyped{TKey,TValue}"/>
        /// worker over the dict's <c>(TKey, TValue)</c> generic args, interns the resulting
        /// <c>Func&lt;object&gt;</c> into <see cref="SerializationCommandObjectTable"/>, and
        /// returns the int index. The managed-block dictionary command stores this index in its
        /// header; the read dispatcher (<c>ConsumeDictionaryRead</c>) reads it back and invokes
        /// the interned factory directly when the live dictionary field is null — no reflection,
        /// no hash lookup, no per-call generic dispatch. Matches the legacy
        /// <see cref="DictionaryField"/> ctor's default-allocate behavior at
        /// DictionaryField.cpp:100-102.
        /// </summary>
        /// <param name="dictTypeRaw">Raw runtime type pointer for the closed <c>Dictionary&lt;TKey, TValue&gt;</c>
        /// (i.e. <c>scripting_class_get_type(klass).GetBackendPtr()</c>) — same encoding as the
        /// <c>elementTypeHandle</c> the dict header stamps for the entry type. Returns -1 when
        /// the type can't be resolved or isn't a 2-arg generic.</param>
        [RequiredByNativeCode]
        internal static int InternDictionaryDefaultAllocateFactory(IntPtr dictTypeRaw)
        {
            return InternClosedDelegateForDictionaryType(
                dictTypeRaw, s_CreateEmptyDictionaryTypedInfo, typeof(Func<object>));
        }

        /// <summary>
        /// Single-lookup retrieval of a dictionary's ignored rows: the disjoint duplicate-key and null-key index
        /// sets (both non-null, each empty when there are none). Shares one cache lookup for the Editor UI.
        /// </summary>
        internal static DictionaryIgnoredEntries GetIgnoredEntryIndices(EntityId entityId, string dictionaryPropertyPath)
        {
            if (s_IgnoredEntriesForDictionaries == null || entityId == EntityId.None || string.IsNullOrEmpty(dictionaryPropertyPath))
                return DictionaryIgnoredEntries.Empty;
            var data = s_IgnoredEntriesForDictionaries.Get(entityId, dictionaryPropertyPath);
            return new DictionaryIgnoredEntries(
                data.duplicateKeyIndices ?? Array.Empty<int>(),
                data.nullKeyIndices ?? Array.Empty<int>());
        }

        #region Required by native code

        /// <summary>
        /// Deserializes a dictionary from the native backing <c>Entry[]</c> array: clears the dictionary and repopulates it
        /// from each serialized key/value entry. Ignored rows (duplicate-key and null-key placeholder rows) are tracked in
        /// <see cref="s_IgnoredEntriesForDictionaries"/> when the Editor context is set so Apply/Update can preserve them.
        /// </summary>
        /// <param name="warnAboutIgnoredEntries">Set on the load/instantiate transfer types (serialized-file loads and
        /// <c>Object.Instantiate</c> clones); <c>false</c> for Inspector ApplyModifiedProperties and other in-memory
        /// transfers. When set and an ignored row is found, the editor-only <see cref="s_PostDictionaryKeyWarning"/>
        /// hook is invoked to surface a single combined Console warning (emitted on the main thread once it next drains).</param>
        /// <returns><c>true</c> when the dictionary was recognized and repopulated; <c>false</c> when the argument is
        /// null or not a supported generic <c>Dictionary&lt;TKey, TValue&gt;</c>.</returns>
        [RequiredByNativeCode]
        internal static bool SetEntriesFromSerializedData(EntityId hostingEntityId, object dictionary, object entriesArray, string dictionaryIdentifier, bool warnAboutIgnoredEntries)
        {
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
            return setEntries(hostingEntityId, dictionary, array, dictionaryIdentifier, warnAboutIgnoredEntries);
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
        /// key/value pairs and any ignored entries stored in the static context at their original indices.
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
