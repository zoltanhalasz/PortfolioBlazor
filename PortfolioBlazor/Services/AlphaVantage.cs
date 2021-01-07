using Newtonsoft.Json;
using PortfolioBlazor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PortfolioBlazor.Services
{
    public class AlphaVantage : IAlphaVantage
    {
        private string API_KEY = "78LCJM1YNTKSZY9K";

        public async Task<QuoteData> GetQuote(string Symbol)
        {
            var client = new HttpClient();
            string uriString = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={Symbol}&apikey={API_KEY}";
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uriString),
                Headers =
                {
                    //{ "x-rapidapi-key", API_KEY },
                    //{ "x-rapidapi-host", "alpha-vantage.p.rapidapi.com" },
                },
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    string body = await response.Content.ReadAsStringAsync();
                    var globalquote = JsonConvert.DeserializeObject<GlobalQuote>(body);
                    //Console.WriteLine(body);
                    return globalquote.QuoteData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task GetDailyData(string Symbol)
        {
            var client = new HttpClient();
            string uriString = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={Symbol}&apikey={API_KEY}";
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uriString),
                Headers =
                {
                    //{ "x-rapidapi-key", API_KEY },
                    //{ "x-rapidapi-host", "alpha-vantage.p.rapidapi.com" },
                },
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    string body = await response.Content.ReadAsStringAsync();
                    var globalquotedaily = JsonConvert.DeserializeObject<TimeSeriesDaily>(body);
                    //Console.WriteLine(globalquotedaily.ListDaily);                  
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);                
            }
        }
    }
}
