using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;

namespace StegaX
{
    static public class StegaXKernel
    {
        static UInt32 Mask = PrepareByte(0xFF);
        
        /// <summary>
        /// This function prepare one payload byte into three byte for RGB insertion
        /// </summary>
        /// <param name="inputByte">Payload byte</param>
        /// <returns></returns>
        static UInt32 PrepareByte(byte inputByte)
        {
            byte[] part = new byte[4];
            part[0] = (byte)(inputByte & 0x7);            // 00 00 01 11
            part[1] = (byte)((inputByte & 0x38) >> 3);    // 00 11 10 00
            part[2] = (byte)((inputByte & 0xC0) >> 6);     // 11 00 00 00

            return BitConverter.ToUInt32(part, 0);
        }

        /// <summary>
        /// This function extract one byte from one pixel
        /// </summary>
        /// <param name="InputPixel">RGB Pixel with encoded data</param>
        /// <returns></returns>
        static byte GetByteFromPixel(Int32 InputPixel)
        {
            byte[] part = BitConverter.GetBytes(InputPixel & Mask);
            byte b = part[0];
            b |= (byte)(part[1] << 3);
            b |= (byte)(part[2] << 6);

            return b;
        }

        /// <summary>
        /// This function encode one byte into one Pixel
        /// </summary>
        /// <param name="Pixel">RGB Pixel from image source</param>
        /// <param name="data">Payload byte</param>
        /// <returns>RGB Pixel with data encoded</returns>
        static Int32 CreatePixel(Int32 Pixel, byte data)
        {
            Int32 ret = (int)(Pixel & (~Mask)); // Setta a zero i bit che conterranno il payload
            ret |= (int)PrepareByte(data);  // Setta i bit del payload
            return ret;
        }

        /// <summary>
        /// This function create the payload to be encoded into image.
        /// </summary>
        /// <param name="FileToEncode">Filename of the payload file</param>
        /// <param name="payloadSize">Number of byte that can be encoded into image</param>
        /// <param name="passphrase">Passphrase (if empty payload will not be encrypted)</param>
        /// <returns></returns>
        static byte[] PreparePayload(string FileToEncode, int payloadSize, string passphrase)
        {

            MemoryStream str = new MemoryStream();
            byte[] data = File.ReadAllBytes(FileToEncode);
            Int32 FileSize = data.Length;
            string filename = Path.GetFileName(FileToEncode);
            Int32 FileNameLen = filename.Length;

            // la struttura del payload è la segg.
            // Uint32 FileSize
            // Uint32 FileNameLen
            // String Filename
            // byte[] data

            str.Write(BitConverter.GetBytes(FileSize), 0, 4);
            str.Write(BitConverter.GetBytes(FileNameLen), 0, 4);
            str.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(filename), 0, filename.Length);
            str.Write(data, 0, data.Length);
            byte[] hash = Crypto.ComputeHash(data);
            str.Write(hash, 0, hash.Length);

            byte[] payload = str.ToArray();

            if (payload.Length >= payloadSize)
                throw new Exception("File too Big! Max Size is "+payloadSize.ToString()+" bytes");

            Array.Resize(ref payload, payloadSize);

            str.Close();

            if (passphrase == "")
                return payload;
            else
                return Crypto.Encrypt(payload, passphrase);
        }

        /// <summary>
        /// This function encode one file into one image and write the encoded data into an image
        /// </summary>
        /// <param name="ImageFileName">Image File Name</param>
        /// <param name="FileToEncode">Paylaod File Name</param>
        /// <param name="OutputFileName">Output File Name</param>
        /// <param name="passphrase">Passphrase to encrypt data</param>
        static public void Encode(string ImageFileName, string FileToEncode, string OutputFileName, string passphrase)
        {

            Bitmap Image = new Bitmap(ImageFileName);
            EncodeIntoImage(ref Image, FileToEncode, passphrase);
            Image.Save(OutputFileName, System.Drawing.Imaging.ImageFormat.Png);

        }

        /// <summary>
        /// This file create an image with data encoded into
        /// </summary>
        /// <param name="Image">Image Data</param>
        /// <param name="FileToEncode">Patlaod File Name</param>
        /// <param name="passphrase">Passphrase to encrypt data</param>
        static void EncodeIntoImage(ref Bitmap Image, string FileToEncode, string passphrase)
        {

            int height = Image.Height;
            int width = Image.Width;
            int payloadSize = height * width - 32;
            byte[] payload = PreparePayload(FileToEncode, payloadSize, passphrase);
            
            int count = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (count < payload.Length)
                    {
                        int pixel = Image.GetPixel(j, i).ToArgb();
                        pixel = CreatePixel(pixel, payload[count]);
                        Image.SetPixel(j, i, Color.FromArgb(pixel));
                        count++;
                    }
                    else
                        return;

                }
            }

        }

        /// <summary>
        /// This Function extract a Payload Data from an RGB Image
        /// </summary>
        /// <param name="Image">RGB Image Data</param>
        /// <param name="passphrase">Passphrase to decrypt data</param>
        /// <returns></returns>
        static PayloadFile GetPayloadFromImage(Bitmap Image, string passphrase)
        {
            int height = Image.Height;
            int width = Image.Width;

            PayloadFile payload = new PayloadFile();

            MemoryStream str = new MemoryStream();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Int32 pixel = Image.GetPixel(j, i).ToArgb();
                    str.WriteByte(GetByteFromPixel(pixel));
                }
            }

            if (passphrase != "")
            {
                byte[] cipherByte = str.ToArray();
                int cipherTextLength = (cipherByte.Length - 32) / 16;

                cipherTextLength = (cipherTextLength + 1) * 16;

                Array.Resize(ref cipherByte, cipherTextLength);

                str = new MemoryStream(Crypto.Decrypt(cipherByte, passphrase));
            }

            str.Seek(0, SeekOrigin.Begin);
            byte[] temp = new byte[4];
            str.Read(temp, 0, 4);
            UInt32 FileSize = BitConverter.ToUInt32(temp, 0);

            str.Read(temp, 0, 4);
            UInt32 FileNameLen = BitConverter.ToUInt32(temp, 0);

            byte[] buffer = new byte[FileNameLen];
            str.Read(buffer, 0, (int)FileNameLen);

            payload.Filename = System.Text.ASCIIEncoding.ASCII.GetString(buffer);
            payload.data = new byte[FileSize];

            str.Read(payload.data, 0, (int)FileSize);

            payload.hash = new byte[32];
            str.Read(payload.hash, 0, 32);

            str.Close();

            return payload;

        }

        /// <summary>
        /// This function decode and extract the payload from an image
        /// </summary>
        /// <param name="ImageFileName">Image File Name</param>
        /// <param name="OriginalFileName">returns the Decoded Payload File Name</param>
        /// <param name="passphrase">Passphrase for data decryption</param>
        /// <param name="hash">return the file hash</param>
        static public byte[] Decode(string ImageFileName, ref string OriginalFileName, string passphrase)
        {
            /*PayloadFile payload = new PayloadFile();
            Bitmap Image = new Bitmap(ImageFileName);
            payload = GetPayloadFromImage(Image, passphrase);
            OriginalFileName = payload.Filename;
            return payload.data;*/
            byte[] dummy = new byte[32];
            return Decode(ImageFileName, ref OriginalFileName, passphrase, ref dummy);

        }

        /// <summary>
        /// This function decode and extract the payload from an image
        /// </summary>
        /// <param name="ImageFileName">Image File Name</param>
        /// <param name="OriginalFileName">returns the Decoded Payload File Name</param>
        /// <param name="passphrase">Passphrase for data decryption</param>
        /// <param name="hash">return the file hash</param>
        /// <returns>Payload data</returns>
        static public byte[] Decode(string ImageFileName, ref string OriginalFileName, string passphrase, ref byte[] hash)
        {
            PayloadFile payload = new PayloadFile();
            Bitmap Image = new Bitmap(ImageFileName);
            payload = GetPayloadFromImage(Image, passphrase);
            OriginalFileName = payload.Filename;
            hash = payload.hash;
            return payload.data;
        }

    }
}
