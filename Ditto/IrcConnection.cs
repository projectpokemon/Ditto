using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ditto
{
    public class IrcConnection
    {
        // Combine connections from different channel pairs whenever possible, because a nick can only be used once per server
        // Key: {server}:{nick}
        private static Dictionary<string, StandardIrcClient> ConnectionPool = new Dictionary<string, StandardIrcClient>();

        public IrcConnection(IrcConnectionInfo ircConnectionInfo)
        {
            IrcConnectionInfo = ircConnectionInfo;

            var connectionName = $"{ircConnectionInfo.Server}:{ircConnectionInfo.Nick}";
            if (!ConnectionPool.ContainsKey(connectionName))
            {
                IrcClient = new StandardIrcClient();
                IrcClient.RawMessageReceived += Irc_RawMessageReceived;
                IrcClient.ErrorMessageReceived += Irc_ErrorMessageReceived;
                IrcClient.Connected += Irc_Connected;
                IrcClient.ConnectFailed += Irc_ConnectFailed;
                IrcClient.MotdReceived += Irc_MotdReceived;
                IrcClient.Connect(ircConnectionInfo.Server, ircConnectionInfo.Port, false, ircConnectionInfo.GetRegistrationInfo());

                // Wait for event handlers to authenticate and join the channel
                Thread.Sleep(30 * 1000);

                ConnectionPool.Add(connectionName, IrcClient);
            }
            else
            {
                IrcClient = ConnectionPool[connectionName];
                IrcClient.LocalUser.JoinedChannel += Irc_JoinedChannel;
                JoinIrcChannel();
            }
        }

        public event EventHandler<IrcMessageEventArgs> MessageReceived;

        private StandardIrcClient IrcClient { get; set; }
        private IrcConnectionInfo IrcConnectionInfo { get; set; }
        public bool EnableConsoleLogging { get; set; } = true;

        public string Nick
        {
            get
            {
                return IrcClient.LocalUser.NickName;
            }
        }

        public string Channel
        {
            get
            {
                return IrcConnectionInfo.Channel;
            }
        }

        public IrcChannelUserCollection GetOnlineUsers()
        {
            return IrcClient.Channels.First(x => x.Name == Channel).Users;
        }

        public void SendMessage(string channel, string msg)
        {
            IrcClient.LocalUser.SendMessage(channel, msg);
        }

        private void Irc_ChannelMessageReceived(object sender, IrcMessageEventArgs e)
        {
            MessageReceived?.Invoke(sender, e);
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
            // Only rejoin if the channel left corresponds to this IrcConnection instance.
            // This class may have a StandardIrcClient that's shared with another IrcConnection that handles another channel.
            if (e.Channel.Name == IrcConnectionInfo.Channel)
            {
                JoinIrcChannel();
            }
            
        }

        private void Irc_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            if (EnableConsoleLogging) Console.WriteLine("Connect failed.");
            if (EnableConsoleLogging) Console.WriteLine(e.Error);
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
    }
}
