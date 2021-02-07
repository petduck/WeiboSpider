using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Core
{
    public class HttpService
    {
        private HttpClient httpClient;
        private IEnumerable<string> cookies;

        public HttpService(HttpClient _httpClient)
        {
            httpClient = _httpClient;

            cookies = new List<string>();

            if ("https".Equals(httpClient.BaseAddress.Scheme,StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback =
                                new RemoteCertificateValidationCallback(CheckValidationResult);
            }
        }

        /// <summary>
        /// Get请求
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="cookiestr"></param>
        /// <returns></returns>
        public async Task<string> GetAsync(string Url,string cookiestr="")
        {
            if (!httpClient.DefaultRequestHeaders.Contains("Method"))
            {
                httpClient.DefaultRequestHeaders.Add("Method", "GET");
            }
            if (!httpClient.DefaultRequestHeaders.Contains("cookie"))
            {
                if (cookies.Any())
                {
                    httpClient.DefaultRequestHeaders.Add("cookie", string.Join(';', cookies));
                }
                else
                {
                    httpClient.DefaultRequestHeaders.Add("cookie", cookiestr);
                }
            }
            if (!httpClient.DefaultRequestHeaders.Contains("user-agent"))
            {
                httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36");
            }            
            var response = await httpClient.GetAsync(Url);
            if (response.Headers.Contains("Set-Cookie"))
            {
                cookies = response.Headers.GetValues("Set-Cookie");
            }

            return await response.Content.ReadAsStringAsync();
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            //直接确认，否则打不开    
            return true;
        }
    }
}
