// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace UnityEngine.Serialization
{
    /// <summary>
    /// Serialization support for <see cref="List{T}" /> and <see cref="Dictionary{TKey,TValue}" /> that doesn't rely on reflection
    /// of private members in order to be useable under the CoreCLR security model (WebPlayer).
    /// </summary>
    public class UnitySurrogateSelector : ISurrogateSelector
    {
        public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
        {
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(List<>))
                {
                    selector = this;
                    return ListSerializationSurrogate.Default;
                }
                if (genericTypeDefinition == typeof(Dictionary<, >))
                {
                    selector = this;
                    var dictSurrogateType = typeof(DictionarySerializationSurrogate<, >).MakeGenericType(type.GetGenericArguments());
                    return (ISerializationSurrogate)Activator.CreateInstance(dictSurrogateType);
                }
            }

            selector = null;
            return null;
        }

        public void ChainSelector(ISurrogateSelector selector)
        {
            throw new NotImplementedException();
        }

        public ISurrogateSelector GetNextSelector()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Serialization support for <see cref="List{T}" /> that doesn't rely on reflection of private members.
    /// </summary>
    class ListSerializationSurrogate : ISerializationSurrogate
    {
        public static readonly ISerializationSurrogate Default = new ListSerializationSurrogate();

        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var list = (IList)obj;
            info.AddValue("_size", list.Count);
            info.AddValue("_items", ArrayFromGenericList(list));
            info.AddValue("_version", 0); // required for compatibility with platform deserialization
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var list = (IList)Activator.CreateInstance(obj.GetType());
            var size = info.GetInt32("_size");
            if (size == 0)
                return list;

            var items = ((IEnumerable)info.GetValue("_items", typeof(IEnumerable))).GetEnumerator();
            for (var i = 0; i < size; ++i)
            {
                if (!items.MoveNext())
                    throw new InvalidOperationException();
                list.Add(items.Current);
            }
            return list;
        }

        private static Array ArrayFromGenericList(IList list)
        {
            var items = Array.CreateInstance(list.GetType().GetGenericArguments()[0], list.Count);
            list.CopyTo(items, 0);
            return items;
        }
    }

    /// <summary>
    /// Serialization support for <see cref="Dictionary{TKey,TValue}" /> that doesn't rely on non public members.
    /// </summary>
    class DictionarySerializationSurrogate<TKey, TValue> : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var dictionary = ((Dictionary<TKey, TValue>)obj);
            dictionary.GetObjectData(info, context);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var comparer = (IEqualityComparer<TKey>)info.GetValue("Comparer", typeof(IEqualityComparer<TKey>));
            var dictionary = new Dictionary<TKey, TValue>(comparer);
            if (info.MemberCount > 3) // KeyValuePairs might not be present if the dictionary was empty
            {
                var keyValuePairs =
                    (KeyValuePair<TKey, TValue>[])info.GetValue("KeyValuePairs", typeof(KeyValuePair<TKey, TValue>[]));
                if (keyValuePairs != null)
                    foreach (var kvp in keyValuePairs)
                        dictionary.Add(kvp.Key, kvp.Value);
            }
            return dictionary;
        }
    }
}
