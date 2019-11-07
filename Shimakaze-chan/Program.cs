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
using System.Threading;
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
        public static CommandsNextConfiguration CommandConfig = new CommandsNextConfiguration();
        public static Dictionary<ulong, string> CustomPrefixes = new Dictionary<ulong, string>();
        public static string DefaultPrefix = "!";

        public static LavalinkNodeConnection lvn;
        public static Dictionary<DiscordGuild, GuildPlayer> musicLists = new Dictionary<DiscordGuild, GuildPlayer>();

        public static void FetchPrefixes()
        {
            var prefixes = DbCtx.GuildPrefix.ToList();
            prefixes.ForEach(g => CustomPrefixes.Add(g.GuildId, g.Prefix));
        }

        public static void DumpPrefixes()
        {
            //TODO: SLAVE VISCO SLAVE

            CommandConfig.StringPrefixes = CustomPrefixes.Values.ToArray().Prepend(DefaultPrefix);
        }
    }

    public class Commands : BaseCommandModule
    {
        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            CancellationToken token = new CancellationToken(true);
            Task cancelledTask = Task.FromCanceled(token);

            if (ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id))
            {
                if (ctx.Prefix != ShimakazeBot.CustomPrefixes[ctx.Guild.Id])
                {
                    //return cancelledTask; 
                }
            }
            else if (ctx.Prefix != ShimakazeBot.DefaultPrefix)
            {
                //return cancelledTask;
            }
            return Task.CompletedTask; 
        }

        [Command("prefix")]
        public async Task DisplayPrefix(CommandContext ctx)
        {
            await ctx.RespondAsync("This server\'s prefix is: **" + 
                             (ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) ? ShimakazeBot.CustomPrefixes[ctx.Guild.Id] : ShimakazeBot.DefaultPrefix) +
                             "**\n You can change the prefix with **cprefix**");
        }

        [Command("cprefix")]
        public async Task CustomizePrefix(CommandContext ctx, [RemainingText] string newPrefix)
        {
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
            // TODO: move db stuff here maybe
            // ShimakazeBot.DumpPrefixes();
        }

        //todo: also seems that join > play > leave > join > play doesn't play
        [Command("j")]
        public async Task Join(CommandContext ctx)
        {
            var lv = ctx.Client.GetLavalink();
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
            if (lvc != null)
            {
                await ctx.RespondAsync("Already connected in this guild.");
                return;
            }

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel.");
                return;
            }

            try
            {
                await ShimakazeBot.lvn.ConnectAsync(chn).ConfigureAwait(false);
               
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(e.ToString());
                throw;
            }

            if (!ShimakazeBot.musicLists.ContainsKey(ctx.Guild))
            {
                ShimakazeBot.musicLists.Add(ctx.Guild, new GuildPlayer());
            }
            ShimakazeBot.lvn.GetConnection(ctx.Guild).PlaybackFinished += PlayNextTrack;

            await ctx.RespondAsync("Joined");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            ctx.Client.GetLavalink();

            var lvc = ShimakazeBot.lvn.GetConnection(ctx.Guild);
            if (ShimakazeBot.lvn == null || lvc == null)
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
            if (chn != lvc.Channel)
            {
                await ctx.RespondAsync("You need to be in the same voice channel.");
                return;
            }

            ShimakazeBot.musicLists.Remove(ctx.Guild);
            lvc.PlaybackFinished -= PlayNextTrack;
            lvc.Stop();

            try
            {
                await ShimakazeBot.lvn.StopAsync();
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(e.ToString());
                throw;
            }
        }

        [Command("p")]
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
                //fock u vosco
                lavaConnection.Resume();
                ShimakazeBot.musicLists[ctx.Guild].isPaused = false;
                await ctx.RespondAsync("Music resumed.");
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), songName);

            lavalinkLoadResult = songName.StartsWith("http")
                    ? await ShimakazeBot.lvn.GetTracksAsync(new Uri(songName))
                    : await ShimakazeBot.lvn.GetTracksAsync(songName);

            await ctx.RespondAsync("👌");

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

            // == null for structs, can be overloaded
            // https://stackoverflow.com/a/15199135/8873517
            if (lavaConnection.CurrentState.CurrentTrack.Equals(default(LavalinkTrack)))
            {
                lavaConnection.Play(ShimakazeBot.musicLists[ctx.Guild].playlist.First().track);
            }
        }

        [Command("list")]
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
                lavaConnection.Resume();
                ShimakazeBot.musicLists[ctx.Guild].isPaused = false;
                await ctx.RespondAsync("Music resumed.");
            }
            else
            {
                lavaConnection.Pause();
                ShimakazeBot.musicLists[ctx.Guild].isPaused = true;
                await ctx.RespondAsync("Music paused.");
            }
        }

        [Command("skip")]
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
                lavaConnection.Play(ShimakazeBot.musicLists[ctx.Guild].playlist.First().track);
                await ctx.RespondAsync("Skipped *" + title +"*.");
            }
            else
            {
                lavaConnection.Stop();
                await ctx.RespondAsync("Playlist ended with skip. (Skipped *" + title + "*)");
            }
        }

        private Task PlayNextTrack(TrackFinishEventArgs e)
        {
            
            if (e.Reason != TrackEndReason.Finished)
                return Task.CompletedTask;

            ShimakazeBot.musicLists[e.Player.Guild].playlist.RemoveAt(0);
            ShimakazeBot.Client.DebugLogger.LogMessage(LogLevel.Info, "DSharpPlus", e.Handled + ShimakazeBot.Client.VersionString, DateTime.Now);

            if (ShimakazeBot.musicLists[e.Player.Guild].playlist.Count > 0)
            {
                e.Player.Play(ShimakazeBot.musicLists[e.Player.Guild].playlist.First().track);
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
                Token = "NDc2MTUxMjIwMDA0OTc4Njg5.DnXuqg.ANWX8zmMBLU5U7XLI9ZA-8E0nRQ",
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            
            ShimakazeBot.DbCtx = new ShimaContext();

            ShimakazeBot.FetchPrefixes();

            ShimakazeBot.CommandConfig.PrefixResolver = (msg) =>
            {
                ShimakazeBot.Client.DebugLogger.LogMessage(
                    LogLevel.Info,
                    "DSharpPlus",
                    $"Processing {msg.Content}",
                    DateTime.Now);
                return Task.Run(() =>
                {
                    var guild = msg.Channel.Guild;
                    return msg.GetStringPrefixLength(ShimakazeBot.CustomPrefixes.ContainsKey(guild.Id)
                        ? ShimakazeBot.CustomPrefixes[guild.Id]
                        : ShimakazeBot.DefaultPrefix);
                });
            };

            ShimakazeBot.Client.UseCommandsNext(ShimakazeBot.CommandConfig).RegisterCommands<Commands>();

            ShimakazeBot.Client.UseLavalink();

            await ShimakazeBot.Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}