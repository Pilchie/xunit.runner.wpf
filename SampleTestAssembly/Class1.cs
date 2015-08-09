using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SampleTestAssembly
{
    public class Class1
    {
        [Fact]
        public void Pass() { }

        [Fact]
        public void Fail()
        {
            Assert.True(false);
        }

        [Fact(Skip = "Testing")]
        public void Skip() { }
    }
}
