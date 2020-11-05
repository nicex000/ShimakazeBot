using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
        public async Task PressF(CommandContext ctx, [RemainingText] string unusedSuffix)
        {
            var DbItem = await ShimakazeBot.DbCtx.ShimaGeneric.FindAsync(ShimaConsts.DbPressFKey);
            int totalFCount;
            if (DbItem == null || !int.TryParse(DbItem.Value, out totalFCount))
            {
                totalFCount = 0;
            }
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("🇫")
                .WithAuthor($"{(ctx.Guild == null ? ctx.User.Username : ctx.Member.DisplayName)}" +
                " has paid their respects.", null, ctx.User.AvatarUrl)
                .WithDescription($"Today: **{++ShimakazeBot.DailyFCount}**" +
                $"\nTotal: **{++totalFCount}**");

            await CTX.RespondSanitizedAsync(ctx, null, false, embedBuilder);

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
            await ShimakazeBot.DbCtx.SaveChangesAsync();
        }
    }
}
