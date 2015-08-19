using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.data
{
    public static class Constants
    {
        public const string PipeName = "xunit.runners.pipe";
        public const string ActionDiscover = "discover";
        public const string ActionRun = "run";
        public static readonly Encoding Encoding = Encoding.UTF8;
    }
}
