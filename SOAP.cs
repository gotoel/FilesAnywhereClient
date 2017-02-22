using System;
using System.IO;
using System.Net;
using System.Xml.Linq;

namespace FileAnywhereClient
{
    class SOAP
    {
        /// <summary>
        /// Sends a SOAP request.
        /// </summary>
        /// <param name="action">API action to perform.</param>
        /// <param name="content">XML content data.</param>
        /// <returns></returns>
        public static string SendSOAPRequest(string action, string content)
        {
            try
            {
                // Builds a request, including required SOAP request header.
                HttpWebRequest req = (HttpWebRequest) WebRequest.Create(Globals.API_URL);
                req.Headers.Add("SOAPAction", action);
                req.ContentType = "text/xml;charset=\"utf-8\"";
                req.Accept = "text/xml";
                req.Method = "POST";

                // Write XML data to request stream.
                using (Stream stm = req.GetRequestStream())
                {
                    using (StreamWriter stmw = new StreamWriter(stm))
                    {
                        stmw.Write(content);
                    }
                }

                // Gets the response from the server.
                WebResponse response = req.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                return reader.ReadToEnd();
            }
            catch (WebException webex)
            {
                // This error typically occurs if the server doesn't like our 
                // request (most likely incorrect data types).
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();

                    // Reads out the faultstring that's returned by the server
                    // if one is available.
                    XDocument doc = XDocument.Parse(text);
                    XNamespace xmlnsa = "http://api.filesanywhere.com/";
                    var nodeList = doc.Descendants("faultstring");

                    foreach (XElement e in nodeList)
                    {
                        Console.WriteLine("\r\nRequest error: " + e.Value);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}
