using Discord;
using Discord.WebSocket;
using IrcDotNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto
{
    public class Program
    {
        private static List<ChannelPair> Pairs;

        private static bool _listen;

        public static void Main(string[] args)
        {
            _listen = true;
            Pairs = new List<ChannelPair>();

            var discordFilenames = Directory.GetFiles(".", "*.discord.json");
            foreach (var discordFilename in discordFilenames)
            {
                var ircFilename = discordFilename.Replace(".discord.json", ".irc.json");
                if (File.Exists(ircFilename))
                {
                    var discordInfo = JsonConvert.DeserializeObject<DiscordConnectionInfo>(File.ReadAllText(discordFilename));
                    var ircInfo = JsonConvert.DeserializeObject<IrcConnectionInfo>(File.ReadAllText(ircFilename));

                    var pair = new ChannelPair(ircInfo, discordInfo);
                    if (args.Contains("noprompt"))
                    {
                        pair.EnableConsoleLogging = false;
                    }
                    Console.WriteLine("Connecting...");
                    pair.Connect().Wait();
                    Console.WriteLine("Ready.");
                    Pairs.Add(pair);
                }
            }
           
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            // Listen for mannual commands
            if (args.Contains("noprompt"))
            {
                while (true)
                {
                    // Block until process is manually stopped
                }
            }
            else
            {
                while (_listen)
                {
                    var input = Console.ReadLine();
                    var cmd = input.Split(" ".ToCharArray(), 2);
                    switch (cmd[0].ToLower())
                    {
                        case "say":
                            if (Pairs.Count > 1)
                            {
                                Console.WriteLine("There is currently more than 1 channel pair active. Manual input is not currently supported.");
                                break;
                            }

                            if (cmd.Length > 1)
                            {
                                await Pairs[0].SendDiscordMessage(cmd[1]);
                                Pairs[0].SendIrcMessage(cmd[1]);
                            }
                            else
                            {
                                Console.WriteLine("Usage: say <message>");
                            }
                            break;
                        case "exit":
                        case "quit":
                            _listen = false;
                            break;
                        default:
                            break;
                    }
                }
            }            
        }        
    }
}
