using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBuild2
{
    interface ITest
    {
        bool say();
    }

    public class TestDriver2 : ITest
    {
        public bool say()
        {
            TestedOne2 one = new TestedOne2();
            one.say();
            TestedTwo2 two = new TestedTwo2();
            two.say();
            return true;  // just pretending to test something
        }

    }
}
