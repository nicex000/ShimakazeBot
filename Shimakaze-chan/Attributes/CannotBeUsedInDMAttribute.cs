using System;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;

namespace Shimakaze.Attributes
{
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        class CannotBeUsedInDMAttribute : SlashCheckBaseAttribute
    {
            public string failMessage;

            public CannotBeUsedInDMAttribute(string failMessage = "You can't use this command in a DM dummy~!")
            {
                this.failMessage = failMessage;
            }

            public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
            {
                if (ctx.Guild != null)
                {
                    return true;
                }
                await SCTX.RespondSanitizedAsync(ctx, failMessage);
                return false;
            }
        }
}