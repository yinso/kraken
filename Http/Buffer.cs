using System;
using System.Collections.Generic;
using System.IO;

namespace Http
{
    public class Buffer : Stream
    {
        Stream stream;
        MemoryStream buffer;
        long pos = 0;
        long length = 0;
        byte[] readBuffer = new byte[8192];
        public Buffer(Stream s)
        {
            stream = s;
            buffer = new MemoryStream();
        }

        public void Release()
        {
            MemoryStream newBuffer = new MemoryStream();
            buffer.CopyTo(newBuffer);
            buffer.Close();
            buffer = newBuffer;
            buffer.Position = 0;
            pos = 0; 
        }

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
                return true;
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
                return buffer.Length;
            }
        }

        public override long Position
        {
            get
            {
                return pos;
            }
            set
            {
                pos = value;
            }
        }

        public override void Flush() {

        }

        public override int Read(byte[] bytes, int offset, int count)
        {
            // determine if we need to read more.
            if (needToReadMore(count))
            {
                readMore(count);
            } 
            // take the data from the 
            return read(bytes, offset, count);
        }

        long currentBufferSize
        {
            get
            {
                return buffer.Length;
            }
        }

        bool needToReadMore(int count)
        {
            return (pos + count) > currentBufferSize;
        }

        void readMore(int count)
        {
            int totalBytesRead = 0;
            buffer.Position = buffer.Length;
            while (totalBytesRead <= count)
            {
                int bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
                buffer.Write(readBuffer, 0, bytesRead); // we now have a new lengthl
                totalBytesRead += bytesRead;
            }
        }

        int read(byte[] bytes, int offset, int count)
        {
            buffer.Position = pos;
            int bytesRead = buffer.Read(bytes, offset, count);
            pos += bytesRead;
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {

            return buffer.Seek(offset, origin);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }
    }
}

