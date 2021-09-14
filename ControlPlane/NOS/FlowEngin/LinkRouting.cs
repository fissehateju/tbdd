using TBDD.Dataplane;
using TBDD.Dataplane.NOS;
using TBDD.Dataplane.PacketRouter;
using TBDD.Intilization;
using System;
using System.Collections.Generic;
using System.Windows;

namespace TBDD.ControlPlane.NOS.FlowEngin
{
    public class LinkFlowEnery
    {
        // Elementry values:
        public double D { get; set; } // direction value tworads the end node
        public double DN { get; set; } // R NORMALIZEE value of To. 
        public double DP { get; set; } // defual.

    }

    public class LinkRouting
    {
        public static FlowTableEntry getBiggest(List<FlowTableEntry> table)
        {
            double offset = -10;
            FlowTableEntry biggest = null;
            foreach (FlowTableEntry entry in table)
            {                
                if (entry.DownLinkPriority > offset)
                {
                    offset = entry.DownLinkPriority;
                    biggest = entry;
                }
            }
            return biggest;
        }
        public static void sortTable(Sensor sender)
        {
            List<FlowTableEntry> beforeSort = sender.TBDDFlowTable;
            List<FlowTableEntry> afterSort = new List<FlowTableEntry>();
            do
            {
                FlowTableEntry big = getBiggest(beforeSort);
                if (big != null)
                {
                    afterSort.Add(big);
                    beforeSort.Remove(big);
                }
                else
                {                
                    beforeSort.Clear();
                    return;
                }
                if (afterSort.Count > 20)
                {
                    beforeSort.Clear();
                    return;
                }

            } while (beforeSort.Count > 0);
            sender.TBDDFlowTable.Clear();
            sender.TBDDFlowTable = afterSort;

        }
        /// <summary>
        /// This will be change per sender.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endNode"></param>

        public static void GetD_Distribution(Sensor sender, Packet packet)
        {
            List<int> path = Operations.PacketPathToIDS(packet.Path);
            sender.TBDDFlowTable.Clear();
            List<int> PacketPath = Operations.PacketPathToIDS(packet.Path);

            Sensor sourceNode = sender; // packet.Source;
            Point endNodePosition;
            if (packet.Destination != null)
            {
                endNodePosition = packet.Destination.CenterLocation;
            }
            else
            {
                endNodePosition = packet.DestinationAddress;
            }

            double n = Convert.ToDouble(sender.NeighborsTable.Count) + 1;

            foreach (NeighborsTableEntry neiEntry in sender.NeighborsTable)
            {
                if (packet.PacketType == PacketType.Data && !packet.isDataStartUsingTree &&  neiEntry.NeiNode.inCell != -1 && neiEntry.NeiNode.TBDDNodeTable.CellHeaderTable.isHeader) 
                {
                    continue;
                }

                if (neiEntry.NeiNode.ResidualEnergyPercentage > 0)
                {
                    if (neiEntry.ID != PublicParameters.SinkNode.ID)
                    {
                        FlowTableEntry MiniEntry = new FlowTableEntry();
                        MiniEntry.SID = sender.ID;
                        MiniEntry.NeighborEntry = neiEntry;
                       
                        MiniEntry.NeighborEntry.DirProb = Operations.GetAngleDotProduction(sender.CenterLocation, MiniEntry.NeighborEntry.CenterLocation, endNodePosition);
                        if (MiniEntry.NeighborEntry.DirProb < 0)
                        {
                            MiniEntry.NeighborEntry.DirProb = 0;
                        }

                        MiniEntry.NeighborEntry.pirDisProb = Operations.GetPerpendicularProbability(sourceNode.CenterLocation, MiniEntry.NeighborEntry.CenterLocation, endNodePosition);
                        MiniEntry.NeighborEntry.TransDisProb = Operations.GetTransmissionDistanceProbability(MiniEntry.NeighborEntry.CenterLocation, endNodePosition);
                        MiniEntry.NeighborEntry.ResEnergyProb = Operations.GetResidualEnergyProbability(MiniEntry.NeighborEntry.NeiNode.ResidualEnergy);

                        MiniEntry.NeighborEntry.pirDis = Operations.GetPerpindicularDistance(sourceNode.CenterLocation, MiniEntry.NeighborEntry.CenterLocation, endNodePosition);

                        MiniEntry.NeighborEntry.pirDisNorm = (MiniEntry.NeighborEntry.pirDis / PublicParameters.CommunicationRangeRadius);
                        MiniEntry.NeighborEntry.RE = MiniEntry.NeighborEntry.ResEnergyProb;
                        MiniEntry.NeighborEntry.EDNorm = MiniEntry.NeighborEntry.TransDisProb;
                        MiniEntry.NeighborEntry.Direction = MiniEntry.NeighborEntry.DirProb;

                        sender.TBDDFlowTable.Add(MiniEntry);

                    }                   
                }
            }

            double RESum = 0;
            double TDSum = 0;
            double DirSum = 0;
            double PirSum = 0;
         
            sender.CW.getDynamicWeight(packet, sender);
            foreach (FlowTableEntry MiniEntry in sender.TBDDFlowTable)
            {
                MiniEntry.NeighborEntry.TransDisProb *= sender.CW.TDWeight;
                if (MiniEntry.NeighborEntry.DirProb < 0)
                {
                    MiniEntry.NeighborEntry.DirProb = 0;
                }
                else
                {
                    MiniEntry.NeighborEntry.DirProb *= sender.CW.DirWeight;
                }
              
                
                MiniEntry.NeighborEntry.pirDisProb *= sender.CW.PirpWeight;
                MiniEntry.NeighborEntry.ResEnergyProb *= sender.CW.EnergyWeight;
            }

         
            foreach (FlowTableEntry MiniEntry in sender.TBDDFlowTable)
            {
                if (MiniEntry.NID != PublicParameters.SinkNode.ID)
                {
                    RESum += MiniEntry.NeighborEntry.ResEnergyProb;
                    TDSum += MiniEntry.NeighborEntry.TransDisProb;
                    DirSum += MiniEntry.NeighborEntry.DirProb;
                    PirSum += MiniEntry.NeighborEntry.pirDisProb;
                }
               
            }
           
            double downLinkSum =0;
            foreach (FlowTableEntry MiniEntry in sender.TBDDFlowTable)
            {
                if (MiniEntry.NID != PublicParameters.SinkNode.ID)
                {
                    MiniEntry.NeighborEntry.TransDisProb /=TDSum;
                    if (MiniEntry.NeighborEntry.DirProb < 0)
                    {
                        MiniEntry.NeighborEntry.DirProb = 0;
                    }
                    else if(DirSum !=0)
                    {
                        MiniEntry.NeighborEntry.DirProb /= DirSum;
                    }
                 
                    MiniEntry.NeighborEntry.ResEnergyProb /= RESum;
                    MiniEntry.NeighborEntry.pirDisProb /=PirSum;
                    /*  if (packet.PacketType == PacketType.QReq || packet.PacketType == PacketType.QResp)
                      {
                          MiniEntry.NeighborEntry.pirDisProb = 0;
                      }
                      */
                    if (endNodePosition != packet.Source.CenterLocation)
                    {
                        MiniEntry.DownLinkPriority = (MiniEntry.NeighborEntry.TransDisProb + MiniEntry.NeighborEntry.DirProb + MiniEntry.NeighborEntry.ResEnergyProb + MiniEntry.NeighborEntry.pirDisProb) / 4;
                    }
                    else
                    {
                        MiniEntry.DownLinkPriority = (MiniEntry.NeighborEntry.TransDisProb + MiniEntry.NeighborEntry.DirProb + MiniEntry.NeighborEntry.ResEnergyProb) / 3;
                    }
                    downLinkSum += MiniEntry.DownLinkPriority;

                }                             
            }

            sortTable(sender);

            //int a = packet.Hops;
            //a ++;  
           
                double average = 1 / Convert.ToDouble(sender.TBDDFlowTable.Count);
                int Ftheashoeld = Convert.ToInt16(Math.Ceiling(Math.Sqrt(Math.Sqrt(n)))); // theshold.
                int forwardersCount = 0;
                int minus;
                if (path.Count < 2)
                {
                    minus = 1;
                }
                else
                {
                    minus = 2;
                }
                int lastForwarder = path[path.Count - minus];


            foreach (FlowTableEntry MiniEntry in sender.TBDDFlowTable)
            {

                double dir = Operations.GetDirectionAngle(sender.CenterLocation, MiniEntry.NeighborEntry.CenterLocation, endNodePosition);
                double dist = Operations.DistanceBetweenTwoPoints(sender.CenterLocation, endNodePosition);
                double distCand = Operations.DistanceBetweenTwoPoints(MiniEntry.NeighborEntry.CenterLocation, endNodePosition);
                if (MiniEntry.DownLinkPriority >= average && forwardersCount <= Ftheashoeld)// && MiniEntry.NID != lastForwarder)

                {
                    if (!path.Contains(MiniEntry.NID))
                    {
                        MiniEntry.DownLinkAction = FlowAction.Forward;
                        forwardersCount++;
                    }
                    else if (dir < 0.2 && (distCand < dist))
                    {
                        MiniEntry.DownLinkAction = FlowAction.Forward;
                        forwardersCount++;
                    }
                    else
                    {
                        MiniEntry.DownLinkAction = FlowAction.Drop;
                    }


                }
                else
                {
                    MiniEntry.DownLinkAction = FlowAction.Drop;
                }

            }

            if (forwardersCount == 0)
            {
                foreach (FlowTableEntry MiniEntry in sender.TBDDFlowTable)
                {
                    double srcEnd = Operations.DistanceBetweenTwoPoints(sender.CenterLocation, endNodePosition);
                    double candEnd = Operations.DistanceBetweenTwoPoints(MiniEntry.NeighborEntry.CenterLocation, endNodePosition);
                    if (MiniEntry.DownLinkPriority >= average && forwardersCount <= Ftheashoeld)
                    {
                        MiniEntry.DownLinkAction = FlowAction.Forward;
                        forwardersCount++;
                    }
                    else
                    {
                        MiniEntry.DownLinkAction = FlowAction.Drop;
                    }

                }
            }

        }

         
    }
}
