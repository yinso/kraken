using System;
using System.IO;
using System.Security.Cryptography;

namespace Kraken.Util
{

    public enum EncryptionType
    {
        NONE, AES128 , AES256
    }

    public class EncryptionUtil
    {

        private EncryptionUtil()
        {
        }

        public static EncryptionType StringToEncryptionType(string type)
        {
            return (EncryptionType)Enum.Parse(typeof(EncryptionType), type, true);
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
                //crypto.Mode = CipherMode.CFB;
                //crypto.Padding = PaddingMode.None;
                return crypto;
            } else if (type == EncryptionType.AES256) {
                SymmetricAlgorithm crypto = Aes.Create();
                crypto.BlockSize = 128;
                crypto.KeySize = 256;
                //crypto.Mode = CipherMode.CFB;
                //crypto.Padding = PaddingMode.None;
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

        public static CryptoStream GetEncryptStream(Stream dest, EncryptionType type, byte[] key, byte[] iv) {
            using (SymmetricAlgorithm algo = GetSymmetricAlgorithm(type))
            {
                ICryptoTransform transform = algo.CreateEncryptor(key, iv);
                return new CryptoStream(dest, transform, CryptoStreamMode.Write);
            }
        }

        public static void Encrypt(Stream source, Stream dest, EncryptionType type, byte[] key, byte[] iv)
        {
            using (CryptoStream cs = GetEncryptStream(dest, type, key, iv)) {
                source.CopyTo(dest);
            }
        }

        public static CryptoStream GetDecryptStream(Stream source, EncryptionType type, byte[] key, byte[] iv)
        {
            using (SymmetricAlgorithm algo = GetSymmetricAlgorithm(type))
            {
                ICryptoTransform transform = algo.CreateDecryptor(key, iv);
                return new CryptoStream(source, transform, CryptoStreamMode.Read);
            }
        }

        public static void Decrypt(Stream source, Stream dest, EncryptionType type, byte[] key, byte[] iv)
        {
            using (CryptoStream cs = GetDecryptStream(source, type, key, iv)) {
                cs.CopyTo(dest);
            }
        }
    }
}

