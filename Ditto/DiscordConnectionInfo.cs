using System;
using System.Collections.Generic;
using System.Text;

namespace Ditto
{
    public class DiscordConnectionInfo
    {
        /// <summary>
        /// Access token of the bot to connect with
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// ID of the guild to listen to
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// ID of the channel to listen to
        /// </summary>
        public ulong ChannelId { get; set; }
    }
}
