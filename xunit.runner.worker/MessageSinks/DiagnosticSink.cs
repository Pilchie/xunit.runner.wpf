using Xunit.Abstractions;

namespace Xunit.Runner.Worker.MessageSinks
{
    internal class DiagnosticSink : BaseMessageSink
    {
        protected override bool OnMessage(IMessageSinkMessage message)
        {
            return true;
        }
    }
}
