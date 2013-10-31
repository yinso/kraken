using System;
using System.IO;

namespace Kraken.Util
{
    /// <summary>
    /// UpToStream - a Stream that allows you to read UP TO a particular size.
    /// </summary>
    public class UpToStream : Stream
    {
        Stream inner;
        long sizeLimit;
        public UpToStream(Stream s, long limit)
        {
            inner = s;
            sizeLimit = limit;
        }
        // STREAM PART OF THE INTERFACE
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        
        public override bool CanSeek
        {
            get
            {
                return inner.CanSeek;
            }
        }
        
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }
        
        public override long Length
        {
            get
            {
                return sizeLimit;
            }
        }
        
        public override long Position
        {
            get
            {
                return inner.Position;
            }
            set
            {
                if (value > sizeLimit)
                    inner.Position = sizeLimit;
                else
                    inner.Position = value;
            }
        }
        
        public override void Flush() {
            throw new NotImplementedException("flush_does_not_work_on_blob");
        }
        
        public override int Read(byte[] bytes, int offset, int count)
        {
            // every read it will increase the size.
            long newSize = inner.Position + count;
            if (newSize > sizeLimit)
            {
                return inner.Read(bytes, offset, (int)(sizeLimit - inner.Position));
            } else
            {
                return inner.Read(bytes, offset, count);
            }
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                if (offset > sizeLimit)
                {
                    return inner.Seek(sizeLimit, origin);
                } else
                {
                    return inner.Seek(offset, origin);
                }
            } else if (origin == SeekOrigin.End) // the END is where we call it good.
            {
                // offset MUST be negative...
                long newOffset = sizeLimit + offset;
                if (newOffset > sizeLimit)
                {
                    return inner.Seek(sizeLimit, SeekOrigin.Begin);
                } else
                {
                    return inner.Seek(newOffset, SeekOrigin.Begin);
                }
            } else // this is SeekOrigin.Current
            {
                long newOffset = inner.Position + offset;
                if (newOffset > sizeLimit) {
                    return inner.Seek(sizeLimit, SeekOrigin.Begin);
                } else {
                    return inner.Seek(offset, origin);
                }
            }
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}

