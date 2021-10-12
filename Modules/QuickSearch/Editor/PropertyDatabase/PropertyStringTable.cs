// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace UnityEditor.Search
{
    struct PropertyStringTableHeader
    {
        public int version;
        public int count;
        public int symbolSlots;
        public int allocatedStringBytes;
        public int usedStringBytes;

        public const int size = sizeof(int) * 5;

        public PropertyStringTableHeader(int version, int count, int symbolSlots, int allocatedStringBytes, int usedStringBytes)
        {
            this.version = version;
            this.count = count;
            this.symbolSlots = symbolSlots;
            this.allocatedStringBytes = allocatedStringBytes;
            this.usedStringBytes = usedStringBytes;
        }

        public void ToBinary(BinaryWriter bw)
        {
            bw.Write(version);
            bw.Write(count);
            bw.Write(symbolSlots);
            bw.Write(allocatedStringBytes);
            bw.Write(usedStringBytes);
        }

        public static PropertyStringTableHeader FromBinary(BinaryReader br)
        {
            var version = br.ReadInt32();
            var count = br.ReadInt32();
            var symbolSlots = br.ReadInt32();
            var allocatedStringBytes = br.ReadInt32();
            var usedStringBytes = br.ReadInt32();

            return new PropertyStringTableHeader(version, count, symbolSlots, allocatedStringBytes, usedStringBytes);
        }
    }

    struct PropertyStringHashAndLength
    {
        public uint hash;
        public int length;

        public PropertyStringHashAndLength(string str)
        {
            hash = (uint)str.GetHashCode();
            var strByteCount = PropertyStringTable.encoding.GetByteCount(str);
            length = GetSevenBitEncodedIntByteSize(strByteCount) + strByteCount;
        }

        public static int GetSevenBitEncodedIntByteSize(int number)
        {
            var byteSize = 1;
            while (number > 0)
            {
                number >>= 7;
                if (number > 0)
                    ++byteSize;
            }

            return byteSize;
        }
    }

    class PropertyStringTable : IPropertyLockable
    {
        public const int Version = 0x50535400 | 0x02;
        public const int StringTableFullSymbol = -1;
        public const int EmptyStringSymbol = 0;
        public const int HashFactor = 2;
        public const int StringLengthByteSize = sizeof(int);
        public const int SymbolByteSize = sizeof(int);
        public const int DefaultStringCount = 30;
        public const int DefaultAverageStringSize = 16;

        public string filePath { get; }
        public bool autoGrow { get; }

        public static System.Text.Encoding encoding => System.Text.Encoding.Unicode;

        ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        event Action<PropertyStringTableHeader> m_StringTableChanged;

        public PropertyStringTable(string filePath, int stringCount, int averageStringLength = DefaultAverageStringSize, bool autoGrow = true)
        {
            this.filePath = filePath;
            this.autoGrow = autoGrow;
            if (!File.Exists(filePath))
                Create(stringCount, averageStringLength);
        }

        public PropertyStringTable(string filePath, bool autoGrow = true)
            : this(filePath, DefaultStringCount, DefaultAverageStringSize, autoGrow)
        {}

        void Create(int stringCount, int averageStringLength = DefaultAverageStringSize)
        {
            var header = new PropertyStringTableHeader();
            header.version = Version;
            header.count = 0;
            header.symbolSlots = Math.Max(stringCount * HashFactor, 1);

            var bytesPerString = GetStringByteSize(averageStringLength);
            header.allocatedStringBytes = stringCount * bytesPerString;
            header.usedStringBytes = StringLengthByteSize;

            var buffer = new byte[GetSymbolsByteSize(header.symbolSlots) + header.allocatedStringBytes];
            using (var bw = new BinaryWriter(File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete)))
            {
                header.ToBinary(bw);
                bw.Write(buffer);
            }
        }

        public PropertyStringTableView GetView(bool delayedSync = false)
        {
            return new PropertyStringTableView(this, delayedSync);
        }

        public void RegisterStringTableChangedHandler(Action<PropertyStringTableHeader> handler)
        {
            using (LockWrite())
                m_StringTableChanged += handler;
        }

        public void UnregisterStringTableChangedHandler(Action<PropertyStringTableHeader> handler)
        {
            using (LockWrite())
                m_StringTableChanged -= handler;
        }

        public void NotifyStringTableChanged(PropertyStringTableHeader newHeader)
        {
            using (LockUpgradeableRead())
                m_StringTableChanged?.Invoke(newHeader);
        }

        public static int GetStringByteSize(int stringLength)
        {
            var maxByteCount = encoding.GetMaxByteCount(stringLength);
            return maxByteCount + PropertyStringHashAndLength.GetSevenBitEncodedIntByteSize(maxByteCount) + StringLengthByteSize;
        }

        public static int GetSymbolsByteSize(int symbolCount)
        {
            return symbolCount * sizeof(int);
        }

        public IDisposable LockRead()
        {
            return new ReadLockGuard(m_Lock);
        }

        public IDisposable LockUpgradeableRead()
        {
            return new UpgradeableReadLockGuard(m_Lock);
        }

        public IDisposable LockWrite()
        {
            return new WriteLockGuard(m_Lock);
        }
    }

    class PropertyStringTableView : IDisposable, IPropertyLockable
    {
        bool m_Disposed;
        bool m_DelayedSync;
        FileStream m_Fs;
        BinaryReader m_Br;
        BinaryWriter m_Bw;
        PropertyStringTableHeader m_Header;
        PropertyStringTable m_StringTable;

        public int version
        {
            get
            {
                using (LockRead())
                    return m_Header.version;
            }
        }

        public int count
        {
            get
            {
                using (LockRead())
                    return m_Header.count;
            }
        }

        public int symbolSlots
        {
            get
            {
                using (LockRead())
                    return m_Header.symbolSlots;
            }
        }

        public int allocatedStringBytes
        {
            get
            {
                using (LockRead())
                    return m_Header.allocatedStringBytes;
            }
        }

        public int usedStringBytes
        {
            get
            {
                using (LockRead())
                    return m_Header.usedStringBytes;
            }
        }

        public long fileSize
        {
            get
            {
                using (LockRead())
                    return m_Fs.Length;
            }
        }

        public PropertyStringTableView(PropertyStringTable stringTable, bool delayedSync)
        {
            m_StringTable = stringTable;
            m_Disposed = false;
            m_DelayedSync = delayedSync;

            using (LockRead())
            {
                m_Fs = File.Open(stringTable.filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
                m_Br = new BinaryReader(m_Fs, PropertyStringTable.encoding, true);
                m_Bw = new BinaryWriter(m_Fs, PropertyStringTable.encoding, true);
            }
            stringTable.RegisterStringTableChangedHandler(HandleStringTableChanged);

            m_Header = ReadHeader();
            using (LockUpgradeableRead())
            {
                if (m_Header.version != PropertyStringTable.Version)
                {
                    Clear();
                }
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            if (m_DelayedSync)
                Sync();

            m_StringTable.UnregisterStringTableChangedHandler(HandleStringTableChanged);
            m_Fs?.Dispose();
            m_StringTable = null;
        }

        public IDisposable LockRead()
        {
            return m_StringTable.LockRead();
        }

        public IDisposable LockUpgradeableRead()
        {
            return m_StringTable.LockUpgradeableRead();
        }

        public IDisposable LockWrite()
        {
            return m_StringTable.LockWrite();
        }

        public int ToSymbol(string str)
        {
            if (string.IsNullOrEmpty(str))
                return PropertyStringTable.EmptyStringSymbol;

            var hashAndLength = Hash(str);

            using (LockUpgradeableRead())
            {
                using (new RaceConditionDetector(this))
                {
                    var symbolIndex = (int)(hashAndLength.hash % m_Header.symbolSlots);
                    var symbol = GetSymbol(symbolIndex);
                    while (symbol != 0)
                    {
                        var fetchedString = GetString(symbol);
                        if (fetchedString == str)
                            return symbol;
                        symbolIndex = (symbolIndex + 1) % m_Header.symbolSlots;
                        symbol = GetSymbol(symbolIndex);
                    }


                    if (m_Header.count + 1 >= m_Header.symbolSlots || m_Header.symbolSlots / (float)m_Header.count < PropertyStringTable.HashFactor)
                    {
                        if (m_StringTable.autoGrow)
                        {
                            // Double the string count
                            Grow(m_Header.symbolSlots / PropertyStringTable.HashFactor * 2);

                            // Get a new symbol index
                            symbolIndex = (int)(hashAndLength.hash % m_Header.symbolSlots);
                            symbol = GetSymbol(symbolIndex);
                            while (symbol != 0)
                            {
                                symbolIndex = (symbolIndex + 1) % m_Header.symbolSlots;
                                symbol = GetSymbol(symbolIndex);
                            }
                        }
                        else
                            return PropertyStringTable.StringTableFullSymbol;
                    }

                    if (m_Header.usedStringBytes + PropertyStringTable.StringLengthByteSize + hashAndLength.length > m_Header.allocatedStringBytes)
                    {
                        if (m_StringTable.autoGrow)
                        {
                            var sizeNeeded = m_Header.usedStringBytes + PropertyStringTable.StringLengthByteSize + hashAndLength.length - m_Header.allocatedStringBytes;
                            m_Fs.SetLength(m_Fs.Length + sizeNeeded);
                            m_Header.allocatedStringBytes += sizeNeeded;
                        }
                        else
                            return PropertyStringTable.StringTableFullSymbol;
                    }

                    symbol = m_Header.usedStringBytes;
                    WriteStringAtSymbol(symbolIndex, symbol, str, hashAndLength);
                    return symbol;
                }
            }
        }

        public int Contains(string str)
        {
            if (string.IsNullOrEmpty(str))
                return PropertyStringTable.EmptyStringSymbol;

            var hashAndLength = Hash(str);

            using (LockRead())
            {
                var symbolIndex = (int)(hashAndLength.hash % m_Header.symbolSlots);
                var symbol = GetSymbol(symbolIndex);
                while (symbol != 0)
                {
                    var fetchedString = GetString(symbol);
                    if (fetchedString == str)
                        return symbol;
                    symbolIndex = (symbolIndex + 1) % m_Header.symbolSlots;
                    symbol = GetSymbol(symbolIndex);
                }

                return PropertyStringTable.StringTableFullSymbol;
            }
        }

        public string GetString(int symbol)
        {
            if (symbol == PropertyStringTable.EmptyStringSymbol)
                return string.Empty;
            if (symbol == PropertyStringTable.StringTableFullSymbol)
                return null;

            using (LockRead())
            {
                if (symbol >= m_Header.usedStringBytes)
                    return null;
                var byteOffset = GetStringsOffset() + symbol;
                if (byteOffset > m_Fs.Length - PropertyStringTable.StringLengthByteSize)
                    return null;

                m_Fs.Seek(byteOffset, SeekOrigin.Begin);
                var strLength = ReadInt32();

                if (strLength == 0)
                    return string.Empty;
                if (strLength < 0)
                    return null;
                if (byteOffset + PropertyStringTable.StringLengthByteSize + strLength > m_Fs.Length)
                    return null;

                return m_Br.ReadString();
            }
        }

        public void Grow(int newStringCount)
        {
            using (LockWrite())
            {
                var currentCount = m_Header.count;
                if (newStringCount < currentCount)
                    return;

                // Compute new sizes
                var averageBytesPerString = GetAverageBytesPerString();
                var newSymbolSlots = Math.Max(newStringCount * PropertyStringTable.HashFactor, m_Header.symbolSlots);
                var newSymbolsByteSize = newSymbolSlots * PropertyStringTable.SymbolByteSize;
                var newAllocatedStringBytes = (int)(newStringCount * (averageBytesPerString + PropertyStringTable.StringLengthByteSize));
                newAllocatedStringBytes = Math.Max(newAllocatedStringBytes, m_Header.allocatedStringBytes);

                if (newSymbolSlots == m_Header.symbolSlots)
                    return;

                var oldStringsOffset = GetStringsOffset();
                m_Header.symbolSlots = newSymbolSlots;
                m_Header.allocatedStringBytes = newAllocatedStringBytes;
                var newStringsOffset = GetStringsOffset();

                // Get new total size
                var totalSize = PropertyStringTableHeader.size + newSymbolsByteSize + newAllocatedStringBytes;

                // Resize file
                if (totalSize > m_Fs.Length)
                    m_Fs.SetLength(totalSize);

                // Move strings
                var allocatedStringsBuffer = new byte[newAllocatedStringBytes];
                m_Fs.Seek(oldStringsOffset, SeekOrigin.Begin);
                m_Fs.Read(allocatedStringsBuffer, 0, newAllocatedStringBytes);
                m_Fs.Seek(newStringsOffset, SeekOrigin.Begin);
                m_Fs.Write(allocatedStringsBuffer, 0, newAllocatedStringBytes);

                // Rebuild symbol table
                m_Fs.Seek(GetSymbolsOffset(), SeekOrigin.Begin);
                for (var i = 0; i < m_Header.symbolSlots; ++i)
                    WriteInt32(0);
                m_Fs.Seek(newStringsOffset, SeekOrigin.Begin);
                // Start byteOffset at first string by skipping symbol 0
                var byteOffset = PropertyStringTable.StringLengthByteSize;
                for (var i = 0; i < m_Header.count; ++i)
                {
                    var currentString = GetString(byteOffset);
                    if (string.IsNullOrEmpty(currentString))
                    {
                        byteOffset += PropertyStringTable.StringLengthByteSize;
                        continue;
                    }
                    var hashAndLength = Hash(currentString);

                    var symbolIndex = (int)(hashAndLength.hash % m_Header.symbolSlots);
                    var symbol = GetSymbol(symbolIndex);
                    while (symbol != 0)
                    {
                        symbolIndex = (symbolIndex + 1) % m_Header.symbolSlots;
                        symbol = GetSymbol(symbolIndex);
                    }

                    symbol = byteOffset;
                    WriteSymbol(symbolIndex, symbol);

                    byteOffset += hashAndLength.length + PropertyStringTable.StringLengthByteSize;
                }

                WriteHeader(false);

                if (!m_DelayedSync)
                    Sync();
            }
        }

        public void Clear()
        {
            using (LockWrite())
            {
                m_Header.version = PropertyStringTable.Version;
                m_Header.count = 0;
                m_Header.symbolSlots = Math.Max(PropertyStringTable.DefaultStringCount * PropertyStringTable.HashFactor, 1);

                var bytesPerString = PropertyStringTable.GetStringByteSize(PropertyStringTable.DefaultAverageStringSize);
                m_Header.allocatedStringBytes = PropertyStringTable.DefaultStringCount * bytesPerString;
                m_Header.usedStringBytes = PropertyStringTable.StringLengthByteSize;

                var newFileSize = PropertyStringTableHeader.size + PropertyStringTable.GetSymbolsByteSize(m_Header.symbolSlots) + m_Header.allocatedStringBytes;
                m_Fs.Seek(0, SeekOrigin.Begin);
                for (var i = 0; i < newFileSize; ++i)
                    m_Fs.WriteByte(0);
                m_Fs.Seek(0, SeekOrigin.Begin);
                WriteHeader(false);
                m_Fs.SetLength(newFileSize);
                Sync();
            }
        }

        public long GetAverageBytesPerString()
        {
            using (LockRead())
            {
                if (m_Header.count == 0)
                    return 0;
                return (long)Math.Ceiling((m_Header.usedStringBytes - (m_Header.count + 1) * PropertyStringTable.StringLengthByteSize) / (float)m_Header.count);
            }
        }

        public IList<string> GetAllStrings()
        {
            using (LockRead())
            {
                var strings = new List<string>(m_Header.count);
                // Skip the first length for empty string (symbol 0)
                var startOffset = GetStringsOffset() + PropertyStringTable.StringLengthByteSize;
                m_Fs.Seek(startOffset, SeekOrigin.Begin);
                for (var i = 0; i < m_Header.count; ++i)
                {
                    var strLength = ReadInt32();

                    if (strLength == 0)
                        strings.Add(string.Empty);
                    if (strLength < 0)
                        break;
                    if (m_Fs.Position + PropertyStringTable.StringLengthByteSize + strLength > m_Fs.Length)
                        break;

                    var str = m_Br.ReadString();
                    strings.Add(str);
                }

                return strings;
            }
        }

        void HandleStringTableChanged(PropertyStringTableHeader newHeader)
        {
            using (LockWrite())
                m_Header = newHeader;
        }

        void NotifyStringTableChanged(PropertyStringTableHeader newHeader)
        {
            m_StringTable.NotifyStringTableChanged(newHeader);
        }

        PropertyStringTableHeader ReadHeader()
        {
            using (LockRead())
            {
                m_Fs.Seek(0, SeekOrigin.Begin);
                return PropertyStringTableHeader.FromBinary(m_Br);
            }
        }

        void WriteHeader(bool notify)
        {
            using (LockWrite())
            {
                m_Fs.Seek(0, SeekOrigin.Begin);
                m_Header.ToBinary(m_Bw);

                if (notify && !m_DelayedSync)
                    Sync();
            }
        }

        static PropertyStringHashAndLength Hash(string str)
        {
            return new PropertyStringHashAndLength(str);
        }

        int GetSymbol(int symbolIndex)
        {
            using (LockRead())
            {
                m_Fs.Seek(GetSymbolsOffset() + symbolIndex * PropertyStringTable.SymbolByteSize, SeekOrigin.Begin);
                return ReadInt32();
            }
        }

        void WriteSymbol(int symbolIndex, int symbol)
        {
            using (LockWrite())
            {
                m_Fs.Seek(GetSymbolsOffset() + symbolIndex * PropertyStringTable.SymbolByteSize, SeekOrigin.Begin);
                WriteInt32(symbol);
            }
        }

        void WriteStringAtSymbol(int symbolIndex, int symbol, string str, PropertyStringHashAndLength hashAndLength)
        {
            using (LockWrite())
            {
                WriteSymbol(symbolIndex, symbol);
                m_Fs.Seek(GetStringsOffset() + symbol, SeekOrigin.Begin);
                WriteInt32(hashAndLength.length);
                m_Bw.Write(str);

                m_Header.count++;
                m_Header.usedStringBytes += PropertyStringTable.StringLengthByteSize + hashAndLength.length;

                // Notify other views that the
                WriteHeader(false);
                if (!m_DelayedSync)
                    Sync();
            }
        }

        static int GetSymbolsOffset()
        {
            return PropertyStringTableHeader.size;
        }

        int GetStringsOffset()
        {
            return GetSymbolsOffset() + m_Header.symbolSlots * PropertyStringTable.SymbolByteSize;
        }

        int ReadInt32()
        {
            return m_Br.ReadInt32();
        }

        void WriteInt32(int value)
        {
            m_Bw.Write(value);
        }

        void FlushFile()
        {
            m_Fs.Flush();
        }

        public void Sync()
        {
            using (LockWrite())
            {
                FlushFile();
                NotifyStringTableChanged(m_Header);
            }
        }
    }
}
