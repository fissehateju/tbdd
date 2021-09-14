using TBDD.Dataplane;
using TBDD.Intilization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TBDD.Models.Cell;

namespace TBDD.Constructor
{
    public static class PrintingOutput
    {
        public static void printing()
        {
            foreach (LocalRegProp lp in PublicParameters.listOfRegs)
            {
                Console.WriteLine("Region : {0}", lp.Id);
                Console.WriteLine("topleft:( {0}, {1} )", lp.BorderPoints[0].X, lp.BorderPoints[0].Y);
                Console.WriteLine("topright:( {0}, {1} )", lp.BorderPoints[1].X, lp.BorderPoints[1].Y);
                Console.WriteLine("Bottomleft:( {0}, {1} )", lp.BorderPoints[2].X, lp.BorderPoints[2].Y);
                Console.WriteLine("Bottomright:( {0}, {1} )", lp.BorderPoints[3].X, lp.BorderPoints[3].Y);
                Console.WriteLine("number of nodes: {0}", lp.numberOfNodes);
                Console.WriteLine("local Centeriod: ( {0},{1} )", lp.localCenteriod.X, lp.localCenteriod.Y);
                Console.WriteLine("Most Center Node : {0} ", lp.centerNode.ID);
                Console.WriteLine("=====================================================");
            }
            foreach (CellGroup cluster in PublicParameters.networkCells)
            {
                Console.WriteLine("cluster {0}: location {1}: Actualcenter {2}", cluster.id, cluster.clusterReg, cluster.clusterActualCenter);

            }
            foreach (Sensor sen in PublicParameters.myNetwork)
            {
                Console.WriteLine("sensor {0}: location {1}: inside cell {2}: nearCell {3}", sen.ID, sen.sReg, sen.inCell, sen.TBDDNodeTable.NearestCellId);
            }
            foreach (LocalRegProp reg in PublicParameters.listOfRegs)
            {
                Console.WriteLine("\nregion {0}: contains the following cells:", reg.Id);
                foreach (CellGroup cltr in reg.Cells)
                {
                    Console.Write("cluster {0} ; ", cltr.id);

                }
                Console.WriteLine("");
            }

        }
    }
}
