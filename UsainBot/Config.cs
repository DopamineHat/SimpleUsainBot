namespace UsainBot
{
    public class Config
    {
        public string apiKey;
        public string apiSecret;
        public decimal quantity; // the quantity of BTC to use
        public decimal risktaking; // the risk the bot ll willingly take (put 1 to 5)
        public string name;
        public string expiry;
        public string discord_token;
    }
}