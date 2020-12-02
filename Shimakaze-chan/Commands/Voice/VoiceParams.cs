using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Shimakaze
{
    public struct SongRequest
    {
        public string requester;
        public DiscordMember requestMember;
        public DiscordChannel requestedChannel;
        public LavalinkTrack track;
        //public Guid unique;

        public SongRequest(string requester, DiscordMember requestMember, DiscordChannel requestedChannel,
            LavalinkTrack track)
        {
            this.requester = requester;
            this.requestMember = requestMember;
            this.requestedChannel = requestedChannel;
            this.track = track;
            //unique = Guid.NewGuid();
        }
    }
    public class VoteSkip
    {
        public DiscordMessage message;
        public DiscordMember requester;

        public VoteSkip(DiscordMessage message, DiscordMember requester)
        {
            this.message = message;
            this.requester = requester;
        }
    }
    public class GuildPlayer
    {
        public List<SongRequest> songRequests;
        public bool isPaused;
        public int loopCount;
        public VoteSkip voteSkip;

        public GuildPlayer()
        {
            songRequests = new List<SongRequest>();
            isPaused = false;
            loopCount = 0;
            voteSkip = null;
        }
    }
    public class SpotifyLoadResult
    {
        public LavalinkLoadResultType LoadResultType { get; set; }
        public List<LavalinkTrack> Tracks { get; set; }

        public static explicit operator LavalinkLoadResult(SpotifyLoadResult v)
        {
            LavalinkLoadResult l = new LavalinkLoadResult();

            var Tracks = typeof(LavalinkLoadResult).GetField("<Tracks>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Tracks.SetValue(l, new ReadOnlyCollection<LavalinkTrack>(v.Tracks));

            var LoadResultType = typeof(LavalinkLoadResult).GetField("<LoadResultType>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            LoadResultType.SetValue(l, v.LoadResultType);

            return l;
        }
    }
}
