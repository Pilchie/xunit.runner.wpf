using Xunit.Abstractions;

namespace Xunit.Runner.Worker
{
    internal sealed class MessageVisitor : TestMessageVisitor
    {
        public override bool OnMessage(IMessageSinkMessage message)
        {
            return base.OnMessage(message);
        }
    }
}
