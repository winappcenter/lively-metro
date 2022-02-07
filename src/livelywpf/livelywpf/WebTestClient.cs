using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Web;
using System.Xml.Serialization;

namespace WebReuest
{
    public class PromotionInfo
    {
        public string Name;
        public string Logo;
        public string DownloadUrl;
        public PromotionInfo()
        {
            Name = "Coolle Office Suite Pro";
            Logo = "https://www.coolleget.com/images/CoolleLibreOffice/logo.png";
            DownloadUrl = "ms-windows-store://pdp/?productid=9MTWCD8H4F5P";
        }
    }
    public class WebTestClient : WebClient
    {
        public int ConnectionLimit { get; set; }

        public WebTestClient()
        {
            ConnectionLimit = 10;
        }

        public T GetConfigFromJson<T>(string url)
        {
            var data = "";
            try
            {
                data = DownloadString(url);
                var location = JsonConvert.DeserializeObject<T>(data);
                return location;

            }
            catch(Exception)
            {
                var location = JsonConvert.DeserializeObject<T>(data);
                return location;
            }
           


        }
        //protected override WebRequest GetWebRequest(Uri address)
        //{
        //    var request = base.GetWebRequest(AddTimeStamp(address)) as HttpWebRequest;

        //    request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko";
        //    request.Accept = "text/html, application/xhtml+xml, */*";
        //    request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
        //    request.ServicePoint.ConnectionLimit = ConnectionLimit;

        //    return request;
        //}

        //private static Uri AddTimeStamp(Uri address)
        //{
        //    var uriBuilder = new UriBuilder(address);
        //    var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        //    query["x"] = DateTime.Now.ToFileTime().ToString(CultureInfo.InvariantCulture);
        //    uriBuilder.Query = query.ToString();
        //    return uriBuilder.Uri;
        //}
    }
}
