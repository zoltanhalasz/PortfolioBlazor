using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PortfolioBlazor.Services
{
    public interface ITradingStrategy
    {
        Task Execute();
    }

    public class TradingStrategy: ITradingStrategy
    {
        private readonly IAlphaVantage alphaVantage;        

        private readonly IPortfolioOperations portOper;

        private readonly decimal PurchaseAmount = 5000;

        private readonly decimal ExposurePerStock = 25000;

        private readonly decimal lossPercentage = 5;

        private readonly int HistoricIntervalDays = 30;

        private List<string> TargetSymbols = new List<string> { "AAPL", "MSFT", "AMZN", "AMD", "FB", "IBM", "TSLA", "PG", "CRM", "ADBE", "NVDA","AZN", "NFLX" };
        public TradingStrategy(IAlphaVantage _alphaVantage, IPortfolioOperations _portOper)
        {
            alphaVantage = _alphaVantage;
            portOper = _portOper;
        }

        public async Task Execute()
        {
            TargetSymbols = await portOper.GetSymbols();
            Console.WriteLine("EXECUTING STRATEGY-------------------------------");
            foreach (var symbol in TargetSymbols)
            {
                var myq = await alphaVantage.GetQuote(symbol);
                if (myq != null)
                {
                    Console.WriteLine($" {DateTime.Now.ToShortDateString()} {symbol} Price: {myq.Price}");
                    if (await portOper.QuoteNotExists(symbol, myq.LatestTradingDay))
                    {                        
                        await portOper.AddQuoteToHistoryDB(myq);
                    }
                    else
                    {
                        //Console.WriteLine("Quote exists. Please don't add to DB");
                    }

                    //mark to market-portfolio
                    await portOper.MarkToMarket(myq);

                    var maxPrice = await portOper.GetMax(HistoricIntervalDays, symbol);

                    if (myq.Price >= maxPrice)
                    {
                        var myTransaction = await portOper.Buy(symbol, myq.Price,ExposurePerStock,PurchaseAmount);
                       
                        if (myTransaction > 0)
                        {
                            Console.WriteLine($"--Decision: Buy {symbol} for {myq.Price} USD");
                        }

                    }
                    else
                    {
                        Console.WriteLine($"--Condition not met to buy: {symbol} for {myq.Price} USD as maxPrice is {maxPrice}");
                    }

                    var avgPrice = await portOper.GetAverage(HistoricIntervalDays, symbol);
                    if (myq.Price <= avgPrice * (1 - lossPercentage / 100))
                    {
                        var myTransaction = await portOper.Sell(symbol, myq.Price);                        
                        if (myTransaction > 0)
                        {
                            Console.WriteLine($"--Decision: Sell all positions for Stock: {symbol} price: {myq.Price} USD");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"--Condition not met to sell: {symbol} for {myq.Price} USD as limit Price is {avgPrice * (1 - lossPercentage / 100)}");
                    }
                }
                Thread.Sleep(12000);
            }

            decimal Portfoliovalue = await portOper.GetPortfolioValue_Historical();
            Console.WriteLine($"Portfolio Value = {Portfoliovalue}\n =================================");
        }
    }
}
