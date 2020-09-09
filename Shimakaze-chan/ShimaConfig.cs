using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shimakaze
{
    public struct SettingsConfig
    {
        public bool isTest;
        public bool sendToDebugRoom;
        public string defaultPrefix;
        public string testPrefix;
        public string liveToken;
        public string testToken;
        public string oauth;
        public ulong debugRoom;
        public List<ulong> defaultTestDebugServers;
    }

    public struct DatabaseConfig
    {
        public string host;
        public int port;
        public string name;
        public string username;
        public string password;
    }

    public struct LavalinkConfig
    {
        public string host;
        public int port;
        public string password;
    }

    public struct AudioPaths
    {
        public string lovelive;
        public string idolmaster;
        public string teamspeak;
    }
    public struct ShimaConfig
    {
        public SettingsConfig settings;
        public DatabaseConfig database;
        public LavalinkConfig lavalink;
        public AudioPaths audioPaths;

        public static ShimaConfig LoadConfig()
        {
            try
            {
                StreamReader streamReader = File.OpenText("config.json");
                string json = streamReader.ReadToEnd();
                if (json.Length < 1)
                {
                    return new ShimaConfig();
                }

                ShimaConfig config = JsonConvert.DeserializeObject<ShimaConfig>(json);
                if (config.settings.isTest)
                {
                    ShimakazeBot.DefaultPrefix = config.settings.testPrefix;
                    ShimakazeBot.guildDebugMode = config.settings.defaultTestDebugServers;
                }
                else
                {
                    ShimakazeBot.DefaultPrefix = config.settings.defaultPrefix;
                }

                return config;
            }
            catch (FileNotFoundException e)
            {
                throw new FileNotFoundException(
                    $"Couldn't find config file at {e.FileName}");
            }
        }
    }
}
