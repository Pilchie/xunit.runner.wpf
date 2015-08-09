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
        //[Trait("TraitName1", "TraitValue1")]
        public void Pass() { }

        [Fact]
        //[Trait("TraitName1", "TraitValue2")]
        public void Fail()
        {
            Assert.True(false);
        }

        [Fact(Skip = "Testing")]
        //[Trait("TraitName2", "TraitValue2")]
        public void Skip() { }
    }
}
