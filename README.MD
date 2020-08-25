# Ditto
.Net Core application to mirror messages between IRC and Discord.

## Projects

* Ditto - the main application
* IrcDotNet - Slightly customized version of [IrcDotNet](https://github.com/IrcDotNet/IrcDotNet).

## Usage
1. Create the json settings files. You need two of them that deserialize into each of these classes:
	- [https://github.com/projectpokemon/Ditto/blob/master/Ditto/DiscordConnectionInfo.cs](DiscordConnectionInfo)
    - [https://github.com/projectpokemon/Ditto/blob/master/Ditto/IrcConnectionInfo.cs](IrcConnectionInfo)
	You can create as many of these as you want, and you can link them by naming them accordingly. For example, Server1.discord.json and Server1.irc.json are a single linked pair, and Server2.discord.json and Server2.irc.json are another linked pair.
2. Place them in the working directory
3. Run Ditto.
