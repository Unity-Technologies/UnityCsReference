// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;

namespace UnityEditor
{
    /// A read-only Stream backed by UDS shared memory. The content is accessed
    /// via a pointer into native memory — no managed copy is made.
    unsafe class UDSReadStream : Stream
    {
        IntPtr m_Handle;
        byte* m_ContentPtr;
        readonly long m_ContentLength;
        long m_Position;

        internal UDSReadStream(IntPtr udsHandle)
        {
            m_Handle = udsHandle;
            ReadOnlySpan<byte> content = UDS.GetContent(udsHandle);
            m_ContentLength = content.Length;
            fixed (byte* p = content)
                m_ContentPtr = p;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => m_ContentLength;

        public override long Position
        {
            get => m_Position;
            set
            {
                if (value < 0 || value > m_ContentLength)
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"Position should be between 0 and {m_ContentLength}");
                m_Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (m_ContentPtr == null)
                throw new ObjectDisposedException(nameof(UDSReadStream));

            long available = m_ContentLength - m_Position;
            if (available <= 0) return 0;
            int toRead = (int)Math.Min(count, available);

            fixed (byte* dst = &buffer[offset])
                Buffer.MemoryCopy(m_ContentPtr + m_Position, dst, count, toRead);

            m_Position += toRead;
            return toRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => m_Position + offset,
                SeekOrigin.End => m_ContentLength + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Invalid seek origin")
            };

            if (target < 0 || target > m_ContentLength)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Seek target should be between 0 and {m_ContentLength}");

            m_Position = target;
            return m_Position;
        }

        public override void Flush() {}
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (m_Handle != IntPtr.Zero)
            {
                UDS.Release(m_Handle);
                m_Handle = IntPtr.Zero;
            }

            m_ContentPtr = null;

            base.Dispose(disposing);
        }
    }
}
