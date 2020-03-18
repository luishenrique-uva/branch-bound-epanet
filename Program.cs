using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace branch_bound_epanet
{
    class Program
    {
        static void Main(string[] args)
        {


            Console.WriteLine("Select \n (1) Begin \n (2) Last Solution");
            int cod = Convert.ToInt32(Console.ReadLine());
            int nmax = 0;
            if (cod == 1)
            {
                Console.WriteLine("Limit Number of actuacions");
                nmax = Convert.ToInt32(Console.ReadLine());
            }
            network net = new network(nmax);
            net.runinp();
            DateTime t1, t2;
            t1 = DateTime.Now;
            try
            {
                string[] vst = net.branch_bound(cod);
                foreach (string st in vst)
                    Console.WriteLine(st);


                t2 = DateTime.Now;
                var time = (t2 - t1).TotalHours;
                Console.WriteLine("Total Time:{0} (hours)", time);
                Console.WriteLine("Check the output.inp file");
                Console.WriteLine("Enter to close");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }



        }



    }
}
