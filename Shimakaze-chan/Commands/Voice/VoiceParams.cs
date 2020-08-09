using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System.Collections.Generic;

namespace Shimakaze
{
    public struct SongRequest
    {
        public string requester;
        public DiscordMember requestMember;
        public DiscordChannel requestedChannel;
        public LavalinkTrack track;
        //public Guid unique;

        public SongRequest(string requester, DiscordMember requestMember, DiscordChannel requestedChannel, LavalinkTrack track)
        {
            this.requester = requester;
            this.requestMember = requestMember;
            this.requestedChannel = requestedChannel;
            this.track = track;
            //unique = Guid.NewGuid();
        }
    }
    public class GuildPlayer
    {
        public List<SongRequest> songRequests;
        public bool isPaused;

        public GuildPlayer()
        {
            songRequests = new List<SongRequest>();
            isPaused = false;
        }
    }
}
