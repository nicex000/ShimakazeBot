using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Shimakaze.Attributes
{
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        class CannotBeUsedInDMAttribute : CheckBaseAttribute
        {
            public string failMessage;

            public CannotBeUsedInDMAttribute(string failMessage = "You can't use this command in a DM dummy~!")
            {
                this.failMessage = failMessage;
            }

            public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                if (!(ctx.Guild is null))
                {
                    return true;
                }
                await CTX.RespondSanitizedAsync(ctx, failMessage);
                return false;
            }
        }
}