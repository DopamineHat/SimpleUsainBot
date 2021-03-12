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
            using (HttpClient clientapi = new HttpClient())
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
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Hello " + config.name + ", your license will expire on " + config.expiry);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"                                                                                                                                                                                          .,.");
            Console.WriteLine($"                                                                                                                                                                                    .* *****.");
            Console.WriteLine($"                                                                                                                                                                                *, .* ****/*");
            Console.WriteLine($"                                                                                                                                                                             , * ,///***/,");
            Console.WriteLine($"                                                                                                                                                                        .*/* *//////*/  ");
            Console.WriteLine($"                                                                                                                                                                    , */,, */////////*. *((,   ");
            Console.WriteLine($"                                                                                                                                                                .* **////////**//*/. **/  .,    ");
            Console.WriteLine($"                                                                                                                                                           ,./       ,/////**/**,.*/,,//(#/((*/.  ");
            Console.WriteLine($"                                                                                                                                                          ..*           ////////.*//////***/(%%%%//  /");
            Console.WriteLine($"                                                                                                                                                 .*.              /////,///////*///////(####*///, /");
            Console.WriteLine($"                                                                                                                                            .*              ,/. ./,////*//**//*//**////(###(*///  * ");
            Console.WriteLine($"                ,,,,.*, *   .                                                                                                          .*            .//*  ,/,///////////*/**/****////////***///,/. ");
            Console.WriteLine($"             ,//////////,,.,,,/////*,,                                                                                            .,           ,//.  ./*,///////////////////******/(%//////////     ");
            Console.WriteLine($"           , *////*,,. .           */**/**.                                                                                   ..          ./,   .//./////////////*********/*****,,,****,,,,,*/*  ");
            Console.WriteLine($"          ,///****,.,**,...    .,      ,*/*                                                                             .        .*,      ,/,,////////////////////////*//////**///%%%%****,,**   ");
            Console.WriteLine($"         ,//***************,,.       ..* ,//,,                                                                  .  .     ./,        **/,,/////**////////////////////,////   ");
            Console.WriteLine($"         , , **///******,,,,,,           ,**//**.                                                           .        */      ***/**.,*///****/**//////////////////  ,    ");
            Console.WriteLine($"        . ///////********,///*****,.      *///, */.                                                             ./  .//////*,///////**************////////////*    .        ");
            Console.WriteLine($"        . ////*****/*/////////////**,,.    ./.,,,  ,                                             .            / ./////*///////////************/*////////////  *   .*        ");
            Console.WriteLine($"        . ////*****/////////*,,**,,,,.,     *..*/*/*.                .//*.          ,*.        .            /*///*//////////////*/***/******//////////////. *  ,        ");
            Console.WriteLine($"        /  //////////***//////***,,,,,....  ,.  ,  ,.,           /,        .*.             ** ,/*          .//////////////////**/*******/*///////////////..               ");
            Console.WriteLine($"        ,, ,/////////***,,,*,****,,,,,,,,,....  *//*           ,   ***,,****   ***//       *.*////.,,,,,,,,*//*////////////**//********/////////////// /             ");
            Console.WriteLine($"         .* *//**////**,,.****.***,,,,,**,,,..  //  ,     *//****//**////*/*********///**/*, .,,,,,,**********////*///////*////*/*/*///////////////.,.                   ");
            Console.WriteLine($"          ., *///****/*****/****,,,,,,..,,,,,..,   .      *  .*/////////////////*////,. ,,*,,*******************///////*////////////////////////,.*               ");
            Console.WriteLine($"            . * *******////*****/***,, .....,,*,,,.      /,,,,,,,,,,,,,,*////////,,*,*,,,********************/////////////////////////////////, ,              ");
            Console.WriteLine($"              , ., ***/////*******///**,..,,,*,,****/**.////////***********,,///,,,**************************///////////////////////////.    .              ");
            Console.WriteLine($"               *, ***//*//*///**/**,******,,******//**//////////////**///**,**//****/****/**/**///////*****////////////////////*,.*/*   ,                   ");
            Console.WriteLine($"                 /******/////*,,,,*//**/**,*/*/////**//////////*/////*,*///*/////////////////////////****/////////////////////*     *             ");
            Console.WriteLine($"                   /, *////***///////*******///////*//*///////**//*****///////////////////////**//////////////*   ./*.,        ,*                  ");
            Console.WriteLine($"                    .*/, *///////////////////////////,,/////*//*****///////////////////////////////////////////.            ,                        ");
            Console.WriteLine($"                         / *////////////////////////*,,,/*/////**////////////////////////////////,   /*//   ,       .   ,.                       ");
            Console.WriteLine($"                           .* **//////////////////*/*,*.,./*//***/**////////////////////////////****,,*,/  .* /                                  ");
            Console.WriteLine($"                                ,.,/////////*//////**.,///*,/******/////////////////////////////***/,,/*/    **  ,                           ");
            Console.WriteLine($"                                     .  , **///////**///...,*,/*****//////////////////////////////,,*////  ./   *                            ");
            Console.WriteLine($"                                     .  ,  ///////*/..,,,,,,*,****//**///////////////////////*,,*////////..   ,/ ,                   ");
            Console.WriteLine($"                                .           ./////*.*,,,,**,..*,,*/////////////////////*****/////////////, .*//   /                  ");
            Console.WriteLine($"                                              , /*////,,,,,,...../*****//////*****,**////////////////,////, .  .//  .                     ");
            Console.WriteLine($"                                             ...//*/**,.........*//////////////,/,///////////////,//,*/////* ///*     /                 ");
            Console.WriteLine($"                                      ./*      .,///*...//,,,/.,/**//////*,,*...,.,./////////,/,/////////**** ,    *.  *                        ");
            Console.WriteLine($"                                  .//***,.    ,/////...,,,,,*/,,,***,,*,,,,.....,.,,,*///,,/////////////////// ./*   ,/.                              ");
            Console.WriteLine($"                              , ***/**/*     /,////,..,,,*,/,,,,,,*/*,/.,,,,////(//*,,,,.*///////////***/////// .//(/.  .,                       ");
            Console.WriteLine($"                           /, ******////*,.   /////*...,,,*,,,,.,*..****.,//*,...,.*,////*,*,//////////////////*/, ,.  ./,                    ");
            Console.WriteLine($"                      .*//, ********//*///*,  *////...,,,,,,,,....,**,,..,**,.,,,,,,******%%%%(/####///////#%%%%//.%%%*     ,                     ");
            Console.WriteLine($"                      , **/******/*********//////////,,,,,*..,***,.,*,,,,.,,,..,//////(/////*,***###///////(##%%// *.. ,#%%#  (                              ");
            Console.WriteLine($"                    /********/******//////////////////*,.....*(#/*,,......,*,,,,,,,,*,...........,///////**//(///// /*..    .%%.#                       ");
            Console.WriteLine($"                    *//(/*////*//*****////////*,///////*///..****./*,............,////,.,/////////////////////////**. ,//(////.  /*                    ");
            Console.WriteLine($"                    . * ***((((*****///////////**////////,,../(**(,........,//////////**,...............*,//////////////. /////. /// *                  ");
            Console.WriteLine($"                      **/*/(((/**//////////////////////%(/#(,,.....,***,............,,,,,,,,,****///////////////////////..    .,..*  //.                 ");
            Console.WriteLine($"                     ****///(//####///(/////(///##((#*,/.(,//,....,,,,,,,,,,*/,*/////////////*/*,,,,,,,,,,,/,//////////// ///*  .  .  ./.                  ");
            Console.WriteLine($"                         *****///////////////***//**    *////  ,,,,*/,**/////////////*,,,,,,,,,,,,,,,,,,,,,,,,**///////****/ .    ,*     ,*                          ");
            Console.WriteLine($"                      ****///////////////*.*.            /*////*/////,.........,,,,,,,,,,,,,,,....,*//////////,///*/*****/ *,,,,    *//,                           ");
            Console.WriteLine($"                        , ****////////////.,/      .,        /    .................,......,//////////////**,,,,,,,,/,/////////* ,......    ,/                            ");
            Console.WriteLine($"                           * .* ***/////*,,*..,,,               *       ,,*//////*,,/////*,..................,*///////////////////. ,////////////*                    ");
            Console.WriteLine($"                               .////,                          ..   //   ,.......................**,/////////////*********/*///////,  .          ,/*             ");
            Console.WriteLine($"                                                                //,      ................,,*/////*,,..................,///////////////  ////////,     ,         ");
            Console.WriteLine($"                                                                  /        , *//////*,,,....,,..................,*//////,........,,.///// .,             * ");
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
            client.Spot.Order.PlaceOrder("ETHBTC", OrderSide.Buy, OrderType.Market, null, 0);
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
                string symbol;
                string symbolpair;
                string qtusd;
                string srisk;
                decimal quantity;
                Thread t3 = new Thread(PingThread);
                t3.Start();
                void PingThread()
                {
                    while (closet3 == 0)
                    {
                        float reply = client.Spot.System.Ping().Data;
                        Console.Title = "Ping to Binance: " + reply + "ms";
                    }
                }
                ShowWindow(ThisConsole, RESTORE);
                Console.SetWindowSize(80, 40);
                Utilities.Write(ConsoleColor.Yellow, "How much USD to allocate?");
                Console.ForegroundColor = ConsoleColor.White;
                qtusd = Console.ReadLine();
                if (string.IsNullOrEmpty(qtusd))
                    return;
                decimal usd = Convert.ToDecimal(qtusd);
                if (usd < 10)
                {
                    Utilities.Write(ConsoleColor.Red, $"Has to be higher than 10");
                    return;
                }
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
                    else
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
                }
                //Exit the program if nothing was entered
                if (string.IsNullOrEmpty(symbol))
                    return;
                //Try to execute the order
                closet3 = 1;
                client.ShouldCheckObjects = false;
                ExecuteOrder(symbol, quantity, strategyrisk, sellStrategy, maxsecondsbeforesell, client, pairend, exchangeInfo);
                client = new BinanceClient();
                exchangeInfo = client.Spot.System.GetExchangeInfo();
                client.Spot.Order.PlaceOrder("ETHBTC", OrderSide.Buy, OrderType.Market, null, 0);
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

        private static void ExecuteOrder(string symbol, decimal quantity, decimal strategyrisk, decimal sellStrategy, decimal maxsecondsbeforesell, BinanceClient client,string pairend, WebCallResult<BinanceExchangeInfo> exchangeInfo)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            using (client)
            {
                string pair = symbol + pairend;
                client.ShouldCheckObjects = false;
                WebCallResult<BinancePlacedOrder> order = client.Spot.Order.PlaceOrder(pair, OrderSide.Buy, OrderType.Market, null, quantity);
                if (!order.Success)
                {
                    Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Market order. Error code: " + order.Error?.Message);
                    return;
                }
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
                int count = -1;
                int stoplossex = 0;
                int first = 1;
                int usainsell = 0;
                int imincharge = 0;
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
                            tab.Add(priceResult2.Data.BestBidPrice);
                        else
                            return;
                        if (count % 10 == 0 && count > 25)
                        {
                            int countca = count;
                            decimal espa = 0;
                            int x2a = -1;
                            while (--countca > 0 && ++x2a < 10)
                            {
                                espa += (tab[countca] - tab[countca - 1]) / 10;
                            }
                            decimal esp2a = espa / 3;
                            while (--countca > 0 && ++x2a < 30)
                            {
                                esp2a += (tab[countca] - tab[countca - 1]) / 30;
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
                        try
                        {
                            priceResult3 = client.Spot.Market.GetBookPrice(pair);
                            tab.Add(priceResult3.Data.BestBidPrice);
                        }
                        catch (Exception e)
                        {
                            tab.Add(tab[count - 1]);
                        }
                        Console.Title = $"Price for {pair} is {priceResult3.Data.BestBidPrice} to {priceResult3.Data.BestAskPrice} in iteration  " + count + "  negative volatility ratio is " + Math.Round(volasellmax, 2) + " stop limit is placed at " + currentstoploss;
                        if ((count + 5) % 10 == 0 && count > 25)
                        {
                            int countc = count;
                            decimal esp = 0;
                            int x2 = -1;
                            while (--countc > 0 && ++x2 < 10)
                            {
                                esp += (tab[countc] - tab[countc - 1]) / 10;
                            }
                            decimal esp2 = esp / 3;
                            while (--countc > 0 && ++x2 < 30)
                            {
                                esp2 += (tab[countc] - tab[countc - 1]) / 30;
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