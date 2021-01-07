using System;
using System.Collections.Generic;
using System.Text;

namespace PortfolioBlazor.Data
{
    public class Position
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal Qty { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal MarketPrice { get; set; }

    }
}
