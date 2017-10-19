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
        public ChannelPair(IrcConnection ircConnection, DiscordConnectionInfo discordConnectionInfo)
        {
            this.DiscordConnectionInfo = discordConnectionInfo;
            this.IrcConnection = ircConnection;
            this.EnableConsoleLogging = true;
        }

        private DiscordConnectionInfo DiscordConnectionInfo { get; set; } 
        private DiscordSocketClient DiscordClient { get; set; }

        private IrcConnection IrcConnection { get; set; }

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
            IrcConnection.MessageReceived += Irc_ChannelMessageReceived;
        }

        public void SendIrcMessage(string msg)
        {
            IrcConnection.SendMessage(IrcConnection.Channel, msg);
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
            else if (message.Content == "!online")
            {
                var users = IrcConnection.GetOnlineUsers();
                await message.Channel.SendMessageAsync("Users in IRC: " + string.Join(", ", users.Select(x => x.User.NickName)));
            }
            else
            {
                var lines = message.Content.Split('\n').Select(x => x.Trim()).ToArray();                
                for (int i = 0; i < Math.Min(lines.Length, 4); i++)
                {
                    var formattedMessage = $"<{message.Author.Username}> {lines[i]}";                    
                    foreach (var item in message.Tags)
                    {
                        switch (item.Type)
                        {
                            case TagType.ChannelMention:
                                formattedMessage = formattedMessage.Replace("<#" + item.Key + ">", "(#" + item.Value + ")");
                                break;                 
                            case TagType.RoleMention:
                                formattedMessage = formattedMessage.Replace("<@&" + item.Key + ">", "(@" + item.Value + ")");
                                break;
                            case TagType.UserMention:
                                formattedMessage = formattedMessage.Replace("<@" + item.Key + ">", "(@" + item.Value + ")");
                                break;
                        }
                        
                    }
                    SendIrcMessage(formattedMessage);
                    foreach (var item in message.Attachments)
                    {
                        SendIrcMessage($"* {message.Author.Username} attached file '{item.Filename}': {item.Url}");
                    }
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
            if (!e.Targets.Any(x => x.Name == IrcConnection.Channel) || e.Source.Name == IrcConnection.Nick)
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
                SendDiscordMessage($"**{e.Source}** {e.Text.Substring(8, e.Text.Length - 9)}").Wait();
            }
            else
            {
                SendDiscordMessage($"<**{e.Source}**> {e.Text}").Wait();
            }
        }
        #endregion
    }
}
