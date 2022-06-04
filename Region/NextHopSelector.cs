using TBDD.Dataplane;
using TBDD.Dataplane.NOS;
using TBDD.Dataplane.PacketRouter;
using TBDD.Intilization;
using TBDD.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TBDD.Constructor;
using TBDD.Models.Cell;
using TBDD.ControlPlane.NOS.FlowEngin;
using TBDD.Region.Routing;

namespace TBDD.Region
{
    public class CoordinationEntry
    {
        public double Priority { get; set; }
        public int SensorID { get { return Sensor.ID; } }
        public Sensor Sensor { get; set; }

        public double MinRange { get; set; }
        public double MaxRange { get; set; }

    }
    public class NextHopSelector
    {
        private NetworkOverheadCounter counter = new NetworkOverheadCounter();
        public Sensor selectQRYNextHop(Sensor ni, Packet packet)
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            double sum = 0;
            Sensor sj = null;
            foreach (Sensor nj in ni.myNeighborsTable)
            {
                if (nj.ResidualEnergyPercentage > 0)
                {
                    double Norangle;
                    if (nj.CenterLocation == packet.DestinationAddress)
                    {
                        return nj;
                    }
                    else
                    {
                        Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
                        if (Double.IsNaN(Norangle) || Norangle == 0 || Norangle < 0.0001)
                        {
                            // for none-recovery we had the destination.
                            return nj;
                        }
                        else
                        {
                            if (Norangle <= 0.5)
                            {
                                double ij_ψ = Operations.GetPerpendicularProbability(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
                                double ij_σ = Operations.GetResidualEnergyProbability(nj.ResidualEnergy);
                                double ij_d = Operations.GetTransmissionDistanceProbability(nj.CenterLocation, packet.DestinationAddress);

                                double defual = ij_ψ + ij_d + ij_σ;

                                double aggregatedValue = defual;
                                sum += aggregatedValue;
                                coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj });
                            }
                        }
                    }
                }
            }

            //sj = counter.RandomCoordinate(coordinationEntries, packet, sum);

            double min = 0.0;

            foreach (CoordinationEntry neiEntry in coordinationEntries)
            {
                if (neiEntry.Sensor.ResidualEnergy > 0)
                {
                    double val = neiEntry.Priority;
                    double normalized = val / sum;
                    neiEntry.Priority = normalized;
                }
                else
                {
                    neiEntry.Priority = 0;
                }

                if (neiEntry.Priority > min)
                {
                    min = neiEntry.Priority;
                    sj = neiEntry.Sensor;
                }

            }

            return sj;

        }
    }
}
