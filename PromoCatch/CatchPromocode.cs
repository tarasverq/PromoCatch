using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LanguageExt;
using Telega;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Messages;
using Tesseract;

namespace PromoCatch
{
    static class CatchPromo
    {
        public static async Task Run(TelegramClient tg)
        {
            List<string> ignored = new List<string>();
            if (File.Exists("ignored"))
                ignored = new List<string>(File.ReadAllLines("ignored"));
            
            Dialogs chatsType = await tg.Messages.GetDialogs();
            Dialogs.SliceTag chats = chatsType.AsSliceTag().Head();
            IEnumerable<Chat.ChannelTag> channels = chats.Chats.Choose(Chat.AsChannelTag);

            Chat.ChannelTag firstChannel = channels.Filter(x => x.Username == "publisher_push_house")
                .HeadOrNone()
                .IfNone(() => throw new Exception("A channel is not found"));

            
            InputPeer.ChannelTag channelPeer = new InputPeer.ChannelTag(
                channelId: firstChannel!.Id,
                accessHash: (long) firstChannel!.AccessHash
            );

            ulong i = 1;
            TesseractEngine? engine = new TesseractEngine(@"tessdata", "eng", EngineMode.Default);
            HttpClient client = new HttpClient();
            while (true)
            {
                Task<Messages> task = Task.Run(async () => { return await tg.Messages.GetHistory(channelPeer, limit: 30); });
                if (await Task.WhenAny(task, Task.Delay(10000)) != task)
                {
                    Console.WriteLine("ERROR");
                    continue;
                }
                
                Messages? top100Messages = task.Result;
                DateTime receiveTime = DateTime.Now;
                Arr<Message> messages = top100Messages.AsChannelTag().Head().Messages;
                if (messages != null)
                {
                    foreach (Message channelMessage in messages)
                    {
                        if(channelMessage.AsTag().IsNone)
                            continue;
                        Message.Tag message = channelMessage.AsTag().Head();
                        string id = (message?.Id ?? -1).ToString();
                        string? text = message?.Message;
                        if (ignored.Contains(id))
                            continue;
                        if (!string.IsNullOrWhiteSpace(text) && text.Contains("promocodes"))
                        {
                            DateTime findTime = DateTime.Now;
                            string url = Regex.Match(text, "https://.+").Value;

                            byte[] image = await (await client.GetAsync(url)).Content.ReadAsByteArrayAsync();
                            DateTime imageTime = DateTime.Now;
                            string recognizedText;
                            using (Pix img = Pix.LoadFromMemory(image))
                            {
                                using (Tesseract.Page? recognizedPage = engine.Process(img))
                                {
                                    Console.WriteLine(
                                        $"Mean confidence for page #{i}: {recognizedPage.GetMeanConfidence()}");


                                    recognizedText = recognizedPage.GetText().Trim();
                                    WindowsFunctions.SetClipboardText(recognizedText);
                                }
                            }

                            DateTime recognitionTime = DateTime.Now;

                            WindowsFunctions.OpenLink("https://push.house/finances/refill");

                            DateTimeOffset postTime = DateTimeOffset.FromUnixTimeSeconds(message?.Date ?? 0)
                                .ToOffset(TimeSpan.FromHours(3));
                            Console.WriteLine(text);
                            Console.WriteLine($"Code is: {recognizedText}");
                            Console.WriteLine($"postTime:        {postTime.ToString("O")}{Environment.NewLine}" +
                                              $"receiveTime:     {receiveTime.ToString("O")}{Environment.NewLine}" +
                                              $"findTime:        {findTime.ToString("O")}{Environment.NewLine}" +
                                              $"imageTime:       {imageTime.ToString("O")}{Environment.NewLine}" +
                                              $"recognitionTime: {recognitionTime.ToString("O")}{Environment.NewLine}" +
                                              $"time total:      {(recognitionTime - postTime).ToString("c")}{Environment.NewLine}");
                            await WindowsFunctions.Tannenbaum();
                            Console.WriteLine();
                        }

                        File.AppendAllText("ignored", id + Environment.NewLine);
                        ignored.Add(id);
                    }
                }

                //await Task.Delay(500);
                Console.WriteLine($"Ping {DateTime.Now} {i++}");
            }
        }

       
    }
}