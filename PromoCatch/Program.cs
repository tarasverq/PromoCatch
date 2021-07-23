using System;
using System.Threading.Tasks;
using Telega;

namespace PromoCatch
{
    class Program
    {
        public const int ApiId = -1;
        public const string ApiHash = "your_api_key_here";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Connecting to Telegram.");
            using TelegramClient tg = await TelegramClient.Connect(ApiId);
            await Authorizer.Authorize(tg);
            await CatchPromo.Run(tg);
        }
    }
}
