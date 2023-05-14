using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequirePermissionsAttribute : SlashCheckBaseAttribute
    {
        public Permissions permissions;

        public RequirePermissionsAttribute(Permissions permissions)
        {
            this.permissions = permissions;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            CannotBeUsedInDMAttribute checkDM = new CannotBeUsedInDMAttribute();
            if (!await checkDM.ExecuteChecksAsync(ctx))
            {
                return false;
            }

            var user = ctx.Member;
            if (user == null)
            {
                return false;
            }
            var pUser = ctx.Channel.PermissionsFor(user);

            var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
            if (bot == null)
            {
                return false;
            }
          
            var pBot = ctx.Channel.PermissionsFor(bot);

            var userSuccess = user.Id == ctx.Guild.Owner.Id ||
                        ((pUser & Permissions.Administrator) != 0 || (pUser & permissions) == permissions);
            var botSuccess = (pBot & Permissions.Administrator) != 0 || (pBot & permissions) == permissions;

            string failMessage = "";
            if (!userSuccess)
            {
                failMessage += $"**Permissions missing for {user.DisplayName}:** " +
                    $"{((pUser & permissions) ^ permissions).ToPermissionString()}\n";
            }
            if (!botSuccess)
            {
                failMessage += $"**Permissions missing for {bot.DisplayName}:** " +
                    $"{((pBot & permissions) ^ permissions).ToPermissionString()}";
            }

            if (!string.IsNullOrWhiteSpace(failMessage)) await SCTX.RespondSanitizedAsync(ctx, failMessage);
            return userSuccess && botSuccess;
        }
    }
}
