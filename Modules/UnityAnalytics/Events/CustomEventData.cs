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
        private CustomEventData() {}

        public CustomEventData(string name) { InternalCreate(name); }

        ~CustomEventData() { InternalDestroy(); }

        public void Dispose()
        {
            InternalDestroy();
            GC.SuppressFinalize(this);
        }

        public bool Add(string key, string value) { return AddString(key, value); }
        public bool Add(string key, bool value) { return AddBool(key, value); }
        public bool Add(string key, char value) { return AddChar(key, value); }
        public bool Add(string key, byte value) { return AddByte(key, value); }
        public bool Add(string key, sbyte value) { return AddSByte(key, value); }
        public bool Add(string key, Int16 value) { return AddInt16(key, value); }
        public bool Add(string key, UInt16 value) { return AddUInt16(key, value); }
        public bool Add(string key, Int32 value) { return AddInt32(key, value); }
        public bool Add(string key, UInt32 value) { return AddUInt32(key, value); }
        public bool Add(string key, Int64 value) { return AddInt64(key, value); }
        public bool Add(string key, UInt64 value) { return AddUInt64(key, value); }
        public bool Add(string key, float value) { return AddDouble(key, (double)System.Convert.ToDecimal(value)); }
        public bool Add(string key, double value) { return AddDouble(key, value); }
        public bool Add(string key, Decimal value) { return AddDouble(key, (double)System.Convert.ToDecimal(value)); }


        public bool Add(IDictionary<string, object> eventData)
        {
            foreach (var item in eventData)
            {
                string key = item.Key;
                object value = item.Value;
                if (value == null)
                {
                    Add(key, "null");
                    continue;
                }
                Type type = value.GetType();
                if (type == typeof(System.String))
                    Add(key, (string)value);
                else if (type == typeof(System.Char))
                    Add(key, (Char)value);
                else if (type == typeof(System.SByte))
                    Add(key, (SByte)value);
                else if (type == typeof(System.Byte))
                    Add(key, (Byte)value);
                else if (type == typeof(System.Int16))
                    Add(key, (Int16)value);
                else if (type == typeof(System.UInt16))
                    Add(key, (UInt16)value);
                else if (type == typeof(System.Int32))
                    Add(key, (Int32)value);
                else if (type == typeof(System.UInt32))
                    Add(item.Key, (UInt32)value);
                else if (type == typeof(System.Int64))
                    Add(key, (Int64)value);
                else if (type == typeof(System.UInt64))
                    Add(key, (UInt64)value);
                else if (type == typeof(System.Boolean))
                    Add(key, (bool)value);
                else if (type == typeof(System.Single))
                    Add(key, (Single)value);
                else if (type == typeof(System.Double))
                    Add(key, (double)value);
                else if (type == typeof(System.Decimal))
                    Add(key, (Decimal)value);
                else if (type.IsValueType)
                    Add(key, value.ToString());
                else
                    throw new ArgumentException(String.Format("Invalid type: {0} passed", type));
            }
            return true;
        }
    }
}

