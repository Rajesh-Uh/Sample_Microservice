using FluentValidation;
using Precision.WebApi.Implementation;

namespace Precision.Contracts.Request.ChatService
{
    public class ChatPatchRequest : EasyPatchModelMapBase<ChatPatchRequest>
    {
        public ChatPatchRequest() : base(new ChatPatchValidator()) { }
        public override IEnumerable<KeyValuePair<string, string>> Validate()
        {
            return base.GetValidationErrors(this);
        }
        private class ChatPatchValidator : AbstractPatchValidator<ChatPatchRequest>
        {
            public ChatPatchValidator()
            {
                WhenBound(x => x.IsRead, rule => rule.NotEmpty());
            }
        }

        public bool IsRead { get; set; }
    }
}