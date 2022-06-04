using System.Collections.Generic;
using TBDD.Dataplane;
using TBDD.Intilization;
using TBDD.Dataplane.PacketRouter;

namespace TBDD.DataPlane.NeighborsDiscovery
{
    public class NeighborsDiscovery
    {
       private List<Sensor> Network;
       public NeighborsDiscovery(List<Sensor> _NetworkNodes)
       {
           Network=_NetworkNodes; 
       }
        /// <summary>
        /// only those nodes which follow within the range of i.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private void DiscoverMyNeighbors(Sensor i)
        {
            i.NeighborsTable = new List<NeighborsTableEntry>();
            i.myNeighborsTable = new List<Sensor>();
            // get the overlapping nodes:
            if (Network != null)
            {
                if (Network.Count > 0)
                {
                    foreach (Sensor node in Network)
                    {
                        if (i.ID != node.ID && node.ID != PublicParameters.SinkNode.ID)
                        {
                            bool isOverlapped = Operations.isInMyComunicationRange(i, node);
                            if (isOverlapped)
                            {
                                NeighborsTableEntry en = new NeighborsTableEntry();
                                en.NeiNode = node;
                          
                                i.NeighborsTable.Add(en);
                                i.myNeighborsTable.Add(node);
                            }
                        }
                    }
                }
            }
        }

       /// <summary>
       /// for all nodes inside the newtwork find the overllapping nodes.
       /// </summary>
       public void GetOverlappingForAllNodes()
       {
           foreach(Sensor node in Network)
           {
               DiscoverMyNeighbors(node);
           }
       }









    }
}
