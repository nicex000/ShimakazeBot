using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireBotPermissionsAttribute : CheckBaseAttribute
    {
        public Permissions permissions;
        public string failMessage;

        public RequireBotPermissionsAttribute(Permissions permissions, string failMessage = "")
        {
            this.permissions = permissions;
            this.failMessage = failMessage;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (ctx.Guild == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "This command can't be used in DMs.");
                return false;
            }

            var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
            if (bot == null)
            {
                return false;
            }
            var pBot = ctx.Channel.PermissionsFor(bot);


            var botSuccess = (pBot & Permissions.Administrator) != 0 || (pBot & permissions) == permissions;

            if (!botSuccess && string.IsNullOrWhiteSpace(failMessage))
            {
                failMessage = $"**Permissions missing for {bot.DisplayName}:** {((pBot & permissions) ^ permissions).ToPermissionString()}";
            }

            if (!botSuccess) await CTX.RespondSanitizedAsync(ctx, failMessage);
            return botSuccess;
        }
    }
}
