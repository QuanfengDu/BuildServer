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

    public class TestDriver : ITest
    {
        public bool say()
        {
            TestedOne one = new TestedOne();
            one.say();
            TestedTwo two = new TestedTwo();
            two.say();
            return true;  // just pretending to test something
        }

    }
}
