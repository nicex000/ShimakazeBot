using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireServerOwnerAttribute : SlashCheckBaseAttribute
    {
        public string failMessage;

        public RequireServerOwnerAttribute(string failMessage = "Only the server owner can use this command.")
        {
            this.failMessage = failMessage;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            CannotBeUsedInDMAttribute checkDM = new CannotBeUsedInDMAttribute();
            if (!await checkDM.ExecuteChecksAsync(ctx))
            {
                return false;
            }

            var app = ctx.Client.CurrentApplication;
            if (app != null)
            {
                bool success = ctx.Member == ctx.Guild.Owner || app.Owners.Any(x => x.Id == ctx.User.Id);
                if (!success && !string.IsNullOrWhiteSpace(failMessage))
                {
                    await SCTX.RespondSanitizedAsync(ctx, failMessage);
                }

                return success;
            }

            var me = ctx.Client.CurrentUser;
            return ctx.User.Id == me.Id;
        }
    }
}
