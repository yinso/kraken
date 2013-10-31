using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

using Kraken.Util;

namespace Kraken.Core
{
    /// <summary>
    /// BLOB.
    /// 
    /// Blob is the abstraction of a value stored within Kraken.
    /// 
    /// It is identified by a hash ID (can be any particular type of hash as long as it can readily
    /// identify the particular file with little worry for collision).
    /// 
    /// in principle SHA1 & SHA256 are good hash to use. By default Kraken will use SHA1.
    /// 
    /// BLOG prepends a header section (not part of the checksum calculation) that describes minimally the following
    /// information.
    /// 
    /// original size (in bytes).
    /// compression scheme
    /// encryption scheme
    /// encryption IV (hex'd and encrypted)
    /// 
    /// If there are no compression or encryption involved both field will be none
    /// 
    /// The above for values will be in a single line, terminated by \r\n
    /// 
    /// Additionally - additional header/value can be added as long as they conform to the following
    /// 
    /// (this is for future extension).
    /// A single header is a key/value pair, terminated by \r\n
    /// key/value are separated by colon.
    /// value can be anything, but \r\n within value will need to be written out as \r\n (i.e. no extension to the next line).
    /// 
    /// An empty \r\n signals the start of the blob.
    /// 
    /// Also - this is a local blob. (i.e. external blob don't look like this).
    /// 
    /// Note - Blob doesn't create the actual path - that's up to LocalStore (i.e. LocalStore manages the creation 
    /// of the Blob).
    /// 
    /// Blob will simply attempt to OPEN a Blob, and handle the needed processing. 
    /// 
    /// </summary>
    public class Blob : Stream
    {
        string filePath;
        Stream inner;
        Reader reader;
        //Regex splitter = new Regex(@"\s+");
        //Regex integer = new Regex(@"^\d+$");
        //long fileSize;
        //CompressionType compression = CompressionType.NONE;
        //EncryptionType encryption = EncryptionType.NONE;
        //byte[] encryptionIV = new byte[0];
        long headerOffset = 0;
        byte[] encryptionKey;

        public BlobEnvelope Envelope { get; internal set; }

        // do I want this to represent the stream? I think so...
        public Blob(string path, byte[] key)
        {
            filePath = path;
            encryptionKey = key;
            inner = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            reader = new Reader(inner);
            parseEnvelope();
            // we should figure out what the offset is here...
            headerOffset = reader.Position;
            setupStream();
        }

        public static void CreateBlob(Stream s, string filePath)
        {
            // this is impossible without knowing the target directory...!!!
        }

        void parseEnvelope() {
            Envelope = BlobEnvelope.Parse(reader);
        }

        void setupStream()
        {
            // before we use the inner stream - we need to have it SET to the Reader's position.
            inner.Position = reader.Position;
            if (Envelope.EncryptionScheme != EncryptionType.NONE)
            {
                inner = EncryptionUtil.GetDecryptStream(inner, Envelope.EncryptionScheme, encryptionKey, Envelope.EncryptionIV);
            }
            if (Envelope.CompressionScheme == CompressionType.GZIP)
            {
                inner = CompressUtil.GetDecompressStream(inner);
            }
        }

        public override void Close()
        {

            inner.Close();
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
                return Envelope.OriginalLength;
            }
        }
        
        public override long Position
        {
            get
            {
                return inner.Position - headerOffset;
            }
            set
            {
                inner.Position = value + headerOffset;
            }
        }
        
        public override void Flush() {
            throw new NotImplementedException("flush_does_not_work_on_blob");
        }
        
        public override int Read(byte[] bytes, int offset, int count)
        {
            // do I have much use for the reader beyond reading the headers? No not really.
            return inner.Read(bytes, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return inner.Seek(offset, origin);
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

