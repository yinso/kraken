using System;
using System.IO;
using System.Security.Cryptography;

namespace Kraken.CommandLine
{
    public class EncryptionUtil
    {
        public enum EncryptionType
        {
            AES128 , AES256
        }

        private EncryptionUtil()
        {
        }

        public static byte[] GetRandomBytes (int size)
        {
            byte[] bytes = new byte[size];
            var rng2 = new RNGCryptoServiceProvider();
            rng2.GetBytes(bytes); 
            return bytes;
        }

        public static SymmetricAlgorithm GetSymmetricAlgorithm(EncryptionType type)
        {
            if (type == EncryptionType.AES128) // the key itself needs to be 
            {
                SymmetricAlgorithm crypto = Aes.Create();
                crypto.BlockSize = 128;
                crypto.KeySize = 128;
                return crypto;
            } else if (type == EncryptionType.AES256) {
                SymmetricAlgorithm crypto = Aes.Create();
                crypto.BlockSize = 256;
                crypto.KeySize = 256;
                return crypto;
            } else
            {
                throw new Exception(string.Format("unsupported_encryption_agorithm: {0}", type));
            }
        }

        public static byte[] EncryptString(string text, EncryptionType type, byte[] key, byte[] iv)
        {
            using (Stream source = StreamUtil.StreamFromString(text))
            {
                using (MemoryStream dest = new MemoryStream()) {
                    Encrypt(source, dest, type, key, iv);
                    return dest.ToArray();
                }
            }
        }

        public static void Encrypt(Stream source, Stream dest, EncryptionType type, byte[] key, byte[] iv)
        {
            using (SymmetricAlgorithm algo = GetSymmetricAlgorithm(type))
            {
                ICryptoTransform transform = algo.CreateEncryptor(key, iv);
                using (CryptoStream cs = new CryptoStream(dest, transform, CryptoStreamMode.Write)) {
                    source.CopyTo(dest);
                }
            }
        }

        public static void Decrypt(Stream source, Stream dest, EncryptionType type, byte[] key, byte[] iv)
        {
            using (SymmetricAlgorithm algo = GetSymmetricAlgorithm(type))
            {
                ICryptoTransform transform = algo.CreateDecryptor(key, iv);
                using (CryptoStream cs = new CryptoStream(dest, transform, CryptoStreamMode.Read)) {
                    source.CopyTo(dest);
                }
            }
        }
    }
}

