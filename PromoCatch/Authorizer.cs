using System;
using System.Threading.Tasks;
using LanguageExt;
using Telega;
using Telega.Rpc.Dto.Types.Account;

namespace PromoCatch
{
    public static class Authorizer {
       
        
        static T ReadString<T>(
            Func<string, T?> mapper
        ) {
            while (true) {
                T? input = mapper(Console.ReadLine() ?? "");
                if (input != null) {
                    return input;
                }
                Console.WriteLine("Invalid input. Try again.");
            }
        }
        
        static string ReadPassword()
        {
            string pass = "";
            while (true) {
                ConsoleKeyInfo input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.Enter) {
                    Console.WriteLine();
                    break;
                }

                if (!char.IsControl(input.KeyChar)) {
                    pass += (input.KeyChar);
                    Console.Write("*");
                }
                else if (input.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass.Substring(0, pass.Length - 1);
                    Console.Write("\b \b");
                }
            }
            return pass;
        }

        static async Task SignInViaCode(TelegramClient tg) {
            Console.WriteLine("Type your phone number.");
            string phone = ReadString(x => x
                .Replace(" ", "")
                .Replace("(", "")
                .Replace(")", "")
                .Trim()
                .Apply(x => x.Length > 0 ? x : null)
            );
            
            Console.WriteLine("Requesting login code.");
            string codeHash = await tg.Auth.SendCode(Program.ApiHash, phone);

            while (true) {
                try {
                    Console.WriteLine("Type the login code.");
                    string code = ReadString(x => x
                        .Apply(x => int.TryParse(x, out int res) ? (int?) res : null)
                        .Apply(x => x?.ToString())
                    );
                    await tg.Auth.SignIn(phone, codeHash, code);
                    break;
                }
                catch (TgInvalidPhoneCodeException) {
                    Console.WriteLine("Invalid login code. Try again.");
                }
            }
        }

        static async Task SignInViaPassword(TelegramClient tg) {
            while (true) {
                Console.WriteLine("Type the password.");
                string password = ReadPassword();
                Password pwdInfo = await tg.Auth.GetPasswordInfo();
                try {
                    await tg.Auth.CheckPassword(pwdInfo, password );
                    break;
                }
                catch (TgInvalidPasswordException) {
                    Console.WriteLine("Invalid password. Try again.");
                }
            }
        }

        public static async Task Authorize(TelegramClient tg) {
            if (tg.Auth.IsAuthorized) {
                Console.WriteLine("You're already authorized.");
                return;
            }

            try {
                Console.WriteLine("Authorizing.");
                await SignInViaCode(tg);
            }
            catch (TgPasswordNeededException) {
                Console.WriteLine("Cloud password is needed.");
                await SignInViaPassword(tg);
            }

            Console.WriteLine("Authorization is completed.");
        }
    }
}