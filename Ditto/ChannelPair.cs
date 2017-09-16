using Discord;
using Discord.WebSocket;
using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ditto
{
    public class ChannelPair
    {
        public ChannelPair(IrcConnectionInfo ircConnectionInfo, DiscordConnectionInfo discordConnectionInfo)
        {
            this.DiscordConnectionInfo = discordConnectionInfo;
            this.IrcConnectionInfo = ircConnectionInfo;
            this.EnableConsoleLogging = true;
        }

        private DiscordConnectionInfo DiscordConnectionInfo { get; set; } 
        private DiscordSocketClient DiscordClient { get; set; }

        private IrcConnectionInfo IrcConnectionInfo { get; set; }
        private StandardIrcClient IrcClient { get; set; }

        public bool EnableConsoleLogging { get; set; }

        /// <summary>
        /// Connects to both IRC and Discord
        /// </summary>
        public async Task Connect()
        {
            await ConnectDiscord();
            ConnectIrc();
        }        

        private async Task ConnectDiscord()
        {
            DiscordClient = new DiscordSocketClient();
            DiscordClient.Log += Discord_Log;
            DiscordClient.MessageReceived += Discord_MessageReceived;
            await DiscordClient.LoginAsync(TokenType.Bot, DiscordConnectionInfo.Token);
            await DiscordClient.StartAsync();
        }

        public async Task SendDiscordMessage(string msg)
        {
            await DiscordClient.GetGuild(DiscordConnectionInfo.GuildId).GetTextChannel(DiscordConnectionInfo.ChannelId).SendMessageAsync(msg);
        }

        private void ConnectIrc()
        {
            IrcClient = new StandardIrcClient();
            IrcClient.RawMessageReceived += Irc_RawMessageReceived;
            IrcClient.ErrorMessageReceived += Irc_ErrorMessageReceived;
            IrcClient.Connected += Irc_Connected;
            IrcClient.ConnectFailed += Irc_ConnectFailed;
            IrcClient.MotdReceived += Irc_MotdReceived;
            IrcClient.Connect(IrcConnectionInfo.Server, IrcConnectionInfo.Port, false, IrcConnectionInfo.GetRegistrationInfo());
        }

        private void JoinIrcChannel()
        {
            if (!string.IsNullOrEmpty(IrcConnectionInfo.ChannelPassword))
            {
                IrcClient.Channels.Join(new[] { new Tuple<string, string>(IrcConnectionInfo.Channel, IrcConnectionInfo.ChannelPassword) });
            }
            else
            {
                IrcClient.Channels.Join(new[] { IrcConnectionInfo.Channel });
            }
        }

        public void SendIrcMessage(string msg)
        {
            IrcClient.LocalUser.SendMessage(IrcConnectionInfo.Channel, msg);
        }

        #region Discord Event Handlers

        private Task Discord_Log(LogMessage msg)
        {
            if (EnableConsoleLogging) Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task Discord_MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id != DiscordConnectionInfo.ChannelId || message.Author.Username == DiscordClient.CurrentUser.Username)
            {
                return;
            }

            if (EnableConsoleLogging) Console.WriteLine($"#{message.Channel.Name}: [{message.Author.Username}] {message.Content}");

            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            else
            {
                var lines = message.Content.Split('\n').Select(x => x.Trim()).ToArray();
                for (int i = 0; i < Math.Min(lines.Length, 4); i++)
                {
                    SendIrcMessage($"<{message.Author.Username}> {lines[i]}");
                }
                if (lines.Length > 4)
                {
                    SendIrcMessage($"({message.Author.Username} posted {lines.Length - 5} more lines not shown here)");
                }
            }
        }
        #endregion

        #region IRC Event Handlers
        private void Irc_ChannelMessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (!e.Targets.Any(x => x.Name == IrcConnectionInfo.Channel) || e.Source.Name == IrcClient.LocalUser.NickName)
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

        private void Irc_RawMessageReceived(object sender, IrcRawMessageEventArgs e)
        {
            if (EnableConsoleLogging) Console.WriteLine(e.RawContent);
        }

        private void Irc_ErrorMessageReceived(object sender, IrcErrorMessageEventArgs e)
        {
            if (EnableConsoleLogging) Console.WriteLine(e.Message);
        }

        private void Irc_Connected(object sender, EventArgs e)
        {
            if (EnableConsoleLogging) Console.WriteLine("Connected to IRC");
        }

        private void Irc_MotdReceived(object sender, EventArgs e)
        {
            if (EnableConsoleLogging) Console.WriteLine("Motd received");
            IrcClient.LocalUser.JoinedChannel += Irc_JoinedChannel;
            IrcClient.LocalUser.LeftChannel += Irc_LeftChannel;
            JoinIrcChannel();
        }

        private void Irc_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            if (EnableConsoleLogging) Console.WriteLine("Joined channel");
            e.Channel.MessageReceived += Irc_ChannelMessageReceived;
        }

        private void Irc_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            if (EnableConsoleLogging) Console.WriteLine("Left channel");
            e.Channel.MessageReceived -= Irc_ChannelMessageReceived;

            // Try to join again
            JoinIrcChannel();
        }

        private void Irc_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            if (EnableConsoleLogging) Console.WriteLine("Connect failed.");
            if (EnableConsoleLogging) Console.WriteLine(e.Error);
        }
        #endregion
    }
}
