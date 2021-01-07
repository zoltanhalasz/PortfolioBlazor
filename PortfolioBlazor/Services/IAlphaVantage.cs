using PortfolioBlazor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioBlazor.Services
{
    public interface IAlphaVantage
    {
        Task<QuoteData> GetQuote(string symbol);

        Task GetDailyData(string symbol);
    }
}
