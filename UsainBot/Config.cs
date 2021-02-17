namespace OpenCryptShot
{
    public class Config
    {
        public string apiKey;
        public string apiSecret;
        public decimal quantity; // the quantity of BTC to use
        public decimal sellStrategy; // ratio between 0.5 and .95 for which the starting stop loss start and the whole strategy is based on, the higher the safer.
        public decimal strategyrisk; // sensibility of the bot and pressure on the bot to sell, the bigger the value is the greedier the bot ll be and the slower he ll sell.
        public decimal maxsecondsbeforesell;
        public string name;
        public string expiry;
        public string discord_token;
        public string channel_id;
    }
}