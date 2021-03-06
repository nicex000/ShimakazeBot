using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Shimakaze
{
    class VoiceCommands : Commands
    {
        [Command("join")]
        [Aliases("j", "s", "join-voice")]
        [Description("Joins the voice channel you\'re in.")]
        public async Task Join(CommandContext ctx, bool reconnect = false)
        {
            DebugString debugResponse = new DebugString();

            //db shit first
            var existingJoin = ShimakazeBot.DbCtx.GuildJoin.FirstOrDefault(p => p.GuildId == ctx.Guild.Id);
            if (existingJoin == null)
            {
                await ShimakazeBot.DbCtx.GuildJoin.AddAsync(
                    new GuildJoin { GuildId = ctx.Guild.Id, ChannelId = ctx.Channel.Id });
            }
            else
            {
                existingJoin.ChannelId = ctx.Channel.Id;
                ShimakazeBot.DbCtx.GuildJoin.Update(existingJoin);
            }
            await ShimakazeBot.DbCtx.SaveChangesAsync();

            var lv = ctx.Client.GetLavalink();
            debugResponse.AddWithDebug("Unable to get LavaLink", ctx, lv == null);
            if (ShimakazeBot.lvn == null)
                try
                {
                    var c = await lv.ConnectAsync(new LavalinkConfiguration
                    {
                        Password = ShimakazeBot.Config.lavalink.password,
                        SocketEndpoint = new ConnectionEndpoint(
                            ShimakazeBot.Config.lavalink.host,
                            ShimakazeBot.Config.lavalink.port),
                        RestEndpoint = new ConnectionEndpoint(
                            ShimakazeBot.Config.lavalink.host,
                            ShimakazeBot.Config.lavalink.port),
                        SocketAutoReconnect = false,
                        ResumeTimeout = 1
                    });
                    if (ShimakazeBot.lvn != null && !reconnect)
                    {
                        ShimakazeBot.lvn.ConnectedGuilds.ToList().ForEach(async g =>
                        {
                            await ForceLeave(g.Value.Guild);
                        });
                        ShimakazeBot.lvn.Disconnected -= LavalinkDisconnected;
                        ShimakazeBot.lvn.LavalinkSocketErrored -= LavalinkErrored;
                        ShimakazeBot.lvn = null;
                        ShimakazeBot.Client.MessageReactionAdded -= VoteSkipUpdate;
                        await Join(ctx, true);
                        return;
                    }
                    ShimakazeBot.lvn = c;
                    ShimakazeBot.lvn.Disconnected += LavalinkDisconnected;
                    ShimakazeBot.lvn.LavalinkSocketErrored += LavalinkErrored;
                    ShimakazeBot.Client.MessageReactionAdded += VoteSkipUpdate;
                }
                catch (Exception e)
                {
                    if (e.Message == "Unable to connect to the remote server")
                    {
                        await CTX.RespondSanitizedAsync(ctx,
                            "Lavalink is dead. Please try again in a few minutes.");
                    }
                    else
                    {
                        await CTX.RespondSanitizedAsync(ctx, $"**Unknown error.**\n{e}");
                    }
                    throw;
                }

            var lvc = ShimakazeBot.lvn.GetGuildConnection(ctx.Guild);
            debugResponse.AddWithDebug("LVC status: " + (lvc != null).ToString(), ctx);
            if (lvc != null)
            {
                await CTX.RespondSanitizedAsync(ctx, $"{debugResponse}Already connected in this guild.");
                return;
            }

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await CTX.RespondSanitizedAsync(ctx, $"{debugResponse}You need to be in a voice channel.");
                return;
            }

            try
            {
                if (ShimakazeBot.CheckDebugMode(ctx.Guild.Id))
                {
                    await CTX.RespondSanitizedAsync(ctx, "attempting connectAsync to " + chn.Name);
                }
                await ShimakazeBot.lvn.ConnectAsync(chn).ConfigureAwait(false);
                debugResponse.AddWithDebug("Connection status after: "
                    + (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild) != null).ToString(), ctx);
                if (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild) != null)
                {
                    debugResponse.AddWithDebug("  - Is connected: " +
                        ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).IsConnected.ToString() +
                        "\n  - Channel: " +
                        ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).Channel.Name, ctx);
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("The WebSocket is in an invalid state ('Aborted')"))
                {
                    await CTX.RespondSanitizedAsync(ctx,
                           "Lavalink is dead. Please try again in a few minutes." +
                           $"\n{debugResponse}");
                }
                else
                {
                    await CTX.RespondSanitizedAsync(ctx, $"{debugResponse}\n**Unknown error**\n{e}");
                }
                throw;
            }

            if (!ShimakazeBot.playlists.ContainsKey(ctx.Guild))
            {
                ShimakazeBot.playlists.Add(ctx.Guild, new GuildPlayer());
                debugResponse.AddWithDebug($"Playlist: **new** ({lvc != null})", ctx);
            }
            else
            {
                if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count > 0)
                {
                    lvc = ShimakazeBot.lvn.GetGuildConnection(ctx.Guild);
                    await lvc.PlayAsync(ShimakazeBot.playlists[ctx.Guild].songRequests.First().track);
                }
                debugResponse.AddWithDebug("Playlist: " +
                    $"**{ShimakazeBot.playlists[ctx.Guild].songRequests.Count} songs**", ctx);
            }
            ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).PlaybackFinished += PlayNextTrack;
            ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).DiscordWebSocketClosed += DiscordSocketClosed;

            if (!string.IsNullOrWhiteSpace(debugResponse.ToString()) ||
                (!reconnect && !ctx.Message.Content.Substring(ctx.Prefix.Length).StartsWith("s")))
            {
                await CTX.RespondSanitizedAsync(ctx, debugResponse + "Joined");
            }
        }

        [Command("leave")]
        [Aliases("leave-voice")]
        [Description("Leaves the voice channel.")]
        public async Task Leave(CommandContext ctx, [RemainingText] string remainingText)
        {

            DebugString debugResponse = new DebugString();

            var lv = ctx.Client.GetLavalink();

            debugResponse.AddWithDebug("Unable to get LavaLink", ctx, lv == null);

            var lvc = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
            if (ShimakazeBot.lvn == null || lvc == null)
            {
                await CTX.RespondSanitizedAsync(ctx, $"{debugResponse}Not connected in this guild.");
                return;
            }

            debugResponse.AddWithDebug("Connection state before: " +
                "\n  - Is connected: " +
                ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).IsConnected.ToString() +
                "\n  - Channel: " +
                ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).Channel.Name,
                ctx);

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await CTX.RespondSanitizedAsync(ctx, $"{debugResponse}You need to be in a voice channel.");
                return;
            }
            if (chn != lvc.Channel)
            {
                await CTX.RespondSanitizedAsync(ctx, $"{debugResponse}You need to be in the same voice channel.");
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
                    $"**{ShimakazeBot.playlists[ctx.Guild].songRequests.Count} songs**" : "**already removed**"),
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
                        (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild) != null).ToString(),
                        ctx);

                    if (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild) != null)
                    {
                        debugResponse.AddWithDebug("\n  - Is connected: " +
                            ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).IsConnected.ToString() +
                            "\n  - Channel: " +
                            ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).Channel.Name,
                            ctx);
                    }
                }
                await CTX.RespondSanitizedAsync(ctx, debugResponse + "Attempted leave.");
            }
        }

        [Command("list")]
        [Aliases("l", "playlist")]
        [Description("Displays the playlist.")]
        public async Task List(CommandContext ctx)
        {
            if (!ShimakazeBot.playlists.ContainsKey(ctx.Guild))
            {
                await CTX.RespondSanitizedAsync(ctx, "No playlist. Try making Shima join voice first.");
            }
            else
            {
                if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count > 0)
                {
                    int i = 0;
                    var lvc = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
                    string msg = "";
                    foreach (var req in ShimakazeBot.playlists[ctx.Guild].songRequests)
                    {
                        if (i == 0)
                        {
                            msg += lvc == null ? "Starting with " :
                                (ShimakazeBot.playlists[ctx.Guild].isPaused ?
                                "***PAUSED*** " : "Now playing ");
                        }
                        else if (i > 10)
                        {
                            msg += $"\n*And {ShimakazeBot.playlists[ctx.Guild].songRequests.Count - 11} more...*";
                            break;
                        }
                        else
                        {
                            msg += $"\n{i}. ";
                        }
                        msg += $"**{req.track.Title}** Requested by *{req.requester}*";
                        if (i == 0 && ShimakazeBot.playlists[ctx.Guild].loopCount > 0)
                        {
                            msg += $" ({ShimakazeBot.playlists[ctx.Guild].loopCount} " +
                                $"loop{(ShimakazeBot.playlists[ctx.Guild].loopCount > 1 ? "s" : "")} remaining)";
                        }
                        i++;
                    }

                    await CTX.RespondSanitizedAsync(ctx, msg);
                }
                else
                {
                    await CTX.RespondSanitizedAsync(ctx, "Playlist is empty.");
                }
            }

        }

        [Command("pause")]
        [Aliases("resume", "playpause")]
        [Description("Pauses or resumes the music playback.")]
        public async Task Pause(CommandContext ctx)
        {
            var lavaConnection = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
            if (!await CheckVoiceAndChannel(ctx, lavaConnection))
            {
                return;
            }

            if (ShimakazeBot.playlists[ctx.Guild].isPaused)
            {
                await lavaConnection.ResumeAsync();
                ShimakazeBot.playlists[ctx.Guild].isPaused = false;
                await CTX.RespondSanitizedAsync(ctx, "Music resumed.");
            }
            else
            {
                await lavaConnection.PauseAsync();
                ShimakazeBot.playlists[ctx.Guild].isPaused = true;
                await CTX.RespondSanitizedAsync(ctx, "Music paused.");
            }
        }

        [Command("clearplaylist")]
        [Aliases("clear", "clearlist", "clist", "cl")]
        [Description("Clears the playlist.")]
        public async Task ClearPlaylist(CommandContext ctx)
        {
            var lavaConnection = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
            if (!await CheckVoiceAndChannel(ctx, lavaConnection))
            {
                return;
            }

            ShimakazeBot.playlists[ctx.Guild].loopCount = 0;
            ShimakazeBot.playlists[ctx.Guild].songRequests = new List<SongRequest>
            {
                ShimakazeBot.playlists[ctx.Guild].songRequests[0]
            };
            await CTX.RespondSanitizedAsync(ctx, "Playlist cleared.");
        }

        [Command("play")]
        [Aliases("p", "r", "req", "request")]
        [Description("Plays the requested link or youtube search.")]
        public async Task Play(CommandContext ctx, [RemainingText] string songName)
        {

            LavalinkTrack track;
            LavalinkLoadResult lavalinkLoadResult;
            var lavaConnection = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
            if (!await CheckVoiceAndChannel(ctx, lavaConnection))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(songName) && ShimakazeBot.playlists[ctx.Guild].isPaused)
            {
                await lavaConnection.ResumeAsync();
                ShimakazeBot.playlists[ctx.Guild].isPaused = false;
                await CTX.RespondSanitizedAsync(ctx, "Music resumed.");
                return;
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), songName);

            if (songName.StartsWith("http"))
            {
                if (songName.StartsWith("https://open.spotify"))
                {
                    lavalinkLoadResult = await GetSpotifyTracks(ctx, songName);
                }
                else
                {
                    lavalinkLoadResult = await ShimakazeBot.lvn.Rest.GetTracksAsync(new Uri(songName));
                }
            }
            else if (songName.StartsWith("local"))
            {
                await CTX.RespondSanitizedAsync(ctx, songName.Substring(songName.IndexOf("local ") + 6));
                lavalinkLoadResult = await ShimakazeBot.lvn.Rest.GetTracksAsync(
                    new FileInfo(songName.Substring(songName.IndexOf("local ") + 6)));
            }
            else
            {
                lavalinkLoadResult = await ShimakazeBot.lvn.Rest.GetTracksAsync(songName);
            }

            if (lavalinkLoadResult == null)
            {
                return;
            }

            switch (lavalinkLoadResult.LoadResultType)
            {
                case LavalinkLoadResultType.SearchResult:
                case LavalinkLoadResultType.TrackLoaded:
                    track = lavalinkLoadResult.Tracks.First();
                    ShimakazeBot.playlists[ctx.Guild].songRequests.Add(new SongRequest(
                        ctx.Member.DisplayName, ctx.Member, ctx.Channel, track));
                    break;
                case LavalinkLoadResultType.PlaylistLoaded:
                    ShimakazeBot.playlists[ctx.Guild].songRequests.AddRange(lavalinkLoadResult.Tracks.Select(t =>
                    new SongRequest(ctx.Member.DisplayName, ctx.Member, ctx.Channel, t)));
                    break;
                case LavalinkLoadResultType.NoMatches:
                    await CTX.RespondSanitizedAsync(ctx, "No matches found.");
                    break;
                case LavalinkLoadResultType.LoadFailed:
                    await CTX.RespondSanitizedAsync(ctx, "Load failed.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            string responseString = "";

            if (lavaConnection.CurrentState.CurrentTrack == null)
            {
                await lavaConnection.PlayAsync(ShimakazeBot.playlists[ctx.Guild].songRequests.First().track);
                responseString = $"Playing **{ShimakazeBot.playlists[ctx.Guild].songRequests.First().track.Title}** " +
                    $"Requested by *{ShimakazeBot.playlists[ctx.Guild].songRequests.First().requester}*";
            }
            else
            {
                responseString = $"Added **{lavalinkLoadResult.Tracks.First().Title}** to the queue. " +
                    $"Requested by *{ctx.Member.DisplayName}*";
            }

            if (lavalinkLoadResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded &&
                lavalinkLoadResult.Tracks.Count() > 1)
            {
                responseString += "\nAlso added " +
                    $"**{lavalinkLoadResult.Tracks.Count() - 1}** more songs to the queue.";
            }

            await CTX.RespondSanitizedAsync(ctx, responseString);
        }

        [Command("skip")]
        [Description("Skips the current song in the playlist.")]
        public async Task Skip(CommandContext ctx)
        {
            var lavaConnection = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
            if (!await CheckVoiceAndChannel(ctx, lavaConnection))
            {
                return;
            }

            if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count == 0)
            {
                await CTX.RespondSanitizedAsync(ctx, "Playlist is empty.");
                return;
            }

            if (lavaConnection.Channel.Users.Count() > 3 &&
                ctx.User.Id != ShimakazeBot.playlists[ctx.Guild].songRequests[0].requestMember.Id &&
                !ShimakazeBot.Client.CurrentApplication.Owners.Contains(ctx.User))
            {
                await CTX.RespondSanitizedAsync(ctx, "You can only skip songs you requested, use voteskip instead");
                return;
            }

            await SkipSong(ctx.Guild, ctx.Channel, lavaConnection);
        }

        [Command("voteskip")]
        [Description("Skips the current song if the majority votes so")]
        [Aliases("vskip")]
        public async Task VoteSkip(CommandContext ctx)
        {
            var lavaConnection = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
            if (!await CheckVoiceAndChannel(ctx, lavaConnection))
            {
                return;
            }

            if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count == 0)
            {
                await CTX.RespondSanitizedAsync(ctx, "Playlist is empty.");
                return;
            }

            if (lavaConnection.Channel.Users.Count() <= 3)
            {
                ShimakazeBot.playlists[ctx.Guild].voteSkip = new VoteSkip(ctx.Message, ctx.Member);
                await SkipSong(ctx.Guild, ctx.Channel, lavaConnection, true);
                return;
            }
            if (ShimakazeBot.playlists[ctx.Guild].voteSkip != null)
            {
                await CTX.RespondSanitizedAsync(ctx, "Vote skip already requested: " +
                    ShimakazeBot.playlists[ctx.Guild].voteSkip.message.JumpLink);
                return;
            }

            var reactionEmote = DiscordEmoji.FromName(ShimakazeBot.Client, $":{ShimaConsts.VoteSkipEmote}:");

            ShimakazeBot.playlists[ctx.Guild].voteSkip = new VoteSkip(
                await CTX.RespondSanitizedAsync(ctx,
                    $"{ctx.Member.Mention} requested a voteskip on" +
                    $" **{ShimakazeBot.playlists[ctx.Guild].songRequests[0].track.Title}**" +
                    $"\nReact with {reactionEmote} to vote.",
                    false, null, new List<IMention> { }),
                ctx.Member
                );
            try
            {
                await ShimakazeBot.playlists[ctx.Guild].voteSkip.message.CreateReactionAsync(reactionEmote);
            }
            catch (Exception e)
            {
                await CTX.RespondSanitizedAsync(ctx, e.Message);
            }
        }

        [Command("shuffle")]
        [Description("Shuffles the playlist")]
        public async Task Shuffle(CommandContext ctx)
        {
            var lavaConnection = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
            if (!await CheckVoiceAndChannel(ctx, lavaConnection))
            {
                return;
            }
            if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count > 2)
            {
                var message = await CTX.RespondSanitizedAsync(ctx, "Shuffling...");
                for (int index = ShimakazeBot.playlists[ctx.Guild].songRequests.Count - 1; index > 1; index--)
                {
                    int randomIndex = ThreadSafeRandom.ThisThreadsRandom.Next(1, index + 1);
                    if (randomIndex == index) continue;
                    SongRequest randomSong = ShimakazeBot.playlists[ctx.Guild].songRequests[randomIndex];

                    ShimakazeBot.playlists[ctx.Guild].songRequests[randomIndex] =
                        ShimakazeBot.playlists[ctx.Guild].songRequests[index];

                    ShimakazeBot.playlists[ctx.Guild].songRequests[index] = randomSong;
                }
                await message.ModifyAsync("Successfully shuffled " +
                    $"**{ShimakazeBot.playlists[ctx.Guild].songRequests.Count - 1}** songs." +
                    $"\nNext up is: **{ShimakazeBot.playlists[ctx.Guild].songRequests[1].track.Title}**" +
                    $" Requested by *{ShimakazeBot.playlists[ctx.Guild].songRequests[1].requester}*");
            }
            else
            {
                await CTX.RespondSanitizedAsync(ctx, "Not enough songs to shuffle.");
            }
        }

        [Command("loop")]
        [Description("loop the current song a specified number of times")]
        public async Task Loop(CommandContext ctx, [RemainingText] string loopString)
        {
            var lavaConnection = ShimakazeBot.lvn?.GetGuildConnection(ctx.Guild);
            if (!await CheckVoiceAndChannel(ctx, lavaConnection))
            {
                return;
            }

            int loopCount = 0;
            if (string.IsNullOrWhiteSpace(loopString))
            {
                loopCount = 1;
            }
            else if (!int.TryParse(loopString, out loopCount) ||
                loopCount < 0 || loopCount > ShimaConsts.MaxSongLoopCount)
            {
                await CTX.RespondSanitizedAsync(ctx,
                    $"Please type a number between **0** and **{ShimaConsts.MaxSongLoopCount}**");
                return;
            }
            if (ShimakazeBot.playlists[ctx.Guild].songRequests.Count > 0)
            {
                if (ShimakazeBot.playlists[ctx.Guild].loopCount > 0 && loopCount == 0)
                {
                    await CTX.RespondSanitizedAsync(ctx,
                        $"**{ShimakazeBot.playlists[ctx.Guild].songRequests[0].track.Title}** will no longer loop.");
                }
                else if (loopCount > 0)
                {
                    await CTX.RespondSanitizedAsync(ctx,
                        $"Set **{ShimakazeBot.playlists[ctx.Guild].songRequests[0].track.Title}** " +
                        $"to loop {loopCount} time{(loopCount > 1 ? "s" : "")}.");
                }
                else
                {
                    await CTX.RespondSanitizedAsync(ctx, "Playlist will continue to **not** loop.");
                    return;
                }
                ShimakazeBot.playlists[ctx.Guild].loopCount = loopCount;
            }
            else
            {
                await CTX.RespondSanitizedAsync(ctx, "Playlist is empty, request a song first.");
            }
        }

        private async Task<LavalinkLoadResult> GetSpotifyTracks(CommandContext ctx, string spotifyURI)
        {
            var tracks = new List<string>();
            spotifyURI = spotifyURI.Replace("https://open.spotify.com/", "");
            var type = spotifyURI.Substring(0, spotifyURI.IndexOf("/"));
            var id = spotifyURI.Substring(type.Length + 1);
            bool isPlaylist = type is "playlist";


            if (!isPlaylist && type != "track")
            {
                await CTX.RespondSanitizedAsync(ctx, "Use a spotify playlist or track URL");
                return null;
            }
            if (string.IsNullOrWhiteSpace(ShimakazeBot.SpotifyToken))
            {
                var tokenResponse = await ShimaHttpClient.HttpPost("https://accounts.spotify.com/api/token",
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        $"{ShimakazeBot.Config.apiKeys.spotifyClientId}:" +
                        $"{ShimakazeBot.Config.apiKeys.spotifyClientSecret}"))));
                if (tokenResponse == null)
                {
                    await CTX.RespondSanitizedAsync(ctx, "Spotify authentication failed.");
                    return null;
                }
                ShimakazeBot.SpotifyToken = tokenResponse["access_token"].Value<string>();
                ShimakazeBot.events.InitializeSpotifyResetTimer(tokenResponse["expires_in"].Value<int>());
            }

            var response = await ShimaHttpClient.HttpGet($"https://api.spotify.com/v1/{type}s/{id}" +
                (isPlaylist ? "/tracks" : ""), new AuthenticationHeaderValue("Bearer", ShimakazeBot.SpotifyToken));
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "Bad response from Spotify.");
                return null;
            }

            if (!isPlaylist)
            {
                tracks.Add($"{response["artists"]?.Children().ToArray()[0]["name"].Value<string>()}" +
                    $" - {response["name"].Value<string>()}");
            }
            else
            {
                response["tracks"]?["items"]?.Children().ToList().ForEach(item =>
                {
                    tracks.Add($"{item["track"]?["artists"]?.Children().ToArray()[0]["name"].Value<string>()}" +
                        $" - {item["track"]?["name"].Value<string>()}");
                });
            }

            var result = new SpotifyLoadResult();
            result.Tracks = new List<LavalinkTrack>();
            bool firstLoaded = false;
            foreach (var track in tracks)
            {
                var trackResult = await ShimakazeBot.lvn.Rest.GetTracksAsync(track);
                if (trackResult.LoadResultType != LavalinkLoadResultType.NoMatches &&
                    trackResult.LoadResultType != LavalinkLoadResultType.LoadFailed)
                {
                    if (!firstLoaded)
                    {
                        await Play(ctx, track);
                        firstLoaded = true;
                    }
                    else
                    {
                        result.Tracks.Add(trackResult.Tracks.First());
                    }
                }
            }
            result.LoadResultType = firstLoaded ?
                LavalinkLoadResultType.PlaylistLoaded : LavalinkLoadResultType.NoMatches;

            return (LavalinkLoadResult)result;
        }

        private async Task PlayNextTrack(LavalinkGuildConnection lvc, TrackFinishEventArgs e)
        {
            if (e.Reason == TrackEndReason.Cleanup)
            {
                ShimakazeBot.SendToDebugRoom("Lavalink failed in **" +
                    $"{e.Player.Guild.Name}** ({e.Player.Guild.Id}) - **" +
                    $"{e.Player.Channel.Name}** ({e.Player.Channel.Id}) - song: " +
                    e.Track.Title);
                ShimakazeBot.Client.Logger.Log(LogLevel.Warning,
                    LogSources.PLAYLIST_NEXT_EVENT,
                    $"{e.Reason} - playlist length at error: " +
                    ShimakazeBot.playlists[e.Player.Guild].songRequests.Count);

                if (ShimakazeBot.playlists[e.Player.Guild].songRequests.Count > 0)
                {
                    await ShimakazeBot.playlists[e.Player.Guild].songRequests[0].requestedChannel.SendMessageAsync(
                        ShimakazeBot.playlists[e.Player.Guild].songRequests[0].requestMember.Mention +
                        "Lavalink failed... Shima will leave the voice channel. " +
                        "Don't worry your playlist has been saved. You can make her rejoin." +
                        (ShimakazeBot.shouldSendToDebugRoom ? " The devs have been notified." : ""));
                    await ForceLeave(e.Player.Guild);
                }


                return;
            }
            if (e.Reason == TrackEndReason.LoadFailed)
            {
                ShimakazeBot.Client.Logger.Log(LogLevel.Warning,
                    LogSources.PLAYLIST_NEXT_EVENT,
                    $"{e.Reason} - playlist length at error: " +
                    ShimakazeBot.playlists[e.Player.Guild].songRequests.Count);
            }

            if (e.Reason == TrackEndReason.Replaced)
            {
                return;
            }

            if (ShimakazeBot.playlists[e.Player.Guild].songRequests.Count <= 0)
            {
                return;
            }

            if (ShimakazeBot.playlists[e.Player.Guild].loopCount > 0)
            {
                ShimakazeBot.playlists[e.Player.Guild].loopCount--;
                await e.Player.PlayAsync(e.Track);
                return;
            }

            ShimakazeBot.playlists[e.Player.Guild].songRequests.RemoveAt(0);
            ShimakazeBot.playlists[e.Player.Guild].voteSkip = null;
            ShimakazeBot.Client.Logger.Log(LogLevel.Information,
                LogSources.PLAYLIST_NEXT_EVENT,
                $"{e.Reason} - " +
                    $"{ShimakazeBot.playlists[e.Player.Guild].songRequests.Count} " +
                    $"songs remaining in {e.Player.Guild.Name}.");

            if (ShimakazeBot.playlists[e.Player.Guild].songRequests.Count > 0)
            {
                await e.Player.PlayAsync(ShimakazeBot.playlists[e.Player.Guild].songRequests.First().track);

                if (ShimakazeBot.CheckDebugMode(e.Player.Guild.Id))
                {
                    ShimakazeBot.Client.Logger.Log(LogLevel.Information,
                        $"{LogSources.PLAYLIST_NEXT_EVENT} SupaDebug @{e.Player.Guild.Name}",
                        "next track: " +
                        ShimakazeBot.lvn.GetGuildConnection(e.Player.Guild)?.CurrentState?.CurrentTrack?.Title);
                }
            }


            return;
        }

        private async Task ForceLeave(DiscordGuild guild)
        {
            var lvc = ShimakazeBot.lvn.GetGuildConnection(guild);
            lvc.PlaybackFinished -= PlayNextTrack;
            await lvc.StopAsync();
            await lvc.DisconnectAsync();
        }

        private async Task<bool> CheckVoiceAndChannel(CommandContext ctx, LavalinkGuildConnection lvc)
        {
            if (lvc == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "Not connected in this guild.");
                return false;
            }

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "You need to be in a voice channel.");
                return false;
            }
            if (chn != lvc.Channel)
            {
                await CTX.RespondSanitizedAsync(ctx, "You need to be in the same voice channel.");
                return false;
            }
            return true;
        }

        private async Task SkipSong(DiscordGuild guild, DiscordChannel channel,
            LavalinkGuildConnection lvc, bool fromVoteSkip = false)
        {
            string title = ShimakazeBot.playlists[guild].songRequests[0].track.Title;
            ShimakazeBot.playlists[guild].songRequests.RemoveAt(0);
            bool wasLooping = false;
            if (ShimakazeBot.playlists[guild].loopCount > 0)
            {
                wasLooping = true;
                ShimakazeBot.playlists[guild].loopCount = 0;
            }

            if (ShimakazeBot.playlists[guild].songRequests.Count > 0)
            {
                await lvc.PlayAsync(ShimakazeBot.playlists[guild].songRequests.First().track);
                await CTX.SendSanitizedMessageAsync(channel,
                    $"Skipped *{title}*{(wasLooping ? " and stopped loop" : "")}." +
                    (fromVoteSkip ? 
                        $"\nSkip requested by **{ShimakazeBot.playlists[guild].voteSkip.requester.DisplayName}**" :
                        ""));
            }
            else
            {
                await lvc.StopAsync();
                await CTX.SendSanitizedMessageAsync(channel, $"Playlist ended with skip. (Skipped *{title}*" +
                    $"{(wasLooping ? " and stopped loop" : "")})" +
                    (fromVoteSkip ?
                        $"\nSkip requested by **{ShimakazeBot.playlists[guild].voteSkip.requester.DisplayName}**" :
                        ""));
            }

            ShimakazeBot.playlists[guild].voteSkip = null;
        }

        private async Task VoteSkipUpdate(DSharpPlus.DiscordClient client,
            DSharpPlus.EventArgs.MessageReactionAddEventArgs messageReactionAdd)
        {
            var voteSkipMessage = ShimakazeBot.playlists[messageReactionAdd.Guild]?.voteSkip?.message;
            if (voteSkipMessage == messageReactionAdd.Message)
            {
                var lvc = ShimakazeBot.lvn?.GetGuildConnection(messageReactionAdd.Guild);
                
                if (lvc != null)
                {
                    var reactionEmote = DiscordEmoji.FromName(ShimakazeBot.Client, $":{ShimaConsts.VoteSkipEmote}:");

                    

                    if (ShimakazeBot.playlists[messageReactionAdd.Guild]
                        .voteSkip.requester.Id != messageReactionAdd.User.Id &&

                        lvc.Channel.Users.FirstOrDefault(member => member.Id == messageReactionAdd.User.Id) != null &&

                        messageReactionAdd.Emoji == reactionEmote)
                    {
                        if (messageReactionAdd.Message.Reactions.FirstOrDefault(reaction => 
                                reaction.Emoji == reactionEmote)
                            .Count >= Math.Ceiling(lvc.Channel.Users.Count() / 2.0))
                        {
                            await SkipSong(messageReactionAdd.Guild, messageReactionAdd.Channel, lvc, true);
                        }
                    }
                    else
                    {
                        await messageReactionAdd.Message.DeleteReactionAsync(
                            messageReactionAdd.Emoji, messageReactionAdd.User);
                    }
                }
            }
        }

        private Task LavalinkDisconnected(LavalinkNodeConnection lvn, NodeDisconnectedEventArgs e)
        {
            ShimakazeBot.SendToDebugRoom("Lavalink died.");

           // ShimakazeBot.lvn.LavalinkSocketErrored -= LavalinkErrored;
            //ShimakazeBot.lvn.Disconnected -= LavalinkDisconnected;
            //ShimakazeBot.lvn = null;
            return Task.CompletedTask;
        }

        private Task LavalinkErrored(LavalinkNodeConnection lvn, DSharpPlus.EventArgs.SocketErrorEventArgs e)
        {
            if (e.Exception.Message == 
                "An attempt was made to transition a task to a final state when it had already completed.")
            {
                return Task.CompletedTask;
            }
            ShimakazeBot.SendToDebugRoom($" Lavalink error:\n{e.Exception?.Message}");
            return Task.CompletedTask;
        }

        private async Task DiscordSocketClosed(LavalinkGuildConnection lvc, WebSocketCloseEventArgs e)
        {
            if (e.Code == 4014)
            {
                return;
            }
            if (e.Code == 1001 || e.Code == 1006 || e.Code == 4000)
            {
                var track = lvc.CurrentState?.CurrentTrack;
                var time = lvc.CurrentState?.PlaybackPosition;
                var ch = lvc.Channel;
                await lvc.DisconnectAsync();
                var newLvc = await ShimakazeBot.lvn.ConnectAsync(ch);
                if (track != null)
                {
                    await newLvc.PlayAsync(track);
                    if (time != null)
                    {
                        await newLvc.SeekAsync(time.Value);
                    }
                }
                return;
            }

            string str = $"Discord socket closed:" +
                $"\n - Code: {e.Code}" +
                $"\n - Reason: {e.Reason}" +
                $"\n - Remote: {e.Remote}" +
                $"\n - Channel: {lvc?.Channel.Name} ({lvc?.Guild.Name})";
            
            ShimakazeBot.SendToDebugRoom(str);
            return;
        }
    }
}
