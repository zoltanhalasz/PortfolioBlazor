using System;
using System.Collections.Generic;
using System.Text;

namespace PortfolioBlazor.Data
{
    public class Transaction
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }        

    }
}
