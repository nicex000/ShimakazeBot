using Microsoft.Extensions.Logging;

namespace Shimakaze
{
    public static class LogSources
    {
        public static readonly EventId WEBSOCKET_EVENT = new EventId(1, "Websocket event");
        public static readonly EventId LAUNCHTIME_EVENT = new EventId(1, "Launchtime message");
        public static readonly EventId PLAYLIST_NEXT_EVENT = new EventId(1, "Playlist next event"); 
        public static readonly EventId COMMAND_EXECUTION_EVENT = new EventId(1, "User command");
        public static readonly EventId SLASH_COMMAND_EXECUTION_EVENT = new EventId(1, "Slash command");
        public static readonly EventId TIMER_EVENT_EVENT = new EventId(1, "Timer event");
    }
}
