using TBDD.ControlPlane.NOS.FlowEngin;
using TBDD.Properties;
using System;
using System.Collections.Generic;

namespace TBDD.Charts.Intilization
{
    public class DistrubtionsTests
    {
        /// <summary>
        ///  en.H = (i * Hpiovot);
        /// </summary>
        /// <param name="neigCount"></param>
        /// <param name="Hpiovot"></param>
        /// <returns></returns>


        /// <summary>
        /// 5, 200, 10
        /// </summary>
        /// <param name="neiCount"></param>
        /// <param name="disPiovot"></param>
        /// <returns></returns>
        public static List<LinkFlowEnery> TestDvalue(int neiCount,int step, int disPiovot) 
        {
            List<LinkFlowEnery> table = new List<LinkFlowEnery>();
            // normalized values.

            for (int i = 1; i <= neiCount; i++)
            {
                LinkFlowEnery en = new LinkFlowEnery();
                en.D = step + (disPiovot * i);
                en.DN = (en.D) / ((step + (disPiovot * (neiCount + 1))));
                table.Add(en);
            }

            // pro sum
            double DpSum = 0;

            foreach (LinkFlowEnery en in table)
            {
                DpSum += (Math.Pow((1 - Math.Sqrt(en.DN)), 1 + Settings.Default.ExpoDirCnt));
            }

            foreach (LinkFlowEnery en in table)
            {
                en.DP = (Math.Pow((1 - Math.Sqrt(en.DN)), 1 + Settings.Default.ExpoDirCnt)) / DpSum;
            }
            return table;
        }

    


        }
}
