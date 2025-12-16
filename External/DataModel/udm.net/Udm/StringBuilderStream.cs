
using System;
using System.IO;
using System.Text;

namespace Unity.DataModel
{
    internal sealed class StringBuilderStream : Stream
    {
        private readonly StringBuilder _stringBuilder;

        internal StringBuilderStream()
        {
            _stringBuilder = new StringBuilder();
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _stringBuilder.Length;
        public override long Position
        {
            get => _stringBuilder.Length;
            set => throw new NotSupportedException("Stream does not support seeking.");
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Stream does not support reading.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Stream does not support seeking.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Stream does not support resizing.");
        }

        public override string ToString() => _stringBuilder.ToString();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            var text = Encoding.UTF8.GetString(buffer, offset, count);
            _stringBuilder.Append(text);
        }
    }
}
