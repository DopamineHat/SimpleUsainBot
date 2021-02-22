﻿using System;
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

namespace UsainBot
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "UsainBot Alpha";
            Config config = LoadOrCreateConfig();
            if (config == null)
            {
                Console.Read();
                return;
            }

            try
            {
                BinanceClient.SetDefaultOptions(new BinanceClientOptions
                {
                    ApiCredentials = new ApiCredentials(config.apiKey, config.apiSecret),
                    LogVerbosity = LogVerbosity.None,
                    LogWriters = new List<TextWriter> { Console.Out }
                });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR! Could not set Binance options. Error message: {ex.Message}");
                Console.Read();
                return;
            }
            decimal strategyrisk = Math.Round((decimal)Math.Pow((double)config.risktaking,  1.5), 3);
            decimal sellStrategy = Math.Round((decimal).95 - config.risktaking * (decimal).03, 3);
            decimal maxsecondsbeforesell = config.risktaking * (decimal)5.0;
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
            int red = 0;
            while (true)
            {
                int closet3 = 0;
                Thread t3 = new Thread(PingThread);
                t3.Start();
                void PingThread()
                {
                    while (closet3 == 0)
                    {
                        Ping pingSender = new Ping();

                        // Create a buffer of 32 bytes of data to be transmitted.
                        string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                        byte[] buffer = Encoding.ASCII.GetBytes(data);

                        PingOptions options = new PingOptions(64, true);

                        // Send the request.
                        PingReply reply = pingSender.Send("139.162.78.33", 1000, buffer, options);

                        if (reply.Status == IPStatus.Success)
                            Console.Title = "Ping to Tokyo: " + reply.RoundtripTime + "ms";
                    }
                }
                string symbol;
                if (config.channel_id.Length > 0 && config.discord_token.Length > 0 && red == 0)
                {
                    symbol = null;
                    Utilities.Write(ConsoleColor.Yellow, "Looking for ticker...");
                    while (null == symbol)
                    {
                        symbol = FindTicker(config.discord_token, config.channel_id);
                        Thread.Sleep(1000);
                    }
                    red = 1;
                    Utilities.Write(ConsoleColor.Green, "Found ticker " + symbol);
                    Console.ForegroundColor = ConsoleColor.White;
                    symbol = symbol.Remove(0, 1);
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
                ExecuteOrder(symbol, config.quantity, strategyrisk, sellStrategy, maxsecondsbeforesell, client, exchangeInfo);
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
                    quantity = (decimal)0.00025,
                    risktaking = (decimal)3.0,
                    discord_token = "",
                    channel_id = ""
                });
                File.WriteAllText("config.json", json);
                Utilities.Write(ConsoleColor.Red, "config.json was missing and has been created. Please edit the file and restart the application.");
                return null;
            }
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
        }

        private static string FindTicker(string discord_token, string channel_id)
        {
            Regex regex = new Regex(@"(\$)[a-zA-Z]{1,5}");
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v8/channels/" + channel_id + "/messages?limit=1");
            req.Headers.Add("Authorization", discord_token);
            req.Accept = "*/*";
            req.ContentType = "application/json";
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) discord/0.0.309 Chrome/83.0.4103.122 Electron/9.3.5 Safari/537.36";
            WebResponse res = req.GetResponse();
            Stream dataStream = res.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string resJson = reader.ReadToEnd();
            JsonSerializerOptions options = new JsonSerializerOptions
            { IncludeFields = true };
            Message[] msg = JsonSerializer.Deserialize<Message[]>(resJson, options);
            Match match = regex.Match(msg[0].content);
            reader.Close();
            dataStream.Close();
            if (match.Success)
            {
                return match.Value;
            }
            else
            {
                return null;
            }
        }

        private static void ExecuteOrder(string symbol, decimal quantity, decimal strategyrisk, decimal sellStrategy, decimal maxsecondsbeforesell, BinanceClient client, WebCallResult<BinanceExchangeInfo> exchangeInfo)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            using (client)
            {
                string pair = symbol.ToUpper() + "BTC";
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
                BinanceSymbol symbolInfo = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.QuoteAsset == "BTC" && s.BaseAsset == symbol.ToUpper());
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
                decimal sellPriceAskRatio = (decimal).995;
                decimal StartSellStrategy = sellStrategy;
                decimal MaxSellStrategy = 1 - ((1 - sellStrategy) / 5);
                decimal volasellmax = (decimal).01;
                decimal currentstoploss = 0;
                List<decimal> tab = new List<decimal>();
                int count = -1;
                int x = 0;
                int n = 1;
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
                    while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime() && x != 2 && imincharge == 0)
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
                            decimal volasell = (esp2a - espa) / priceResult2.Data.BestBidPrice * 100 * (decimal)Math.Pow(count, .7);
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
                                        Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Market order sell, trying another time. Error code: " + ordersell.Error?.Message);
                                        ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    }
                                    Thread.Sleep(1000);
                                    int y = 0;
                                    while (y == 0)
                                    {
                                        if (ordersell.Data.Fills != null)
                                        {
                                            paidPrice = ordersell.Data.Fills.Average(trade => trade.Price);
                                            orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                            try
                                            {
                                                WebCallResult<BinancePlacedOrder> ordersell4 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel); // for if the previous limit order is filled but not 100%
                                            }
                                            finally
                                            {
                                                Utilities.Write(ConsoleColor.Green, "100% sold");
                                            }
                                        }
                                        else
                                        {
                                            priceResult2 = client.Spot.Market.GetBookPrice(pair);
                                            orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                            ordersell = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult2.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                        }
                                        y = 1;
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
                    while ((timestamp + maxsecondsbeforesell * 10000000) > DateTime.Now.ToFileTime() && x != 2 && imincharge == 0)
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
                            decimal volasell = (esp2 - esp) / priceResult3.Data.BestBidPrice * 100 * (decimal)Math.Pow(count, .7);
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
                                        Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Market order sell, trying another time. Error code: " + ordersell2.Error?.Message);
                                        ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestAskPrice * sellPriceAskRatio - (decimal)0.00000001, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                                    }
                                    Thread.Sleep(1000);
                                    int y = 0;
                                    while (y == 0)
                                    {
                                        if (ordersell2.Data.Fills != null)
                                        {
                                            paidPrice = ordersell2.Data.Fills.Average(trade => trade.Price);
                                            orderspanic = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                                            try
                                            {
                                                WebCallResult<BinancePlacedOrder> ordersell4 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel); // for if the previous limit order is filled but not 100%
                                            }
                                            finally
                                            {
                                                Utilities.Write(ConsoleColor.Green, "100% sold");
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
                            WebCallResult<IEnumerable<BinanceCancelledId>> orders = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                            if (!orders.Success)
                            {
                                if (n == 0)
                                    if (x == 0)
                                    {
                                        x = 1;
                                        Utilities.Write(ConsoleColor.Red, $"ERROR! Could not remove orders. Error code: " + orders.Error?.Message);
                                    }
                                    else
                                    {
                                        x = 2;
                                        Utilities.Write(ConsoleColor.Red, "StopLoss executed : " + orders.Error?.Message);
                                        return;
                                    }
                            }
                            else
                                Utilities.Write(ConsoleColor.Blue, $"Orders successfully removed.");
                            n = 0;
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
                    else
                    {
                        imincharge = 1;
                        WebCallResult<IEnumerable<BinanceCancelledId>> orderspanic2 = client.Spot.Order.CancelAllOpenOrders(symbol: pair);
                        WebCallResult<BinancePlacedOrder> ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                        while (!ordersell2.Success)
                        {
                            Utilities.Write(ConsoleColor.Red, $"ERROR! Could not place the Order sell, trying another time. Error code: " + ordersell2.Error?.Message);
                            ordersell2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, OrderQuantity, price: Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision), timeInForce: TimeInForce.GoodTillCancel);
                        }
                        usainsell = 1;
                        paidPrice = 0;
                        if (ordersell2.Data.Fills != null)
                        {
                            paidPrice = ordersell2.Data.Fills.Average(trade => trade.Price);
                        }
                        Utilities.Write(ConsoleColor.Green, "UsainBot TIME SOLD successfully  " + OrderQuantity + " " + ordersell2.Data.Symbol + $" sold at " + paidPrice);
                        return;
                    }
                }
                Thread.Sleep(2000);
            }
        }
    }
}