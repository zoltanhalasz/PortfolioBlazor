using PortfolioBlazor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioBlazor.Services
{
    public class TestService: ITestService
    {
        private readonly IAlphaVantage alphaVantage;

        private readonly IPortfolioOperations portOper;
        public TestService(IAlphaVantage _alphaVantage, IPortfolioOperations _portOper)
        {
            alphaVantage = _alphaVantage;
            portOper = _portOper;
        }
        public async Task DoSomething()
        {
            Console.WriteLine("Recurring job.");
            var quoateData = await alphaVantage.GetQuote("MSFT");
            Console.WriteLine(quoateData.Open + "for MSFT");
            var portfvalue = await portOper.GetPortfolioValue_Historical();
            Console.WriteLine("Portfolio value " + portfvalue);
        }
    }
}
