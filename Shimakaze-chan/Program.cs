using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Shimakaze
{
    //lets use singletons because haHAA
    //but first the struct
    // Visco: Useless since we can use lavalink internal stuff
    // nice lie ^
    
    public struct SongRequest
    {
        public string requester;
        public LavalinkTrack track;
        //public Guid unique;

        public SongRequest(string requester, LavalinkTrack track)
        {
            this.requester = requester;
            this.track = track;
            //unique = Guid.NewGuid();
        }
    }

    public class GuildPlayer
    {
        public List<SongRequest> playlist;
        public bool isPaused;

        public GuildPlayer()
        {
            playlist = new List<SongRequest>();
            isPaused = false;
        }
    }

    public static class ShimakazeBot
    {
        public static DiscordClient Client;
        public static ShimaContext DbCtx;
        public static Dictionary<ulong, string> CustomPrefixes = new Dictionary<ulong, string>();
        public static string DefaultPrefix = "!";

        public static LavalinkNodeConnection lvn;
        public static Dictionary<DiscordGuild, GuildPlayer> musicLists = new Dictionary<DiscordGuild, GuildPlayer>();

        public static List<ulong> guildDebugMode = new List<ulong>();
        public static bool shouldSendToDebugRoom = true;

        public static void FetchPrefixes()
        {
            var prefixes = DbCtx.GuildPrefix.ToList();
            prefixes.ForEach(g => CustomPrefixes.Add(g.GuildId, g.Prefix));
        }

        public static bool CheckDebugMode(ulong guildId)
        {
            return guildDebugMode.Contains(guildId);
        }

        public static string AddWithDebug(string text, CommandContext ctx, bool condition)
        {
            return (condition && guildDebugMode.Contains(ctx.Guild.Id)) ? (text + "\n") : "";
        }

        public async static void SendToDebugRoom(string text)
        {
            var channel = await Client.GetChannelAsync(421236403515686913);
            await channel.SendMessageAsync(text, true);
        }
    }

    public class Commands : BaseCommandModule
    {
        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            ShimakazeBot.Client.DebugLogger.LogMessage(
                LogLevel.Info,
                "DSharpPlus",
                $"Executing {ctx.Message.Content} from {ctx.User.Username} in {ctx.Guild.Name}",
                ctx.Message.Timestamp.DateTime);
            return Task.CompletedTask;
        }
       
        [Command("cprefix")]
        [Description("Changes the prefix.")]
        public async Task CustomizePrefix(CommandContext ctx, [RemainingText] string newPrefix)
        {
            if (ctx.Member != ctx.Guild.Owner)
            {
                await ctx.RespondAsync("Only the server owner can change the prefix.");
                return;
            }

            if (ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) || newPrefix == ShimakazeBot.DefaultPrefix)
            {
                if (string.IsNullOrWhiteSpace(newPrefix) || newPrefix == ShimakazeBot.DefaultPrefix)
                {
                    ShimakazeBot.CustomPrefixes.Remove(ctx.Guild.Id);
                    ShimakazeBot.DbCtx.GuildPrefix.RemoveRange(ShimakazeBot.DbCtx.GuildPrefix.Where(p => p.GuildId == ctx.Guild.Id));
                    ShimakazeBot.DbCtx.SaveChanges();
                    await ctx.RespondAsync($"Prefix reset to default: **{ShimakazeBot.DefaultPrefix}**");
                }
                else
                {
                    ShimakazeBot.CustomPrefixes[ctx.Guild.Id] = newPrefix;
                    var guildPrefix = ShimakazeBot.DbCtx.GuildPrefix.First(p => p.GuildId == ctx.Guild.Id);
                    guildPrefix.Prefix = newPrefix;
                    ShimakazeBot.DbCtx.GuildPrefix.Update(guildPrefix);
                    ShimakazeBot.DbCtx.SaveChanges();
                    await ctx.RespondAsync($"Prefix updated to: **{newPrefix}**");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(newPrefix))
                {
                    await ctx.RespondAsync("Default prefix is: **" + ShimakazeBot.DefaultPrefix + "**\nProvide a prefix to change it.");
                }
                else
                {
                    ShimakazeBot.CustomPrefixes.Add(ctx.Guild.Id, newPrefix);
                    ShimakazeBot.DbCtx.GuildPrefix.Add(new GuildPrefix {GuildId = ctx.Guild.Id, Prefix = newPrefix});
                    ShimakazeBot.DbCtx.SaveChanges();
                    await ctx.RespondAsync("Prefix updated to: **" + newPrefix + "**");
                }
            }
        }

        [Command("debug")]
        [Aliases("supadebug", "sd", "スーパーデバッグモード")]
        public async Task DebugMode(CommandContext ctx)
        {
            bool toRemove = ShimakazeBot.guildDebugMode.Contains(ctx.Guild.Id);
            if (toRemove) ShimakazeBot.guildDebugMode.Remove(ctx.Guild.Id);
            else ShimakazeBot.guildDebugMode.Add(ctx.Guild.Id);

            await ctx.RespondAsync("スーパーデバッグモード" + (!toRemove ? " **enabled** for " : " **disabled** for ") + ctx.Guild.Name + " (" + ctx.Guild.Id + ")");
        }

        [Command("debugchannel")]
        [Aliases("debugflag")]
        public async Task DebugChannelFlag(CommandContext ctx)
        {
            ShimakazeBot.shouldSendToDebugRoom = !ShimakazeBot.shouldSendToDebugRoom;
            await ctx.RespondAsync("Shima debug channel " + (ShimakazeBot.shouldSendToDebugRoom ? " **enabled**" : "**disabled**"));
        }

        [Command("info")]
        [Description("Quick tooltip regarding the purpose of this bot.")]
        public async Task DisplayInfo(CommandContext ctx)
        {
            await ctx.RespondAsync("This bot serves as a temporary hotfix compliment to the original Shimakaze's broken voicechat functions." +
                " We will notify You when we're done with rewriting mainline Shimakaze and bring all of her functionality back up. At least " + 
                "the parts that were actively used, we will leave out some useless shit like cleverbot.");
        }

        [Command("join")]
        [Aliases("j", "s", "join-voice")]
        [Description("Joins the voice channel you\'re in.")]
        public async Task Join(CommandContext ctx)
        {
            string debugResponse = "";

            //db shit first
            var existingJoin = ShimakazeBot.DbCtx.GuildJoin.FirstOrDefault(p => p.GuildId == ctx.Guild.Id);
            if (existingJoin == null)
            {
                ShimakazeBot.DbCtx.GuildJoin.Add(new GuildJoin { GuildId = ctx.Guild.Id, ChannelId = ctx.Channel.Id });
            }
            else
            {
                existingJoin.ChannelId = ctx.Channel.Id;
                ShimakazeBot.DbCtx.GuildJoin.Update(existingJoin);
            }
            ShimakazeBot.DbCtx.SaveChanges();

            var lv = ctx.Client.GetLavalink();
            debugResponse += ShimakazeBot.AddWithDebug("Unable to get LavaLink", ctx, lv == null);
            if (ShimakazeBot.lvn == null)
                try
                {
                    ShimakazeBot.lvn = await lv.ConnectAsync(new LavalinkConfiguration
                    {
                        Password = "mcdonalds",
                        SocketEndpoint = new ConnectionEndpoint("localhost", 2333),
                        RestEndpoint = new ConnectionEndpoint("localhost", 2333)

                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await ctx.RespondAsync(e.ToString());
                }

            var lvc = ShimakazeBot.lvn.GetConnection(ctx.Guild);
            debugResponse += ShimakazeBot.AddWithDebug("LVC status: " +
                (lvc != null).ToString(), ctx, true);
            if (lvc != null)
            {

                await ctx.RespondAsync(debugResponse + "Already connected in this guild.");
                return;
            }

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync(debugResponse + "You need to be in a voice channel.");
                return;
            }

            try
            {
                if (ShimakazeBot.CheckDebugMode(ctx.Guild.Id)) await ctx.RespondAsync(
                    "attempting connectAsync to " + chn.Name);
                await ShimakazeBot.lvn.ConnectAsync(chn).ConfigureAwait(false);
                debugResponse += ShimakazeBot.AddWithDebug("Connection status after: " +
                    (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null).ToString(), ctx, true);
                if (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null)
                {
                    debugResponse += ShimakazeBot.AddWithDebug("  - Is connected: " +
                        ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected.ToString() + 
                        "\n  - Channel: " +
                        ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Name, ctx, true);
                }
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(debugResponse + e.ToString());
                throw;
            }

            if (!ShimakazeBot.musicLists.ContainsKey(ctx.Guild))
            {
                ShimakazeBot.musicLists.Add(ctx.Guild, new GuildPlayer());
            }
            ShimakazeBot.lvn.GetConnection(ctx.Guild).PlaybackFinished += PlayNextTrack;


            await ctx.RespondAsync(debugResponse + "Joined");
        }

        [Command("leave")]
        [Aliases("leave-voice")]
        [Description("Leaves the voice channel.")]
        public async Task Leave(CommandContext ctx)
        {

            string debugResponse = "";

            var lv = ctx.Client.GetLavalink();

            debugResponse += ShimakazeBot.AddWithDebug("Unable to get LavaLink", ctx, lv == null);

            var lvc = ShimakazeBot.lvn.GetConnection(ctx.Guild);
            if (ShimakazeBot.lvn == null || lvc == null)
            {
                await ctx.RespondAsync(debugResponse + "Not connected in this guild.");
                return;
            }

            debugResponse += ShimakazeBot.AddWithDebug("Connection state before: " +
                "\n  - Is connected: " +
                ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected.ToString() +
                "\n  - Channel: " +
                ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Name, ctx, true);

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync(debugResponse + "You need to be in a voice channel.");
                return;
            }
            if (chn != lvc.Channel)
            {
                await ctx.RespondAsync(debugResponse + "You need to be in the same voice channel.");
                return;
            }

            ShimakazeBot.musicLists.Remove(ctx.Guild);
            lvc.PlaybackFinished -= PlayNextTrack;
            await lvc.StopAsync();
            await lvc.DisconnectAsync();

            if (ShimakazeBot.CheckDebugMode(ctx.Guild.Id))
            {
                if (lvc != null || ShimakazeBot.lvn != null)
                {
                    debugResponse += "lvn node endpoint after: " +
                        ShimakazeBot.lvn.NodeEndpoint +
                        "\nConnection state after: " +
                        (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null).ToString() + "\n";

                    if (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null)
                    {
                        debugResponse += "\n  - Is connected: " +
                            ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected.ToString() +
                            "\n  - Channel: " +
                            ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Name;
                    }
                }
                await ctx.RespondAsync(debugResponse + "Attempted leave.");
            }
        }

        [Command("list")]
        [Aliases("l", "playlist")]
        [Description("Displays the playlist.")]
        public async Task List(CommandContext ctx)
        {
            var lvc = ShimakazeBot.lvn.GetConnection(ctx.Guild);
            if (lvc == null || !ShimakazeBot.musicLists.ContainsKey(ctx.Guild))
            {
                await ctx.RespondAsync("No playlist. Try making Shima join voice first.");

            }
            else
            {
                if (ShimakazeBot.musicLists[ctx.Guild].playlist.Count > 0)
                {
                    int i = 0;
                    string msg = "";
                    foreach (var req in ShimakazeBot.musicLists[ctx.Guild].playlist)
                    {
                        if (i == 0)
                        {
                            msg += ShimakazeBot.musicLists[ctx.Guild].isPaused ? "***PAUSED*** " : "Now playing ";
                        }
                        else if (i > 10)
                        {
                            msg += "\n*And " + (ShimakazeBot.musicLists[ctx.Guild].playlist.Count - 11) + " more...*";
                            break;
                        }
                        else
                        {
                            msg += "\n" + i + ". ";
                        }
                        msg += "**" + req.track.Title + "** Requested by *" + req.requester + "*";
                        i++;
                    }

                    await ctx.RespondAsync(msg);
                }
                else
                {
                    await ctx.RespondAsync("Playlist is empty.");
                }
            }

        }

        [Command("pause")]
        [Aliases("resume", "playpause")]
        [Description("Pauses or resumes the music playback.")]
        public async Task Pause(CommandContext ctx)
        {

            var lavaConnection = ShimakazeBot.lvn.GetConnection(ctx.Guild);

            if (lavaConnection == null)
            {
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel.");
                return;
            }
            if (chn != lavaConnection.Channel)
            {
                await ctx.RespondAsync("You need to be in the same voice channel.");
                return;
            }

            if (ShimakazeBot.musicLists[ctx.Guild].isPaused)
            {
                await lavaConnection.ResumeAsync();
                ShimakazeBot.musicLists[ctx.Guild].isPaused = false;
                await ctx.RespondAsync("Music resumed.");
            }
            else
            {
                await lavaConnection.PauseAsync();
                ShimakazeBot.musicLists[ctx.Guild].isPaused = true;
                await ctx.RespondAsync("Music paused.");
            }
        }

        [Command("play")]
        [Aliases("p", "r", "req", "request")]
        [Description("Plays the requested link or youtube search.")]
        public async Task Play(CommandContext ctx, [RemainingText] string songName)
        {
           
            LavalinkTrack track;
            LavalinkLoadResult lavalinkLoadResult;
            var lavaConnection = ShimakazeBot.lvn.GetConnection(ctx.Guild);

            if (lavaConnection == null)
            {
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel.");
                return;
            }
            if (chn != lavaConnection.Channel)
            {
                await ctx.RespondAsync("You need to be in the same voice channel.");
                return;
            }

            if (string.IsNullOrWhiteSpace(songName) && ShimakazeBot.musicLists[ctx.Guild].isPaused)
            {
                await lavaConnection.ResumeAsync();
                ShimakazeBot.musicLists[ctx.Guild].isPaused = false;
                await ctx.RespondAsync("Music resumed.");
                return;
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), songName);
            
            lavalinkLoadResult = songName.StartsWith("http")
                    ? await ShimakazeBot.lvn.Rest.GetTracksAsync(new Uri(songName))
                    : await ShimakazeBot.lvn.Rest.GetTracksAsync(songName);

            switch (lavalinkLoadResult.LoadResultType)
            {
                case LavalinkLoadResultType.SearchResult:
                case LavalinkLoadResultType.TrackLoaded:
                    track = lavalinkLoadResult.Tracks.First();
                    ShimakazeBot.musicLists[ctx.Guild].playlist.Add(new SongRequest(ctx.Member.Nickname, track));
                    break;
                case LavalinkLoadResultType.PlaylistLoaded:
                    ShimakazeBot.musicLists[ctx.Guild].playlist.AddRange(lavalinkLoadResult.Tracks.Select(t => new SongRequest(ctx.Member.Nickname, t)));
                    break;
                case LavalinkLoadResultType.NoMatches:
                    await ctx.RespondAsync("No matches found.");
                    break;
                case LavalinkLoadResultType.LoadFailed:
                    await ctx.RespondAsync("Load failed.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (lavaConnection.CurrentState.CurrentTrack == null)
            {
                await lavaConnection.PlayAsync(ShimakazeBot.musicLists[ctx.Guild].playlist.First().track);
                await ctx.RespondAsync($"Playing **{ShimakazeBot.musicLists[ctx.Guild].playlist.First().track.Title}** Requested by *{ShimakazeBot.musicLists[ctx.Guild].playlist.First().requester}*");
            }
            else
            {
                await ctx.RespondAsync($"Added **{lavalinkLoadResult.Tracks.First().Title}** to the queue. Requested by *{ctx.Member.Nickname}*");
            }
        }

        [Command("prefix")]
        [Description("Displays the current prefix, if you\'re that confused.")]
        public async Task DisplayPrefix(CommandContext ctx)
        {
            await ctx.RespondAsync("This server\'s prefix is: **" + 
                             (ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) ? ShimakazeBot.CustomPrefixes[ctx.Guild.Id] : ShimakazeBot.DefaultPrefix) +
                             "**\n You can change the prefix with **cprefix**");
        }

        [Command("skip")]
        [Description("Skips the current song in the playlist.")]
        public async Task Skip(CommandContext ctx)
        {
            var lavaConnection = ShimakazeBot.lvn.GetConnection(ctx.Guild);

            if (lavaConnection == null)
            {
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel.");
                return;
            }
            if (chn != lavaConnection.Channel)
            {
                await ctx.RespondAsync("You need to be in the same voice channel.");
                return;
            }

            if (ShimakazeBot.musicLists[ctx.Guild].playlist.Count == 0)
            {
                await ctx.RespondAsync("Playlist is empty.");
                return;
            }

            string title = ShimakazeBot.musicLists[ctx.Guild].playlist[0].track.Title;
            ShimakazeBot.musicLists[ctx.Guild].playlist.RemoveAt(0);

            if (ShimakazeBot.musicLists[ctx.Guild].playlist.Count > 0)
            {
                await lavaConnection.PlayAsync(ShimakazeBot.musicLists[ctx.Guild].playlist.First().track);
                await ctx.RespondAsync("Skipped *" + title +"*.");
            }
            else
            {
                await lavaConnection.StopAsync();
                await ctx.RespondAsync("Playlist ended with skip. (Skipped *" + title + "*)");
            }
        }

        [Command("status")]
        public async Task Status(CommandContext ctx)
        {
            string responseString = "⛺\n";


            responseString += "**LavalinkNodeConnection status**\n";

            responseString += "  - connected: " + (ShimakazeBot.lvn != null ?
                ShimakazeBot.lvn.IsConnected.ToString() : "false") + "\n";

            if (ShimakazeBot.lvn != null)
            {
                responseString += "  - node endpoint: " + ShimakazeBot.lvn.NodeEndpoint + "\n";


                responseString += "**LavalinkGuildConnection status**\n";

                responseString += "  - connected: " +
                    (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null ?
                    ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected.ToString() :
                    "**GetConnection Failed**") + "\n";

                if (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null &&
                    ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected)
                {
                    responseString += "  - channel: " +
                        (ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel != null ?
                        ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Name +
                        " (*" + ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Id + "*)" :
                        "**NULL**") + "\n";

                    responseString += "  - current state: ";
                    if (ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState != null)
                    {
                        responseString += "\n";
                        responseString += "        - track: " +
                            (ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState.CurrentTrack != null ?
                            ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState.CurrentTrack.Title :
                            "**not playing**") + "\n";

                        responseString += "        - position: " +
                            ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState.PlaybackPosition.ToString() + "\n";
                        
                        responseString += "        - last update: " +
                            ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState.LastUpdate.ToString() + "\n";


                    }
                    else responseString += "**NULL**";
                }
            }

            responseString += "**Playlist status**\n";
            responseString += "  - length: " + ShimakazeBot.musicLists.Count() + "\n";
            responseString += "  - guild length: " +
                (ShimakazeBot.musicLists.ContainsKey(ctx.Guild) ?
                ShimakazeBot.musicLists[ctx.Guild].playlist.Count().ToString() :
                "**no playlist**");





            await ctx.RespondAsync(responseString);
        }

        private Task PlayNextTrack(TrackFinishEventArgs e)
        {
            
            if (e.Reason != TrackEndReason.Finished)
                return Task.CompletedTask;

            ShimakazeBot.musicLists[e.Player.Guild].playlist.RemoveAt(0);
            ShimakazeBot.Client.DebugLogger.LogMessage(LogLevel.Info, "DSharpPlus", e.Handled + ShimakazeBot.Client.VersionString, DateTime.Now);

            if (ShimakazeBot.musicLists[e.Player.Guild].playlist.Count > 0)
            {
                e.Player.PlayAsync(ShimakazeBot.musicLists[e.Player.Guild].playlist.First().track);
            }


            return Task.CompletedTask;
        }
    }

    class Program
    {
        static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            ShimakazeBot.Client = new DiscordClient(new DiscordConfiguration
            {
                // Token = "NDc2MTUxMjIwMDA0OTc4Njg5.DnXuqg.ANWX8zmMBLU5U7XLI9ZA-8E0nRQ", //test
                Token = "NjQyNDc4MDIzOTQ5NjgwNjYx.XtpHoA.P6U_GOWwkYOBML1lUCM5whbTN9s", //voice
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            
            ShimakazeBot.DbCtx = new ShimaContext();

            ShimakazeBot.DbCtx.Database.Migrate();

            ShimakazeBot.FetchPrefixes();

            CommandsNextConfiguration commandConfig = new CommandsNextConfiguration
            {
                PrefixResolver = (msg) =>
                {
                    return Task.Run(() =>
                    {
                        var guild = msg.Channel.Guild;
                        return msg.GetStringPrefixLength(ShimakazeBot.CustomPrefixes.ContainsKey(guild.Id)
                            ? ShimakazeBot.CustomPrefixes[guild.Id]
                            : ShimakazeBot.DefaultPrefix);
                    });
                }
            };


            ShimakazeBot.Client.UseCommandsNext(commandConfig).RegisterCommands<Commands>();

            ShimakazeBot.Client.UseLavalink();

            await ShimakazeBot.Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
