using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Http
{
    /// <summary>
    /// Chunked stream.
    /// 
    /// for abstracting the Transfer Encoding ==> Chunked.
    /// 
    /// This should only be called if we know for sure that it *is* a chunked encoding, and that logic
    /// lives outside of this module.
    /// </summary>
    public class ChunkedStream : Stream
    {
        Reader reader;
        MemoryStream buffer;
		bool EOF = false;
		long pos = 0;
        //long threshold = 10 * 1024 * 1024; // 10MB
		public ChunkedStream(Reader r)
        {
            reader = r;
			buffer = new MemoryStream();
        }

        public ChunkedStream(Stream s)
        {
            reader = new Reader(s);
            buffer = new MemoryStream();
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
			if (needToReadMore(count) && !EOF)
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
				// we'll read in one chunk at a time.
				int chunkSize = readChunkSize();
				byte[] bytes = reader.ReadBytes(chunkSize);
				reader.ReadLine(); // skip the CRLF
				buffer.Write(bytes, 0, bytes.Length); 
				if (chunkSize == 0) { // this is the last chunk, and we should mark EOF as such 
					return;
				}
				totalBytesRead += bytes.Length;
			}
		}

		int readChunkSize() {
			string size = reader.ReadLine(); // the size is going to be a hex number.
			return int.Parse(size, System.Globalization.NumberStyles.HexNumber);
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

