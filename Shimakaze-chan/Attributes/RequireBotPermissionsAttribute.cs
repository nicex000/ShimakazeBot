using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireBotPermissionsAttribute : SlashCheckBaseAttribute
    {
        public Permissions permissions;
        public string failMessage;

        public RequireBotPermissionsAttribute(Permissions permissions, string failMessage = "")
        {
            this.permissions = permissions;
            this.failMessage = failMessage;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            CannotBeUsedInDMAttribute checkDM = new CannotBeUsedInDMAttribute();
            if (!await checkDM.ExecuteChecksAsync(ctx))
            {
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
                failMessage = $"**Permissions missing for {bot.DisplayName}:** " +
                    $"{((pBot & permissions) ^ permissions).ToPermissionString()}";
            }

            if (!botSuccess) await SCTX.RespondSanitizedAsync(ctx, failMessage);
            return botSuccess;
        }
    }
}
