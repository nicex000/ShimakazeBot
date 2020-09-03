using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze
{
    class VoiceCommands : Commands
    {
        [Command("join")]
        [Aliases("j", "s", "join-voice")]
        [Description("Joins the voice channel you\'re in.")]
        public async Task Join(CommandContext ctx)
        {
            DebugString debugResponse = new DebugString();

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
            debugResponse.AddWithDebug("Unable to get LavaLink", ctx, lv == null);
            if (ShimakazeBot.lvn == null)
                try
                {
                    ShimakazeBot.lvn = await lv.ConnectAsync(new LavalinkConfiguration
                    {
                        Password = ShimakazeBot.Config.lavalink.password,
                        SocketEndpoint = new ConnectionEndpoint(
                            ShimakazeBot.Config.lavalink.host,
                            ShimakazeBot.Config.lavalink.port),
                        RestEndpoint = new ConnectionEndpoint(
                            ShimakazeBot.Config.lavalink.host,
                            ShimakazeBot.Config.lavalink.port),

                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await ctx.RespondAsync(e.ToString());
                }

            var lvc = ShimakazeBot.lvn.GetConnection(ctx.Guild);
            debugResponse.AddWithDebug("LVC status: " + (lvc != null).ToString(), ctx);
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
                if (ShimakazeBot.CheckDebugMode(ctx.Guild.Id))
                {
                    await ctx.RespondAsync("attempting connectAsync to " + chn.Name);
                }
                await ShimakazeBot.lvn.ConnectAsync(chn).ConfigureAwait(false);
                debugResponse.AddWithDebug("Connection status after: " 
                    + (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null).ToString(), ctx);
                if (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null)
                {
                    debugResponse.AddWithDebug("  - Is connected: " +
                        ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected.ToString() +
                        "\n  - Channel: " +
                        ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Name, ctx);
                }
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(debugResponse + e.ToString());
                throw;
            }

            if (!ShimakazeBot.playlists.ContainsKey(ctx.Guild))
            {
                ShimakazeBot.playlists.Add(ctx.Guild, new GuildPlayer());
                debugResponse.AddWithDebug($"Playlist: **new** ({lvc != null})", ctx);
            }
            else
            {
                lvc = ShimakazeBot.lvn.GetConnection(ctx.Guild);
                await lvc.PlayAsync(ShimakazeBot.playlists[ctx.Guild].songRequests.First().track);
                debugResponse.AddWithDebug("Playlist: " +
                    $"**{ShimakazeBot.playlists[ctx.Guild].songRequests.Count} songs**", ctx);
            }
            ShimakazeBot.lvn.GetConnection(ctx.Guild).PlaybackFinished += PlayNextTrack;
            ShimakazeBot.lvn.GetConnection(ctx.Guild).DiscordWebSocketClosed += DiscordSocketClosed;


            await ctx.RespondAsync(debugResponse + "Joined");
        }

        [Command("leave")]
        [Aliases("leave-voice")]
        [Description("Leaves the voice channel.")]
        public async Task Leave(CommandContext ctx, [RemainingText] string remainingText)
        {

            DebugString debugResponse = new DebugString();

            var lv = ctx.Client.GetLavalink();

            debugResponse.AddWithDebug("Unable to get LavaLink", ctx, lv == null);

            var lvc = ShimakazeBot.lvn.GetConnection(ctx.Guild);
            if (ShimakazeBot.lvn == null || lvc == null)
            {
                await ctx.RespondAsync(debugResponse + "Not connected in this guild.");
                return;
            }

            debugResponse.AddWithDebug("Connection state before: " +
                "\n  - Is connected: " +
                ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected.ToString() +
                "\n  - Channel: " +
                ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Name, ctx);

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

            if (!string.IsNullOrWhiteSpace(remainingText))
            {
                ShimakazeBot.playlists.Remove(ctx.Guild);
                debugResponse.AddWithDebug("Playlist: **removed**", ctx);
            }
            else
            {
                debugResponse.AddWithDebug("Playlist: " +
                    (ShimakazeBot.playlists.ContainsKey(ctx.Guild) ?
                    $"**{ShimakazeBot.playlists[ctx.Guild].songRequests.Count} songs**" : $"**already removed**"),
                    ctx);
            }

            lvc.PlaybackFinished -= PlayNextTrack;
            lvc.DiscordWebSocketClosed -= DiscordSocketClosed;
            await lvc.StopAsync();
            await lvc.DisconnectAsync();

            if (ShimakazeBot.CheckDebugMode(ctx.Guild.Id))
            {
                if (lvc != null || ShimakazeBot.lvn != null)
                {
                    debugResponse.AddWithDebug("lvn node endpoint after: " +
                        ShimakazeBot.lvn.NodeEndpoint +
                        "\nConnection state after: " +
                        (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null).ToString(),
                        ctx);

                    debugResponse.AddWithDebug("\n  - Is connected: " +
                        ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected.ToString() +
                        "\n  - Channel: " +
                        ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Name,
                        ctx,
                        ShimakazeBot.lvn.GetConnection(ctx.Guild) != null); //condition
                }
                await ctx.RespondAsync(debugResponse + "Attempted leave.");
            }
        }

        [Command("list")]
        [Aliases("l", "playlist")]
        [Description("Displays the playlist.")]
        public async Task List(CommandContext ctx)
        {
            if (!ShimakazeBot.playlists.ContainsKey(ctx.Guild))
            {
                await ctx.RespondAsync("No playlist. Try making Shima join voice first.");
            }
            else
            {
                if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count > 0)
                {
                    int i = 0;
                    var lvc = ShimakazeBot.lvn.GetConnection(ctx.Guild);
                    string msg = "";
                    foreach (var req in ShimakazeBot.playlists[ctx.Guild].songRequests)
                    {
                        if (i == 0)
                        {
                            msg += lvc == null ? "Starting with " :
                                (ShimakazeBot.playlists[ctx.Guild].isPaused ? "***PAUSED*** " : "Now playing ");
                        }
                        else if (i > 10)
                        {
                            msg += "\n*And " + (ShimakazeBot.playlists[ctx.Guild].songRequests.Count - 11) + " more...*";
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

            if (ShimakazeBot.playlists[ctx.Guild].isPaused)
            {
                await lavaConnection.ResumeAsync();
                ShimakazeBot.playlists[ctx.Guild].isPaused = false;
                await ctx.RespondAsync("Music resumed.");
            }
            else
            {
                await lavaConnection.PauseAsync();
                ShimakazeBot.playlists[ctx.Guild].isPaused = true;
                await ctx.RespondAsync("Music paused.");
            }
        }

        [Command("clearplaylist")]
        [Aliases("clear", "clearlist", "clist", "cl")]
        [Description("Clears the playlist.")]
        public async Task ClearPlaylist(CommandContext ctx)
        {
            ShimakazeBot.playlists[ctx.Guild].songRequests = new List<SongRequest> 
            {
                ShimakazeBot.playlists[ctx.Guild].songRequests[0] 
            };
            await ctx.RespondAsync("Playlist cleared.");
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

            if (string.IsNullOrWhiteSpace(songName) && ShimakazeBot.playlists[ctx.Guild].isPaused)
            {
                await lavaConnection.ResumeAsync();
                ShimakazeBot.playlists[ctx.Guild].isPaused = false;
                await ctx.RespondAsync("Music resumed.");
                return;
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), songName);

            if (songName.StartsWith("local")) await ctx.RespondAsync(songName.Substring(songName.IndexOf("local ") + 6));

            lavalinkLoadResult = songName.StartsWith("http")
                ? await ShimakazeBot.lvn.Rest.GetTracksAsync(new Uri(songName))
                : songName.StartsWith("local") ?
                    await ShimakazeBot.lvn.Rest.GetTracksAsync(new FileInfo(songName.Substring(songName.IndexOf("local ")+6))) :
                    await ShimakazeBot.lvn.Rest.GetTracksAsync(songName);

            switch (lavalinkLoadResult.LoadResultType)
            {
                case LavalinkLoadResultType.SearchResult:
                case LavalinkLoadResultType.TrackLoaded:
                    track = lavalinkLoadResult.Tracks.First();
                    ShimakazeBot.playlists[ctx.Guild].songRequests.Add(new SongRequest(
                        ctx.Member.Nickname, ctx.Member, ctx.Channel, track));
                    break;
                case LavalinkLoadResultType.PlaylistLoaded:
                    ShimakazeBot.playlists[ctx.Guild].songRequests.AddRange(lavalinkLoadResult.Tracks.Select(t => new SongRequest(
                        ctx.Member.Nickname, ctx.Member, ctx.Channel, t)));
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
                await lavaConnection.PlayAsync(ShimakazeBot.playlists[ctx.Guild].songRequests.First().track);
                await ctx.RespondAsync($"Playing **{ShimakazeBot.playlists[ctx.Guild].songRequests.First().track.Title}** Requested by *{ShimakazeBot.playlists[ctx.Guild].songRequests.First().requester}*");
            }
            else
            {
                await ctx.RespondAsync($"Added **{lavalinkLoadResult.Tracks.First().Title}** to the queue. Requested by *{ctx.Member.Nickname}*");
            }
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

            if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count == 0)
            {
                await ctx.RespondAsync("Playlist is empty.");
                return;
            }

            string title = ShimakazeBot.playlists[ctx.Guild].songRequests[0].track.Title;
            ShimakazeBot.playlists[ctx.Guild].songRequests.RemoveAt(0);

            if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count > 0)
            {
                await lavaConnection.PlayAsync(ShimakazeBot.playlists[ctx.Guild].songRequests.First().track);
                await ctx.RespondAsync("Skipped *" + title + "*.");
            }
            else
            {
                await lavaConnection.StopAsync();
                await ctx.RespondAsync("Playlist ended with skip. (Skipped *" + title + "*)");
            }
        }

        

        private Task PlayNextTrack(TrackFinishEventArgs e)
        {
            if (ShimakazeBot.CheckDebugMode(e.Player.Guild.Id))
            {
                ShimakazeBot.Client.DebugLogger.LogMessage(LogLevel.Info,
                    LogMessageSources.PLAYLIST_NEXT_EVENT + " SupaDebug @" + e.Player.Guild.Name,
                    e.Reason.ToString(),
                    DateTime.Now);
            }

            if (e.Reason == TrackEndReason.Cleanup)
            {
                ShimakazeBot.SendToDebugRoom("Lavalink failed in **" +
                    e.Player.Guild.Name + "** (" + e.Player.Guild.Id + ") - **" +
                    e.Player.Channel.Name + "** (" + e.Player.Channel.Id + ") - song: " +
                    e.Track.Title);
                ShimakazeBot.Client.DebugLogger.LogMessage(LogLevel.Warning,
                    LogMessageSources.PLAYLIST_NEXT_EVENT,
                    e.Reason + " - playlist length at error: " +
                    ShimakazeBot.playlists[e.Player.Guild].songRequests.Count,
                    DateTime.Now);

                if (ShimakazeBot.playlists[e.Player.Guild].songRequests.Count > 0)
                {
                    ShimakazeBot.playlists[e.Player.Guild].songRequests[0].requestedChannel.SendMessageAsync(
                        ShimakazeBot.playlists[e.Player.Guild].songRequests[0].requestMember.Mention +
                        "Lavalink failed... Shima will leave the voice channel. Don't worry your playlist has been saved. You can make her rejoin." +
                        (ShimakazeBot.shouldSendToDebugRoom ? " The devs have been notified." : ""));
                    ForceLeave(e.Player.Guild);
                }


                return Task.CompletedTask;
            }
            if (e.Reason == TrackEndReason.LoadFailed)
            {
                ShimakazeBot.Client.DebugLogger.LogMessage(LogLevel.Warning,
                    LogMessageSources.PLAYLIST_NEXT_EVENT,
                    e.Reason + " - playlist length at error: " +
                    ShimakazeBot.playlists[e.Player.Guild].songRequests.Count,
                    DateTime.Now);
            }

            if (e.Reason == TrackEndReason.Replaced)
                return Task.CompletedTask;

            if (ShimakazeBot.playlists[e.Player.Guild].songRequests.Count <= 0)
                return Task.CompletedTask;

            ShimakazeBot.playlists[e.Player.Guild].songRequests.RemoveAt(0);
            ShimakazeBot.Client.DebugLogger.LogMessage(LogLevel.Info,
                LogMessageSources.PLAYLIST_NEXT_EVENT,
                $"{e.Reason} - " +
                $"{ShimakazeBot.playlists[e.Player.Guild].songRequests.Count} songs remaining in {e.Player.Guild.Name}.",
                DateTime.Now);

            if (ShimakazeBot.playlists[e.Player.Guild].songRequests.Count > 0)
            {
                e.Player.PlayAsync(ShimakazeBot.playlists[e.Player.Guild].songRequests.First().track);

                if (ShimakazeBot.CheckDebugMode(e.Player.Guild.Id))
                {
                    ShimakazeBot.Client.DebugLogger.LogMessage(LogLevel.Info,
                        LogMessageSources.PLAYLIST_NEXT_EVENT + " SupaDebug @" + e.Player.Guild.Name,
                        ShimakazeBot.lvn.GetConnection(e.Player.Guild)?.CurrentState?.CurrentTrack?.Title +
                        " - " + e.Handled.ToString(),
                        DateTime.Now);
                }
            }


            return Task.CompletedTask;
        }

        private async void ForceLeave(DiscordGuild guild)
        {
            var lvc = ShimakazeBot.lvn.GetConnection(guild);
            lvc.PlaybackFinished -= PlayNextTrack;
            await lvc.StopAsync();
            await lvc.DisconnectAsync();
        }


        private Task DiscordSocketClosed(WebSocketCloseEventArgs e)
        {
            string str = $"Discord socket closed:" +
                $"\n - Code: {e.Code}" +
                $"\n - Reason: {e.Reason}" +
                $"\n - Remote: {e.Remote}";
            
            ShimakazeBot.SendToDebugRoom(str);
            return Task.CompletedTask;
        }
    }
}
