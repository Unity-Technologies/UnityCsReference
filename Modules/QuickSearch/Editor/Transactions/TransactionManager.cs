// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using DateTime = System.DateTime;
using System.Security.AccessControl;


namespace UnityEditor.Search
{
    interface ITransactionReader
    {
        Transaction Read(int transactionOffset);
        Transaction[] Read(TimeRange timeRange);
        Transaction[] Read(TimeRange timeRange, long transactionCount);
        long Read(TimeRange timeRange, Transaction[] transactions);
        long NumberOfTransactionsInRange(TimeRange timeRange);
        long NumberOfTransactions();
    }

    interface ITransactionWriter
    {
        Task Write(string id, int state);
        Task Write(Transaction t);
        Task Write(IEnumerable<Transaction> transactions);
        Task ClearRange(TimeRange timeRange);
        Task ClearAll();
    }

    struct BinarySearchRange : IEquatable<BinarySearchRange>
    {
        public long startOffset;
        public long endOffset;
        public long halfOffset;

        public static BinarySearchRange invalid = new BinarySearchRange { startOffset = -1, endOffset = -1 };

        public bool Equals(BinarySearchRange other)
        {
            return startOffset == other.startOffset && endOffset == other.endOffset;
        }

        public override bool Equals(object obj)
        {
            return obj is BinarySearchRange other && Equals(other);
        }

        public static bool operator==(BinarySearchRange lhs, BinarySearchRange rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(BinarySearchRange lhs, BinarySearchRange rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (startOffset.GetHashCode() * 397) ^ endOffset.GetHashCode();
            }
        }
    }

    static class TaskHelper
    {
        public static bool IsTaskFinished(Task task)
        {
            return task.IsCompleted || task.IsCanceled || task.IsFaulted;
        }
    }

    abstract class TransactionHandler : IDisposable
    {
        const int k_DateTimeSize = sizeof(long);
        byte[] m_DateTimeBuffer;

        bool m_Disposed;

        protected string m_FileName;
        protected FileStream m_Fs;
        protected int m_HeaderSize;

        public static readonly int transactionSize = Marshal.SizeOf<Transaction>();

        public bool Opened => m_Fs != null;

        protected TransactionHandler(string fileName, int headerSize)
        {
            m_FileName = fileName;
            m_HeaderSize = headerSize;
            m_DateTimeBuffer = new byte[k_DateTimeSize];
        }

        ~TransactionHandler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
            {
                Close();
            }

            m_DateTimeBuffer = null;

            m_Disposed = true;
        }

        public abstract bool Open();

        public virtual void Close()
        {
            if (m_Fs != null)
            {
                m_Fs.Dispose();
                m_Fs = null;
            }
        }

        public BinarySearchRange FindRange(TimeRange range, FileStream fs)
        {
            {
                var nbTransaction = GetTotalReadableTransactions(fs);
                if (nbTransaction == 0)
                    return BinarySearchRange.invalid;

                BinarySearchRange binarySearchRangeStart = new BinarySearchRange { startOffset = 0, endOffset = nbTransaction, halfOffset = nbTransaction / 2 };
                BinarySearchRange binarySearchRangeEnd = new BinarySearchRange { startOffset = 0, endOffset = nbTransaction, halfOffset = nbTransaction / 2 };
                var foundStartOffset = false;
                var foundEndOffset = false;
                while (!foundStartOffset || !foundEndOffset)
                {
                    if (!foundStartOffset)
                    {
                        // Update StartIndex
                        var startDateTime = ReadDateTime(fs, binarySearchRangeStart.halfOffset);
                        if (range.first.InRange(startDateTime))
                        {
                            binarySearchRangeStart.endOffset = binarySearchRangeStart.halfOffset;
                            binarySearchRangeStart.halfOffset = binarySearchRangeStart.startOffset + (binarySearchRangeStart.endOffset - binarySearchRangeStart.startOffset) / 2;

                            if (binarySearchRangeStart.endOffset == binarySearchRangeStart.halfOffset)
                                foundStartOffset = true;
                        }
                        else
                        {
                            // timeRange is outside of the file
                            if (binarySearchRangeStart.halfOffset >= nbTransaction - 1)
                                return BinarySearchRange.invalid;

                            binarySearchRangeStart.startOffset = binarySearchRangeStart.halfOffset;
                            binarySearchRangeStart.halfOffset = binarySearchRangeStart.startOffset + (binarySearchRangeStart.endOffset - binarySearchRangeStart.startOffset) / 2;

                            if (binarySearchRangeStart.startOffset == binarySearchRangeStart.halfOffset)
                                foundStartOffset = true;
                        }
                    }

                    if (!foundEndOffset)
                    {
                        // Update EndIndex
                        var endDateTime = ReadDateTime(fs, binarySearchRangeEnd.halfOffset);
                        if (range.last.InRange(endDateTime))
                        {
                            binarySearchRangeEnd.startOffset = binarySearchRangeEnd.halfOffset;
                            binarySearchRangeEnd.halfOffset = binarySearchRangeEnd.startOffset + (binarySearchRangeEnd.endOffset - binarySearchRangeEnd.startOffset) / 2;

                            if (binarySearchRangeEnd.startOffset == binarySearchRangeEnd.halfOffset)
                                foundEndOffset = true;
                        }
                        else
                        {
                            // timeRange is outside of the file
                            if (binarySearchRangeEnd.halfOffset == 0)
                                return BinarySearchRange.invalid;

                            binarySearchRangeEnd.endOffset = binarySearchRangeEnd.halfOffset;
                            binarySearchRangeEnd.halfOffset = binarySearchRangeEnd.startOffset + (binarySearchRangeEnd.endOffset - binarySearchRangeEnd.startOffset) / 2;

                            if (binarySearchRangeEnd.endOffset == binarySearchRangeEnd.halfOffset)
                                foundEndOffset = true;
                        }
                    }
                }

                // We take the endOffset because we know the transactions of interests lie on these offset.
                return new BinarySearchRange { startOffset = binarySearchRangeStart.endOffset, endOffset = binarySearchRangeEnd.endOffset };
            }
        }

        public long GetTotalReadableTransactions(FileStream fs)
        {
            var totalSize = Math.Max(fs.Length - m_HeaderSize, 0);
            var nbTransaction = totalSize / transactionSize;
            return nbTransaction;
        }

        public DateTime ReadDateTime(FileStream fs, long transactionOffset)
        {
            if (m_DateTimeBuffer == null || m_DateTimeBuffer.Length != k_DateTimeSize)
                throw new Exception($"DateTime Buffer not initialized correctly: {(m_DateTimeBuffer == null ? "Buffer is null" : $"Buffer size is {m_DateTimeBuffer.Length}")}");
            lock (fs)
            {
                fs.Seek(transactionOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                ReadWholeArray(fs, m_DateTimeBuffer);
                return TransactionUtils.TimeStampFromByte(m_DateTimeBuffer);
            }
        }

        public void ReadWholeArray(FileStream fs, byte[] data)
        {
            var offset = 0;
            var remaining = data.Length;
            while (remaining > 0)
            {
                var read = fs.Read(data, offset, remaining);
                if (read <= 0)
                    throw new EndOfStreamException($"End of stream reached with {remaining} bytes left to read");
                remaining -= read;
                offset += read;
            }
        }
    }

    class TransactionWriter : TransactionHandler, ITransactionWriter
    {
        Task m_CurrentTask;

        public TransactionWriter(string fileName, int headerSize)
            : base(fileName, headerSize)
        {}

        public override bool Open()
        {
            if (m_Fs == null)
            {
                var tryCount = 0;
                while (tryCount++ < 10)
                {
                    try
                    {
                        m_Fs = File.Open(m_FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                        m_Fs.Seek(0, SeekOrigin.End);
                        break;
                    }
                    catch (System.IO.IOException ex)
                    {
                        System.Threading.Thread.Sleep(100);
                        if (tryCount == 10)
                            throw ex;
                    }
                }
            }

            return m_Fs != null;
        }

        public override void Close()
        {
            if (m_CurrentTask != null && !TaskHelper.IsTaskFinished(m_CurrentTask))
            {
                m_CurrentTask.Wait();
            }
            m_CurrentTask = null;
            base.Close();
        }

        public Task Write(string id, int state)
        {
            var transaction = new Transaction(id, state);
            return Write(transaction);
        }

        public Task Write(Transaction t)
        {
            var bytes = TransactionUtils.Serialize(t);
            return WriteBuffer(bytes);
        }

        public Task Write(IEnumerable<Transaction> transactions)
        {
            var enumerable = transactions as Transaction[] ?? transactions.ToArray();
            var count = enumerable.Length;
            var buffer = new byte[count * transactionSize];
            var transactionOffset = 0;
            foreach (var transaction in enumerable)
            {
                TransactionUtils.SerializeInto(transaction, buffer, transactionOffset * transactionSize);
                ++transactionOffset;
            }

            return WriteBuffer(buffer);
        }

        public Task ClearAll()
        {
            return AppendTask(() =>
            {
                lock (m_Fs)
                {
                    m_Fs.SetLength(m_HeaderSize);
                    m_Fs.Flush(true);
                }
            });
        }

        public Task ClearRange(TimeRange timeRange)
        {
            return AppendTask(() =>
            {
                lock (m_Fs)
                {
                    var searchRange = FindRange(timeRange, m_Fs);
                    RemoveTransactions(searchRange);
                }
            });
        }

        public Task WriteHeader(int header)
        {
            return AppendTask(() =>
            {
                lock (m_Fs)
                {
                    m_Fs.Seek(0, SeekOrigin.Begin);
                    var bytes = TransactionUtils.Serialize(header);
                    m_Fs.Write(bytes, 0, bytes.Length);
                    m_Fs.Flush(true);
                }
            });
        }

        Task WriteBuffer(byte[] buffer)
        {
            return AppendTask(() =>
            {
                lock (m_Fs)
                {
                    m_Fs.Write(buffer, 0, buffer.Length);
                    m_Fs.Flush(true);
                }
            });
        }

        Task AppendTask(Action task)
        {
            if (m_CurrentTask == null || TaskHelper.IsTaskFinished(m_CurrentTask))
            {
                m_CurrentTask = new Task(task, TaskCreationOptions.None);
                m_CurrentTask.Start();
            }
            else
            {
                m_CurrentTask = m_CurrentTask.ContinueWith(lastAction => task());
            }

            return m_CurrentTask;
        }

        void RemoveTransactions(BinarySearchRange range)
        {
            if (range == BinarySearchRange.invalid)
                return;

            var totalTransactions = GetTotalReadableTransactions(m_Fs);
            var nbRemoved = range.endOffset - range.startOffset;
            var newSize = m_HeaderSize + (totalTransactions - nbRemoved) * transactionSize;

            var transactionsToShift = totalTransactions - range.endOffset;
            var byteSizeToRead = transactionsToShift * transactionSize;
            if (transactionsToShift > 0)
            {
                m_Fs.Seek(range.endOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                var buffer = new byte[byteSizeToRead];
                ReadWholeArray(m_Fs, buffer);
                m_Fs.Seek(range.startOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                m_Fs.Write(buffer, 0, (int)byteSizeToRead);
            }
            m_Fs.SetLength(newSize);
            m_Fs.Flush(true);
        }
    }

    class TransactionReader : TransactionHandler, ITransactionReader
    {
        FileSystemWatcher m_FileWatcher;
        DateTime m_LastTransaction = DateTime.MinValue;

        public event Action<DateTime> transactionsAdded;

        public TransactionReader(string fileName, int headerSize)
            : base(fileName, headerSize)
        {}

        public override bool Open()
        {
            if (m_Fs == null)
            {
                if (!File.Exists(m_FileName))
                    return false;
                m_Fs = File.Open(m_FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var totalTransactions = GetTotalReadableTransactions(m_Fs);
                if (totalTransactions > 0)
                    m_LastTransaction = ReadDateTime(m_Fs, totalTransactions - 1);

                var fileInfo = new FileInfo(m_FileName);

                m_FileWatcher = new FileSystemWatcher
                {
                    Path = fileInfo.DirectoryName,
                    Filter = fileInfo.Name,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                };
                m_FileWatcher.Changed += OnTransactionDatabaseChanged;
                m_FileWatcher.Created += OnTransactionDatabaseChanged;
                m_FileWatcher.Error += OnFileWatcherError;
                m_FileWatcher.EnableRaisingEvents = !Application.isBatchMode;
            }

            return m_Fs != null && m_FileWatcher != null;
        }

        void OnFileWatcherError(object sender, ErrorEventArgs e)
        {
            throw new Exception($"FileWatcherException: {e.GetException()}");
        }

        private void OnTransactionDatabaseChanged(object sender, FileSystemEventArgs e)
        {
            if (m_Fs == null || m_FileWatcher == null)
                return;

            if (Monitor.TryEnter(m_FileWatcher, 100))
            {
                try
                {
                    var allDates = ReadAllDateTimes();

                    foreach (var newDate in allDates)
                    {
                        if (newDate > m_LastTransaction)
                        {
                            transactionsAdded?.Invoke(newDate);
                            break;
                        }
                    }

                    if (allDates.Length > 0)
                        m_LastTransaction = allDates.Last();
                    else
                        m_LastTransaction = DateTime.MinValue;
                }
                catch (Exception exception)
                {
                    Debug.Log(exception);
                }
                finally
                {
                    Monitor.Exit(m_FileWatcher);
                }
            }
        }

        public override void Close()
        {
            transactionsAdded = null;

            if (m_FileWatcher != null)
            {
                lock (m_FileWatcher)
                {
                    m_FileWatcher.EnableRaisingEvents = false;
                    m_FileWatcher.Changed -= OnTransactionDatabaseChanged;
                    m_FileWatcher.Created -= OnTransactionDatabaseChanged;
                    m_FileWatcher.Error -= OnFileWatcherError;
                }
                m_FileWatcher.Dispose();
                m_FileWatcher = null;
            }

            base.Close();
        }

        public int ReadHeader()
        {
            if (m_Fs.Length < m_HeaderSize)
                return 0;
            m_Fs.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[m_HeaderSize];
            ReadWholeArray(m_Fs, bytes);
            var fileHeader = TransactionUtils.Deserialize<int>(bytes);
            return fileHeader;
        }

        public Transaction Read(int transactionOffset)
        {
            var bytes = new byte[transactionSize];
            lock (m_Fs)
            {
                m_Fs.Seek(transactionOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                ReadWholeArray(m_Fs, bytes);
            }
            return TransactionUtils.Deserialize<Transaction>(bytes);
        }

        public Transaction[] Read(TimeRange timeRange)
        {
            return Read(timeRange, m_Fs);
        }

        public Transaction[] Read(TimeRange timeRange, long transactionCount)
        {
            return Read(timeRange, transactionCount, m_Fs);
        }

        public long Read(TimeRange timeRange, Transaction[] transactions)
        {
            return Read(timeRange, transactions, m_Fs);
        }

        public DateTime[] ReadAllDateTimes()
        {
            var startOffset = 0;
            var endOffset = GetTotalReadableTransactions(m_Fs);
            return ReadDateTimes(startOffset, endOffset);
        }

        public long NumberOfTransactionsInRange(TimeRange timeRange)
        {
            var transactionRange = FindRange(timeRange, m_Fs);
            if (transactionRange.startOffset == -1 || transactionRange.endOffset == -1)
                return 0;
            return transactionRange.endOffset - transactionRange.startOffset;
        }

        public long NumberOfTransactions()
        {
            return GetTotalReadableTransactions(m_Fs);
        }

        Transaction[] Read(TimeRange timeRange, FileStream fs)
        {
            var transactionRange = FindRange(timeRange, fs);
            if (transactionRange.startOffset == -1 || transactionRange.endOffset == -1)
                return new Transaction[0];
            return Read(transactionRange.startOffset, transactionRange.endOffset, fs);
        }

        Transaction[] Read(TimeRange timeRange, long transactionCount, FileStream fs)
        {
            var transactionRange = FindRange(timeRange, fs);
            if (transactionRange.startOffset == -1 || transactionRange.endOffset == -1)
                return new Transaction[0];
            var maxTransactionCount = Math.Min(transactionCount, transactionRange.endOffset - transactionRange.startOffset);
            transactionRange.endOffset = transactionRange.startOffset + maxTransactionCount;
            return Read(transactionRange.startOffset, transactionRange.endOffset, fs);
        }

        long Read(TimeRange timeRange, Transaction[] transactions, FileStream fs)
        {
            var transactionRange = FindRange(timeRange, fs);
            if (transactionRange.startOffset == -1 || transactionRange.endOffset == -1)
                return 0;
            var maxTransactionCount = Math.Min(transactions.Length, transactionRange.endOffset - transactionRange.startOffset);
            transactionRange.endOffset = transactionRange.startOffset + maxTransactionCount;
            return Read(transactionRange.startOffset, transactionRange.endOffset, transactions, fs);
        }

        Transaction[] Read(long startTransactionOffset, long endTransactionOffset, FileStream fs)
        {
            if (startTransactionOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(startTransactionOffset));
            if (endTransactionOffset < 0 || endTransactionOffset * transactionSize > fs.Length)
                throw new ArgumentOutOfRangeException(nameof(endTransactionOffset));

            var nbTransaction = endTransactionOffset - startTransactionOffset;
            var transactions = new Transaction[nbTransaction];
            Read(startTransactionOffset, endTransactionOffset, transactions, fs);
            return transactions;
        }

        long Read(long startTransactionOffset, long endTransactionOffset, Transaction[] transactions, FileStream fs)
        {
            if (startTransactionOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(startTransactionOffset));
            if (endTransactionOffset < 0 || endTransactionOffset * transactionSize > fs.Length)
                throw new ArgumentOutOfRangeException(nameof(endTransactionOffset));

            var nbTransaction = endTransactionOffset - startTransactionOffset;
            var buffer = new byte[nbTransaction * transactionSize];
            lock (fs)
            {
                fs.Seek(startTransactionOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                ReadWholeArray(fs, buffer);
            }

            return TransactionUtils.ArrayDeserializeInto(buffer, transactions);
        }

        DateTime[] ReadDateTimes(long startTransactionOffset, long endTransactionOffset)
        {
            if (startTransactionOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(startTransactionOffset));
            if (endTransactionOffset < 0 || endTransactionOffset * transactionSize > m_Fs.Length)
                throw new ArgumentOutOfRangeException(nameof(endTransactionOffset));

            var nbTransaction = endTransactionOffset - startTransactionOffset;
            var dateTimes = new DateTime[nbTransaction];
            var currentIndex = 0;
            for (var i = startTransactionOffset; i < endTransactionOffset; ++i, ++currentIndex)
            {
                dateTimes[currentIndex] = ReadDateTime(m_Fs, i);
            }

            return dateTimes;
        }
    }

    class TransactionManager : ITransactionReader, ITransactionWriter
    {
        protected string m_FilePath;

        protected TransactionWriter m_Writer;
        protected TransactionReader m_Reader;
        protected bool m_Initialized;

        const short k_Version = 1;

        public const int Header = (0x54 << 24) | (0x4D << 16) | k_Version;
        public static int HeaderSize { get; } = Marshal.SizeOf(Header);

        public event Action<DateTime> transactionsAdded;

        public bool Initialized => m_Initialized;

        public TransactionManager(string filePath)
        {
            m_FilePath = filePath;
            m_Initialized = false;
        }

        public TransactionManager()
        {
            m_FilePath = null;
            m_Initialized = false;
        }

        public virtual bool Init()
        {
            if (m_Initialized)
                return true;

            MakeDBFolder();

            m_Writer = new TransactionWriter(m_FilePath, HeaderSize);
            var opened = m_Writer.Open();

            m_Reader = new TransactionReader(m_FilePath, HeaderSize);
            opened &= OpenReader();

            FixHeader();

            m_Initialized = opened;
            return opened;
        }

        void MakeDBFolder()
        {
            try
            {
                var dbFolder = Path.GetDirectoryName(m_FilePath);
                if (!Directory.Exists(dbFolder))
                    Directory.CreateDirectory(dbFolder);
            }
            catch (IOException)
            {
                // ignore
            }
        }

        void HandleTransactionsAdded(DateTime newDateTime)
        {
            transactionsAdded?.Invoke(newDateTime);
        }

        public void SetFilePath(string filePath)
        {
            m_FilePath = filePath;
        }

        public virtual void Shutdown()
        {
            if (!m_Initialized)
                return;
            CloseAll();
            m_Writer = null;
            m_Reader = null;
            m_Initialized = false;
            transactionsAdded = null;
        }

        public virtual Task Write(string guid, int state)
        {
            return m_Writer.Write(guid, state);
        }

        public virtual Task Write(Transaction t)
        {
            return m_Writer.Write(t);
        }

        public virtual Task Write(IEnumerable<Transaction> transactions)
        {
            return m_Writer.Write(transactions);
        }

        public virtual Transaction Read(int transactionOffset)
        {
            return m_Reader.Read(transactionOffset);
        }

        public virtual Transaction[] Read(TimeRange timeRange)
        {
            return m_Reader.Read(timeRange);
        }

        public virtual Transaction[] Read(TimeRange timeRange, long transactionCount)
        {
            return m_Reader.Read(timeRange, transactionCount);
        }

        public virtual long Read(TimeRange timeRange, Transaction[] transactions)
        {
            return m_Reader.Read(timeRange, transactions);
        }

        public virtual long NumberOfTransactionsInRange(TimeRange timeRange)
        {
            return m_Reader.NumberOfTransactionsInRange(timeRange);
        }

        public virtual long NumberOfTransactions()
        {
            return m_Reader.NumberOfTransactions();
        }

        public virtual Task ClearAll()
        {
            return m_Writer.ClearAll();
        }

        public virtual Task ClearRange(TimeRange timeRange)
        {
            return m_Writer.ClearRange(timeRange);
        }

        void FixHeader()
        {
            // TODO: Make sure it's the right asset type
            if (!ValidateHeader())
            {
                ClearAll().Wait();
                WriteHeader();
            }
        }

        protected bool ValidateHeader()
        {
            if (m_Reader == null || !m_Reader.Opened)
                return false;

            var fileHeader = m_Reader.ReadHeader();
            return fileHeader == Header;
        }

        void WriteHeader()
        {
            m_Writer.WriteHeader(Header).Wait();
        }

        protected virtual void CloseAll()
        {
            m_Writer.Close();
            m_Reader.Close();
        }

        protected virtual bool OpenReader()
        {
            var opened = m_Reader.Open();
            if (opened)
                m_Reader.transactionsAdded += HandleTransactionsAdded;
            return opened;
        }
    }

    class ReadOnlyTransactionManager : TransactionManager
    {
        public ReadOnlyTransactionManager(string filePath)
            : base(filePath)
        {}

        public ReadOnlyTransactionManager() {}

        public override bool Init()
        {
            if (m_Initialized)
                return true;

            m_Reader = new TransactionReader(m_FilePath, TransactionManager.HeaderSize);
            var opened = OpenReader();
            if (!opened)
                return false;

            if (!ValidateHeader())
                return false;

            m_Initialized = true;
            return true;
        }

        public override Task Write(string guid, int state)
        {
            throw new NotSupportedException();
        }

        public override Task Write(Transaction t)
        {
            throw new NotSupportedException();
        }

        public override Task Write(IEnumerable<Transaction> transactions)
        {
            throw new NotSupportedException();
        }

        public override Task ClearAll()
        {
            throw new NotSupportedException();
        }

        public override Task ClearRange(TimeRange timeRange)
        {
            throw new NotSupportedException();
        }

        protected override void CloseAll()
        {
            m_Reader.Close();
        }
    }
}
