using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static RpanList.Logger;
namespace RpanList.Classes
{
    public class RpanApi
    {
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        static HttpClient client;
        static HttpResponseMessage response;
        public static async Task<ApiResponse> grabResponse()
        {
            if (client == null)
            {
                client = new HttpClient(handler);
                client.BaseAddress = new Uri("https://strapi.reddit.com");
                Log(LogSeverity.Debug, "Created new HttpClient");
            }

            try
            {
                Log(LogSeverity.Debug, "Fetching RPAN API response...");
                response = await client.GetAsync("videos/seed/360");
            }
            catch (Exception e)
            {
                Log(LogSeverity.Debug, "HttpClient failed: " + e.Message);
                return new ApiResponse { status = "Couldn't connect" };
            }

            Log(LogSeverity.Debug, "Received response from HttpClient");
            try
            {
                string responseString = await response.Content.ReadAsStringAsync();
                Log(LogSeverity.Debug, "Response: " + responseString);
                return JsonConvert.DeserializeObject<ApiResponse>(responseString);
            }
            catch (Exception)
            {
                return new ApiResponse { status = "Couldn't parse JSON" };
            }
        }
    }
}
