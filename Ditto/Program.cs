using Discord;
using Discord.WebSocket;
using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto
{
    public class Program
    {
        private static string _discordToken;
        private static ulong _discordGuildId;
        private static ulong _discordChannelId;
        private static DiscordSocketClient _discordClient;

        private static IrcDotNet.StandardIrcClient _ircClient;
        private static Uri _ircServer;
        private static IrcDotNet.IrcUserRegistrationInfo _ircRegInfo;
        private static string _ircChannel;

        private static bool _listen;

        public static void Main(string[] args)
        {
            _listen = true;

            _discordToken = "token";
            _discordGuildId = 342007647702220800;
            _discordChannelId = 342032979327057922;

            _ircServer = new Uri("irc://irc.projectpokemon.org:6667/ProjectPokemon");
            _ircRegInfo = new IrcUserRegistrationInfo();
            _ircRegInfo.NickName = "Ditto";
            _ircRegInfo.UserName = "Ditto";
            _ircRegInfo.Password = "password";
            _ircRegInfo.RealName = "Ditto";
            _ircChannel = "#pp-test";

            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            // IRC
            _ircClient = new IrcDotNet.StandardIrcClient();
            _ircClient.RawMessageReceived += Irc_RawMessageReceived;
            _ircClient.ErrorMessageReceived += Irc_ErrorMessageReceived;
            _ircClient.Connected += Irc_Connected;
            _ircClient.ConnectFailed += Irc_ConnectFailed;
            _ircClient.MotdReceived += Irc_MotdReceived;
            _ircClient.Connect("irc.projectpokemon.org", 6667, false, _ircRegInfo);

            // Discord
            _discordClient = new DiscordSocketClient();
            _discordClient.Log += Discord_Log;
            _discordClient.MessageReceived += Discord_MessageReceived;
            await _discordClient.LoginAsync(TokenType.Bot, _discordToken);
            await _discordClient.StartAsync();

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
                            await SendDiscordMessage(cmd[1]);
                            await SendIrcMessage(cmd[1]);
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

        private static async Task SendDiscordMessage(string msg)
        {
            await _discordClient.GetGuild(_discordGuildId).GetTextChannel(_discordChannelId).SendMessageAsync(msg);
        }

        private static Task SendIrcMessage(string msg)
        {
            _ircClient.LocalUser.SendMessage("#pp-test", msg);
            return Task.CompletedTask;
        }

        private static Task Discord_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private static async Task Discord_MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id != _discordChannelId || message.Author.Username == _discordClient.CurrentUser.Username)
            {
                return;
            }

            Console.WriteLine($"#{message.Channel.Name}: [{message.Author.Username}] {message.Content}");

            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            else
            {
                var lines = message.Content.Split('\n').Select(x => x.Trim()).ToArray();
                for (int i = 0; i < Math.Min(lines.Length, 4); i++)
                {
                    await SendIrcMessage($"<{message.Author.Username}> {lines[i]}");
                }
                if (lines.Length > 4)
                {
                    await SendIrcMessage($"({message.Author.Username} posted {lines.Length - 5} more lines not shown here)");
                }
            }
        }

        private static void Irc_ChannelMessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (!e.Targets.Any(x => x.Name == _ircChannel) || e.Source.Name == _ircClient.LocalUser.NickName)
            {
                return;
            }

            if (e.Text == "!ping")
            {
                SendIrcMessage("Pong!");
            }
            else if (e.Text.StartsWith((char)1 + "ACTION ") && e.Text.Last() == (char)1)
            {
                // The /me command
                SendDiscordMessage($"**{e.Source}** {e.Text.Substring(8, e.Text.Length - 8)}").Wait();
            }
            else
            {
                SendDiscordMessage($"<**{e.Source}**> {e.Text}").Wait();
            }
        }

        private static void Irc_RawMessageReceived(object sender, IrcRawMessageEventArgs e)
        {
            Console.WriteLine(e.RawContent);
        }

        private static void Irc_ErrorMessageReceived(object sender, IrcErrorMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        private static void Irc_Connected(object sender, EventArgs e)
        {
            Console.WriteLine("Connected to IRC");
        }

        private static void Irc_MotdReceived(object sender, EventArgs e)
        {
            Console.WriteLine("Motd received");
            _ircClient.LocalUser.JoinedChannel += Irc_JoinedChannel;
            _ircClient.LocalUser.LeftChannel += Irc_LeftChannel;
            _ircClient.Channels.Join(_ircChannel);
        }

        private static void Irc_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            Console.WriteLine("Joined channel");
            e.Channel.MessageReceived += Irc_ChannelMessageReceived;
        }

        private static void Irc_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            Console.WriteLine("Left channel");
            e.Channel.MessageReceived -= Irc_ChannelMessageReceived;

            // Try to join again
            _ircClient.Channels.Join(_ircChannel);
        }

        private static void Irc_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            Console.WriteLine("Connect failed.");
            Console.WriteLine(e.Error);
        }
    }
}
