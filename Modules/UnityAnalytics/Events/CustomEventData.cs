// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;


namespace UnityEngine.Analytics
{
    internal partial class CustomEventData : IDisposable
    {
        public bool AddDictionary(IDictionary<string, object> eventData)
        {
            foreach (var item in eventData)
            {
                string key = item.Key;
                object value = item.Value;
                if (value == null)
                {
                    AddString(key, "null");
                    continue;
                }
                Type type = value.GetType();
                if (type == typeof(System.String))
                    AddString(key, (string)value);
                else if (type == typeof(System.Char))
                    AddString(key, Char.ToString((Char)value));
                else if (type == typeof(System.SByte))
                    AddInt32(key, (SByte)value);
                else if (type == typeof(System.Byte))
                    AddInt32(key, (Byte)value);
                else if (type == typeof(System.Int16))
                    AddInt32(key, (Int16)value);
                else if (type == typeof(System.UInt16))
                    AddUInt32(key, (UInt16)value);
                else if (type == typeof(System.Int32))
                    AddInt32(key, (Int32)value);
                else if (type == typeof(System.UInt32))
                    AddUInt32(item.Key, (UInt32)value);
                else if (type == typeof(System.Int64))
                    AddInt64(key, (Int64)value);
                else if (type == typeof(System.UInt64))
                    AddUInt64(key, (UInt64)value);
                else if (type == typeof(System.Boolean))
                    AddBool(key, (bool)value);
                else if (type == typeof(System.Single))
                    AddDouble(key, (double)System.Convert.ToDecimal((Single)value));
                else if (type == typeof(System.Double))
                    AddDouble(key, (double)value);
                else if (type == typeof(System.Decimal))
                    AddDouble(key, (double)System.Convert.ToDecimal((Decimal)value));
                else if (type.IsValueType)
                    AddString(key, value.ToString());
                else
                    throw new ArgumentException(String.Format("Invalid type: {0} passed", type));
            }
            return true;
        }
    }
}

