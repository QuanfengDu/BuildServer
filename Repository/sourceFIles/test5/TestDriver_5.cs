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

    public class TestDriver5 : ITest
    {
        public bool say()
        {
            TestedOne5 one = new TestedOne5();
            one.say();
            TestedTwo5 two = new TestedTwo5();
            two.say();
            return true;  // just pretending to test something
        }

    }
}
