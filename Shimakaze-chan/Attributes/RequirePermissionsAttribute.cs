using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequirePermissionsAttribute : CheckBaseAttribute
    {
        public Permissions permissions;

        public RequirePermissionsAttribute(Permissions permissions)
        {
            this.permissions = permissions;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            CannotBeUsedInDMAttribute checkDM = new CannotBeUsedInDMAttribute();
            if (!await checkDM.ExecuteCheckAsync(ctx, help))
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
            
            if (help)
            {
                return true;
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

            if (!string.IsNullOrWhiteSpace(failMessage)) await CTX.RespondSanitizedAsync(ctx, failMessage);
            return userSuccess && botSuccess;
        }
    }
}
