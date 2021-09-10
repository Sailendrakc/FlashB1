using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlashPeer
{
    public class Crypto
    {
        private AesManaged ClientAES;

        private RSACryptoServiceProvider OurRsa;
        private string testkey = "<RSAKeyValue><Modulus>mgMp3fgUlldflNQY00sCvP9DHy8zysSt4KdQFkPjGoAkzon9+QLdiTa2G57fC" +
            "/29ahhTrsRRtPhhQVJyckl72Nm+/31ceD1bOvjVQF7vfTbnoyDJGuwu9DAcQwjUk6BIn8ScfVuFVFdRAxCPgrK3h/PGgDKP+wLD2U87La68WuTmOep1" +
            "/BhVTVET7woa91ijTr4GUoLLSYJOH3+/MRYyIGZuDxTqXXD79XdqlsdxU0PsEuilKeKEk3uv3vE40eoQi6MrjEBW8xGI1RpG3Ya3Cq6VsWMRU" +
            "+xB4X8mY48Vh6pYbpoNWAVI5rGbnOeh7IwBgEiN6urllE26KGLtGUzUpQ==</Modulus><Exponent>AQAB</Exponent><P>yHAzAoWgw1G" +
            "ABJLrmdSoUZBOdhStgznI2iS5s2t01+cGCOlwFlZop5f9znCcferLAIvmZd2xoI55ereb7WHHBgKA9/LlnY8UV0uYYRlzFavXr7YecO1OsCP" +
            "/yq0YG8AuOJ805301H5vUU4hGwnEqbTd3jQRgLPmkg9Pb38utT/8=</P><Q>xLRrL5vrQJYWNzVHxB8kdwsnAjHmC4eNfM4GWauNeLgllvC" +
            "zOjBqW1WgY1CmyJkB1GRGjuBIniCEhLSQcGd/LM6zKKDWaa20+lVYYW3lyCIB34t3ovNc2xGWulMQzYmRJX7q0SBWSdvk7XrAaI1eJ+A5V62gO" +
            "mCCqb79U5y+m1s=</Q><DP>DPCpamVEHrf5QtJVYYYII9PkLN1i4lDttZ+fWYT7cyDYE+U2Nkr30LIQUa6Ve8+XsX5Wrobke9AW6aRG7cldNvcca" +
            "mWFC2n8TzJzMPmao3CHqTFhv7qiVN7OGcCZCNmcYk1s9fDwaA0AZTAsGUuDCLAHCNSafOzVASnBTS5yDvM=</DP><DQ>UshPN7kZt5Oyg8eDjXFBy" +
            "mvCHfVcCEwi6nxWRdSh9EUjZLOl6f5INGoD1ugxWMiz8WvGGgkf5pRu0N6gzv1vky7mTVnrAoydVqEmUdKLWr+dJDQwxD5BPNzZH08oCig0EqCoOB" +
            "yw0+KcJKl9YkLkdkmyOEkU3pyRQNjlChQ0T3M=</DQ><InverseQ>SaV+snltT7hiWDrEXjYp/oKT2QG/296uQCNnfhdwPnt911+eNHUD4NOh7" +
            "eUykVfxD42loO35666o6teccbwlHcgDWIIgTP/+7x/AkZPOABymRj18QeB1kN+kdZuG201a7xavcxhPqT4E65smwOHgZv2B8mZ3VqcGwJ" +
            "XjKhmxREY=</InverseQ><D>PYmKDwDy6Odcb5EXokVUgMPVw/4OSbSwbRUtMNhLQ+lzy3rjmb2FWzAbL4oZQSdPqbayqSAULaUY5wrUY8ns" +
            "zEakxF0It5p8e2G1g5TrSDLJ9ypAcJtyX2thv38lwR7IJd5fUM9ixMJmwjy5utVB8/Z5l/ucAAWoz4mS8bvh0scIbzQb4JU0aNtxRoOC3onh" +
            "BsW0zBv5Kce3aY/mz/iloAcpIYpvl4tjV7l5tgarPF4vRleMNHGIi7WU7SavBhLDo5x+27GyT/km5Za5I94J8P6FJeLYWla/Jo06/wZyIwNipQ" +
            "lcFW5g0mw1d0py+sMva9M9cFpxuTZx8cYoBf63+Q==</D></RSAKeyValue>";
        public Crypto(string rsaKeytoUse, bool isServer)
        {
            //crypto stuffs
            if(rsaKeytoUse == null)
            {
                rsaKeytoUse = testkey;
            }
            ClientAES = new AesManaged();
            ClientAES.Padding = PaddingMode.PKCS7;
            ClientAES.KeySize = 256;
            ClientAES.GenerateIV();
            ClientAES.GenerateKey();


            OurRsa = new RSACryptoServiceProvider(2048);

            if (rsaKeytoUse != null)
            {
                if (isServer)
                {
                    RSAfromXmlString(true, ref OurRsa, rsaKeytoUse);
                    
                }
                else
                {
                    RSAfromXmlString(false, ref OurRsa, rsaKeytoUse);
                }
            }
        }


        public bool HelloUnpacker(byte[] udata, FlashPeer p)
        {
            byte[] payld = new byte[256]; //(u.data.Length - FlashProtocol.Instance.Pmanager.Overhead)
            Array.Copy(udata, PacketSerializer.PayloadSTR, payld, 0, 256);

            byte[] b = RSADecrypt(payld);

            if (b == null)
            {
                return false;
            }

            byte[] key = new byte[ClientAES.Key.Length];//32
            byte[] iv = new byte[ClientAES.IV.Length]; //16

            Array.Copy(b, 0, key, 0, key.Length);
            Array.Copy(b, key.Length, iv, 0, iv.Length);

            p.crypto.ClientAES.IV = iv;
            p.crypto.ClientAES.Key = key;
            return true;
        }

        public byte[] RSADecrypt(byte[] data)
        {
            if (data.Length != 256)
            {
                FlashProtocol.Instance.RaiseOtherEvent("Cannot decrypt data with other length than 256", null, EventType.cryptography, null);
                return null;
            }

            try
            {
                return OurRsa.Decrypt(data, false);
            }
            catch (Exception e)
            {
                byte[] toCheck = Encoding.UTF8.GetBytes(RSAtoXmlString(false, ref OurRsa));
                byte tcrc = FlashProtocol.Instance.Pmaker.ComputeChecksum(toCheck, null, toCheck.Length);
                FlashProtocol.Instance.RaiseOtherEvent($"ERROR:: crc of our public key is {tcrc} and e.msg is {e.Message}", null, EventType.cryptography, null);

                return null;
            }

        }

        public byte[] RSAEncrypt(ref byte[] data)
        {
            if (data.Length > 245) // just a test
            {
                FlashProtocol.Instance.RaiseOtherEvent("RSA cannot encrypt data more than 245 bytes.", null, EventType.cryptography, null);
                return null;
            }

            return OurRsa.Encrypt(data, false);
        }

        public byte[] AESEncryptText(string plainText)
        {
            byte[] encrypted;

            using (ICryptoTransform encryptor = ClientAES.CreateEncryptor())
            {
                // Create MemoryStream    
                using (MemoryStream ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption    
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
                    // to encrypt    
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream    
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }

                        encrypted = ms.ToArray();
                    }
                }
            }
            // Return encrypted data    
            return encrypted;
        }
        public string AESDecryptText(ref byte[] cipherText)
        {
            string plaintext = null;
            // Create AesManaged    

            // Create a decryptor    
            using (ICryptoTransform decryptor = ClientAES.CreateDecryptor())
            {
                // Create the streams used for decryption.    
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    // Create crypto stream    
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        // Read crypto stream    
                        using (StreamReader reader = new StreamReader(cs))
                        {
                            plaintext = reader.ReadToEnd();
                            Console.WriteLine("MSG: " + plaintext);
                        }

                    }
                }
            }

            return plaintext;
        }

        public byte[] AESEncryptBytes(ref byte[] plainBytes)
        {
            string s = Encoding.UTF8.GetString(plainBytes);
            return AESEncryptText(s);
        }

        public byte[] AESDecryptBytes(ref byte[] cipherBytes)
        {
            return Encoding.UTF8.GetBytes(AESDecryptText(ref cipherBytes));
        }

        private void RSAfromXmlString(bool fullLen, ref RSACryptoServiceProvider rsaToChange, string XmlString)
        {
            RSAParameters p = new RSAParameters();
            string[] ss = new string[2];
            if (fullLen)
            {
                ss[0] = "<Modulus>";
                ss[1] = "</Modulus>";

                p.Modulus = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

                ss[0] = "<Exponent>";
                ss[1] = "</Exponent>";
                p.Exponent = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

                ss[0] = "<P>";
                ss[1] = "</P>";
                p.P = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

                ss[0] = "<Q>";
                ss[1] = "</Q>";
                p.Q = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

                ss[0] = "<DP>";
                ss[1] = "</DP>";
                p.DP = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

                ss[0] = "<DQ>";
                ss[1] = "</DQ>";
                p.DQ = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

                ss[0] = "<InverseQ>";
                ss[1] = "</InverseQ>";
                p.InverseQ = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

                ss[0] = "<D>";
                ss[1] = "</D>";
                p.D = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

            }
            else
            {
                ss[0] = "<Modulus>";
                ss[1] = "</Modulus>";
                p.Modulus = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);

                ss[0] = "<Exponent>";
                ss[1] = "</Exponent>";
                p.Exponent = Convert.FromBase64String(XmlString.Split(ss, 4, StringSplitOptions.RemoveEmptyEntries)[1]);
            }

            rsaToChange.ImportParameters(p);
        }

        /*public void RSASet(string key, bool full)
        {
            PeerRSAkey = key;
            RSASetKeys(full, ref OurRsa, key);
        }*/

        private string RSAtoXmlString(bool fullLen, ref RSACryptoServiceProvider rsatoGet)
        {
            if (rsatoGet == null)
            {
                return null;
            }

            RSAParameters p = rsatoGet.ExportParameters(fullLen);

            StringBuilder sb = new StringBuilder();

            sb.Append("<RSAKeyValue>");

            if (fullLen)
            {
                //all public and private

                //moduls
                sb.Append("<Modulus>");
                sb.Append(Convert.ToBase64String(p.Modulus));
                sb.Append("</Modulus>");

                //<Exponent>
                sb.Append("<Exponent>");
                sb.Append(Convert.ToBase64String(p.Exponent));
                sb.Append("</Exponent>");

                //<P>
                sb.Append("<P>");
                sb.Append(Convert.ToBase64String(p.P));
                sb.Append("</P>");

                //<Q>
                sb.Append("<Q>");
                sb.Append(Convert.ToBase64String(p.Q));
                sb.Append("</Q>");

                //<DP>
                sb.Append("<DP>");
                sb.Append(Convert.ToBase64String(p.DP));
                sb.Append("</DP>");

                //<DQ>
                sb.Append("<DQ>");
                sb.Append(Convert.ToBase64String(p.DQ));
                sb.Append("</DQ>");

                //<InverseQ>
                sb.Append("<InverseQ>");
                sb.Append(Convert.ToBase64String(p.InverseQ));
                sb.Append("</InverseQ>");

                //<D>
                sb.Append("<D>");
                sb.Append(Convert.ToBase64String(p.D));
                sb.Append("</D>");


            }
            else
            {
                //only public

                sb.Append("<Modulus>");
                sb.Append(Convert.ToBase64String(p.Modulus));
                sb.Append("</Modulus>");

                //<Exponent>
                sb.Append("<Exponent>");
                sb.Append(Convert.ToBase64String(p.Exponent));
                sb.Append("</Exponent>");
            }

            sb.Append("</RSAKeyValue>");

            return sb.ToString();
        }

        public byte[] GetAESKey()
        {
            return ClientAES.Key;
        }

        public byte[] GetAESIV()
        {
            return ClientAES.IV;
        }

        public void SetAesKey(byte[] key, byte[] iv)
        {
            if ((key.Length != 32) || (iv.Length != 16))
            {
                FlashProtocol.Instance.RaiseOtherEvent("AES key or IV length not 32 or 16", null, EventType.cryptography, null);
                return;
            }
            ClientAES.Key = key;
            ClientAES.IV = iv;
        }
    }
}
