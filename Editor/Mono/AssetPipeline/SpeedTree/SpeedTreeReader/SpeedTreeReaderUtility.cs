// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UnityEditor.SpeedTree.Importer
{
    class Data
    {
        protected byte[] _data;
        protected int _offset;

        public Data()
        {
            _data = null;
            _offset = 0;
        }

        public void SetData(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;
        }

        public bool IsValid => _data != null;
    }

    class SpeedTreeDataArray<T> : Data where T : struct
    {
        public bool IsEmpty => BitConverter.ToUInt32(_data, _offset) > 0;

        public uint Count => BitConverter.ToUInt32(_data, _offset);

        public T this[int index]
        {
            get
            {
                return MemoryMarshal.Cast<byte, T>(_data.AsSpan().Slice(_offset + 4))[index];
            }
        }
    }

    class TableArray<T> : Table
    where T : Table, new()
    {
        public T this[int index]
        {
            get
            {
                return GetContainer<T>(index);
            }
        }
    }

    class Table : Data
    {
        public uint Count => BitConverter.ToUInt32(_data, _offset);

        protected int GetOffset(int index)
        {
            int offset = _offset + (index + 1) * 4;
            return _offset + (int)BitConverter.ToUInt32(_data, offset);
        }

        protected T GetContainer<T>(int index)
            where T : Data, new()
        {
            int offset = GetOffset(index);
            T value = new T();
            value.SetData(_data, offset);
            return value;
        }

        protected byte GetByte(int index)
        {
            return _data[GetOffset(index)];
        }

        protected bool GetBool(int index)
        {
            return (GetInt(index) != 0);
        }

        protected float GetFloat(int index)
        {
            return BitConverter.ToSingle(_data, GetOffset(index));
        }

        protected int GetInt(int index)
        {
            return BitConverter.ToInt32(_data, GetOffset(index));
        }

        protected uint GetUInt(int index)
        {
            return BitConverter.ToUInt32(_data, GetOffset(index));
        }

        protected string GetString(int index)
        {
            int offset = GetOffset(index);
            int length = (int)BitConverter.ToUInt32(_data, offset);
            return System.Text.Encoding.UTF8.GetString(_data, offset + 4, length - 1);
        }

        protected T GetStruct<T>(int index)
            where T : struct
        {
            return MemoryMarshal.Cast<byte, T>(_data.AsSpan().Slice(GetOffset(index)))[0];
        }
    }

    class Reader : Table
    {
        protected bool LoadFile(string filename, string token)
        {
            bool valid = false;

            _data = File.ReadAllBytes(filename);
            if (_data.Length > token.Length)
            {
                valid = true;
                for (int i = 0; i < token.Length && valid; ++i)
                {
                    valid &= (token[i] == _data[i]);
                }

                if (valid)
                {
                    _offset = token.Length;
                }
            }

            if (!valid)
            {
                Array.Clear(_data, 0, _data.Length);
                _offset = 0;
            }

            return valid;
        }
    }
}
