using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CheckinHoyoverse
{
    internal static class AES
    {
        internal static byte[] Encrypt(string plainText, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                byte[] iv = aes.IV;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        byte[] encryptedBytes = memoryStream.ToArray();
                        byte[] combinedIVCiphertext = new byte[iv.Length + encryptedBytes.Length];
                        Array.Copy(iv, 0, combinedIVCiphertext, 0, iv.Length);
                        Array.Copy(encryptedBytes, 0, combinedIVCiphertext, iv.Length, encryptedBytes.Length);
                        return combinedIVCiphertext;
                    }
                }
            }
        }

        internal static string Decrypt(byte[] encryptedBytes, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] ciphertext = new byte[encryptedBytes.Length - iv.Length];
                Array.Copy(encryptedBytes, iv, iv.Length);
                Array.Copy(encryptedBytes, iv.Length, ciphertext, 0, ciphertext.Length);
                aes.IV = iv;

                using (MemoryStream memoryStream = new MemoryStream(ciphertext))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
