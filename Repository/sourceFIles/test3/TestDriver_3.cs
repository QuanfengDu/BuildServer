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

    public class TestDriver3 : ITest
    {
        public bool say()
        {
            TestedOne3 one = new TestedOne3();
            one.say();
            TestedTwo3 two = new TestedTwo3();
            two.say();
            return true;  // just pretending to test something
        }

    }
}
