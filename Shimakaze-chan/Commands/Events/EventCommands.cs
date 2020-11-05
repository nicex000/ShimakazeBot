using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shimakaze
{
    class EventCommands : Commands
    {
        [Command("addreminder")]
        [Aliases("remindme", "reminder", "makereminder")]
        [Description("Usage: reminder date <time> <message>" +
                     "\ndate format is `d-M-y` (or a single number for in x days)," +
                     "\ntime format is `H:m:s` (or a single number for in x minutes).")]
        public async Task AddReminder(CommandContext ctx, [RemainingText] string suffix)
        {
            await AddEventAsync(ctx, suffix, EventType.REMINDER);
        }

        [Command("addevent")]
        [Aliases("createevent", "newevent", "makeevent")]
        [Description("Usage: reminder date <time> <channel> <message>" +
                     "\ndate format is `d-M-y` (or a single number for in x days)," +
                     "\ntime format is `H:m:s` (or a single number for in x minutes).")]
        public async Task AddEvent(CommandContext ctx, [RemainingText] string suffix)
        {
            await AddEventAsync(ctx, suffix, EventType.EVENT);
        }

        [Command("removeevent")]
        [Aliases("removereminder", "deleteevent", "deletereminder")]
        public async Task RemoveEvent(CommandContext ctx, [RemainingText] string idString)
        {
            int id;
            if (!Int32.TryParse(idString, out id))
            {
                await CTX.RespondSanitizedAsync(ctx, $"invalid event ID. {idString} is not a proper id number.");
                return;
            }

            if (await ShimakazeBot.events.RemoveTimerEvent(id))
            {
                await CTX.RespondSanitizedAsync(ctx, $"Sucessfully stopped and removed the event with ID #{id}.");
            }
            else
            {
                await CTX.RespondSanitizedAsync(ctx, $"No event was found that matched ID #{id}.");
            }
        }

        private async Task AddEventAsync(CommandContext ctx, string suffix, EventType type)
        {
            DateTimeWithMessage formattedData = await ExtractDateTimeAndMessageAsync(ctx, suffix, type);
            if (formattedData == null)
            {
                return;
            }

            int id = await ShimakazeBot.events.AddTimerEvent(
                ctx, type, formattedData.message, formattedData.dateTime, formattedData.channelId);
            if (id < 0)
            {
                await CTX.RespondSanitizedAsync(ctx, 
                    $"Database didn't make an ID, the {(type == EventType.EVENT ? "event" : "reminder")}" +
                    $" was probably created but good luck removing it now.");
                return;
            }
            await CTX.RespondSanitizedAsync(ctx,
                $"{(type == EventType.EVENT ? "Event" : "Reminder")} created with id #{id}");
        }


        private async Task<DateTimeWithMessage> ExtractDateTimeAndMessageAsync(CommandContext ctx, string suffix,
            EventType type)
        {
            var suffixArray = suffix.Split(" ").ToList();
            DateTimeWithMessage result = new DateTimeWithMessage();
            DateTime outParse;
            if (DateTime.TryParseExact(suffixArray[0] + " " + suffixArray[1],
                ShimaConsts.DateFormat + " " + ShimaConsts.TimeFormat,
                null, System.Globalization.DateTimeStyles.None, out outParse))
            {
                result.dateTime = outParse;
                suffixArray.RemoveRange(0, 2);
            }
            else
            {
                bool noDate = false;
                //date
                double days;
                if (double.TryParse(suffixArray[0], out days))
                {
                    result.dateTime = DateTime.UtcNow.AddDays(days);
                }
                else if (suffixArray[0].Contains("-"))
                {
                    if (DateTime.TryParseExact(suffixArray[0],
                    ShimaConsts.DateFormat,
                    null, System.Globalization.DateTimeStyles.None, out outParse))
                    {
                        result.dateTime = outParse;
                    }
                    else
                    {
                        await CTX.RespondSanitizedAsync(ctx, "Invalid date format");
                        return null;
                    }
                }
                else
                {
                    result.dateTime = DateTime.UtcNow.Date;
                    noDate = true;
                }

                suffixArray.RemoveAt(0);

                //time
                double minutes;
                if (double.TryParse(suffixArray[0], out minutes))
                {
                    result.dateTime = result.dateTime.AddMinutes(minutes);
                    suffixArray.RemoveAt(0);
                }
                else if (suffixArray[0].Contains(":"))
                {
                    if (DateTime.TryParseExact(suffixArray[0],
                    ShimaConsts.TimeFormat,
                    null, System.Globalization.DateTimeStyles.None, out outParse))
                    {
                        result.dateTime = result.dateTime.Date
                            .AddHours(outParse.Hour)
                            .AddMinutes(outParse.Minute)
                            .AddSeconds(outParse.Second);
                        suffixArray.RemoveAt(0);
                    }
                    else
                    {
                        await CTX.RespondSanitizedAsync(ctx, "Invalid time format");
                        return null;
                    }
                }
                else if (noDate)
                {
                    await CTX.RespondSanitizedAsync(ctx, "No date or time provided");
                    return null;
                }

            }

            //channel for event
            if (type == EventType.EVENT && suffixArray.Count > 0 && suffixArray[0].Length > 15)
            {
                ulong channelId;
                if (!ulong.TryParse(suffixArray[0], out channelId) && suffixArray[0].StartsWith("<#"))
                {
                    //[2..^1] substrings away the first 2 and last 1 chara
                    ulong.TryParse(suffixArray[0][2..^1], out channelId);
                }
                if (channelId > 0 && ctx.Guild.Channels.ContainsKey(channelId))
                {
                    result.channelId = channelId;
                    suffixArray.RemoveAt(0);
                }
            }

            result.message = string.Join(" ", suffixArray);
            return result;
        }
    }
}
