using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ReadTestResult
{
    public class HttpAdapter
    {
        public static async Task<Stream> GetAsync(string url)
        {
            try
            {
                string credentials = GlobalVariables.User + ":" + GlobalVariables.ApiToken;
                var authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorization);

                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsStreamAsync();
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error retrieving response from {url}: {e.Message}");
                throw;
            }
        }
    }
}
