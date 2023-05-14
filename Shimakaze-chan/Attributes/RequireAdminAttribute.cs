using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireAdminAttribute : SlashCheckBaseAttribute
    {
        public string failMessage;
        public bool skipDMMessage;

        public RequireAdminAttribute(string failMessage = "Only a server admin can use this command.",
            bool skipDMMessage = false)
        {
            this.failMessage = failMessage;
            this.skipDMMessage = skipDMMessage;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            CannotBeUsedInDMAttribute checkDM =
                skipDMMessage ? new CannotBeUsedInDMAttribute("") : new CannotBeUsedInDMAttribute();
            if (!await checkDM.ExecuteChecksAsync(ctx))
            {
                return false;
            }

            var user = ctx.Member;
            if (user == null)
            {
                return false;
            }

            var app = ctx.Client.CurrentApplication;
            if (app == null)
            {
                return false;
            }

            if (user.Id == ctx.Guild.Owner.Id || app.Owners.Any(x => x.Id == ctx.User.Id))
            {
                return true;
            }

            var pUser = ctx.Channel.PermissionsFor(user);
            if ((pUser & Permissions.Administrator) != 0)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(failMessage))
            {
                await SCTX.RespondSanitizedAsync(ctx, failMessage);
            }
            
            return false;
        }
    }
}
