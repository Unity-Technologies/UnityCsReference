// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Surfaces the Console warning for dictionary fields that deserialize duplicate-key or null-key rows on a
    /// serialized-file load or <see cref="Object.Instantiate(Object)"/> clone (UUM-146883).
    /// </summary>
    /// <remarks>
    /// Dictionary reads run on serialization worker threads, so <see cref="DictionarySerialization"/> just posts the
    /// warning here; the emit is deferred to the main thread, which buys the two things that matter:
    /// <list type="bullet">
    /// <item>redundant warnings are filtered out -- one operation reads the dict through several transient hosts, but
    /// only the still-loaded host survives the emit-time liveness check, so it warns once instead of N times; and</item>
    /// <item>the host's name can be read (a main-thread-only API) and put in the message, so the user can tell which
    /// object owns the offending field -- the clickable ping alone can't (e.g. a child of a Prefab Asset only pings the
    /// asset root).</item>
    /// </list>
    /// Only dictionaries surface warnings today, but the deferred-emit pipeline (capture, enqueue, liveness-filter,
    /// host-resolve, log) is container-neutral. When new containers such as <c>HashSet</c> gain the same warning, this
    /// class can be extended to support them with minor refactoring: wire their hook in <see cref="Initialize"/> and add
    /// the per-container message wording alongside <see cref="ComposeDictionaryMessage"/>.
    /// </remarks>
    internal static partial class DeserializationWarningHandler
    {
        // Captured in Initialize; worker threads Post onto it (Current is per-thread, but Post is thread-safe).
        [NoAutoStaticsCleanup] // re-captured per domain in Initialize on reload
        static SynchronizationContext s_MainThreadContext;

        // [OnCodeLoaded] runs on the main thread before any worker-thread serialization, so the context is
        // captured and the hook wired before the first dictionary read can post a warning.
        [OnCodeLoaded]
        static void Initialize()
        {
            s_MainThreadContext = SynchronizationContext.Current;
            DictionarySerialization.s_PostDictionaryKeyWarning = Enqueue;
        }

        // Worker-thread hook: hand the raw ingredients to the main thread for a deferred, filtered emit.
        static void Enqueue(EntityId hostingEntityId, string fieldIdentifier, bool hadDuplicates, bool hadNullKeys)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldIdentifier));
            s_MainThreadContext?.Post(Emit, (hostingEntityId, fieldIdentifier, hadDuplicates, hadNullKeys));
        }

        // Main thread, once the queue next drains: log a single clickable warning per still-loaded host. The
        // 'Resources.IsInstanceLoaded(host)' check does two things: it dedups the N transient hosts one operation reads
        // through down to the one still-loaded host (so we warn once, not N times), and it guarantees the logged object
        // is pingable -- transient import objects (e.g. during Prefab import) are dropped rather than logged unclickable.
        static void Emit(object state)
        {
            var (host, fieldIdentifier, hadDuplicates, hadNullKeys) = ((EntityId, string, bool, bool))state;
            if (!Resources.IsInstanceLoaded(host))
                return;
            var context = Object.FindObjectFromInstanceID(host);
            string message = ComposeDictionaryMessage(context.name, fieldIdentifier, hadDuplicates, hadNullKeys);
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, context, "{0}", message);
        }

        static string ComposeDictionaryMessage(string hostName, string fieldIdentifier, bool hadDuplicates, bool hadNullKeys)
        {
            Debug.Assert(hadDuplicates || hadNullKeys);
            string body = string.Empty;
            if (hadDuplicates)
                body = "contains duplicate key entries. Ensure all keys are unique. Only the first occurrence of each key will be added to the dictionary object.";
            if (hadNullKeys)
            {
                if (body.Length > 0)
                    body += " It also ";
                body += "contains entries with a null key. A dictionary cannot contain a null key, so Unity excludes these entries from the dictionary object.";
            }
            return $"Dictionary field '{fieldIdentifier}' on '{hostName}' {body}";
        }
    }
}
