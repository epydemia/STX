using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace StegaX
{
    static public class Crypto
    {

        static SHA256Cng sha256 = new SHA256Cng();
        static MD5Cng md5 = new MD5Cng();
        static string IVPhrase = "StegaX";

        static public byte[] Encrypt(byte[] plainText, string passphrase)
        {
            byte[] encrypted;
            
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.KeySize = 256;
                rijAlg.Key = sha256.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(passphrase));
                rijAlg.IV = md5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(IVPhrase));

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {

                        csEncrypt.Write(plainText, 0, plainText.Length);
                        csEncrypt.FlushFinalBlock();
                        encrypted = msEncrypt.ToArray();
                    }
                }
                return encrypted;
            }
        }

        static public byte[] Decrypt(byte[] cipherText, string passphrase)
        {

            // Declare the string used to hold
            // the decrypted text.
            byte[] plaintext;

            // Create an Rijndael object
            // with the specified key and IV.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.KeySize = 256;
                rijAlg.Key = sha256.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(passphrase));
                rijAlg.IV = md5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(IVPhrase));

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {

                        csDecrypt.Write(cipherText, 0, cipherText.Length);
                        csDecrypt.FlushFinalBlock();
                        plaintext = msDecrypt.ToArray();
                    }
                }

            }

            return plaintext;

        }
        
        static public byte[] ComputeHash(byte[] Payload)
        {
            
            return sha256.ComputeHash(Payload);
        }

        static public bool CheckHash(byte[] Payload, byte[] Hash)
        {
            return Array.Equals(ComputeHash(Payload), Hash);
        }
    
    }

}
