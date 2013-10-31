using System;
using System.Text;

namespace Kraken.Util
{
    public class ByteUtil
    {
        private ByteUtil()
        {
        }

        public static string ByteArrayToHexString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-","");
        }
        
        public static byte[] HexStringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                // ascii value lookup.
                
                bytes [i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static string ByteArrayToString(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes) {
                sb.AppendFormat("{0} ", b);
            }
            return sb.ToString();
        }

        public static byte[] ReverseEndian(byte[] bytes)
        {
            byte[] result = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; ++i) {
                result[bytes.Length - i - 1] = bytes[i];
            }
            return result;
        }
    }
}

