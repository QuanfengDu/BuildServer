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

    public class TestDriver4 : ITest
    {
        public bool say()
        {
            TestedOne4 one = new TestedOne4();
            one.say();
            TestedTwo4 two = new TestedTwo4();
            two.say();
            return true;  // just pretending to test something
        }

    }
}
