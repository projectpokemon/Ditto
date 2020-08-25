using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ditto
{
    public class IrcConnectionInfo
    {
        public IrcConnectionInfo()
        {
            Port = 6667;
        }

        public string Server { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public string Channel { get; set; }
        public string ChannelPassword { get; set; }
        public string Nick { get; set; }
        public string Usernamne { get; set; }
        public string RealName { get; set; }
        public string UserPassword { get; set; }

        public Uri GetUri()
        {
            return new Uri($"irc://{Server}:{Port}/{Channel.TrimStart('#')}");
        }

        public IrcUserRegistrationInfo GetRegistrationInfo()
        {
            return new IrcUserRegistrationInfo()
            {
                NickName = this.Nick,
                UserName = this.Usernamne,
                Password = this.UserPassword,
                RealName = this.RealName
            };
        }
    }
}
