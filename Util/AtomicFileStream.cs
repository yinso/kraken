using System;
using System.IO;

namespace Kraken.Util
{
    public class AtomicFileStream : Stream
    {
        FileStream inner;
        string filePath;
        string tempPath;
        bool overwrite;
        bool disposed = false;

        public AtomicFileStream(string filePath)
        {
            initialize(filePath, true);
        }

        public AtomicFileStream(string filePath, bool overwrite)
        {
            initialize(filePath, overwrite);
        }

        void initialize(string filePath, bool overwrite)
        {
            this.filePath = filePath;
            this.tempPath = FileUtil.TempFilePath(this.filePath);
            this.overwrite = overwrite;
            this.inner = File.Open(this.tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }

        public override void Close()
        {
            inner.Flush();
            inner.Close();
            if (overwrite) 
                FileUtil.Rename(tempPath, filePath);
            else
            {
                try {
                    File.Move(tempPath, filePath);
                } catch (Exception) { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                inner.Dispose();
                disposed = true;
            }
        }

        // STREAM PART OF THE INTERFACE
        public override bool CanRead
        {
            get
            {
                return inner.CanRead;
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
                return inner.CanWrite;
            }
        }
        
        public override long Length
        {
            get
            {
                return inner.Length;
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
                inner.Position = value;
            }
        }
        
        public override void Flush() {
            inner.Flush();
        }
        
        public override int Read(byte[] bytes, int offset, int count)
        {
            return inner.Read(bytes, offset, count);
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            return inner.Seek(offset, origin);
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
        }
        
        public override void SetLength(long value)
        {
            inner.SetLength(value);
        }
    }
}

