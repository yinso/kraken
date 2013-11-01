using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Kraken.Util
{

    public enum ChecksumType {
        SHA1, SHA256, SHA512, MD5
    };

    public class ChecksumUtil
    {
        private ChecksumUtil() {} 

        public static ChecksumType StringToChecksumType(string type)
        {
            return (ChecksumType)Enum.Parse(typeof(ChecksumType), type, true);
        }

        public static HashAlgorithm GetHashAlgorithm(ChecksumType type)
        {
            if (type == ChecksumType.MD5)
            {
                return new MD5CryptoServiceProvider();
            } else if (type == ChecksumType.SHA1)
            {
                return new SHA1CryptoServiceProvider();
            } else if (type == ChecksumType.SHA256)
            {
                return new SHA256Managed();
            } else
            {
                return new SHA512Managed();
            }
        }

        public static string ComputeChecksumOfString(ChecksumType type, string data)
        {
            return ComputeChecksum(type, StreamUtil.StringToStream(data));
        }

        public static string ComputeChecksum(ChecksumType type, string filePath)
        {
            return BytesToString(ComputeHash(type, filePath));
        }

        public static string ComputeChecksum(ChecksumType type, Stream s)
        {
            return BytesToString(ComputeHash(type, s));
        }

        public static byte[] ComputeHash(ChecksumType type, string filePath)
        {
            using (FileStream s = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return ComputeHash(type, s);
            }
        }

        public static byte[] ComputeHash(ChecksumType type, Stream s)
        {
            using (HashAlgorithm hasher = GetHashAlgorithm(type))
            {
                return hasher.ComputeHash(s);
            }
        }

        public static string BytesToString(byte[] bytes) {
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.AppendFormat("{0:x2}", b);
            }
            return builder.ToString();

        }
    }
}

