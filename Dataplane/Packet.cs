using TBDD.Intilization;
using System.Windows;
using System;
using System.Collections.Generic;
using TBDD.Properties;
using TBDD.Constructor;
namespace TBDD.Dataplane.NOS
{
    public enum PacketType 
    { 
        RegBeacon, 
        Beacon, 
        Preamble, 
        ACK, Data, 
        AS,
        FM,
        FSA,
        QReq,
        QResp,
        Control 
    }

    public enum PacketDropedReasons
    {
        NULL,
        TimeToLive,
        WaitingTime,
        Loop,
        RecoveryNoNewAgentFound,
        RecoveryPeriodExpired,
        DeadNode,
        NoForwarderAvail,
        NOdestination,
        Unknow
    }
    public class Packet : ICloneable, IDisposable
    {
        //: Packet section:
        public long PID { get; set; } // SEQ ID OF PACKET.
        public PacketType PacketType { get; set; }
        public bool isDelivered { get; set; }
        public bool IsLooped { get; set; }
        public int LoopCnt { get; set; }
        public double PacketLength { get; set; }
        public int ringSourceID { get; set; }
        public int TimeToLive { get; set; }
        public int Hops { get; set; }
        public string Path { get; set; }
        public int prevSenId { get; set; }
        public double RoutingDistance { get; set; }

        public double UsedEnergy_Joule { get; set; }
        public int WaitingTimes { get; set; }

        public PacketDropedReasons DroppedReason { get; set; }
        public string aDroppedReason { get; set; }
        public Point DestinationAddress { get; set; }
        public bool isDataInsideCell { get; set; }
        public double Delay = 0;
        public int ReTransmissionTry { get; set; }
        public bool isDataRouted = false;
        public Sensor ReRouteSource { get; set; }
        public bool isDataStartUsingTree { get; set; }
        public bool isInSendData { get; set; }
        public int activeRegId { get; set; }
        public bool isDataArrivedAtRootheader = false;
        public bool isDataInsideCluster { get; set; }
        public bool isRedlightON = false;
        public Point PossibleDest { get; set; }


        /// <summary>
        /// Average Transmission Distance (ATD): for〖 P〗_b^s (g_k ), we define average transmission distance per hop as shown in (28).
        /// </summary>
        public double AverageTransDistrancePerHop
        {
            get
            {
                return (RoutingDistance / Hops);
            }
        }


        public double TransDistanceEfficiency
        {
            get
            {
                return 100 * (1 - (RoutingDistance / (PublicParameters.CommunicationRangeRadius * Hops * (Hops + 1))));
            }
        }


        /// <summary>
        /// RoutingEfficiency
        /// </summary>
    

        public void ComputeDelay(Packet pack)
        {
            List<int> myPath = Operations.PacketPathToIDS(Path);
            int j = 1;
            for (int i = 0; i <= myPath.Count - 2; i++)
            {
                Sensor tx = PublicParameters.myNetwork[myPath[i]];
                Sensor rx = PublicParameters.myNetwork[myPath[j]];
                Delay += DelayModel.DelayModel.Delay(tx, rx, pack);
                j++;
            }
            Delay += (Settings.Default.QueueTime * WaitingTimes);

        }

        public bool isAdvirtismentPacket()
        {
            if (this.PacketType != PacketType.Data && this.PacketType != PacketType.ACK && this.PacketType != PacketType.Preamble
                && this.PacketType != PacketType.Beacon)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Sensor Source { get; set; }
        public Sensor Destination { get; set; }
        public Sensor clonedSource {get;set;}
        public Sensor Root { get; set; }
        public Sensor OldAgent { get; set; }
        public Sensor SinkAgent { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }

        // remove the object
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
