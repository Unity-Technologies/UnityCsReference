// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 Dictionary<K,V> element-source handlers (doc
// §2.6). The core's EnterElementSource arms own the structure (count framing,
// staging allocation, element frame and repeat, FUID frame bracketing); these
// handlers own the dictionary side: the GetEntriesTyped/SetEntriesTyped
// bridges (dedup, warnings, ignored-rows cache), default allocation, and the
// identifier-string conversion. The per-instantiation bridge indexes ride the
// command's opaque userData words (source: GetEntriesTyped index; sink:
// SetEntriesTyped in the low 32 bits, the default-allocate index in the high
// 32; -1 means untyped fallback or stay null).
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Write element source: converts the live Dictionary<K,V> to a staged
    // SerializedKeyValue<K,V>[] through the GetEntriesTyped bridge. The core
    // never calls this for a null field. The identifier is only built when a
    // hosting entity is known, because it is only ever consumed alongside a
    // resolved host; a null build from a structural mismatch fails closed.
    private static Array V2DictionaryElementSource(object collection, ref V2FuidBuilder fuid, NativeBufferContext* ctx, ulong userData)
    {
        int getEntriesTypedIndex = unchecked((int)userData);

        byte[] identifierBytes = null;
        if (ctx->hostingEntityId != EntityId.None)
            identifierBytes = fuid.Build();

        if (identifierBytes != null)
        {
            fixed (byte* identifierPtr = identifierBytes)
            {
                return DictionarySerialization.InvokeGetEntriesTyped(
                    getEntriesTypedIndex, ctx->hostingEntityId, collection, (IntPtr)identifierPtr);
            }
        }
        return DictionarySerialization.InvokeGetEntriesTyped(
            getEntriesTypedIndex, ctx->hostingEntityId, collection, IntPtr.Zero);
    }

    // Read element sink: default-allocates a null field through the
    // registered allocator (index -1 leaves it null), then applies the staged
    // entries through SetEntriesTyped, keyed by the dictionary's FUID
    // identifier for the editor duplicate-row cache when a hosting entity is
    // known. The core invokes it at the staging loop's exit, and directly on
    // count 0, inside the dictionary's FUID frame; the open repeats are its
    // collection ancestors, the same stack state the write side builds from.
    private static void V2DictionaryElementSink(ref byte fieldSlot, Array elements, ref V2FuidBuilder fuid, NativeReadBufferContext* ctx, ulong userData)
    {
        int setEntriesTypedIndex = unchecked((int)userData);
        int defaultAllocateFactoryIndex = unchecked((int)(userData >> 32));

        object dictRef = Unsafe.As<byte, object>(ref fieldSlot);
        if (dictRef == null && defaultAllocateFactoryIndex >= 0)
        {
            var factory = (Func<object>)SerializationCommandObjectTable.Get(defaultAllocateFactoryIndex);
            dictRef = factory();
            Unsafe.As<byte, object>(ref fieldSlot) = dictRef;
        }
        if (dictRef == null)
            return;

        string dictionaryIdentifier = string.Empty;
        if (ctx->hostingEntityId != EntityId.None)
        {
            byte[] identifierBytes = fuid.Build();
            if (identifierBytes != null)
            {
                // NUL-terminated UTF-8 in an oversized thread-static buffer.
                int length = 0;
                while (identifierBytes[length] != 0)
                    ++length;
                dictionaryIdentifier = System.Text.Encoding.UTF8.GetString(identifierBytes, 0, length);
            }
        }

        DictionarySerialization.InvokeSetEntriesTyped(
            setEntriesTypedIndex,
            ctx->hostingEntityId, dictRef, elements, dictionaryIdentifier, ctx->warnAboutIgnoredEntries);
    }
}
