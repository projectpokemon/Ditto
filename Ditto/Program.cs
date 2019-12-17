using Azure.Storage.Blobs;
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
        private static bool WriteToConsole;

        public static async Task Main(string[] args)
        {
            var config = JsonConvert.DeserializeObject<AppSettings>("appsettings.json");
            WriteToConsole = !args.Contains("noprompt");
            try
            {
                if (WriteToConsole) Console.WriteLine("Starting...");
                Pairs = new List<ChannelPair>();

                if (!string.IsNullOrEmpty(config.BlobConnectionString))
                {
                    if (WriteToConsole) Console.WriteLine("Loading config from Azure");
                    await LoadFromBlobStorage(config.BlobConnectionString, config.BlobContainerName, config.BlobContainerFolder);
                }
                else
                {
                    if (WriteToConsole) Console.WriteLine("Loading config from disk");
                    await LoadFromDisk();
                }
                
                // Block until process is manually stopped
                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                if (WriteToConsole) Console.Write(ex.ToString());
                File.WriteAllText(DateTime.Now.ToString("error-yyyy-MM-dd_hh-mm-ss.txt"), ex.ToString());
                throw;
            }
        }

        private static async Task LoadFromDisk()
        {
            var discordFilenames = Directory.GetFiles(".", "*.discord.json");
            if (WriteToConsole) Console.WriteLine("Found " + discordFilenames.Length.ToString() + " Discord settings");
            foreach (var discordFilename in discordFilenames)
            {
                if (WriteToConsole) Console.WriteLine(discordFilename);
                var ircFilename = discordFilename.Replace(".discord.json", ".irc.json");
                if (File.Exists(ircFilename))
                {
                    if (WriteToConsole) Console.Write(ircFilename);
                    var discordInfo = JsonConvert.DeserializeObject<DiscordConnectionInfo>(File.ReadAllText(discordFilename));
                    var ircInfo = JsonConvert.DeserializeObject<IrcConnectionInfo>(File.ReadAllText(ircFilename));

                    await LoadChannel(discordInfo, ircInfo);
                }
                else
                {
                    if (WriteToConsole) Console.Write("Did not find IRC settings. Not creating pair.");
                }
            }
        }

        private static async Task LoadFromBlobStorage(string connectionString, string containerName, string containerFolder)
        {
            var client = new BlobContainerClient(connectionString, containerName);
            var blobNames = client.GetBlobs()
                .Where(b => b.Name?.StartsWith(containerFolder + "/") ?? false)
                .Select(b => b.Name)
                .ToList();
            foreach (var discordBlobName in blobNames.Where(b => b.EndsWith(".discord.json", StringComparison.OrdinalIgnoreCase)))
            {
                if (WriteToConsole) Console.WriteLine(discordBlobName);
                var ircBlobName = discordBlobName.Replace(".discord.json", ".irc.json");
                if (blobNames.Contains(ircBlobName, StringComparer.OrdinalIgnoreCase))
                {
                    var discordBlobContent = DeserializeStream<DiscordConnectionInfo>((await new BlobClient(connectionString, containerName, discordBlobName).DownloadAsync()).Value.Content);
                    var ircBlobContent = DeserializeStream<IrcConnectionInfo>((await new BlobClient(connectionString, containerName, ircBlobName).DownloadAsync()).Value.Content);
                   
                    await LoadChannel(discordBlobContent, ircBlobContent);
                }
                else
                {
                    if (WriteToConsole) Console.Write("Did not find IRC settings. Not creating pair.");
                }
            }
        }

        private static T DeserializeStream<T>(Stream stream)
        {
            var serializer = new JsonSerializer();
            using var sr = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(sr);
            return serializer.Deserialize<T>(jsonTextReader);
        }

        private static async Task LoadChannel(DiscordConnectionInfo discordInfo, IrcConnectionInfo ircInfo) 
        {
            var pair = new ChannelPair(new IrcConnection(ircInfo) { EnableConsoleLogging = WriteToConsole }, discordInfo)
            {
                EnableConsoleLogging = WriteToConsole
            };
            if (WriteToConsole) Console.WriteLine("Connecting...");
            await pair.Connect();
            if (WriteToConsole) Console.WriteLine("Ready.");
            Pairs.Add(pair);
        }
    }
}
