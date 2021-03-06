﻿//Uses the following website for the creation of the encrpyted hashes
//http://www.obviex.com/samples/Encryption.aspx
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Text;
using System.Security.Cryptography;
using System.Xml;
using OpenMetaverse.StructuredData;
using Nwc.XmlRpc;
using System.Windows.Forms;

namespace Aurora.Framework
{
    public static class Utilities
    {
        /// <summary>
        /// Get the URL to the release notes for the current version of Aurora
        /// </summary>
        /// <returns></returns>
        public static string GetServerReleaseNotesURL()
        {
            return "http://" + GetExternalIp() + ":" + OpenSim.Framework.MainServer.Instance.Port.ToString() + "/AuroraServerRelease" + AuroraServerVersion() + ".html";
        }

        /// <summary>
        /// Get the URL to our sim
        /// </summary>
        /// <returns></returns>
        public static string GetAddress()
        {
            return "http://" + GetExternalIp() + ":" + OpenSim.Framework.MainServer.Instance.Port.ToString();
        }

        /// <summary>
        /// What is our version?
        /// </summary>
        /// <returns></returns>
        public static string AuroraServerVersion()
        {
            return "1.0";
        }

        static string EncryptorType = "SHA1";
        static int EncryptIterations = 2;
        static int KeySize = 256;
        public static void SetEncryptorType(string type)
        {
            if (type == "SHA1" || type == "MD5")
            {
                EncryptorType = type;
            }
        }
        /// <summary>
        /// This is for encryption, it sets the number of times to iterate through the encryption
        /// </summary>
        /// <param name="iterations"></param>
        public static void SetEncryptIterations(int iterations)
        {
            EncryptIterations = iterations;
        }
        /// <summary>
        /// This is for encryption, it sets the size of the key
        /// </summary>
        /// <param name="size"></param>
        public static void SetKeySize(int size)
        {
            KeySize = size;
        }

        /// <summary>
        /// Encrypts specified plaintext using Rijndael symmetric key algorithm
        /// and returns a base64-encoded result.
        /// </summary>
        /// <param name="plainText">
        /// Plaintext value to be encrypted.
        /// </param>
        /// <param name="passPhrase">
        /// Passphrase from which a pseudo-random password will be derived. The
        /// derived password will be used to generate the encryption key.
        /// Passphrase can be any string. In this example we assume that this
        /// passphrase is an ASCII string.
        /// </param>
        /// <param name="saltValue">
        /// Salt value used along with passphrase to generate password. Salt can
        /// be any string. In this example we assume that salt is an ASCII string.
        /// </param>
        /// <param name="hashAlgorithm">
        /// Hash algorithm used to generate password. Allowed values are: "MD5" and
        /// "SHA1". SHA1 hashes are a bit slower, but more secure than MD5 hashes.
        /// </param>
        /// <param name="passwordIterations">
        /// Number of iterations used to generate password. One or two iterations
        /// should be enough.
        /// </param>
        /// <param name="initVector">
        /// Initialization vector (or IV). This value is required to encrypt the
        /// first block of plaintext data. For RijndaelManaged class IV must be 
        /// exactly 16 ASCII characters long.
        /// </param>
        /// <param name="keySize">
        /// Size of encryption key in bits. Allowed values are: 128, 192, and 256. 
        /// Longer keys are more secure than shorter keys.
        /// </param>
        /// <returns>
        /// Encrypted value formatted as a base64-encoded string.
        /// </returns>
        public static string Encrypt(string plainText,
                                     string passPhrase,
                                     string saltValue)
        {
            // Convert strings into byte arrays.
            // Let us assume that strings only contain ASCII codes.
            // If strings include Unicode characters, use Unicode, UTF7, or UTF8 
            // encoding.
            byte[] initVectorBytes = Encoding.ASCII.GetBytes("@IBAg3D4e5E6g7H5");
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            // Convert our plaintext into a byte array.
            // Let us assume that plaintext contains UTF8-encoded characters.
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // First, we must create a password, from which the key will be derived.
            // This password will be generated from the specified passphrase and 
            // salt value. The password will be created using the specified hash 
            // algorithm. Password creation can be done in several iterations.
            PasswordDeriveBytes password = new PasswordDeriveBytes(
                                                            passPhrase,
                                                            saltValueBytes,
                                                            EncryptorType,
                                                            EncryptIterations);

            // Use the password to generate pseudo-random bytes for the encryption
            // key. Specify the size of the key in bytes (instead of bits).
            byte[] keyBytes = password.GetBytes(KeySize / 8);

            // Create uninitialized Rijndael encryption object.
            RijndaelManaged symmetricKey = new RijndaelManaged();

            // It is reasonable to set encryption mode to Cipher Block Chaining
            // (CBC). Use default options for other symmetric key parameters.
            symmetricKey.Mode = CipherMode.CBC;

            // Generate encryptor from the existing key bytes and initialization 
            // vector. Key size will be defined based on the number of the key 
            // bytes.
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(
                                                             keyBytes,
                                                             initVectorBytes);

            // Define memory stream which will be used to hold encrypted data.
            MemoryStream memoryStream = new MemoryStream();

            // Define cryptographic stream (always use Write mode for encryption).
            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                                                         encryptor,
                                                         CryptoStreamMode.Write);
            // Start encrypting.
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);

            // Finish encrypting.
            cryptoStream.FlushFinalBlock();

            // Convert our encrypted data from a memory stream into a byte array.
            byte[] cipherTextBytes = memoryStream.ToArray();

            // Close both streams.
            memoryStream.Close();
            cryptoStream.Close();

            // Convert encrypted data into a base64-encoded string.
            string cipherText = Convert.ToBase64String(cipherTextBytes);

            // Return encrypted string.
            return cipherText;
        }

        /// <summary>
        /// Decrypts specified ciphertext using Rijndael symmetric key algorithm.
        /// </summary>
        /// <param name="cipherText">
        /// Base64-formatted ciphertext value.
        /// </param>
        /// <param name="passPhrase">
        /// Passphrase from which a pseudo-random password will be derived. The
        /// derived password will be used to generate the encryption key.
        /// Passphrase can be any string. In this example we assume that this
        /// passphrase is an ASCII string.
        /// </param>
        /// <param name="saltValue">
        /// Salt value used along with passphrase to generate password. Salt can
        /// be any string. In this example we assume that salt is an ASCII string.
        /// </param>
        /// <param name="hashAlgorithm">
        /// Hash algorithm used to generate password. Allowed values are: "MD5" and
        /// "SHA1". SHA1 hashes are a bit slower, but more secure than MD5 hashes.
        /// </param>
        /// <param name="passwordIterations">
        /// Number of iterations used to generate password. One or two iterations
        /// should be enough.
        /// </param>
        /// <param name="initVector">
        /// Initialization vector (or IV). This value is required to encrypt the
        /// first block of plaintext data. For RijndaelManaged class IV must be
        /// exactly 16 ASCII characters long.
        /// </param>
        /// <param name="keySize">
        /// Size of encryption key in bits. Allowed values are: 128, 192, and 256.
        /// Longer keys are more secure than shorter keys.
        /// </param>
        /// <returns>
        /// Decrypted string value.
        /// </returns>
        /// <remarks>
        /// Most of the logic in this function is similar to the Encrypt
        /// logic. In order for decryption to work, all parameters of this function
        /// - except cipherText value - must match the corresponding parameters of
        /// the Encrypt function which was called to generate the
        /// ciphertext.
        /// </remarks>
        public static string Decrypt(string cipherText,
                                     string passPhrase,
                                     string saltValue)
        {
            // Convert strings defining encryption key characteristics into byte
            // arrays. Let us assume that strings only contain ASCII codes.
            // If strings include Unicode characters, use Unicode, UTF7, or UTF8
            // encoding.
            byte[] initVectorBytes = Encoding.ASCII.GetBytes("@IBAg3D4e5E6g7H5");
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            // Convert our ciphertext into a byte array.
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            // First, we must create a password, from which the key will be 
            // derived. This password will be generated from the specified 
            // passphrase and salt value. The password will be created using
            // the specified hash algorithm. Password creation can be done in
            // several iterations.
            PasswordDeriveBytes password = new PasswordDeriveBytes(
                                                            passPhrase,
                                                            saltValueBytes,
                                                            EncryptorType,
                                                            EncryptIterations);

            // Use the password to generate pseudo-random bytes for the encryption
            // key. Specify the size of the key in bytes (instead of bits).
            byte[] keyBytes = password.GetBytes(KeySize / 8);

            // Create uninitialized Rijndael encryption object.
            RijndaelManaged symmetricKey = new RijndaelManaged();

            // It is reasonable to set encryption mode to Cipher Block Chaining
            // (CBC). Use default options for other symmetric key parameters.
            symmetricKey.Mode = CipherMode.CBC;

            // Generate decryptor from the existing key bytes and initialization 
            // vector. Key size will be defined based on the number of the key 
            // bytes.
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(
                                                             keyBytes,
                                                             initVectorBytes);

            // Define memory stream which will be used to hold encrypted data.
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);

            // Define cryptographic stream (always use Read mode for encryption).
            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                                                          decryptor,
                                                          CryptoStreamMode.Read);

            // Since at this point we don't know what the size of decrypted data
            // will be, allocate the buffer long enough to hold ciphertext;
            // plaintext is never longer than ciphertext.
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];

            // Start decrypting.
            int decryptedByteCount = 0;
            try
            {
                decryptedByteCount = cryptoStream.Read(plainTextBytes,
                                                           0,
                                                           plainTextBytes.Length);
            }
            catch (Exception)
            {
                return "";
            }

            // Close both streams.
            memoryStream.Close();
            cryptoStream.Close();

            // Convert decrypted data into a string. 
            // Let us assume that the original plaintext string was UTF8-encoded.
            string plainText = Encoding.UTF8.GetString(plainTextBytes,
                                                       0,
                                                       decryptedByteCount);

            // Return decrypted string.   
            return plainText;
        }

        private static string CachedExternalIP = "";
        /// <summary>
        /// Get OUR external IP
        /// </summary>
        /// <returns></returns>
        public static string GetExternalIp()
        {
            if (CachedExternalIP == "")
            {
                // External IP Address (get your external IP locally)
                String externalIp = "";
                UTF8Encoding utf8 = new UTF8Encoding();

                WebClient webClient = new WebClient();
                try
                {
                    //Ask what is my ip for it
                    externalIp = utf8.GetString(webClient.DownloadData("http://whatismyip.com/automation/n09230945.asp"));
                }
                catch (Exception) { }
                CachedExternalIP = externalIp;
                return externalIp;
            }
            else
                return CachedExternalIP;
        }

        /// <summary>
        /// Send a generic XMLRPC request
        /// </summary>
        /// <param name="ReqParams">params to send</param>
        /// <param name="method"></param>
        /// <param name="URL">URL to send the request to</param>
        /// <returns></returns>
        public static Hashtable GenericXMLRPCRequest(Hashtable ReqParams, string method, string URL)
        {
            ArrayList SendParams = new ArrayList();
            SendParams.Add(ReqParams);

            // Send Request
            XmlRpcResponse Resp;
            try
            {
                XmlRpcRequest Req = new XmlRpcRequest(method, SendParams);
                Resp = Req.Send(URL, 30000);
            }
            catch (WebException)
            {
                Hashtable ErrorHash = new Hashtable();
                ErrorHash["success"] = "false";
                return ErrorHash;
            }
            catch (SocketException)
            {
                Hashtable ErrorHash = new Hashtable();
                ErrorHash["success"] = "false";
                return ErrorHash;
            }
            catch (XmlException)
            {
                Hashtable ErrorHash = new Hashtable();
                ErrorHash["success"] = "false";
                return ErrorHash;
            }
            if (Resp.IsFault)
            {
                Hashtable ErrorHash = new Hashtable();
                ErrorHash["success"] = "false";
                return ErrorHash;
            }
            Hashtable RespData = (Hashtable)Resp.Value;
            if (RespData != null)
            {
                RespData["success"] = "true";
                return RespData;
            }
            else
            {
                RespData = new Hashtable();
                RespData["success"] = "false";
                return RespData;
            }
        }
        
        /// <summary>
        /// Send a generic XMLRPC request but with encryption on the values
        /// </summary>
        /// <param name="ReqParams">Params to send</param>
        /// <param name="method"></param>
        /// <param name="URL">URL to post to</param>
        /// <param name="passPhrase">pass to remove the encryption</param>
        /// <param name="salt">Extra additional piece that is added to the password</param>
        /// <returns></returns>
        public static Hashtable GenericXMLRPCRequestEncrypted(Hashtable ReqParams, string method, string URL, string passPhrase, string salt)
        {
            Hashtable reqP = new Hashtable();
            //Encrypt the values
            foreach(KeyValuePair<string, object> kvp in ReqParams)
            {
                reqP.Add(kvp.Key, Encrypt(kvp.Value.ToString(), passPhrase, salt));
            }

            ArrayList SendParams = new ArrayList();
            SendParams.Add(reqP);

            // Send Request
            XmlRpcResponse Resp;
            try
            {
                XmlRpcRequest Req = new XmlRpcRequest(method, SendParams);
                Resp = Req.Send(URL, 30000);
            }
            catch (WebException)
            {
                Hashtable ErrorHash = new Hashtable();
                ErrorHash["success"] = "false";
                return ErrorHash;
            }
            catch (SocketException)
            {
                Hashtable ErrorHash = new Hashtable();
                ErrorHash["success"] = "false";
                return ErrorHash;
            }
            catch (XmlException)
            {
                Hashtable ErrorHash = new Hashtable();
                ErrorHash["success"] = "false";
                return ErrorHash;
            }
            if (Resp.IsFault)
            {
                Hashtable ErrorHash = new Hashtable();
                ErrorHash["success"] = "false";
                return ErrorHash;
            }
            Hashtable RespData = (Hashtable)Resp.Value;
            if (RespData != null)
            {
                RespData["success"] = "true";
                return RespData;
            }
            else
            {
                RespData = new Hashtable();
                RespData["success"] = "false";
                return RespData;
            }
        }

        /// <summary>
        /// Read a website into a string
        /// </summary>
        /// <param name="URL">URL to change into a string</param>
        /// <returns></returns>
        public static string ReadExternalWebsite(string URL)
        {
            String website = "";
            UTF8Encoding utf8 = new UTF8Encoding();

            WebClient webClient = new WebClient();
            try
            {
                website = utf8.GetString(webClient.DownloadData(URL));
            }
            catch (Exception)
            {
            }
            return website;
        }

        /// <summary>
        /// Download the file from downloadLink and save it to Aurora + Version + 
        /// </summary>
        /// <param name="downloadLink">Link to the download</param>
        /// <param name="filename">Name to put the download in</param>
        public static void DownloadFile(string downloadLink, string filename)
        {
            WebClient webClient = new WebClient();
            try
            {
                OpenSim.Framework.Console.MainConsole.Instance.Output("Downloading new file from " + downloadLink + " now into file " + filename + ".", "WARN");
                webClient.DownloadFile(downloadLink, filename);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Bring up a popup with a text box to ask the user for some input
        /// </summary>
        /// <param name="title"></param>
        /// <param name="promptText"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }
    }
}
