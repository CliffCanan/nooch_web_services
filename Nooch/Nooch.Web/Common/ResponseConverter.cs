using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using Nooch.Common;

namespace Nooch.Web.Common
{
    public static class ResponseConverter<T>
    {
        public static T ConvertToCustomEntity(string serviceUrl)
        {
            //System.Net.ServicePointManager.CertificatePolicy = new CustomCertificatePolicy();

            ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);
            WebRequest request = WebRequest.Create(serviceUrl);
            //request.Timeout = 30000;    
            request.Method = "GET";
            request.Credentials = CredentialCache.DefaultCredentials;
            try
            {

                WebResponse response = request.GetResponse();
                var httpWebResponse = (HttpWebResponse)response;
                Logger.Info("Client: Receive Response HTTP:" + "Version[" + httpWebResponse.ProtocolVersion + "] ," + "Status code:[" + (int)httpWebResponse.StatusCode + "], Status description:[" + httpWebResponse.StatusDescription + "] ].");
                //todo: remove the below line before final deployment. Added for development purpose.
                Logger.Info("Client: Target " + "Uri[" + httpWebResponse.ResponseUri + "].");

                var streamReader = new StreamReader(response.GetResponseStream());
                var obj = streamReader.ReadToEnd();


                response.Close();
                streamReader.Close();
                streamReader.Dispose();

                var scriptSerializer = new JavaScriptSerializer();
                var result = scriptSerializer.Deserialize<T>(obj);

                return result;
            }
            finally
            {
                request.Abort();
            }

        }
        public static string CallServicePostMethod1(string serviceUrl, string json)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(serviceUrl);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
            }
            var response = httpWebRequest.GetResponse();
            return response.ToString();
        }
        public static T CallServicePostMethod(string serviceUrl, string json)
        {

            ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);
            var uri = new Uri(serviceUrl);

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.ContentType = "application/json";
            var requestWriter = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII);
            requestWriter.Write(json);
            requestWriter.Close();

            var responseReader = new StreamReader(request.GetResponse().GetResponseStream());
            var response = responseReader.ReadToEnd();

            responseReader.Close();

            


            var scriptSerializer = new JavaScriptSerializer();
            var result = scriptSerializer.Deserialize<T>(response);

            return result;
          

            //string result = string.Empty;
            //foreach (KeyValuePair<string, object> responseCollection in (Dictionary<string, object>)serviceResponse)
            //{
            //    foreach (KeyValuePair<string, object> responseResult in (Dictionary<string, object>)responseCollection.Value)
            //    {
            //        if (responseResult.Key.Equals("Result"))
            //        {
            //            result = responseResult.Value.ToString();
            //            return result;
            //        }
            //    }
            //}

            //return result;
        }
    }
}