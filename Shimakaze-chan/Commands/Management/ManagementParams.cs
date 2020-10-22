using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shimakaze
{
    public class WarnMessage
    {
        public string message;
        public DateTime timeStamp;
        public WarnMessage(string message, DateTime timeStamp)
        {
            this.message = message;
            this.timeStamp = timeStamp;
        }
    }
    public class GuildUsersWarns
    {
        public Dictionary<ulong, UserWarns> userWarns;
        public GuildUsersWarns(Dictionary<ulong, UserWarns> userWarns)
        {
            this.userWarns = userWarns;
        }
    }
    public class UserWarns
    {
        public Dictionary<int, WarnMessage> warningMessages;

        public UserWarns(Dictionary<int, WarnMessage> warningMessages)
        {
            this.warningMessages = warningMessages;
        }

        public async Task<int> AddWarning(ulong guildId, ulong userId, string message)
        {
            GuildWarn warn = (await ShimakazeBot.DbCtx.GuildWarn.AddAsync(new GuildWarn
            {
                GuildId = guildId,
                UserId = userId,
                WarnMessage = message,
                TimeStamp = DateTime.UtcNow
            })).Entity;
            await ShimakazeBot.DbCtx.SaveChangesAsync();

            warningMessages.Add(warn.Id, new WarnMessage(message, warn.TimeStamp));
            return warn.Id;
        }

        public async Task<bool> RemoveWarning(int id)
        {
            GuildWarn warn = await ShimakazeBot.DbCtx.GuildWarn.FindAsync(id);
            if (!warningMessages.ContainsKey(id) &&
                warn == null)
            {
                return false;
            }
            
            if (warningMessages.ContainsKey(id))
            {
                warningMessages.Remove(id);
            }
            if (warn != null)
            {
                ShimakazeBot.DbCtx.GuildWarn.Remove(warn);
                await ShimakazeBot.DbCtx.SaveChangesAsync();
            }

            return true;
        }
    }


}
