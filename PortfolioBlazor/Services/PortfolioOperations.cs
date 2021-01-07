using Microsoft.EntityFrameworkCore;
using PortfolioBlazor.Data;
using PortfolioBlazor.Models;
using PortfolioBlazor.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioBlazor.Services
{
    public interface IPortfolioOperations
    {
        Task<decimal> GetMax(int days, string symbol);

        Task<decimal> GetMin(int days, string symbol);

        Task<decimal> GetAverage(int days, string symbol);

        Task<decimal> GetPortfolioValue_Historical();

        Task<decimal> GetPortfolioValue_Market();

        Task AddQuoteToHistoryDB(QuoteData q);

        Task MarkToMarket(QuoteData q);

        Task DeleteHistoryItemById(int Id);
        Task<List<Transaction>> GetTransactions();

        Task<List<Position>> GetPositions();

        Task<List<History>> GetHistory(string Symbol);

        Task<List<string>> GetSymbols();

        Task<List<QuoteData>> GetQuotesForStocks(string [] StockList);
        Task<bool> QuoteNotExists(string symbol, string prev_close);

        Task<decimal> Buy(string symbol, decimal purchase_price, decimal ExposurePerStock, decimal PurchaseAmount);

        Task<decimal> Sell(string symbol, decimal selling_price);
    }

    public class PortfolioOperations: IPortfolioOperations
    {

        private readonly TradingdbContext context;
        private readonly IAlphaVantage alphaVantage;
        public PortfolioOperations(TradingdbContext _context, IAlphaVantage _alphaVantage)
        {
            context = _context;
            alphaVantage = _alphaVantage;
        }
        public async Task<decimal> GetMax(int days, string symbol)
        {            
            var myList = await context.History.Where(x => x.Timestamp >= DateTime.Now.AddDays(-days) && x.Symbol == symbol).ToListAsync();
            return myList.Max(x => x.Close);            
        }

        public async Task<decimal> GetMin(int days, string symbol)
        {      
               var myList = await context.History.Where(x => x.Timestamp >= DateTime.Now.AddDays(-days) && x.Symbol == symbol).ToListAsync();
               return myList.Min(x => x.Close);         
        }

        public async Task<decimal> GetAverage(int days, string symbol)
        {
            var myList = await context.History.Where(x => x.Timestamp >= DateTime.Now.AddDays(-days) && x.Symbol == symbol).ToListAsync();
            return Math.Round(myList.Average(x => x.Close), 4);            
        }

        public async Task<decimal> GetPortfolioValue_Historical()
        {            
            var myList = await context.Positions.ToListAsync();
            decimal portfoliovalue = 0;
            foreach (var pos in myList)
            {
                portfoliovalue += pos.Qty * pos.PurchasePrice;
            }
            return portfoliovalue;                        
        }

        public async Task<decimal> GetPortfolioValue_Market()
        {
            var myList = await context.Positions.ToListAsync();
            decimal portfoliovalue = 0;
            foreach (var pos in myList)
            {
                portfoliovalue += pos.Qty * pos.MarketPrice;
            }
            return portfoliovalue;
        }

        public async Task<bool> QuoteNotExists(string symbol, string prev_close)
        {         
            DateTime Prev_Close = DateTime.ParseExact(prev_close, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var quoteexists = await context.History.AnyAsync(x => x.Symbol == symbol && x.Timestamp == Prev_Close);
            if (quoteexists) return false;
            return true;
        }

        public async Task<List<QuoteData>> GetQuotesForStocks(string [] StockList)
        {
            var QuoteList = new List<QuoteData>();
            foreach (string stock in StockList)
            {
                var qd = await alphaVantage.GetQuote(stock);
                QuoteList.Add(qd);
            }

            return QuoteList;
        }

        public async Task<List<Transaction>> GetTransactions()
        {
            var myTrans = await context.Transactions.OrderBy(x => x.Timestamp).ThenBy(x => x.Symbol).ToListAsync();
            return myTrans;
        }



        public async Task<List<Position>> GetPositions()
        {
            var myPos = await context.Positions.OrderBy(x => x.Symbol).ToListAsync();
            return myPos;
        }

        public async Task<List<History>> GetHistory(string mySymbol)
        {
            var myHistory = await context.History.Where(x=> x.Symbol== mySymbol).OrderByDescending(x => x.Timestamp).ToListAsync();

            var max = await GetMax(30, mySymbol);
            var average = await GetAverage(30, mySymbol);

            foreach (var he in myHistory)
            {
                he.Average = average * (decimal) 0.95;
                he.RecentMax = max;
            }

            return myHistory;
        }
        public async Task<List<string>> GetSymbols()
        {
            var mySymbols = await context.History.Select(x=> x.Symbol).ToListAsync();
            var returnvalue = mySymbols.Distinct().ToList();
            return returnvalue;
        }

        public async Task AddQuoteToHistoryDB(QuoteData q)
        {            
            DateTime Prev_Close = DateTime.ParseExact(q.LatestTradingDay, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var newHistoryPoint = new History()
            {
                Close = q.Price,
                Symbol = q.Symbol,
                High = q.High,
                Timestamp = Prev_Close,
                Low = q.Low,
                Volume = q.Volume,
                Open = q.Open
            };
            Console.WriteLine($"{q.Symbol} quotation added in Database for date: {q.LatestTradingDay}");
            await context.History.AddAsync(newHistoryPoint);
            await context.SaveChangesAsync();            
        }

        public async Task MarkToMarket(QuoteData q)
        {
            if (!context.Positions.Any(x=> x.Symbol == q.Symbol))
            {
                return;
            }

            var myList = await context.Positions.Where(x => x.Symbol == q.Symbol).ToListAsync();
            foreach (var item in myList)
            {
                item.MarketPrice = q.Price;
            }
            context.Positions.UpdateRange(myList);
            await context.SaveChangesAsync();
        }

        public async Task DeleteHistoryItemById(int id)
        {
            if (!context.History.Any(x => x.Id == id))
            {
                return;
            }

            var myItem = await context.History.FirstAsync(x => x.Id == id);            
            context.History.Remove(myItem);
            await context.SaveChangesAsync();
        }



        public async Task<decimal> Buy(string symbol, decimal purchase_price, decimal ExposurePerStock, decimal PurchaseAmount)
        {
            
            var allPositionsPerSymbol = await context.Positions.Where(x => x.Symbol == symbol).ToListAsync();
            var totalQty = allPositionsPerSymbol.Sum(x => x.Qty);
            if (totalQty * purchase_price >= ExposurePerStock)
            {
                Console.WriteLine($"----No Decision To: Buy {symbol} as Porfolio Exposure exceeded already.");
                return 0;
            }

            var transaction = new Transaction()
            {
                Price = purchase_price,
                Qty = Math.Round(PurchaseAmount / purchase_price, 4),
                Symbol = symbol,
                Timestamp = DateTime.Now,
                Type = "Buy"
            };

            var position = new Position()
            {
                MarketPrice = purchase_price,
                PurchaseDate = DateTime.Now,
                Qty = transaction.Qty,
                PurchasePrice = purchase_price,
                Symbol = symbol
            };

            await context.Transactions.AddAsync(transaction);
            await context.SaveChangesAsync();
            await context.Positions.AddAsync(position);
            await context.SaveChangesAsync();          

            return PurchaseAmount;
        }

        public async Task<decimal> Sell(string symbol, decimal selling_price)
        {                    
            if (context.Positions.Any(x => x.Symbol == symbol))
            {
                var allPositionsPerSymbol = await context.Positions.Where(x => x.Symbol == symbol).ToListAsync();
                var totalQty = allPositionsPerSymbol.Sum(x => x.Qty);
                var transaction = new Transaction()
                {
                    Price = selling_price,
                    Qty = totalQty,
                    Symbol = symbol,
                    Timestamp = DateTime.Now,
                    Type = "Sell"
                };
                context.Transactions.Add(transaction);
                await context.SaveChangesAsync();
                context.Positions.RemoveRange(allPositionsPerSymbol);
                await context.SaveChangesAsync();
                return totalQty * selling_price;
            }
            else
            {
                Console.WriteLine($"----Could not sell {symbol} as not in Portfolio.");
                return 0;
            }           
        }
    }
}
