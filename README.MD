# Ditto
.Net Core application to mirror messages between IRC and Discord.

## Projects

* Ditto - the main application
* IrcDotNet - Slightly customized version of [IrcDotNet](https://github.com/IrcDotNet/IrcDotNet).
* Watchog - Starts Ditto and watches to make sure it's online, restarting it if necessary. This is needed because on some systems, Ditto dies so hard its error handling doesn't kick in, whenever there's a Discord outage for any length.

## Usage
1. Create the json settings files. Instructions coming eventually.
2. Run startup.sh
3. Run shutdown.sh to shutdown
