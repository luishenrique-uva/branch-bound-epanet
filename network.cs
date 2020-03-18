using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EpanetCSharpLibrary;

namespace branch_bound_epanet
{
    class network
    {
        public int actuation_max;
        public string f1 = "input.inp", f2 = "input.rpt";
        public int ntank, npump, nsolutions = 0;
        public bool feasible = true;
        public List<pump> pumps;
        public List<tank> tanks;
        public network(int actuation)
        {
            this.actuation_max = actuation;
            this.pumps = new List<pump>();
            this.tanks = new List<tank>();
        }

        public void runinp()
        {
            int tipo = 0, countNodes = 0, countLinks = 0, countPatterns = 0;

            Epanet.ENopen(f1, f2,"");
            Epanet.ENgetcount(0, ref countNodes);
            Epanet.ENgetcount(2, ref countLinks);
            Epanet.ENgetcount(3, ref countPatterns);

         
            for (int i = 1; i <= countNodes; i++)
            {
                Epanet.ENgetnodetype(i, ref tipo);
                if (tipo == 2)
                {
                    tank r = new tank();
                    r.index = i;
                    Epanet.ENgetnodeid(i, r.id);
                    Epanet.ENgetnodevalue(i, 8, ref r.nivelini);
                    Epanet.ENgetnodevalue(i, 20, ref r.nivelmin);
                    Epanet.ENgetnodevalue(i, 21, ref r.nivelmax);
                    r.k = (Math.Log(r.nivelini) - Math.Log(r.nivelmin)) / 24;
                    tanks.Add(r);
                }
            }
            this.ntank = tanks.Count;

           
            string price = "PRICES";
            for (int i = 1; i <= countLinks; i++)
            {
                Epanet.ENgetlinktype(i, ref tipo);
                if (tipo == 2)
                {
                    pump p= new pump();
                    p.index = i;
                    Epanet.ENgetlinkid(i, p.id);
                    //Leitura dos padrões
                    for (int j = 1; j <= countPatterns; j++)
                    {
                        StringBuilder aux = new StringBuilder("");
                        Epanet.ENgetpatternid(j, aux);
                        string st = "PMP" + Convert.ToString(p.id);
                        //string st = "PMP" + Convert.ToString(aux);
                        if (st == Convert.ToString(aux)) p.indexpattern = j;

                        if (Convert.ToString(aux) == price) p.indextariff = j;
                    }
                    pumps.Add(p);
                }
            }
            this.npump = pumps.Count;

            Epanet.ENclose();
        }

        public string[] branch_bound(int cod)
        {
            List<int> lsoli = new List<int>();
            List<string> lsolst = new List<string>();
            int count = 0;
            List<string> lnivelo = new List<string>();
            double costmin = 1000000000000;
            
            string solstmin = "";
            string solimin = "";
           
            DateTime t1, t2;
          
            int h = 0;
            int[] vn = new int[25];
            int[] vnh = new int[25];
           
            float[] vnivelo = new float[this.ntank + 1];

          

            t1 = DateTime.Now;



            for (int i = 1; i <= 24; i++) vnh[i] = npump;
           
            vnh[18] = 1; vnh[19] = 1; vnh[20] = 1; vnh[21] = 1;
           
            
            string stniveli2 = "";
            foreach (tank r in tanks) stniveli2 = stniveli2 + r.nivelini + "-";
            if (cod == 1)
            {
                lsoli.Add(0);
                lsolst.Add("000");
                lnivelo.Add("");
            }
            if (cod == 2)
            {
                string[] vst = File.ReadAllLines(@"lastsol.txt");
                actuation_max = Convert.ToInt16(vst[1]);
                count = Convert.ToInt32(vst[5]);
                Console.WriteLine(vst[5] + " feasible solutions");
                Console.WriteLine("Best Solution");
                Console.WriteLine(vst[6]);
                Console.WriteLine(vst[7]);
                Console.WriteLine(vst[8]);
                solimin = vst[6];
                solstmin = vst[7];
                costmin = Convert.ToDouble(vst[8]);
                int[] vint = ConverterInt(vst[2]);
                for (int i = 1; i <= vint.Length - 1; i++) lsoli.Add(vint[i]);
                string[] vst2 = vst[3].Split('-');
                foreach (string st in vst2) lsolst.Add(st);
                string[] vst3 = vst[4].Split('#');
                foreach (string st in vst3) lnivelo.Add(st);


            }


            Console.WriteLine("press ESC to stop");

           
            t1 = DateTime.Now;
            while (lsoli.Count > 0)
            {
                t2 = DateTime.Now;
                if ((t2 - t1).TotalMinutes > 5)
                {
                    Console.WriteLine("Refresh lastsol.txt..." + DateTime.Now.ToString());
                    string[] vst = new string[9];
                    vst[0] = DateTime.Now.ToString();
                    vst[1] = actuation_max.ToString();
                    vst[2] = string.Join("", lsoli.ToArray());
                    vst[3] = string.Join("-", lsolst.ToArray());
                    vst[4] = string.Join("#", lnivelo.ToArray());
                    vst[5] = count.ToString();
                    vst[6] = solimin;
                    vst[7] = solstmin;
                    vst[8] = costmin.ToString();
                    System.IO.File.WriteAllLines(@"lastsol.txt", vst);
                    t1 = DateTime.Now;
                }

                int hour = lsoli.Count;
                int ramo = lsoli.Last();
                string[] vsolst = lsolst.ToArray();
                string stniveli = "";
                if (hour == 1) stniveli = stniveli2;
                else stniveli = lnivelo[hour - 2];

                string stsol = "";
                foreach (int intl in lsoli) stsol = stsol + intl;
               

                string[] vsta = actuation(vsolst,actuation_max, lsoli.Last());
                if (vsta[0] == "T") lsolst[lsolst.Count - 1] = vsta[1];
                string[] vstn = new string[2];
                if (vsta[0] == "T") vstn = simulation1h(lsolst.Last(), hour, stniveli);
                if (vstn[0] == "T") lnivelo[lnivelo.Count - 1] = vstn[1];
                if (lsoli.Count == 0) goto end;
               
                if (hour < 24 && vstn[0] == "T" && vsta[0] == "T")
                {

                    lnivelo.Add("");
                    lsoli.Add(0);
                    lsolst.Add("000");
                }
                if (hour <= 24) if (vstn[0] == "F" || vsta[0] == "F")
                    {

                        h = hour;
                    here1:
                        if (h == 0)
                        {
                            goto end;
                        }
                        if (lsoli.Last() < vnh[h])
                        {

                            int int1 = lsoli.Last();
                            int1++;
                            lsoli[lsoli.Count - 1] = int1;
                            lsolst[lsolst.Count - 1] = inttocode(int1);
                        }
                        else
                        {
                            lnivelo.RemoveAt(lnivelo.Count - 1);
                            lsoli.RemoveAt(lsoli.Count - 1);
                            lsolst.RemoveAt(lsolst.Count - 1);
                            h--;
                            goto here1;
                        }


                    }
                if (hour == 24 && vstn[0] == "T" && vsta[0] == "T")
                {
                    count++;
                    hour = 24;
                    double cost = energy(vsolst);
                    

                    if (costmin > cost)
                    {
                        Console.WriteLine(count.ToString() + "   " + stsol + "   " + cost.ToString());
                        costmin = cost;
                        solimin = stsol;
                        solstmin = string.Join("-", vsolst);
                    }

                   
                    h = hour;
                here2:
                    if (h == 0) goto end;
                    if (lsoli.Last() < vnh[h])
                    {
                        int int1 = lsoli.Last();
                        int1++;
                        lsoli[lsoli.Count - 1] = int1;
                        lsolst[lsolst.Count - 1] = inttocode(int1);
                    }
                    else
                    {
                       
                        lnivelo.RemoveAt(lnivelo.Count - 1);
                        lsoli.RemoveAt(lsoli.Count - 1);
                        lsolst.RemoveAt(lsolst.Count - 1);
                        h--;
                        goto here2;
                    }
                }
              


                if ((Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                {
                    string[] vst = new string[9];
                    vst[0] = DateTime.Now.ToString();
                    vst[1] = actuation_max.ToString();
                    vst[2] = string.Join("", lsoli.ToArray());
                    vst[3] = string.Join("-", lsolst.ToArray());
                    vst[4] = string.Join("#", lnivelo.ToArray());
                    vst[5] = count.ToString();
                    vst[6] = solimin;
                    vst[7] = solstmin;
                    vst[8] = costmin.ToString();
                    System.IO.File.WriteAllLines(@"lastsol.txt", vst);
                    Console.WriteLine("Check the lastsol.txt file");
                    goto end2;
                }


            end:;

            }

      
        end2:
            string[] vst1 = new string[5]
            {
               "Best Solution:",
               solstmin,
               solimin,
               costmin.ToString(),
               "Total Solutions: "+count.ToString()
            };
            string[] vsol = solstmin.Split('-');
            saveinp(vsol);
            return vst1;
        }

        public int[] ConverterInt(string st)
        {
            string[] vst = new string[st.Length];
            for (int i = 0; i < st.Length; i++) vst[i] = st[i].ToString();
            int[] re = new int[st.Length + 1];
            for (int i = 1; i <= re.Length - 1; i++)
            {
                re[i] = Convert.ToInt32(vst[i - 1]);
            }
            return re;
        }
        public string[] actuation(string[] vsol, int nmax, int nb)
        {
           
            bool bl1 = true;
            int m = vsol.Length;
            int[,] msol = new int[pumps.Count + 1, m + 1];
            for (int j = 1; j <= m; j++)
            {
                int[] vint = ConverterInt(vsol[j - 1]);
                for (int i = 1; i <= pumps.Count; i++)
                    msol[i, j] = vint[i];
            }
            
            int[] k = new int[pumps.Count + 1];

            int n = 1;
        aqui1:
            for (int i = 1; i <= pumps.Count; i++) k[i] = 0;
            for (int i = 1; i <= pumps.Count; i++)
            {

                for (int j = 1; j <= m - 1; j++)
                    if (msol[i, j] == 0 && msol[i, j + 1] == 1) k[i]++;
            }

            bool ult = false;
            for (int i = 1; i <= pumps.Count; i++) if (k[i] - nmax > 0)
                {
                    ult = true;
                    break;
                }
            if (ult && nb == 1 && n == 1)//100=>010
            {
                msol[1, m] = 0;
                msol[2, m] = 1;
                msol[3, m] = 0;
                n++;
                goto aqui1;
            }
            if (ult && nb == 1 && n == 2)//010=>001
            {
                msol[1, m] = 0;
                msol[2, m] = 0;
                msol[3, m] = 1;
                n++;
                goto aqui1;
            }
            if (ult && nb == 2 && n == 1)//110=>101
            {
                msol[1, m] = 1;
                msol[2, m] = 0;
                msol[3, m] = 1;
                n++;
                goto aqui1;
            }
            if (ult && nb == 2 && n == 2)//101=>011
            {
                msol[1, m] = 0;
                msol[2, m] = 1;
                msol[3, m] = 1;
                n++;
                goto aqui1;
            }

           //24<=

            if (ult) bl1 = false;

            string[] vst2 = new string[2];
            if (bl1 == true)
            {
                string st2 = "";
                for (int i = 1; i <= pumps.Count; i++)
                    st2 = st2 + msol[i, m];
                vst2[0] = "T";
                vst2[1] = st2;
            }
            else
            {
                vst2[0] = "F";
            }
            return vst2;
        }

        public string[] simulation1h(string solh, int h, string niveli)
        {
            Epanet.ENopen(f1, f2, "");

          
            Epanet.ENsettimeparam(0, 3600);

            
            int tstep_hid = 0;
            Epanet.ENgettimeparam(1, ref tstep_hid);

         
            float[] cod = solhtof(solh);
            float[] vniveis = ConvertFloat(niveli);
           
            int cont = 0;
            foreach (pump b in pumps)
            {
                cont++;
                Epanet.ENsetpatternvalue(b.indexpattern, h, cod[cont]);
            }

            
            cont = 0;
            foreach (tank tk in tanks)
            {
                cont++;
                Epanet.ENsetnodevalue(tk.index, 8, vniveis[cont]);
            }

         
            Epanet.ENsettimeparam(4, (h - 1) * 3600);

          
            Epanet.ENopenH();
            Epanet.ENinitH(1);
            int erro = 0;
            long tstep = 1;
            long t = 0;
            bool v = true;
            

            while (tstep > 0)
            {
                erro = Epanet.ENrunH(ref t);
                if (erro > 0) v = false;
              
                if (v)
                {
                    float pressure = 0;
                    //n170
                    Epanet.ENgetnodevalue(13, 11, ref pressure);
                    if (pressure < 30 + 0.0001)
                        feasible = false;
                    //n50
                    Epanet.ENgetnodevalue(9, 11, ref pressure);
                    if (pressure < 42 + 0.0001)
                        feasible = false;
                    //n90
                    Epanet.ENgetnodevalue(6, 11, ref pressure);
                    if (pressure < 51 + 0.0001)
                        feasible = false;

                }
             
                if (v) foreach (tank tk in tanks)
                    {
                        float nivel = 0;
                        Epanet.ENgetnodevalue(tk.index, 11, ref nivel);
                        if ((nivel < tk.nivelmin + 0.0001) || (nivel > tk.nivelmax - 0.0001)) v = false;
                       
                    }
                if (v && t == 3600 && h == 24) foreach (tank tk in tanks)
                    {
                        float nivel = 0;
                        Epanet.ENgetnodevalue(tk.index, 11, ref nivel);
                        if (nivel < tk.nivelini + 0.0001) v = false;
                        if ((nivel < tk.nivelmin + 0.0001) || (nivel > tk.nivelmax - 0.0001)) v = false;
                        
                    }
                if (v)
                {
                    int erro2 = Epanet.ENnextH(ref tstep);
                    if (tstep > 0 && tstep < tstep_hid)
                        v = false;
                }
                if (v == false) break;
            }

            float[] re = new float[this.tanks.Count + 1];
            re[0] = 0;

            
            if (v)
            {
                re[0] = 1;
                cont = 0;
                foreach (tank tk in tanks)
                {
                    cont++;
                    Epanet.ENgetnodevalue(tk.index, 11, ref re[cont]);
                }
            }
            Epanet.ENcloseH();
            Epanet.ENclose();
            string[] vst = new string[2];
            vst[1] = ConvertString(re);
            if (v) vst[0] = "T"; else vst[0] = "F";
            return vst;
        }
        public float[] solhtof(string st)
        {
            string[] vst = new string[st.Length];
            float[] r = new float[st.Length + 1];
            for (int i = 0; i < st.Length; i++)
                r[i + 1] = Convert.ToInt32(st[i].ToString());

            return r;


        }
        public float[] ConvertFloat(string niveis)
        {
            float[] re = new float[tanks.Count + 1];
            string[] aux = niveis.Split('-');
            for (int i = 1; i <= tanks.Count; i++)
            {
                re[i] = (float)Convert.ToSingle(aux[i - 1]);
            }
            return re;
        }

        public string ConvertString(float[] niveis)
        {
            string re = "";
            for (int i = 1; i <= tanks.Count; i++)
            {
                re = re + Convert.ToString(niveis[i]) + "-";
            }
            return re;
        }

        public static string inttocode(int k)
        {
            string st = "";

            if (k == 0) st = "000";
            if (k == 1) st = "100";
            if (k == 2) st = "110";
            if (k == 3) st = "111";
            if (k == 4) st = "010";
            if (k == 5) st = "001";
            if (k == 6) st = "101";
            if (k == 7) st = "011";


            return st;
        }

        public double energy(string[] vsol)
        {
           
            double[] vcost = new double[pumps.Count + 1];
            int[] soli = new int[25];
           
            int[,] msol = new int[pumps.Count + 1, 25];
            for (int j = 1; j <= 24; j++)
            {
                int[] vint = ConverterInt(vsol[j - 1]);
                for (int i = 1; i <= pumps.Count; i++)
                    msol[i, j] = vint[i];
            }

            Epanet.ENopen(f1, f2, "");

           
            Epanet.ENsettimeparam(0, 86400);

           
            int tstep_hid = 0;
            Epanet.ENgettimeparam(1, ref tstep_hid);

            
            Epanet.ENsettimeparam(4, 0);

          
            int cont = 0;
            foreach (pump p in pumps)
            {
                cont++;
                for (int h = 1; h <= 24; h++)
                    Epanet.ENsetpatternvalue(p.indexpattern, h, msol[cont, h]);
            }

            
            Epanet.ENopenH();
            Epanet.ENinitH(1);
            long tstep = 1;
            long t = 0;
            while (tstep > 0)
            {
                Epanet.ENrunH(ref t);
                Epanet.ENnextH(ref tstep);
                double db1 = t / 3600;
                int hora = Convert.ToInt16(Math.Truncate(db1)) + 1;
                cont = 0;
                foreach (pump p in pumps)
                {
                    cont++;
                    float pot = 0;
                    Epanet.ENgetlinkvalue(p.index, 13, ref pot);
                    float tariff = 0;
                    Epanet.ENgetpatternvalue(p.indextariff, hora, ref tariff);
                    vcost[cont] += (tstep / 3600.0) * pot * tariff;
                }
            }
            double re = 0;
            cont = 0;
            double soma = 0;
            foreach (pump p in pumps)
            {
                cont++;
                soma = soma + vcost[cont];
            }
            re = soma;
            Epanet.ENcloseH();
            Epanet.ENclose();
            return re;
        }

        public void saveinp(string[] vsol)
        {
           
            double[] vcustoe = new double[pumps.Count + 1];
            int[] soli = new int[25];
           
            int[,] msol = new int[pumps.Count + 1, 25];
            for (int j = 1; j <= 24; j++)
            {
                int[] vint = ConverterInt(vsol[j - 1]);
                for (int i = 1; i <= pumps.Count; i++)
                    msol[i, j] = vint[i];
            }


            Epanet.ENopen(f1, f2, "");
         
            Epanet.ENsettimeparam(0, 86400);

           

            Epanet.ENsettimeparam(4, 0);

          
            int cont = 0;
            foreach (pump p in pumps)
            {
                cont++;
                for (int h = 1; h <= 24; h++)
                    Epanet.ENsetpatternvalue(p.indexpattern, h, msol[cont, h]);
            }
            Epanet.ENsaveinpfile("output.inp");
            //Epanet.ENsolveH();
            Epanet.ENclose();
        }
    }
}
