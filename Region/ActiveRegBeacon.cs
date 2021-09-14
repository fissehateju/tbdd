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
using TBDD.NetAnimator;
using TBDD.ControlPlane.NOS.FlowEngin;

namespace TBDD.Region
{  
    public class ActiveRegBeacon
    {
        private int newRegId { get; set; }
        private List<Sensor> newRegOnHand = new List<Sensor>();
        private Queue<CellGroup> nextSender = new Queue<CellGroup>();
        private NetworkOverheadCounter counter = new NetworkOverheadCounter();

        public ActiveRegBeacon()
        {

        }
        public ActiveRegBeacon(int ARegid)
        {
            newRegId = ARegid;
            Sensor rootheader = CellGroup.getClusterWithID(Tree.rootClusterID).CellTable.CellHeader;
            rootheader.TBDDNodeTable.CellHeaderTable.activeRegID = ARegid;
            newRegOnHand.Add(rootheader);
            Packet packet = GeneratePacket(rootheader);
            sendingBeaconToNeighbor(CellGroup.getClusterWithID(Tree.rootClusterID), packet);

            //foreach (CellGroup I in PublicParameters.networkCells)
            //    Console.WriteLine("Cell {0} header is {1}", I.getID(), I.CellTable.CellHeader.ID);
        }

        private Packet GeneratePacket(Sensor rootheader)
        {
            Packet pck = new Packet();            
            pck.Path = "" + rootheader.ID;
            pck.Source = rootheader;
            pck.PacketType = PacketType.RegBeacon;
            pck.PacketLength = PublicParameters.ControlDataLength;
            pck.activeRegId = newRegId;

            counter.IncreasePacketsCounter(pck.Source, PacketType.RegBeacon);
            return pck;
        }

        private void sendingBeaconToNeighbor(CellGroup sendingCell, Packet packet)
        {            

            if (sendingCell != null && !sendingCell.isLeafNode)
            {
                foreach (CellGroup recCell in sendingCell.childrenClusters)
                {
                    Sensor sender = sendingCell.CellTable.CellHeader;

                    Packet newpack = packet.Clone() as Packet;

                    PublicParameters.OverallGeneratedPackets += 1;
                    newpack.isDelivered = false;
                    newpack.Source = sendingCell.CellTable.CellHeader;
                    newpack.Destination = recCell.CellTable.CellHeader;
                    newpack.DestinationAddress = newpack.Destination.CenterLocation;
                    newpack.PID = PublicParameters.OverallGeneratedPackets;
                    newpack.Path = "" + sendingCell.CellTable.CellHeader.ID;
                    newpack.Hops = 0;
                    newpack.ReTransmissionTry = 0;
                    double DIS = Math.Sqrt(Math.Pow(PublicParameters.CommunicationRangeRadius, 2) + Math.Pow(PublicParameters.CommunicationRangeRadius, 2)) * 3;
                    newpack.TimeToLive = 3 + Convert.ToInt16((Operations.DistanceBetweenTwoPoints(newpack.Source.CenterLocation, newpack.Destination.CenterLocation) / (PublicParameters.CommunicationRangeRadius / 3)));

                    sendRegbeacon(newpack, sender);
                }
            }                
        }

        public void sendRegbeacon(Packet pck, Sensor sender)
        {       
            sender.Mac.SwichToActive();
            Sensor Reciver = SelectNextHop(sender, pck);

            if (Reciver != null)
            {
                // overhead:
                sender.ComputeOverhead(pck, EnergyConsumption.Transmit, Reciver);
                recieveRegbeacon(pck, Reciver);
            }
            else
            {
                counter.SaveToQueue(sender, pck);
            }

        }

        private void recieveRegbeacon(Packet pck, Sensor reciv)
        {
            pck.Path += ">" + reciv.ID;

            if (reciv == pck.Destination)
            {                   
                reciv.TBDDNodeTable.CellHeaderTable.activeRegID = pck.activeRegId;                   

                if (Settings.Default.ShowAnimation) 
                {
                    Action actionx = () => reciv.Ellipse_indicator.Fill = Brushes.BlueViolet;
                    reciv.Dispatcher.Invoke(actionx);
                    Action actionx1 = () => reciv.Ellipse_indicator.Visibility = Visibility.Visible;
                    reciv.Dispatcher.Invoke(actionx1);
                }

                pck.isDelivered = true;
                reciv.updateStates(pck);
                Console.WriteLine("{0} got packet '{1}' from {2}", reciv.ID, pck.PacketType, pck.Source.ID);

                //Console.WriteLine("{0} recieved packete {1} : Active Region is {2}", reciv.ID, pck.PacketType, pck.DestCell.CellTable.activeRegId);
                //Console.WriteLine("my reg id = {0} or {1} , active Region = {2}", reciv.sReg,pck.DestCell.clusterReg, reciv.TBDDNodeTable.CellHeaderTable.activeRegID);

                sendingBeaconToNeighbor(CellGroup.getClusterWithID(reciv.inCell), pck);
            }
            else
            {
                if (pck.Hops <= pck.TimeToLive)
                {
                    reciv.ComputeOverhead(pck, EnergyConsumption.Recive, null);
                    sendRegbeacon(pck, reciv);
                }
                else
                {
                    MessageBox.Show("beacon TTL dropped");
                    pck.isDelivered = false;
                    reciv.updateStates(pck);
                }
            }

        }

        public static double EnergyDistribution(double CurentEn, double intialEnergy)
        {
            if (CurentEn > 0)
            {
                double σ = CurentEn / intialEnergy;
                double γ = 1.0069, ε = 0.70848, ϑ = 17.80843;
                double re = γ / (1 + Math.Exp(-ϑ * (σ - ε)));
                return re;
            }
            else
                return 0;
        }
        public Sensor SelectNextHop(Sensor ni, Packet packet)
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            double sum = 0;
            Sensor sj = null;
            foreach (Sensor nj in ni.myNeighborsTable)
            {
                   
                if (packet.Destination != null && nj.ID == packet.Destination.ID)
                {
                    return nj;
                }
                else
                {

                    double pj = Operations.Perpendiculardistance(nj.CenterLocation, packet.Source.CenterLocation, packet.Destination.CenterLocation);
                    double pi = Operations.Perpendiculardistance(ni.CenterLocation, packet.Source.CenterLocation, packet.Destination.CenterLocation);
                    double npj = pj / (pi + PublicParameters.CommunicationRangeRadius);
                    double Edis = EnergyDistribution(nj.ResidualEnergyPercentage, 100);
                    double disPij = Math.Exp(-npj);
                    double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, packet.Destination.CenterLocation);

                    if (Double.IsNaN(Norangle) || Norangle < 0.5)
                    {
                        double disAngij;
                        if (Double.IsNaN(Norangle))
                            disAngij = Math.Exp(-0);
                        else
                            disAngij = Math.Exp(-Norangle);

                        double aggregatedValue = disAngij * (disPij + Edis);
                        sum += aggregatedValue;
                        coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
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
