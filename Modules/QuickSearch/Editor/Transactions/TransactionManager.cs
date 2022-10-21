// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DateTime = System.DateTime;
using System.Text;


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

    static class TaskHelper
    {
        public static bool IsTaskFinished(Task task)
        {
            return task.IsCompleted || task.IsCanceled || task.IsFaulted;
        }
    }

    abstract class TransactionHandler : IDisposable, IBinarySearchRangeData<DateTime>
    {
        bool m_Disposed;

        protected string m_FileName;
        protected FileStream m_Fs;
        protected BinaryReader m_Br;
        protected BinaryWriter m_Bw;
        protected int m_HeaderSize;

        public static readonly int transactionSize = Transaction.size;

        public bool Opened => m_Fs != null;

        public long length => GetTotalReadableTransactions(m_Fs);

        public DateTime this[long index] => ReadDateTime(index);

        protected TransactionHandler(string fileName, int headerSize)
        {
            m_FileName = fileName;
            m_HeaderSize = headerSize;
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

            m_Disposed = true;
        }

        public abstract bool Open();

        public virtual void Close()
        {
            if (m_Fs != null)
            {
                m_Br?.Dispose();
                m_Br = null;
                m_Bw?.Dispose();
                m_Bw = null;
                m_Fs.Dispose();
                m_Fs = null;
            }
        }

        public BinarySearchRange FindRange(TimeRange range)
        {
            return BinarySearchFinder.FindRange(range, this);
        }

        public long GetTotalReadableTransactions(FileStream fs)
        {
            var totalSize = Math.Max(fs.Length - m_HeaderSize, 0);
            var nbTransaction = totalSize / transactionSize;
            return nbTransaction;
        }

        public DateTime ReadDateTime(long transactionOffset)
        {
            lock (m_Fs)
            {
                m_Fs.Seek(transactionOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                var dateTimeBinary = m_Br.ReadInt64();
                return DateTime.FromBinary(dateTimeBinary).ToUniversalTime();
            }
        }
    }

    class TransactionWriter : TransactionHandler, ITransactionWriter
    {
        Task m_CurrentTask;

        public TransactionWriter(string fileName, int headerSize)
            : base(fileName, headerSize)
        {}

        void OpenWriterInner()
        {
            m_Fs = File.Open(m_FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            m_Fs.Seek(0, SeekOrigin.End);
            m_Br = new BinaryReader(m_Fs, Encoding.UTF8, true);
            m_Bw = new BinaryWriter(m_Fs, Encoding.UTF8, true);
        }

        public override bool Open()
        {
            if (m_Fs == null)
                RetriableOperation<IOException>.Execute(OpenWriterInner, 10, TimeSpan.FromMilliseconds(100));

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
            return AppendTask(() =>
            {
                lock (m_Fs)
                {
                    m_Fs.Seek(0, SeekOrigin.End);
                    t.ToBinary(m_Bw);
                    m_Fs.Flush(true);
                }
            });
        }

        public Task Write(IEnumerable<Transaction> transactions)
        {
            return AppendTask(() =>
            {
                lock (m_Fs)
                {
                    m_Fs.Seek(0, SeekOrigin.End);
                    foreach (var transaction in transactions)
                    {
                        transaction.ToBinary(m_Bw);
                    }
                    m_Fs.Flush(true);
                }
            });
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
                    var searchRange = FindRange(timeRange);
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
                    m_Bw.Write(header);
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
            if (transactionsToShift > 0)
            {
                m_Fs.Seek(range.endOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                var readTransactions = new Transaction[transactionsToShift];
                for (var i = 0; i < transactionsToShift; ++i)
                    readTransactions[i] = Transaction.FromBinary(m_Br);
                m_Fs.Seek(range.startOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                for (var i = 0; i < transactionsToShift; ++i)
                    readTransactions[i].ToBinary(m_Bw);
            }
            m_Fs.SetLength(newSize);
            m_Fs.Flush(true);
        }
    }

    class TransactionReader : TransactionHandler, ITransactionReader
    {
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
                m_Br = new BinaryReader(m_Fs, Encoding.UTF8, true);
            }

            return m_Fs != null;
        }

        public int ReadHeader()
        {
            if (m_Fs.Length < m_HeaderSize)
                return 0;
            m_Fs.Seek(0, SeekOrigin.Begin);
            var fileHeader = m_Br.ReadInt32();
            return fileHeader;
        }

        public Transaction Read(int transactionOffset)
        {
            lock (m_Fs)
            {
                m_Fs.Seek(transactionOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                return Transaction.FromBinary(m_Br);
            }
        }

        public DateTime[] ReadAllDateTimes()
        {
            var startOffset = 0;
            var endOffset = GetTotalReadableTransactions(m_Fs);
            return ReadDateTimes(startOffset, endOffset);
        }

        public long NumberOfTransactionsInRange(TimeRange timeRange)
        {
            var transactionRange = FindRange(timeRange);
            if (transactionRange.startOffset == -1 || transactionRange.endOffset == -1)
                return 0;
            return transactionRange.endOffset - transactionRange.startOffset;
        }

        public long NumberOfTransactions()
        {
            return GetTotalReadableTransactions(m_Fs);
        }

        public Transaction[] Read(TimeRange timeRange)
        {
            var transactionRange = FindRange(timeRange);
            if (transactionRange.startOffset == -1 || transactionRange.endOffset == -1)
                return new Transaction[0];
            return Read(transactionRange.startOffset, transactionRange.endOffset);
        }

        public Transaction[] Read(TimeRange timeRange, long transactionCount)
        {
            var transactionRange = FindRange(timeRange);
            if (transactionRange.startOffset == -1 || transactionRange.endOffset == -1)
                return new Transaction[0];
            var maxTransactionCount = Math.Min(transactionCount, transactionRange.endOffset - transactionRange.startOffset);
            transactionRange.endOffset = transactionRange.startOffset + maxTransactionCount;
            return Read(transactionRange.startOffset, transactionRange.endOffset);
        }

        public long Read(TimeRange timeRange, Transaction[] transactions)
        {
            var transactionRange = FindRange(timeRange);
            if (transactionRange.startOffset == -1 || transactionRange.endOffset == -1)
                return 0;
            var maxTransactionCount = Math.Min(transactions.Length, transactionRange.endOffset - transactionRange.startOffset);
            transactionRange.endOffset = transactionRange.startOffset + maxTransactionCount;
            return Read(transactionRange.startOffset, transactionRange.endOffset, transactions);
        }

        Transaction[] Read(long startTransactionOffset, long endTransactionOffset)
        {
            if (startTransactionOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(startTransactionOffset));
            if (endTransactionOffset < 0 || endTransactionOffset * transactionSize > m_Fs.Length)
                throw new ArgumentOutOfRangeException(nameof(endTransactionOffset));

            var nbTransaction = endTransactionOffset - startTransactionOffset;
            var transactions = new Transaction[nbTransaction];
            Read(startTransactionOffset, endTransactionOffset, transactions);
            return transactions;
        }

        long Read(long startTransactionOffset, long endTransactionOffset, Transaction[] transactions)
        {
            if (startTransactionOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(startTransactionOffset));
            if (endTransactionOffset < 0 || endTransactionOffset * transactionSize > m_Fs.Length)
                throw new ArgumentOutOfRangeException(nameof(endTransactionOffset));

            var nbTransaction = endTransactionOffset - startTransactionOffset;
            lock (m_Fs)
            {
                m_Fs.Seek(startTransactionOffset * transactionSize + m_HeaderSize, SeekOrigin.Begin);
                for (var i = 0; i < nbTransaction; ++i)
                    transactions[i] = Transaction.FromBinary(m_Br);
            }

            return nbTransaction;
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
                dateTimes[currentIndex] = ReadDateTime(i);
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

        const short k_Version = 3;

        public const int Header = (0x54 << 24) | (0x4D << 16) | k_Version;
        public static int HeaderSize { get; } = sizeof(int);

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

        public string GetInfo()
        {
            if (m_Reader == null || !m_Reader.Opened)
                return string.Empty;

            var header = m_Reader.ReadHeader();
            var nbTransaction = NumberOfTransactions();
            var transactionsSize = nbTransaction * TransactionHandler.transactionSize;
            var sb = new StringBuilder();
            sb.AppendLine($"Transaction Manager: {m_FilePath}");
            sb.AppendLine($"\tVersion: {header}");
            sb.AppendLine($"\tTransaction count: {nbTransaction}");
            sb.AppendLine($"\tFile size: {Utils.FormatBytes(HeaderSize + transactionsSize)}");
            return sb.ToString();
        }

        void FixHeader()
        {
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
            return m_Reader.Open();
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
