using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RpanList.Classes
{
    public class RpanApi
    {
        public static async Task<ApiResponse> grabResponse()
        {
            //return new ApiResponse { status = "success", data = new List<RpanData>() };
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://strapi.reddit.com");
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.GetAsync("videos/seed/360");
            }
            catch (Exception)
            {
                return new ApiResponse { status = "Couldn't connect" };
            }
            try
            {
                string responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(responseString);
            }
            catch (Exception)
            {
                return new ApiResponse { status = "Couldn't parse JSON" };
            }
        }
    }
}
