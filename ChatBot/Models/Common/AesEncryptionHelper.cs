using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatBot.Models.Common
{
    public static class AesEncryptionHelper
    {
        private const string Key = "ThisIsTheMedibankKey2019"; // 16, 24, or 32 bytes
        //private const string IV = "41-9E-A3-46-9A-FD-25-DD-17-71-EA-AF-6B-A3-1C-B7"; // 16 bytes
        private static readonly byte[] Iv = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };

        public static string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(Key);
                // aesAlg.GenerateIV(); // Generate a random IV
                aesAlg.IV = Iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    byte[] ivBytes = aesAlg.IV;
                    byte[] encryptedBytes = msEncrypt.ToArray();
                    byte[] result = new byte[ivBytes.Length + encryptedBytes.Length];
                    Buffer.BlockCopy(ivBytes, 0, result, 0, ivBytes.Length);
                    Buffer.BlockCopy(encryptedBytes, 0, result, ivBytes.Length, encryptedBytes.Length);
                    return Convert.ToBase64String(result);
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(Key);
                byte[] ivBytes = new byte[aesAlg.BlockSize / 8];
                Buffer.BlockCopy(cipherBytes, 0, ivBytes, 0, ivBytes.Length);
                aesAlg.IV = ivBytes;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes, ivBytes.Length, cipherBytes.Length - ivBytes.Length))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static string SplitAndDecrypt(string input)
        {
            string[] substrings = input.Split(' '); // Split the input string by space
            StringBuilder decryptedResult = new StringBuilder();
            string specialCharacter = ""; string decryptedString = "";
            foreach (string substring in substrings)
            {
                if (substring.Length > 40 && !substring.Contains("image"))
                {
                    if (substring.Contains(","))
                    {
                        string[] parts = substring.Split(',');
                        decryptedString = Decrypt(parts[0]); // Call decrypt method for strings with length > 40
                        decryptedString += parts[1];

                    }
                    else
                    {
                        decryptedString = Decrypt(substring); // Call decrypt method for strings with length > 40
                    }

                    decryptedResult.Append(decryptedString).Append(" ");
                }
                else
                {
                    decryptedResult.Append(substring).Append(" ");
                }
            }

            return decryptedResult.ToString().Trim();
        }

        public static void DecryptList<T>(List<T> list) where T : class
        {
            if (list != null && list.Count > 0)
            {
                var properties = typeof(T).GetProperties();
                foreach (var item in list)
                {
                    foreach (var property in properties)
                    {
                        try
                        {
                            var encryptedValue = property.GetValue(item) as string;
                            if (!string.IsNullOrEmpty(encryptedValue))
                            {
                                if (property.Name.Equals("FirstName", StringComparison.OrdinalIgnoreCase) ||
                                    property.Name.Equals("LastName", StringComparison.OrdinalIgnoreCase) ||
                                    property.Name.Equals("ContactNo", StringComparison.OrdinalIgnoreCase))
                                {
                                    var decryptedValue = Decrypt(encryptedValue);
                                    property.SetValue(item, decryptedValue);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Handle decryption error, e.g., log it
                            Console.WriteLine($"Decryption error: {ex.Message}");
                        }
                    }
                }
            }
        }

    }
}
