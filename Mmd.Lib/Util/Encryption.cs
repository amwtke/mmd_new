using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Web.Security;
using System.Globalization;
using System.IO;


namespace MD.Lib.Util
{
     public class Encryption
    {
        public Encryption()
        {

        }
        /// <summary>
        /// MD5 encryption method
        /// </summary>
        /// <returns>MD5 encryption results</returns>
        public static string EncryptMD5(string oldData)
        {
            //Will OldData encoding for a byte sequence. 
            Byte[] clearBytes = new UnicodeEncoding().GetBytes(oldData);
            //Calculating the hash value byte array.
            Byte[] hashedBytes = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(clearBytes);
            //Return string MD5 encrypted ciphertext
            return BitConverter.ToString(hashedBytes);
        }
        

        /// <summary>
        /// DESencryption method
        /// </summary>
        /// <param name="oldData">Want to encrypt the original text</param>
        /// <returns>DES encryption results</returns>
        /// 
        public static string EncryptDES(string oldData)
        {
            string key = @"??^&$%^&"; // Encryption key
            //Will be designated OldData all character encoding for a byte sequence.
            byte[] data = Encoding.UTF8.GetBytes(oldData);

            //Instantiate a DESCryptoServiceProvider
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            //Access to or set up Data Encryption Standard (des) algorithm secret key
            des.Key = ASCIIEncoding.ASCII.GetBytes(key);

            //DES algorithm to obtain or set the initialization vector (IV). (From SymmetricAlgorithm succession.)
            des.IV = ASCIIEncoding.ASCII.GetBytes(key);

            //Create a symmetric Data Encryption Standard (des) encryption object
            ICryptoTransform desencrypt = des.CreateEncryptor();

            //Calculation of specified byte array conversion designated area
            byte[] result = desencrypt.TransformFinalBlock(data, 0, data.Length);
            return BitConverter.ToString(result);
        }

        /// <summary>
        /// DES decryption methods
        /// </summary>
        /// <param name="oldData">To decrypt the original text</param>
        /// <returns>DES decryption results</returns>

        public static string DecodeDES(string oldData)
        {
            try
            {
                string key = @"??^&$%^&"; //decryption key
                //Back to contain OldData in sub-string (from "-" delimited) array of String。 
                string[] inputData = oldData.Split("-".ToCharArray());
                byte[] data = new byte[inputData.Length];
                for (int i = 0; i < inputData.Length; i++)
                {
                    data[i] = byte.Parse(inputData[i], NumberStyles.HexNumber);
                }
                //Instantiate a DESCryptoServiceProvider
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();

                //Access to or set up Data Encryption Standard (des) algorithm secret key 
                des.Key = ASCIIEncoding.ASCII.GetBytes(key);

                //DES algorithm to obtain or set the initialization vector (IV). (From SymmetricAlgorithm succession.)
                des.IV = ASCIIEncoding.ASCII.GetBytes(key);

                //Create a symmetric Data Encryption Standard (des) encryption object
                ICryptoTransform desencrypt = des.CreateDecryptor();

                //Calculation of specified byte array conversion designated area
                byte[] result = desencrypt.TransformFinalBlock(data, 0, data.Length);
                return Encoding.UTF8.GetString(result);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 用來加密url參數
        /// </summary>
        /// <param name="strToEncrypt"></param>
        /// <returns></returns>
        public static string UrlEncrypt(string strToEncrypt)
        {
            string strEncryptKey = @"??^&$%^&";
            Byte[] key;
            Byte[] IV = new Byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            try
            {



                key = Encoding.UTF8.GetBytes(strEncryptKey.Substring(0, 8));
                Byte[] inputByteArray = Encoding.UTF8.GetBytes(strToEncrypt);

                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                return Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return string.Empty;
            }

        }

        /// <summary>
        /// 用來解密url參數
        /// </summary>
        /// <param name="strToDecrypt"></param>
        /// <returns></returns>
        public static string UrlDecrypt(string strToDecrypt)
        {
            Byte[] key;
            Byte[] IV = new Byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            string strEncryptKey = @"??^&$%^&";
            try
            {
                key = Encoding.UTF8.GetBytes(strEncryptKey.Substring(0, 8));
                Byte[] inputByteArray = Convert.FromBase64String(strToDecrypt);

                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string UserMd5(string s)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
            bytes = md5.ComputeHash(bytes);
            md5.Clear();

            string ret = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                ret += Convert.ToString(bytes[i], 16).PadLeft(2, '0');
            }

            return ret.PadLeft(32, '0');
        }
    }
}
