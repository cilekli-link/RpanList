using System.Collections.Generic;

namespace RpanList.Classes
{
    public class RpanApi
    {
        public static ApiResponse grabResponse()
        {
            return new ApiResponse { status = "success", data = new List<RpanData>() };
            //HttpClientHandler handler = new HttpClientHandler()
            //{
            //    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            //};
            //var client = new HttpClient(handler);
            //client.BaseAddress = new Uri("https://strapi.reddit.com");
            //HttpResponseMessage response = new HttpResponseMessage();
            //try
            //{
            //    response = client.GetAsync("videos/seed/360").Result;
            //}
            //catch (Exception)
            //{
            //    return new ApiResponse { status = "Couldn't connect" };
            //}
            //try
            //{
            //    string responseString = response.Content.ReadAsStringAsync().Result;
            //    return JsonConvert.DeserializeObject<ApiResponse>(responseString);
            //}
            //catch (Exception)
            //{
            //    return new ApiResponse { status = "Couldn't parse JSON" };
            //}
        }
    }
}
