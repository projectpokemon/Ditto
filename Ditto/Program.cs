﻿using Discord;
using Discord.WebSocket;
using IrcDotNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto
{
    public class Program
    {
        private static List<ChannelPair> Pairs;

        private static bool _listen;

        public static async Task Main(string[] args)
        {
            var noprompt = args.Contains("noprompt");
            try
            {
                if (!noprompt) Console.WriteLine("Starting...");
                Pairs = new List<ChannelPair>();

                var discordFilenames = Directory.GetFiles(".", "*.discord.json");
                if (!noprompt) Console.WriteLine("Found " + discordFilenames.Length.ToString() + " Discord settings");
                foreach (var discordFilename in discordFilenames)
                {
                    if (!noprompt) Console.WriteLine(discordFilename);
                    var ircFilename = discordFilename.Replace(".discord.json", ".irc.json");
                    if (File.Exists(ircFilename))
                    {
                        Console.Write(ircFilename);
                        var discordInfo = JsonConvert.DeserializeObject<DiscordConnectionInfo>(File.ReadAllText(discordFilename));
                        var ircInfo = JsonConvert.DeserializeObject<IrcConnectionInfo>(File.ReadAllText(ircFilename));

                        var pair = new ChannelPair(new IrcConnection(ircInfo) { EnableConsoleLogging = !noprompt }, discordInfo)
                        {
                            EnableConsoleLogging = !noprompt
                        };
                        if (!noprompt) Console.WriteLine("Connecting...");
                        await pair.Connect();
                        if (!noprompt) Console.WriteLine("Ready.");
                        Pairs.Add(pair);
                    }
                    else
                    {
                        if (!noprompt) Console.Write("Did not find IRC settings. Not creating pair.");
                    }
                }

                // Listen for mannual commands
                if (noprompt)
                {
                    while (true)
                    {
                        // Block until process is manually stopped
                        await Task.Delay(int.MaxValue);
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
                                    if (!noprompt) Console.WriteLine("There is currently more than 1 channel pair active. Manual input is not currently supported.");
                                    break;
                                }

                                if (cmd.Length > 1)
                                {
                                    await Pairs[0].SendDiscordMessage(cmd[1]);
                                    Pairs[0].SendIrcMessage(cmd[1]);
                                }
                                else
                                {
                                    if (!noprompt) Console.WriteLine("Usage: say <message>");
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
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                throw;
            }
        }
    }
}
