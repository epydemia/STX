using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Gnu.Getopt;
using System.Reflection;

namespace StegaX
{
    class Program
    {
        static void Main(string[] args)
        {
            string payloadFile = "";
            string ImageFile = "";
            string outputFile = "";
            string passphrase = "";

            Banner();
            Getopt g = new Getopt("STX", args, "e:d:i:o:p:");
            int c;

            while ((c = g.getopt()) != -1)
            {
                switch (c)
                {
                    case 'e':
                        payloadFile = g.Optarg;
                        break;
                    case 'd':
                        ImageFile = g.Optarg;
                        break;
                    case 'i':
                        ImageFile = g.Optarg;
                        break;
                    case 'o':
                        outputFile = g.Optarg;
                        break;
                    case 'p':
                        passphrase = g.Optarg;
                        break;
                    default:
                        Usage();
                        return;

                }

            }

            if (ImageFile == "")
            {
                Console.WriteLine("No image has been provided");
                Usage();
                return;
            }

            if (!File.Exists(ImageFile))
            {
                Console.WriteLine("{0} does not exists", ImageFile);
                return;
            }

            if (payloadFile == "")
            {
                string originalFileName = "";
                try
                {
                    byte[] hash = new byte[32];

                    byte[] data = StegaXKernel.Decode(ImageFile, ref originalFileName, passphrase, ref hash);

                    Console.WriteLine("Found {0}\tLength: {1} bytes\n", originalFileName, data.Length);

                    if (File.Exists(originalFileName))
                    {
                        Console.Write("{0} already exists!\nOverwrite (Y/N)?", originalFileName);
                        string answer = Console.ReadLine();
                        if (answer.ToUpper() != "Y")
                        {
                            Console.Write("Alternative Filename? ");
                            answer = Console.ReadLine();
                            originalFileName = answer;
                        }

                    }
                    File.WriteAllBytes(originalFileName, data);
                    Console.WriteLine("{0} bytes written to disk", data.Length);
                    if (BitConverter.ToString(hash) == BitConverter.ToString(Crypto.ComputeHash(data)))
                    {
                        Console.WriteLine("Integrity Verification OK");
#if DEBUG
                        Console.WriteLine("Read CRC: {0}", BitConverter.ToString(hash).Replace("-", ""));
                        Console.WriteLine("Data CRC: {0}", BitConverter.ToString(Crypto.ComputeHash(data)).Replace("-", ""));
#endif
                    }
                    else
                    {
                        Console.WriteLine("Integrity Verification FAIL");
                        Console.WriteLine("Read CRC: {0}", BitConverter.ToString(hash).Replace("-", ""));
                        Console.WriteLine("Data CRC: {0}", BitConverter.ToString(Crypto.ComputeHash(data)).Replace("-", ""));
                    }
                }
                catch (System.Security.Cryptography.CryptographicException ex)
                {
                    Console.WriteLine("Cryptographic Exception: "+ex.Message);
                    Console.WriteLine("Incorrect Password?");
                    //throw;
                }
            }
            else
            {
                if (ImageFile != "" & outputFile != "")
                {
                    if (!File.Exists(payloadFile))
                    {
                        Console.WriteLine("{0} does not exists", payloadFile);
                        return;
                    }
                    try
                    {
                        Console.WriteLine("Encoding {0} into {1}", payloadFile, ImageFile);
                        Console.WriteLine("Output File: {0}", outputFile);
                        StegaXKernel.Encode(ImageFile, payloadFile, outputFile, passphrase);
                        Console.WriteLine("Done!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);

                    }

                }
                else
                {
                    Usage();
                    return;
                }
            }

        }

        static void Banner()
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            AssemblyName assemName = assem.GetName();
            Version ver = assemName.Version;
            Console.WriteLine("{0} Version {1}", assemName.Name, ver.ToString());

        }

        static void Usage()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("\tSTX -e <fileToEncode> -i <ImageFile.bmp|png|jpg> -o <OutputFile.png> [-p <passphrase>]");
            Console.WriteLine("\tSTX -d <ImageFile.png> [-p <passphrase>]");

        }
    }
}
