using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace FileAnywhereClient
{
    class Session
    {
        private string sessionToken = String.Empty;
        private string organizationID, username, password;
        public Session(string orgID, string user, string pass)
        {
            organizationID = orgID;
            username = user;
            password = pass;
        }

        #region AccountLogin
        public void Login()
        {
            // Check if all required 
            if (!HasValidLoginDetails())
            {
                return;
            }

            // SOAP XML request 
            string soap =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                 <soap:Body>
                 <AccountLogin xmlns=""http://api.filesanywhere.com/"">
                 <APIKey>{0}</APIKey>
                 <OrgID>{1}</OrgID>
                 <UserName>{2}</UserName>
                 <Password>{3}</Password>
                 <AllowedIPList></AllowedIPList>
                 <ClientEncryptParam></ClientEncryptParam>
                 </AccountLogin>
                 </soap:Body>
                </soap:Envelope>";

            // Send SOAP request
            string response = SOAP.SendSOAPRequest("http://api.filesanywhere.com/AccountLogin",
                String.Format(soap, Globals.API_KEY, organizationID, username, password));

            // Check if response was empty or returned an error.
            if (string.IsNullOrEmpty(response))
            {
                return;
            }

            string error = GetResponseError(response);

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("\r\nError occurred during login: " + error);
                return;
            }

            // Extract token from response and store it in this session.
            XDocument doc = XDocument.Parse(response);
            XNamespace xmlnsa = "http://api.filesanywhere.com/";

            if (doc.Descendants(xmlnsa + "Token").Count() != 0)
            {
                sessionToken = doc.Descendants(xmlnsa + "Token").First().Value;
                if (!string.IsNullOrEmpty(sessionToken))
                {
                    Console.WriteLine("\r\nSuccesfully logged into: " + username);
                }
            }
            else
            {
                Console.WriteLine("\r\nUnknown error occurred. No token received.");
            }
        }
        #endregion

        #region ListItems2
        /// <summary>
        /// Lists all files and folders in specified path.
        /// </summary>
        /// <param name="path">Path to perform the listing of.</param>
        public void ListItems(string path = "")
        {
            // Get list of items as XML needed to be parsed.
            string list = GetItemList(String.IsNullOrEmpty(path) ? string.Format(@"\{0}\", username) : path);

            // Check if the response contains anything.
            if (!string.IsNullOrEmpty(list))
            {
                XmlDocument document = new XmlDocument();

                document.LoadXml(list);
                XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
                manager.AddNamespace("fa", "http://api.filesanywhere.com/");

                // Extract items list 
                XmlNodeList xnList = document.SelectNodes("//fa:Items", manager);

                if (xnList.Count > 0)
                {
                    // Append Items tags so there is only one root element.
                    document.LoadXml("<Items>" + xnList.Item(0).InnerXml + "</Items>");

                    xnList = document.SelectNodes("//fa:Item", manager);

                    // Loop through each item returned.
                    foreach (XmlNode xn in xnList)
                    {
                        if (xn["Type"].InnerText.Equals("file"))
                        {
                            // If the item type is a file, just print out the path and name.
                            Console.WriteLine(xn["Path"].InnerText + xn["Name"].InnerText);
                        }
                        else
                        {
                            // If the item type is a folder, print out the folder path,
                            // but also recursively list all sub-items of the folder.
                            Console.WriteLine(xn["Path"].InnerText + xn["Name"].InnerText + "\\");
                            ListItems(xn["Path"].InnerText + xn["Name"].InnerText);
                        }
                    }
                }
                else
                {
                    // If no items were received in response assume the volume contains no items.
                    Console.WriteLine("\r\nUser volume is empty.");
                }
            }
        }

        private string GetItemList(string folderPath)
        {
            string soap =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                 <soap:Body>
                 <ListItems2 xmlns=""http://api.filesanywhere.com/"">
                 <Token>{0}</Token>
                 <Path>{1}</Path>
                 <PageSize>0</PageSize>
                 <PageNum>0</PageNum>
                 </ListItems2>
                 </soap:Body>
                </soap:Envelope>                ";

            string response = SOAP.SendSOAPRequest("http://api.filesanywhere.com/ListItems2",
                String.Format(soap, sessionToken, folderPath));

            if (string.IsNullOrEmpty(response))
            {
                return "";
            }

            // Check if an error occurred, if not assume action completed successfuly.
            string error = GetResponseError(response);

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("\r\nError getting item list: " + error);
                return "";
            }

            return response;
        }
        #endregion

        #region CreateFolderRecursive
        /// <summary>
        /// Creates a folder in the user's volume.
        /// </summary>
        /// <param name="folderPath">Path to create the new folder in (will be created
        /// recursively if any part of it does not exist.</param>
        /// <param name="folderName">Name of the folder to create.</param>
        public void CreateFolder(string folderPath, string folderName)
        {
            // SOAP XML request 
            string soap =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                 <soap:Body>
                 <CreateFolderRecursive xmlns=""http://api.filesanywhere.com/"">
                 <Token>{0}</Token>
                 <Path>{1}</Path>
                 <NewFolderName>{2}</NewFolderName>
                 </CreateFolderRecursive>
                 </soap:Body>
                </soap:Envelope>";

            // Send the request and check if an error occurred.
            string response = SOAP.SendSOAPRequest("http://api.filesanywhere.com/CreateFolderRecursive",
                String.Format(soap, sessionToken, folderPath, folderName));

            if (string.IsNullOrEmpty(response))
            {
                return;
            }

            string error = GetResponseError(response);

            // If no error occurred, assume that the folder and all parent folders 
            // now exist.
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("\r\nError creating folder: " + error);
            }
            else
            {
                Console.WriteLine("\r\nFolder created successfully.");
            }
        }
        #endregion

        #region AppendChunk

        /// <summary>
        /// Uploads a file by appending chunks of bytes.
        /// </summary>
        /// <param name="destinationPath">Destination path for the file.</param>
        /// <param name="filePath">Local path of the file.</param>
        public void UploadFile(string destinationPath, string filePath)
        {
            if (string.IsNullOrEmpty(destinationPath) || string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("An invalid path was input.");
                return;
            }

            // SOAP XML request 
            string soap =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                 <soap:Body>
                 <AppendChunk xmlns=""http://api.filesanywhere.com/"">
                 <Token>{0}</Token>
                 <Path>{1}</Path>
                 <ChunkData>{2}</ChunkData>
                 <Offset>{3}</Offset>
                 <BytesRead>{4}</BytesRead>
                 <isLastChunk>{5}</isLastChunk>
                 </AppendChunk>
                 </soap:Body>
                </soap:Envelope>";
            try
            {
                const int CHUNK_SIZE = 1024000; // Size of chunk to send each request.
                byte[] buffer = new byte[CHUNK_SIZE]; // Buffer to read data into.
                int numBytesRead = 0, offset = 0; 
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Get file information of the local file.
                    FileInfo fileInfo = new FileInfo(filePath);
                    string fileName = fileInfo.Name;

                    // Read the next chunk of data into the buffer, and store the number
                    // of bytes read for later use.
                    while ((numBytesRead = fileStream.Read(buffer, 0, CHUNK_SIZE)) != 0)
                    {
                        // Check if this chunk is the final one of the file.
                        bool isLastChunk = offset + numBytesRead >= fileInfo.Length;

                        Console.WriteLine("Uploaded {0} of {1} bytes...", offset, fileInfo.Length);
                        try
                        {
                            // Send the AppendChunk request, convert the buffer into a base64 string.
                            string response = SOAP.SendSOAPRequest("http://api.filesanywhere.com/AppendChunk",
                                String.Format(soap, sessionToken, destinationPath + "\\" + fileName,
                                    Convert.ToBase64String(buffer),
                                    offset, numBytesRead, isLastChunk ? 1 : 0));

                            string error = GetResponseError(response);

                            // Check if response contains an error.
                            if (!string.IsNullOrEmpty(error))
                            {
                                Console.WriteLine("\r\nError uploading file: " + error);
                                return;
                            }

                            // If no error occurred, add the number of bytes read to the offset that
                            // will be used to designater where the bytes are being appended.
                            offset += numBytesRead;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("\r\nError uploading file: " + ex.ToString());
                        }
                    }
                    Console.WriteLine("Uploaded {0} of {1} bytes...", offset, fileInfo.Length);
                    Console.WriteLine("File successfuly uploaded.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An unknown error occurred uploading file: {0}", ex.Message);
            }
        }
        #endregion

        #region DeleteItems
        /// <summary>
        /// Deletes an item (file or folder) from the user's volume.
        /// </summary>
        /// <param name="itemPath">Full path of item to delete.</param>
        public void DeleteItem(string itemPath)
        {
            // SOAP XML request 
            string soap =
                        @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                    xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                     <soap:Body>
                     <DeleteItems xmlns=""http://api.filesanywhere.com/"">
                         <Token>{0}</Token>
                         <ItemsToDelete>
                             <Item>
                                 <Type>{1}</Type>
                                 <Path>{2}</Path>
                             </Item>
                         </ItemsToDelete>
                     </DeleteItems>
                     </soap:Body>
                    </soap:Envelope>";

            // Send SOAP request
            string response = SOAP.SendSOAPRequest("http://api.filesanywhere.com/DeleteItems",
                String.Format(soap, sessionToken, Path.HasExtension(itemPath) ? "file" : "folder", itemPath));

            if (string.IsNullOrEmpty(response))
            {
                return;
            }

            // Check if an error occurred, if not assume action completed successfuly.
            string error = GetResponseError(response);

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("\r\nError deleting item: " + error);
            }
            else
            {
                Console.WriteLine("\r\nItem deleted successfully.");
            }
        }
        #endregion

        /// <summary>
        /// Extracts response error, if one exists.
        /// </summary>
        /// <param name="soapResponse">XML Response received.</param>
        /// <returns>Error as string.</returns>
        private string GetResponseError(string soapResponse)
        {
            if (string.IsNullOrEmpty(soapResponse))
            {
                return "";
            }

            XDocument doc = XDocument.Parse(soapResponse);
            XNamespace xmlnsa = "http://api.filesanywhere.com/";
            string errorMessage = doc.Descendants(xmlnsa + "ErrorMessage").First().Value;

            return errorMessage;
        }

        /// <summary>
        /// Checks if the current session is valid by checking if
        /// a session token exists.
        /// </summary>
        /// <returns>Validity of session as boolean.</returns>
        public bool IsValid()
        {
            return !String.IsNullOrEmpty(sessionToken);
        }

        /// <summary>
        /// Checks session login credentials, prints missing input.
        /// </summary>
        /// <returns>If all login credentials are entered.</returns>
        private bool HasValidLoginDetails()
        {
            bool valid = true;
            Console.WriteLine();
            if (string.IsNullOrEmpty(organizationID))
            {
                Console.WriteLine("Error: Empty orginization ID.");
                valid = false;
            }
            int orgID;
            if (!int.TryParse(organizationID, out orgID))
            {
                Console.WriteLine("Error: Orginization ID must be numerical.");
                valid = false;
            }
            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("Error: Empty username.");
                valid = false;
            }
            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Error: Empty password.");
                valid = false;
            }
            return valid;
        }
    }
}
