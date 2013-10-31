using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Kraken.Util;

namespace Kraken.Core
{
    public class BlobEnvelope {
        ResourceType type = ResourceType.Blob; // this defines the type - perhaps it's outside of the header format.
        public short Version { get ; set; }  // this is the version of the envelope. // 4 bytes --> too much anyways.
        public long OriginalLength { get; set; }// tells us how big the file is... and perhaps with a pointer to something longer.
        public CompressionType CompressionScheme { get; set; } // do we want to serialize the string or the number? we'll do string.
        public EncryptionType EncryptionScheme { get ; set ; }
        public byte[] EncryptionIV { get; set; } // we'll always have an encryption IV even when there aren't enryption scheme to make things uniform.

        public BlobEnvelope(short ver, long origLen, CompressionType ctype, EncryptionType etype, byte[] iv)
        {
            Version = ver;
            OriginalLength = origLen;
            CompressionScheme = ctype;
            EncryptionScheme = etype;
            EncryptionIV = iv;
        }

        public BlobEnvelope() { 
            Version = 1;
        } 

        public static BlobEnvelope Parse(Reader reader)
        {
            Regex splitter = new Regex(@"\s+");
            string line = reader.ReadLine();
            string[] values = splitter.Split(line);
            // envelope format.
            // 'blob' -> tells us that this is a blob file.
            // <version> -> this tells us which version should be used handle the rest of the header.
            // <original_length>
            // <compress> -> gzip/none
            // <encryption> -> aes128/aes256/none
            // <iv> -> we should always have an IV even when there aren't encryption.
            // <keyvals> -> arbitrary headers held in querystring format.
            
            if (values.Length < 6)
            {
                throw new Exception(string.Format("error_invalid_header_preamble: {0}", line));
            }
            if (!values [0].Equals("blob", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("incorrect_blob_envelope_format_not_start_with_blob");
            }
            short version = parseShort(values[1]);
            long origSize = parseLong(values[2]);
            CompressionType cType = parseCompressionScheme(values[3]);
            EncryptionType eType = parseEncryptionScheme(values[4]); // things aren't by default encrypted... but we would want it soon.
            byte[] iv = parseEncryptionIV(values[5]);
            return new BlobEnvelope(version, origSize, cType, eType, iv);
        }
        
        static short parseShort(string size) {
            Regex integer = new Regex(@"^\d+$");
            // first one is size.
            Match isInteger = integer.Match(size);
            if (isInteger.Success)
            {
                return short.Parse(size);
            } else
            {
                throw new Exception(string.Format("error_invalid_header_preamble_size_not_integer: {0}", size));
            }
        }
        static int parseInteger(string size) {
            Regex integer = new Regex(@"^\d+$");
            // first one is size.
            Match isInteger = integer.Match(size);
            if (isInteger.Success)
            {
                return int.Parse(size);
            } else
            {
                throw new Exception(string.Format("error_invalid_header_preamble_size_not_integer: {0}", size));
            }
        }

        static long parseLong(string size) {
            Regex integer = new Regex(@"^\d+$");
            // first one is size.
            Match isInteger = integer.Match(size);
            if (isInteger.Success)
            {
                return long.Parse(size);
            } else
            {
                throw new Exception(string.Format("error_invalid_header_preamble_size_not_integer: {0}", size));
            }
        }

        
        static CompressionType parseCompressionScheme(string scheme)
        {
            return CompressUtil.StringToCompressionType(scheme);
        }
        
        static EncryptionType parseEncryptionScheme(string scheme)
        {
            return EncryptionUtil.StringToEncryptionType(scheme);
        }
        
        static byte[] parseEncryptionIV(string iv)
        {
            return ByteUtil.HexStringToByteArray(iv);
        }

        public byte[] Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2} {3} {4} {5}\r\n"
                            , type
                            , Version
                            , OriginalLength
                            , CompressionScheme
                            , EncryptionScheme
                            , ByteUtil.ByteArrayToHexString(EncryptionIV));
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public void WriteTo(Stream s) {
            byte[] bytes = Serialize();
            s.Write(bytes, 0, bytes.Length);
        }
    }
}

