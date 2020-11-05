using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;


namespace Shimakaze
{
   
    class EventTimers
    {
        Timer dailyFTimer;
        List<EventInTimer> events = new List<EventInTimer>();
        public void InitializeTimers()
        {
            InitializeFTimer();
            SetEventTimers();
        }

        public async Task<int> AddEvent(CommandContext ctx, EventType type, string message, DateTime eventTime)
        {
            TimedEvent tEvent = new TimedEvent
            {
                Type = type,
                Message = message,
                EventTime = eventTime,
                UserId = ctx.User.Id,
                ChannelId = ctx.Channel.Id
            };
            tEvent.Id = (await ShimakazeBot.DbCtx.TimedEvents.AddAsync(tEvent)).Entity.Id;
            await ShimakazeBot.DbCtx.SaveChangesAsync();

            return AddAndStartEvent(EventInTimer.MakeTimer(tEvent), tEvent.Id) ? tEvent.Id : -1;
        }

        public async Task<bool> RemoveEvent(int id)
        {
            var tEvent = events.Find(eT => eT.dbEvent != null && eT.dbEvent.Id == id);
            if (tEvent == null)
            {
                return false;
            }
            tEvent.Elapsed -= EventEnded;
            tEvent.Stop();
            
            if (events.Count() > 0)
            {
                events.Remove(tEvent);
            }

            ShimakazeBot.DbCtx.TimedEvents.RemoveRange(
              ShimakazeBot.DbCtx.TimedEvents.Where(tE => tE.Id == id));
            await ShimakazeBot.DbCtx.SaveChangesAsync();
            return true;
        }

        private void InitializeFTimer()
        {
            DateTime now = DateTime.UtcNow;
            DateTime jpReset = DateTime.UtcNow.Date.AddHours(15);
            if (now > jpReset)
            {
                jpReset = jpReset.AddDays(1);
            }
            dailyFTimer = new Timer((jpReset - now).TotalMilliseconds);

            dailyFTimer.Elapsed += FirstResetF;
            dailyFTimer.Start();
        }

        private void FirstResetF(object sender, ElapsedEventArgs e)
        {
            ResetDailyF(sender, e);

            dailyFTimer.Elapsed -= FirstResetF;
            dailyFTimer.Stop();

            dailyFTimer = new Timer(TimeSpan.FromHours(24).TotalMilliseconds);
            dailyFTimer.Elapsed += ResetDailyF;
            dailyFTimer.AutoReset = true;
            dailyFTimer.Start();
        }

        private void ResetDailyF(object sender, ElapsedEventArgs e)
        {
            ShimakazeBot.DailyFCount = 0;
            ShimakazeBot.SendToDebugRoom($"reset - {ShimakazeBot.DailyFCount}");
            
        }

        private void SetEventTimers()
        {
            ShimakazeBot.DbCtx.TimedEvents.ToList().ForEach(tEvent => {
                AddAndStartEvent(EventInTimer.MakeTimer(tEvent), tEvent.Id);
            });
        }

        private bool AddAndStartEvent(EventInTimer eventInTimer, int id)
        {
            if (eventInTimer == null)
            {
                ShimakazeBot.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Error,
                    LogMessageSources.TIMER_EVENT_EVENT,
                    $"Failed to make timer from event #{id}", DateTime.Now);
                return false;
            }
            events.Add(eventInTimer);
            events.Last().Elapsed += EventEnded;
            events.Last().Start();
            return true;
        }

        private async void EventEnded(object sender, ElapsedEventArgs e)
        {
            EventInTimer tEvent = (EventInTimer)sender;
            tEvent.Elapsed -= EventEnded;
            tEvent.Stop();

            if (events.Count() > 0)
            {
                events.Remove(tEvent);
            }

            if (tEvent.dbEvent == null)
            {
                ShimakazeBot.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Error,
                    LogMessageSources.TIMER_EVENT_EVENT,"Timer had no event attached", DateTime.Now);
                return;
            }

            var channel = await ShimakazeBot.Client.GetChannelAsync(tEvent.dbEvent.ChannelId);
            var eType = tEvent.dbEvent.Type;
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithAuthor(ShimakazeBot.Client.CurrentUser.Username, null, ShimakazeBot.Client.CurrentUser.AvatarUrl)
                .WithColor(eType == EventType.REMINDER ? DiscordColor.Purple : DiscordColor.HotPink)
                .WithTimestamp(tEvent.dbEvent.EventTime)
                .WithTitle(eType == EventType.REMINDER ? 
                    "Here's your reminder~~" : "Event Time! - イベント　タイム！")
                .WithFooter($"Event #{tEvent.dbEvent.Id}")
                .WithDescription((string.IsNullOrWhiteSpace(tEvent.dbEvent.Message) ?
                    "*No message.*" : tEvent.dbEvent.Message));
            if (eType == EventType.EVENT)
            {
                embedBuilder.AddField("Created by", $"<@{tEvent.dbEvent.UserId}>");
            }

            await CTX.SendSanitizedMessageAsync(channel,
                tEvent.dbEvent.Type == EventType.REMINDER ? $"<@{tEvent.dbEvent.UserId}>" : null,
                false,
                embedBuilder);

            ShimakazeBot.DbCtx.TimedEvents.RemoveRange(
               ShimakazeBot.DbCtx.TimedEvents.Where(tE => tE.Id == tEvent.dbEvent.Id));
            await ShimakazeBot.DbCtx.SaveChangesAsync();
        }
    }
}
