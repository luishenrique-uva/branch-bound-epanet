using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace branch_bound_epanet
{
    public class tank
    {
        public int index;
        public StringBuilder id;
        public float nivelini, nivelmax, nivelmin, nivelfs;
        public double k;
        public tank()
        {
            this.id = new StringBuilder();
        }
    }
}
