using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shimakaze
{
    class GeneralCommands : Commands
    {
        [Command("f")]
        [Aliases("pressf")]
        public async Task PressF(CommandContext ctx)
        {
            var DbItem = await ShimakazeBot.DbCtx.ShimaGeneric.FindAsync(ShimaConsts.DbPressFKey);
            int totalFCount;
            if (DbItem == null || !int.TryParse(DbItem.Value, out totalFCount))
            {
                totalFCount = 0;
            }

            await ctx.RespondAsync($"total: {++totalFCount} - daily: {++ShimakazeBot.DailyFCount}");

            if (DbItem == null)
            {
                await ShimakazeBot.DbCtx.ShimaGeneric.AddAsync(new ShimaGeneric
                {
                    Key = ShimaConsts.DbPressFKey,
                    Value = totalFCount.ToString()
                });
            }
            else
            {
                DbItem.Value = totalFCount.ToString();
                ShimakazeBot.DbCtx.ShimaGeneric.Update(DbItem);
            }
        }
    }
}
