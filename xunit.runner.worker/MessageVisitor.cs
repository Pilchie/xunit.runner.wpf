using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.worker
{
    internal sealed class MessageVisitor : TestMessageVisitor
    {
        public override bool OnMessage(IMessageSinkMessage message)
        {
            return base.OnMessage(message);
        }
    }
}
