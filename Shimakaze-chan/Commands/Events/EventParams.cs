using System;
using System.Timers;

namespace Shimakaze
{
    public enum EventType
    {
        REMINDER,
        EVENT
    }

    class DateTimeWithMessage
    {
        public DateTime dateTime { get; set; }
        public string message { get; set; }
        public ulong channelId { get; set; }
    }

    public class EventInTimer : Timer
    {
        public TimedEvent dbEvent { get; private set; }

        EventInTimer(TimedEvent dbEvent, double millis) : base(millis)
        {
            this.dbEvent = dbEvent;
        }

        public static EventInTimer MakeTimer(TimedEvent dbEvent)
        {
            double millis = (dbEvent.EventTime - DateTime.UtcNow).TotalMilliseconds;
            if (millis > int.MaxValue)
            {
                return null;
            }
            else if (millis < 0)
            {
                millis = TimeSpan.FromSeconds(10).TotalMilliseconds;
            }

            return new EventInTimer(dbEvent, millis);
        }
    }
}
