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

            var discordInfo = JsonConvert.DeserializeObject<DiscordConnectionInfo>(File.ReadAllText("discord.json"));
            var ircInfo = JsonConvert.DeserializeObject<IrcConnectionInfo>(File.ReadAllText("irc.json"));

            Pair = new ChannelPair(ircInfo, discordInfo);
            Pair.Connect().Wait();

            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            // Listen for mannual commands
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
