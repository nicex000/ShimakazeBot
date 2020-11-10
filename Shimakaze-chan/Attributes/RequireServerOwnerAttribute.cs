using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireServerOwnerAttribute : CheckBaseAttribute
    {
        public string failMessage;

        public RequireServerOwnerAttribute(string failMessage = "Only the server owner can use this command.")
        {
            this.failMessage = failMessage;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            CannotBeUsedInDMAttribute checkDM = new CannotBeUsedInDMAttribute();
            if (!await checkDM.ExecuteCheckAsync(ctx, help))
            {
                return false;
            }

            var app = ctx.Client.CurrentApplication;
            if (app != null)
            {
                bool success = help || ctx.Member == ctx.Guild.Owner || app.Owners.Any(x => x.Id == ctx.User.Id);
                if (!success && !string.IsNullOrWhiteSpace(failMessage))
                {
                    await CTX.RespondSanitizedAsync(ctx, failMessage);
                }

                return success;
            }

            var me = ctx.Client.CurrentUser;
            return ctx.User.Id == me.Id;
        }
    }
}
