using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.SubClients.Spot;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.SocketSubClients;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Net.NetworkInformation;
using System.Text;
using System.Runtime.InteropServices;

namespace UsainBot
{
    internal static class Program
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        private static IntPtr ThisConsole = GetConsoleWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int MAXIMIZE = 3;
        private const int RESTORE = 9;
        public static void Main(string[] args)
        {
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
            ShowWindow(ThisConsole, MAXIMIZE);
            Console.Title = "UsainBot: loading...";
            Config config = LoadOrCreateConfig();
            if (config == null)
            {
                Console.Read();
                return;
            }
/*            using (HttpClient clientapi = new HttpClient())
            {
                clientapi.BaseAddress = new Uri("https://usainbot.com/api/");
                Task<HttpResponseMessage> response = clientapi.GetAsync("?key=" + config.apiKey);
                HttpResponseMessage result = response.Result;
                response.Wait();
                if (result.IsSuccessStatusCode)
                {
                    Task<string> task = result.Content.ReadAsStringAsync();
                    string[] res = task.Result.Split(':');
                    config.name = res[0];
                    config.expiry = res[1];
                }
                else if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("No license for this API key.");
                    Thread.Sleep(3000);
                    return;
                }
                else if (result.StatusCode == HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("Your license has expired.");
                    Thread.Sleep(3000);
                    return;
                }
                else
                {
                    Console.WriteLine("Unknown error while querying API.");
                    Thread.Sleep(3000);
                    return;
                }
            } */
        //    Console.ForegroundColor = ConsoleColor.DarkGreen;
        //    Console.WriteLine("Hello " + config.name + ", your license will expire on " + config.expiry);
            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                BinanceClient.SetDefaultOptions(new BinanceClientOptions
                {
                    ApiCredentials = new ApiCredentials(config.apiKey, config.apiSecret),
                    LogVerbosity = LogVerbosity.None,
                    LogWriters = new List<TextWriter> { null },
                    Proxy = null,
                    ShouldCheckObjects = false,
                    BaseAddress = "https://api.binance.com"
                }); ;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR! Could not set Binance options. Error message: {ex.Message}");
                Console.Read();
                return;
            }
            var client = new BinanceClient();
            Utilities.Write(ConsoleColor.Cyan, $"Loading exchange info...");
            WebCallResult<BinanceExchangeInfo> exchangeInfo = client.Spot.System.GetExchangeInfo();
            var t = exchangeInfo.Data.ServerTime;
            if (!exchangeInfo.Success)
            {
                Utilities.Write(ConsoleColor.Red, $"ERROR! Could not exchange informations. Error code: " + exchangeInfo.Error?.Message);
                return;
            }

            Utilities.Write(ConsoleColor.Green, "Successfully logged in.");
            while (true)
            {
                int closet3 = 0;
                decimal buyorderprice = 0;
                decimal sellorderprice = 0;
                decimal tp = 0;
                string mode;
                string symbol;
                string symbolpair;
                string qtusd;
                string srisk;
                string buyorderpricestring;
                string sellorderpricestring;
                string tpstring;
                decimal quantity;
                Thread t3 = new Thread(PingThread);
                t3.Start();
                void PingThread()
                {
                    while (closet3 == 0)
                    {
                        float reply = client.Spot.System.Ping().Data;
                        Console.Title = "Ping to Binance: " + reply + "ms";
                        Thread.Sleep(500);
                    }
                }
                Thread t4 = new Thread(FakeThread);
                t4.Start();
                void FakeThread()
                {
                    while (closet3 == 0)
                    {
                        client.Spot.Order.PlaceOrder("ETHBTC", OrderSide.Buy, OrderType.Market, null, 0);
                     //   Utilities.Write(ConsoleColor.Green, $"test order sent");
                        Thread.Sleep(100000);
                    }
                }
                ShowWindow(ThisConsole, RESTORE);
                Console.SetWindowSize(80, 40);
                Utilities.Write(ConsoleColor.Yellow, "select mode");
                Utilities.Write(ConsoleColor.Yellow, "input 1: high volatility AI long");
                Utilities.Write(ConsoleColor.Yellow, "input 2: listing market maker script");
                Utilities.Write(ConsoleColor.Yellow, "input 3: volatility listener");
                Utilities.Write(ConsoleColor.Yellow, "input 4: high frequency orderbook listener");
                Console.ForegroundColor = ConsoleColor.White;
                mode = Console.ReadLine();
                if (string.IsNullOrEmpty(mode))
                    return;
                if (mode == "1")
                {
                    Utilities.Write(ConsoleColor.Yellow, "How much USD to allocate?");
                    Console.ForegroundColor = ConsoleColor.White;
                    qtusd = Console.ReadLine();
                    if (string.IsNullOrEmpty(qtusd))
                        return;
                    decimal usd = Convert.ToDecimal(qtusd);
                    if (usd < 8)
                    {
                        Utilities.Write(ConsoleColor.Red, $"Has to be higher than 8");
                        return;
                    }
                    Utilities.Write(ConsoleColor.Yellow, "Input pair:");
                    Console.ForegroundColor = ConsoleColor.White;
                    symbolpair = Console.ReadLine();
                    if (string.IsNullOrEmpty(symbolpair))
                        return;
                    string pairend = symbolpair;
                    pairend = pairend.ToUpper();
                    if (pairend == "USDT" || pairend == "BUSD" || pairend == "TUSD")
                        quantity = usd;
                    else
                    {
                        WebCallResult<BinanceBookPrice> priceinbtc = client.Spot.Market.GetBookPrice(pairend + "USDT");
                        quantity = Math.Round(usd / priceinbtc.Data.BestBidPrice, 8);
                        Utilities.Write(ConsoleColor.Green, $"converting  " + usd + $" USD to  " + quantity + $" " + pairend);
                    }
                    Utilities.Write(ConsoleColor.Green, "Entering high volatility AI long mode");
                    Utilities.Write(ConsoleColor.Yellow, "On a scale of 1 to 5, how much risk should i take?");
                    Console.ForegroundColor = ConsoleColor.White;
                    srisk = Console.ReadLine();
                    if (string.IsNullOrEmpty(srisk))
                        return;
                    decimal risktaking = Convert.ToDecimal(srisk);
                    decimal strategyrisk = Math.Round((decimal)Math.Pow((double)risktaking - 0.5, 1.5), 3);
                    decimal sellStrategy = Math.Round((decimal).95 - risktaking * (decimal).03, 3);
                    decimal maxsecondsbeforesell = risktaking * (decimal)6.0;
                    Utilities.Write(ConsoleColor.Green, $"Usain Bot risk taking set to  " + risktaking);
                    if (config.discord_token.Length > 0)
                    {
                        Utilities.Write(ConsoleColor.Yellow, "Input symbol or Discord channel ID:");
                        Console.ForegroundColor = ConsoleColor.White;
                        symbol = Console.ReadLine();

                        // if line is only digits, it's safe to assume it's a Discord channel ID.
                        if (symbol.All(c => c >= '0' && c <= '9'))
                        {
                            string channelId = symbol;
                            symbol = null;
                            Console.WriteLine("Looking for symbol...");
                            // Scrape channel every 100ms
                            while (null == symbol)
                            {
                                symbol = ScrapeChannel(config.discord_token, channelId);
                                Thread.Sleep(100);
                            }
                        }
                        symbol = symbol.ToUpper();

                        //Exit the program if nothing was entered
                        if (string.IsNullOrEmpty(symbol))
                            return;
                    }
                    else
                    {
                        //Wait for symbol input
                        Utilities.Write(ConsoleColor.Yellow, "Input symbol: ");
                        Console.ForegroundColor = ConsoleColor.White;
                        symbol = Console.ReadLine();
                        symbol = symbol.ToUpper();
                    }
                    //Exit the program if nothing was entered
                    if (string.IsNullOrEmpty(symbol))
                        return;
                    //Try to execute the order
                    closet3 = 1;
                    client.ShouldCheckObjects = false;
                    ExecuteOrderLong(symbol, quantity, strategyrisk, sellStrategy, maxsecondsbeforesell, client, pairend, exchangeInfo);
                }
                else if (mode == "2")
                {
                    Utilities.Write(ConsoleColor.Yellow, "How much USD to allocate?");
                    Console.ForegroundColor = ConsoleColor.White;
                    qtusd = Console.ReadLine();
                    if (string.IsNullOrEmpty(qtusd))
                        return;
                    decimal usd = Convert.ToDecimal(qtusd);
                    if (usd < 8)
                    {
                        Utilities.Write(ConsoleColor.Red, $"Has to be higher than 8");
                        return;
                    }
                    Utilities.Write(ConsoleColor.Yellow, "Input pair:");
                    Console.ForegroundColor = ConsoleColor.White;
                    symbolpair = Console.ReadLine();
                    if (string.IsNullOrEmpty(symbolpair))
                        return;
                    string pairend = symbolpair;
                    pairend = pairend.ToUpper();
                    if (pairend == "USDT" || pairend == "BUSD" || pairend == "TUSD")
                        quantity = usd;
                    else
                    {
                        WebCallResult<BinanceBookPrice> priceinbtc = client.Spot.Market.GetBookPrice(pairend + "USDT");
                        quantity = Math.Round(usd / priceinbtc.Data.BestBidPrice, 8);
                        Utilities.Write(ConsoleColor.Green, $"converting  " + usd + $" USD to  " + quantity + $" " + pairend);
                    }
                    Utilities.Write(ConsoleColor.Yellow, "Input limit long order price in BTC:");
                    Console.ForegroundColor = ConsoleColor.White;
                    buyorderpricestring = Console.ReadLine();
                    if (string.IsNullOrEmpty(buyorderpricestring))
                        return;
                    buyorderprice = Convert.ToDecimal(buyorderpricestring);
                    Utilities.Write(ConsoleColor.Yellow, "Input limit sell order price in BTC:");
                    Console.ForegroundColor = ConsoleColor.White;
                    sellorderpricestring = Console.ReadLine();
                    if (string.IsNullOrEmpty(sellorderpricestring))
                        return;
                    sellorderprice = Convert.ToDecimal(sellorderpricestring);
                    Console.ForegroundColor = ConsoleColor.White;
                    Utilities.Write(ConsoleColor.Yellow, "Input symbol: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    symbol = Console.ReadLine();
                    symbol = symbol.ToUpper();
                    if (string.IsNullOrEmpty(symbol))
                        return;
                    closet3 = 1;
                    ExecuteOrderListing(symbol, quantity, client, pairend, exchangeInfo, buyorderprice, sellorderprice);
                }
                else if (mode == "4")
                {
                    Utilities.Write(ConsoleColor.Yellow, "Input pair:");
                    Console.ForegroundColor = ConsoleColor.White;
                    symbolpair = Console.ReadLine();
                    if (string.IsNullOrEmpty(symbolpair))
                        return;
                    string pairend = symbolpair;
                    pairend = pairend.ToUpper();
                    Utilities.Write(ConsoleColor.Yellow, "Input the timespan in which you want to listen:");
                    Console.ForegroundColor = ConsoleColor.White;
                    tpstring = Console.ReadLine();
                    if (string.IsNullOrEmpty(tpstring))
                        return;
                    tp = Convert.ToDecimal(tpstring);
                    Utilities.Write(ConsoleColor.Yellow, "Input symbol: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    symbol = Console.ReadLine();
                    symbol = symbol.ToUpper();
                    if (string.IsNullOrEmpty(symbol))
                        return;
                    closet3 = 1;
                    Listener(symbol, client, pairend, exchangeInfo, tp);
                }
                client = new BinanceClient();
                exchangeInfo = client.Spot.System.GetExchangeInfo();
                if (!exchangeInfo.Success)
                {
                    Utilities.Write(ConsoleColor.Red, $"ERROR! Could not exchange informations. Error code: " + exchangeInfo.Error?.Message);
                    return;
                }
            }
        }

        private static Config LoadOrCreateConfig()
        {
            if (!File.Exists("config.json"))
            {
                string json = JsonConvert.SerializeObject(new Config
                {
                    apiKey = "",
                    apiSecret = "",
                    discord_token = ""
                });
                File.WriteAllText("config.json", json);
                Utilities.Write(ConsoleColor.Red, "config.json was missing and has been created. Please edit the file and restart the application.");
                return null;
            }
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
        }

        private static void ExecuteOrderLong(string symbol, decimal quantity, decimal strategyrisk, decimal sellStrategy, decimal maxsecondsbeforesell, BinanceClient client,string pairend, WebCallResult<BinanceExchangeInfo> exchangeInfo)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            using (client)
            {
                string pair = symbol + pairend;
                WebCallResult<BinancePlacedOrder> order = client.Spot.Order.PlaceOrder(pair, OrderSide.Buy, OrderType.Market, null, quantity);
                stopWatch.Stop();
                var timestamp = DateTime.Now.ToFileTime();
                TimeSpan ts = stopWatch.Elapsed;
                decimal OrderQuantity = order.Data.QuantityFilled;
                decimal paidPrice = 0;
                if (order.Data.Fills != null)
                {
                    paidPrice = order.Data.Fills.Average(trade => trade.Price);
                }

                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds);
                Utilities.Write(ConsoleColor.Green, $"Order submitted and accepted, Got: {OrderQuantity} coins from {pair} at {paidPrice}, time: + {elapsedTime} ms");
                BinanceSymbol symbolInfo = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.QuoteAsset == pairend && s.BaseAsset == symbol);
                if (symbolInfo == null)
                {
                    Utilities.Write(ConsoleColor.Red, $"ERROR! Could not get symbol informations.");
                    return;
                }
                int symbolPrecision = 1;
                decimal ticksize = symbolInfo.PriceFilter.TickSize;
                while ((ticksize = ticksize * 10) < 1)
                    ++symbolPrecision;
                decimal sellPriceRiskRatio = (decimal).95;
                decimal sellPriceAskRatio = (decimal).998;
                decimal StartSellStrategy = sellStrategy;
                decimal MaxSellStrategy = 1 - ((1 - sellStrategy) / 5);
                decimal volasellmax = (decimal).01;
                decimal currentstoploss = 0;
                List<decimal> tab = new List<decimal>();
                List<decimal> tabAsk = new List<decimal>();
                int count = -1;
                int stoplossex = 0;
                int first = 1;
                int usainsell = 0;
                int imincharge = 0;
                int protect = 0;
                Thread t = new Thread(NewThread);
                t.Start();
                WebCallResult<BinanceBookPrice> priceResult3 = client.Spot.Market.GetBookPrice(pair);
                Thread t2 = new Thread(NewThread2);
                t2.Start();
                WebCallResult<BinanceBookPrice> priceResult2 = client.Spot.Market.GetBookPrice(pair);
                void NewThread()
                {
                    while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime() && stoplossex == 0 && imincharge == 0)
                    {
                        count++;
                        priceResult2 = client.Spot.Market.GetBookPrice(pair);
                        if (priceResult2.Success)
                        {
                            tab.Add(priceResult2.Data.BestBidPrice);
                            tabAsk.Add(priceResult2.Data.BestAskPrice);
                        }
                        else
                            return;
                        if (count % 10 == 0 && count > 25)
                        {
                            if (count == 30)
                                protect = 1;
                            int countca = count;
                            decimal espa = 0;
                            int x2a = -1;
                            decimal tabAskavg = 0;
                            while (--countca > 0 && ++x2a < 10)
                            {
                                espa += (tab[countca] - tab[countca - 1]) / 10;
                                tabAskavg += tabAsk[countca];
                            }
                            --x2a;
                            ++countca;
                            decimal esp2a = espa / 3;
                            while (--countca > 0 && ++x2a < 30)
                            {
                                esp2a += (tab[countca] - tab[countca - 1]) / 30;
                                tabAskavg += tabAsk[countca];
                            }
                            tabAskavg += tabAsk[countca];
                            if (protect == 1)
                            {
                                protect = 0;
                                tabAskavg /= 30;
                                if (tabAskavg < paidPrice && usainsell == 0 && imincharge == 0)
                                {
                                    imincharge = 1;
                                    WebCallResult<IEnumerable<BinanceCancelledId>> orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                    WebCallResult<BinancePlacedOrder> ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    while (!ordersell.Success)
                                    {
                                        Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Limit order sell, trying another time. Error code: " + ordersell.Error?.Message);
                                        orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                        ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    }
                                    Thread.Sleep(1000);
                                    int y = 0;
                                    while (y == 0)
                                    {
                                        paidPrice = ordersell.Data.Fills.Average(trade => trade.Price);
                                        if (ordersell.Data.Fills != null)
                                        {
                                            if (ordersell.Data.QuantityFilled < OrderQuantity)
                                            {
                                                try
                                                {
                                                    orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                                    WebCallResult<BinancePlacedOrder> ordersell4 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel); // for if the previous limit order is filled but not 100%
                                                }
                                                finally
                                                {
                                                    Utilities.Write(ConsoleColor.Green, "100% sold");
                                                }
                                            }
                                            y = 1;
                                        }
                                        else
                                        {
                                            priceResult2 = client.Spot.Market.GetBookPrice(pair);
                                            orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                            ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                        }
                                    }
                                    usainsell = 1;
                                    Utilities.Write(ConsoleColor.Green, "UsainBot DUMP PROTECTION SOLD successfully  " + OrderQuantity + " " + ordersell.Data.Symbol + $" at " + paidPrice);
                                    return;
                                }
                            }
                            Utilities.Write(ConsoleColor.Green, $" {Math.Round(espa / priceResult2.Data.BestBidPrice * 100000, 2)}");
                            Utilities.Write(ConsoleColor.Red, $" {Math.Round(esp2a / priceResult2.Data.BestBidPrice * 100000, 2)}");
                            decimal volasell = (esp2a - espa) / priceResult2.Data.BestBidPrice * 64 * (decimal)Math.Pow(count - 10, .7);
                            if (volasell > strategyrisk / 4)
                            {
                                Utilities.Write(ConsoleColor.Red, $" negative volatility detected at a {Math.Round(volasell, 2)} ratio");
                                if (volasell < strategyrisk)
                                {
                                    if (volasell > volasellmax)
                                    {
                                        volasellmax = volasell;
                                        sellStrategy = Math.Round(StartSellStrategy + (decimal)Math.Pow((double)volasell / (double)strategyrisk, 1.5) * (MaxSellStrategy - StartSellStrategy), 3);
                                    }
                                }
                                else if (usainsell == 0 && imincharge == 0)
                                {
                                    imincharge = 1;
                                    WebCallResult<IEnumerable<BinanceCancelledId>> orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                    WebCallResult<BinancePlacedOrder> ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    while (!ordersell.Success)
                                    {
                                        Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Limit order sell, trying another time. Error code: " + ordersell.Error?.Message);
                                        orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                        ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    }
                                    Thread.Sleep(1000);
                                    int y = 0;
                                    while (y == 0)
                                    {
                                        paidPrice = ordersell.Data.Fills.Average(trade => trade.Price);
                                        if (ordersell.Data.Fills != null)
                                        {
                                            if (ordersell.Data.QuantityFilled < OrderQuantity)
                                            {
                                                try
                                                {
                                                    orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                                    WebCallResult<BinancePlacedOrder> ordersell4 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel); // for if the previous limit order is filled but not 100%
                                                }
                                                finally
                                                {
                                                    Utilities.Write(ConsoleColor.Green, "100% sold");
                                                }
                                            }
                                            y = 1;
                                        }
                                        else
                                        {
                                            priceResult2 = client.Spot.Market.GetBookPrice(pair);
                                            orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                            ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                        }
                                    }
                                    usainsell = 1;
                                    Utilities.Write(ConsoleColor.Green, "UsainBot AI SOLD successfully  " + OrderQuantity + " " + ordersell.Data.Symbol + $" at " + paidPrice);
                                    return;
                                }
                                else
                                    return;
                            }
                        }
                        Console.Title = $"Price for {pair} is {priceResult2.Data.BestBidPrice} to {priceResult2.Data.BestAskPrice} in iteration  " + count + "  negative volatility ratio is " + Math.Round(volasellmax, 2) + " stop limit is placed at " + currentstoploss;
                    }
                }
                void NewThread2()
                {
                    while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime() && stoplossex == 0 && imincharge == 0)
                    {
                        count++;
                        priceResult3 = client.Spot.Market.GetBookPrice(pair);
                        if (priceResult3.Success)
                        {
                            tab.Add(priceResult3.Data.BestBidPrice);
                            tabAsk.Add(priceResult3.Data.BestAskPrice);
                        }
                        else
                            return;
                        Console.Title = $"Price for {pair} is {priceResult3.Data.BestBidPrice} to {priceResult3.Data.BestAskPrice} in iteration  " + count + "  negative volatility ratio is " + Math.Round(volasellmax, 2) + " stop limit is placed at " + currentstoploss;
                        if ((count + 5) % 10 == 0 && count > 25)
                        {
                            if (count == 30)
                                protect = 1;
                            int countc = count;
                            decimal esp = 0;
                            int x2 = -1;
                            decimal tabAskavg2 = 0;
                            while (--countc > 0 && ++x2 < 10)
                            {
                                esp += (tab[countc] - tab[countc - 1]) / 10;
                                tabAskavg2 += tabAsk[countc];
                            }
                            --x2;
                            ++countc;
                            decimal esp2 = esp / 3;
                            while (--countc > 0 && ++x2 < 30)
                            {
                                esp2 += (tab[countc] - tab[countc - 1]) / 30;
                                tabAskavg2 += tabAsk[countc];
                            }
                            tabAskavg2 += tabAsk[countc];
                            if (protect == 1)
                            {
                                protect = 0;
                                tabAskavg2 /= 30;
                                Console.WriteLine(tabAskavg2);
                                Console.WriteLine(paidPrice);
                                if (tabAskavg2 < paidPrice && usainsell == 0 && imincharge == 0)
                                {
                                    imincharge = 1;
                                    WebCallResult<IEnumerable<BinanceCancelledId>> orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                    WebCallResult<BinancePlacedOrder> ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    while (!ordersell.Success)
                                    {
                                        Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Limit order sell, trying another time. Error code: " + ordersell.Error?.Message);
                                        orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                        ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    }
                                    Thread.Sleep(1000);
                                    int y = 0;
                                    while (y == 0)
                                    {
                                        paidPrice = ordersell.Data.Fills.Average(trade => trade.Price);
                                        if (ordersell.Data.Fills != null)
                                        {
                                            if (ordersell.Data.QuantityFilled < OrderQuantity)
                                            {
                                                try
                                                {
                                                    orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                                    WebCallResult<BinancePlacedOrder> ordersell4 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel); // for if the previous limit order is filled but not 100%
                                                }
                                                finally
                                                {
                                                    Utilities.Write(ConsoleColor.Green, "100% sold");
                                                }
                                            }
                                            y = 1;
                                        }
                                        else
                                        {
                                            priceResult3 = client.Spot.Market.GetBookPrice(pair);
                                            orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                            ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                        }
                                    }
                                    usainsell = 1;
                                    Utilities.Write(ConsoleColor.Green, "UsainBot DUMP PROTECTION SOLD successfully  " + OrderQuantity + " " + ordersell.Data.Symbol + $" at " + paidPrice);
                                    return;
                                }
                            }
                            Utilities.Write(ConsoleColor.Green, $" {Math.Round(esp / priceResult3.Data.BestBidPrice * 100000, 2)}");
                            Utilities.Write(ConsoleColor.Red, $" {Math.Round(esp2 / priceResult3.Data.BestBidPrice * 100000, 2)}");
                            decimal volasell = (esp2 - esp) / priceResult3.Data.BestBidPrice * 64 * (decimal)Math.Pow(count - 10, .7);
                            if (volasell > strategyrisk / 4)
                            {
                                Utilities.Write(ConsoleColor.Red, $" negative volatility detected at a {Math.Round(volasell, 2)} ratio");
                                if (volasell < strategyrisk)
                                {
                                    if (volasell > volasellmax)
                                    {
                                        volasellmax = volasell;
                                        sellStrategy = Math.Round(StartSellStrategy + (decimal)Math.Pow((double)volasell / (double)strategyrisk, 1.5) * (MaxSellStrategy - StartSellStrategy), 3);
                                    }
                                }
                                else if (usainsell == 0 && imincharge == 0)
                                {
                                    imincharge = 1;
                                    WebCallResult<IEnumerable<BinanceCancelledId>> orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                    WebCallResult<BinancePlacedOrder> ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    while (!ordersell2.Success)
                                    {
                                        Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Limit order sell, trying another time. Error code: " + ordersell2.Error?.Message);
                                        orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                        ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    }
                                    Thread.Sleep(1000);
                                    int y = 0;
                                    while (y == 0)
                                    {
                                        paidPrice = ordersell2.Data.Fills.Average(trade => trade.Price);
                                        if (ordersell2.Data.Fills != null)
                                        {
                                            if (ordersell2.Data.QuantityFilled < OrderQuantity)
                                            {
                                                try
                                                {
                                                    orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                                    WebCallResult<BinancePlacedOrder> ordersell4 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel); // for if the previous limit order is filled but not 100%
                                                }
                                                finally
                                                {
                                                    Utilities.Write(ConsoleColor.Green, "100% sold");
                                                }
                                            }
                                            y = 1;
                                        }
                                        else
                                        {
                                            priceResult3 = client.Spot.Market.GetBookPrice(pair);
                                            orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                            ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                        }
                                    }
                                    usainsell = 1;
                                    Utilities.Write(ConsoleColor.Green, "UsainBot AI SOLD successfully  " + OrderQuantity + " " + ordersell2.Data.Symbol + $" at " + paidPrice);
                                    return;
                                }
                                else
                                    return;
                            }
                        }

                    }
                }
                while (sellStrategy <= MaxSellStrategy && sellStrategy >= StartSellStrategy && usainsell == 0 && imincharge == 0)
                {
                    if ((timestamp + (decimal)1.0 * 10000000 * maxsecondsbeforesell) > DateTime.Now.ToFileTime())
                    {
                        priceResult2 = client.Spot.Market.GetBookPrice(pair);
                        decimal stopPrice = Math.Round(priceResult2.Data.BestBidPrice * sellStrategy, symbolPrecision);
                        if (stopPrice > currentstoploss)
                        {
                            currentstoploss = stopPrice;
                            decimal sellPrice = Math.Round(stopPrice * sellPriceRiskRatio, symbolPrecision);
                            WebCallResult<IEnumerable<BinanceCancelledId>> closeorders = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                            if (!closeorders.Success)
                            {
                                if (first == 0)
                                {
                                    stoplossex = 1;
                                    Utilities.Write(ConsoleColor.Red, "StopLoss executed");
                                    return;
                                }
                            }
                            else
                                Utilities.Write(ConsoleColor.Blue, $"Orders successfully removed.");
                            first = 0;
                            if (usainsell == 0)
                            {
                                WebCallResult<BinancePlacedOrder> panicsell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.StopLossLimit, OrderQuantity, price: sellPrice, timeInForce: TimeInForce.GoodTillCancel, stopPrice: stopPrice);
                                if (!panicsell.Success)
                                {
                                    Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the StopLimit order. Error code: " + panicsell.Error?.Message);
                                    return;
                                }
                                else
                                    Utilities.Write(ConsoleColor.Blue, $"StopLoss Order submitted, stop loss price: {stopPrice}, sell price: {sellPrice}");
                            }
                        }
                    }
                    else if (stoplossex == 0)
                    {
                        imincharge = 1;
                        priceResult2 = client.Spot.Market.GetBookPrice(pair);
                        WebCallResult<IEnumerable<BinanceCancelledId>> orderspanic2 = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                        WebCallResult<BinancePlacedOrder> ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                        while (!ordersell2.Success)
                        {
                            Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Limit order sell, trying another time. Error code: " + ordersell2.Error?.Message);
                            ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                        }
                        Thread.Sleep(1000);
                        int y = 0;
                        while (y == 0)
                        {
                            paidPrice = ordersell2.Data.Fills.Average(trade => trade.Price);
                            if (ordersell2.Data.Fills != null)
                            {
                                if(ordersell2.Data.QuantityFilled < OrderQuantity) 
                                {
                                    try
                                    {
                                        orderspanic2 = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                        WebCallResult<BinancePlacedOrder> ordersell4 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel); // for if the previous limit order is filled but not 100%
                                    }
                                    finally
                                    {
                                        Utilities.Write(ConsoleColor.Green, "100% sold");
                                    }
                                }
                                y = 1;
                            }
                            else
                            {
                                priceResult2 = client.Spot.Market.GetBookPrice(pair);
                                orderspanic2 = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                            }
                        }
                        Utilities.Write(ConsoleColor.Green, "UsainBot TIME SOLD successfully  " + OrderQuantity + " " + ordersell2.Data.Symbol + $" at " + paidPrice);
                        return;
                    }
                }
                Thread.Sleep(2000);
            }
        }

        private static void ExecuteOrderListing(string symbol, decimal quantity, BinanceClient client, string pairend, WebCallResult<BinanceExchangeInfo> exchangeInfo, decimal buyorderprice, decimal sellorderprice)
        {
            using (client)
            {
                string pair = symbol + pairend;
                WebCallResult < BinanceBookPrice > priceResult69 = client.Spot.Market.GetBookPrice(pair);
                BinanceSymbol symbolInfo2 = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.QuoteAsset == pairend && s.BaseAsset == symbol);
                if (symbolInfo2 == null)
                {
                    Utilities.Write(ConsoleColor.Red, $"ERROR! Could not get symbol informations.");
                    return;
                }
                int symbolStep = 1;
                decimal stepsize = symbolInfo2.LotSizeFilter.StepSize;
                while ((stepsize = stepsize * 10) < 1)
                    ++symbolStep;
                quantity = Math.Round(quantity / priceResult69.Data.BestAskPrice, symbolStep);
                WebCallResult<BinancePlacedOrder> order = client.Spot.Order.PlaceOrder(pair, OrderSide.Buy, OrderType.Limit, quantity, price: buyorderprice, timeInForce: TimeInForce.GoodTillCancel); 
                if (!order.Success)
                {
                    Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Market order. Error code: " + order.Error?.Message);
                    return;
                }
                else
                    Utilities.Write(ConsoleColor.Green, $"Order placed: {quantity} coins from {pair} at {buyorderprice}");
                Thread.Sleep(5000);
                WebCallResult<BinancePlacedOrder> ordersellXD = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, quantity, price: sellorderprice, timeInForce: TimeInForce.GoodTillCancel);
                return;
            }
        }

        private static void Listener(string symbol, BinanceClient client, string pairend, WebCallResult<BinanceExchangeInfo> exchangeInfo, decimal maxsecondsbeforesell)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            using (client)
            {
                string pair = symbol + pairend;
                stopWatch.Stop();
                var timestamp = DateTime.Now.ToFileTime();
                BinanceSymbol symbolInfo = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.QuoteAsset == pairend && s.BaseAsset == symbol);
                if (symbolInfo == null)
                {
                    Utilities.Write(ConsoleColor.Red, $"ERROR! Could not get symbol informations.");
                    return;
                }
                int symbolPrecision = 1;
                decimal ticksize = symbolInfo.PriceFilter.TickSize;
                while ((ticksize = ticksize * 10) < 1)
                    ++symbolPrecision;
                List<string> tabBid = new List<string>();
                List<string> tabAsk = new List<string>();
                List<string> tabTime = new List<string>();
                int count = -1;
                Thread t = new Thread(NewThread);
                t.Start();
                WebCallResult<BinanceBookPrice> priceResult3 = client.Spot.Market.GetBookPrice(pair);
                Thread t2 = new Thread(NewThread2);
                t2.Start();
                WebCallResult<BinanceBookPrice> priceResult2 = client.Spot.Market.GetBookPrice(pair);
                Thread t3 = new Thread(NewThread3);
                t3.Start();
                WebCallResult<BinanceBookPrice> priceResult5 = client.Spot.Market.GetBookPrice(pair);
                Thread t4 = new Thread(NewThread4);
                t4.Start();
                WebCallResult<BinanceBookPrice> priceResult6 = client.Spot.Market.GetBookPrice(pair);
                void NewThread()
                {
                    while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime())
                    {
                        count++;
                        priceResult2 = client.Spot.Market.GetBookPrice(pair);
                        if (priceResult2.Success)
                        {
                            tabBid.Add(priceResult2.Data.BestBidPrice.ToString());
                            tabAsk.Add(priceResult2.Data.BestAskPrice.ToString());
                            tabTime.Add(DateTime.Now.ToFileTime().ToString());
                        }
                        else
                            return;
                    }
                }
                void NewThread2()
                {
                    while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime())
                    {
                        count++;
                        priceResult3 = client.Spot.Market.GetBookPrice(pair);
                        if (priceResult3.Success)
                        {
                            tabBid.Add(priceResult3.Data.BestBidPrice.ToString());
                            tabAsk.Add(priceResult3.Data.BestAskPrice.ToString());
                            tabTime.Add(DateTime.Now.ToFileTime().ToString());
                        }
                        else
                            return;
                    }
                }
                void NewThread3()
                {
                    while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime())
                    {
                        count++;
                        priceResult5 = client.Spot.Market.GetBookPrice(pair);
                        if (priceResult5.Success)
                        {
                            tabBid.Add(priceResult5.Data.BestBidPrice.ToString());
                            tabAsk.Add(priceResult5.Data.BestAskPrice.ToString());
                            tabTime.Add(DateTime.Now.ToFileTime().ToString());
                        }
                        else
                            return;
                    }
                }
                void NewThread4()
                {
                    while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime())
                    {
                        count++;
                        priceResult6 = client.Spot.Market.GetBookPrice(pair);
                        if (priceResult6.Success)
                        {
                            tabBid.Add(priceResult6.Data.BestBidPrice.ToString());
                            tabAsk.Add(priceResult6.Data.BestAskPrice.ToString());
                            tabTime.Add(DateTime.Now.ToFileTime().ToString());
                        }
                        else
                            return;
                    }
                }
                while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime())
                    Thread.Sleep(1000);
                File.WriteAllLines("dataask.txt", tabAsk);
                using (StreamReader sr = new StreamReader("dataask.txt"))
                {
                    string res = sr.ReadToEnd();
                    Console.WriteLine(res);
                }
                File.WriteAllLines("databid.txt", tabBid);
                using (StreamReader sr = new StreamReader("databid.txt"))
                {
                    string res = sr.ReadToEnd();
                    Console.WriteLine(res);
                }
                File.WriteAllLines("datatime.txt", tabTime);
                using (StreamReader sr = new StreamReader("datatime.txt"))
                {
                    string res = sr.ReadToEnd();
                    Console.WriteLine(res);
                }
            }
        }
        private static string ScrapeChannel(string discordToken, string channelId)
        {
            // Look for something that starts with a '$' followed by 2 to 5 alphabetic characters.
            Regex regex = new Regex(@"(\$)[a-zA-Z]{2,5}");
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v8/channels/" + channelId + "/messages?limit=1");

                req.Headers.Add("Authorization", discordToken);
                req.Accept = "*/*";
                req.ContentType = "application/json";

                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                if (res.StatusCode == HttpStatusCode.OK)
                {

                }
                Stream dataStream = res.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string resJson = reader.ReadToEnd();
                Message[] msg = System.Text.Json.JsonSerializer.Deserialize<Message[]>(resJson);
                Match match = regex.Match(msg[0].content);
                res.Close();
                reader.Close();
                dataStream.Close();
                if (match.Success)
                {
                    // Remove '$' character
                    return match.Value.Remove(0, 1);
                }
                else
                {
                    return null;
                }
            }
            catch (WebException ex)
            {
                Utilities.Write(ConsoleColor.Red, "ERROR: Could not get Discord message. Error code: " + ex.Status);
                return null;
            }
        }
    }
}