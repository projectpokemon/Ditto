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
        private static ChannelPair Pair;

        private static bool _listen;

        public static void Main(string[] args)
        {
            _listen = true;

            if (!File.Exists("discord.json"))
            {
                Console.WriteLine("Can't find discord.json");
                return;
            }

            if (!File.Exists("irc.json"))
            {
                Console.WriteLine("Can't find irc.json");
                return;
            }

            var discordInfo = JsonConvert.DeserializeObject<DiscordConnectionInfo>(File.ReadAllText("discord.json"));
            var ircInfo = JsonConvert.DeserializeObject<IrcConnectionInfo>(File.ReadAllText("irc.json"));

            Pair = new ChannelPair(ircInfo, discordInfo);
            if (args.Contains("noprompt"))
            {
                Pair.EnableConsoleLogging = false;
            }
            Console.WriteLine("Connecting...");
            Pair.Connect().Wait();
            Console.WriteLine("Ready.");
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
                            if (cmd.Length > 1)
                            {
                                await Pair.SendDiscordMessage(cmd[1]);
                                Pair.SendIrcMessage(cmd[1]);
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
