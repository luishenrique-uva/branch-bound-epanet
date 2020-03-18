using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace branch_bound_epanet
{
    public class pump
    {
        public int index;
        public int indexpattern;
        public int indextariff;
        public double energia;
        public StringBuilder id;
        public pump()
        {
            this.id = new StringBuilder();
        }
    }
}


