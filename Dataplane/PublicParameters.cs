using System;
using System.Collections.Generic;
using TBDD.Energy;
using TBDD.ExpermentsResults.Lifetime;
using TBDD.ui;
using System.Windows.Media;
using TBDD.Dataplane.NOS;
using TBDD.Properties;
using TBDD.Constructor;
using System.Windows;
using System.Windows.Shapes;

namespace TBDD.Dataplane
{
    /// <summary>
    /// 
    /// </summary>
    public class PublicParameters
    {

        public static long Rounds { get; set; } // how many rounds.
        public static List<DeadNodesRecord> DeadNodeList = new List<DeadNodesRecord>();

        public static long TotalWaitingTime { get; set; } // how many times the node waitted for its coordinate to wake up.
        public static long TotalReduntantTransmission { get; set; } // how many transmission are redundant, that is to say, recived and canceled.
        public static bool IsNetworkDied { get; set; } // yes if the first node deide.
        public static double SensingRangeRadius { get; set; }
        public static double Density { get; set; } // average number of neighbores (stander deiviation)
        public static string NetworkName { get; set; }
        public static Sensor SinkNode { get; set; }
        public static double BatteryIntialEnergy { get { return Settings.Default.BatteryIntialEnergy; } } //J 0.5 /////////////*******////////////////////////////////////
        public static double BatteryIntialEnergyForSink = 500; //500J.
        public static double RoutingDataLength = 1024; // bit
        public static double ControlDataLength = 128; // bit
        public static double PreamblePacketLength = 128; // bit 
        public static double E_elec = 50; // unit: (nJ/bit) //Energy dissipation to run the radio
        public static double Efs = 0.01;// unit( nJ/bit/m^2 ) //Free space model of transmitter amplifier
        public static double Emp = 0.0000013; // unit( nJ/bit/m^4) //Multi-path model of transmitter amplifier
        public static double CommunicationRangeRadius { get { return SensingRangeRadius * 2; } } // sensing range is R in the DB. 
        public static double TransmissionRate = 2 * 1000000;////2Mbps 100 × 10^6 bit/s , //https://en.wikipedia.org/wiki/Transmission_time
        public static double SpeedOfLight = 299792458;//https://en.wikipedia.org/wiki/Speed_of_light // s
        public static string PowersString { get; set; }
        public static double TotalDelayMs { get; set; } // in ms 
        public static double TotalNumberOfHops { get; set; }
        public static double TotalNumberOfHopsControl { get; set; }
        public static double TotalNumberOfHopsData { get; set; }
        public static double TotalRoutingDistance { get; set; }
        public static double AverageRoutingDistance
        {
            get
            {
                return TotalRoutingDistance / OverallDelieverdPackets;
            }
        }
        public static double AverageTransmissionDistance
        {
            get
            {
                return TotalRoutingDistance / TotalNumberOfHops;
            }
        }
        
        public static double AverageHops
        {
            get
            {
                return TotalNumberOfHops / OverallDelieverdPackets;
            }
        }
        public static double AverageHopscontrol
        {
            get
            {
                return TotalNumberOfHopsControl / NumberOfDeliveredControlPackets;
            }
        }
        public static double AverageHopsData
        {
            get
            {
                return TotalNumberOfHopsData / NumberOfDelieveredDataPackets;
            }
        }
        public static double TotalEnergyConsumptionJoule { get; set; } // keep all energy consumption. 
        public static double TotalWastedEnergyJoule { get; set; } // idel listening energy        

        public static int BufferSize = 100;
        public static int HopsErrorRange = 3;

        public static List<int> HorVerRegs = new List<int>();  // # of regions
        public static List<double> SmallAndBigxy = new List<double>();
        public static List<Point> AllRegCoord = new List<Point>();
        public static List<LocalRegProp> listOfRegs = new List<LocalRegProp>();

        public static double mosttop { get; set; }
        public static double mostbottom { get; set; }
        public static double mostleft { get; set; }
        public static double mostright { get; set; }

        //Added the Network Cluster Group that contains all the clusters in the network
        public static List<CellGroup> networkCells = new List<CellGroup>();

        public static List<Sensor> myNetwork = new List<Sensor>();
        public static double cellDiameter;
        public static Point networkCenter = new Point();
        public static List<CellGroup> currentNetworkTree = new List<CellGroup>();
        public static List<Sensor> BorderNodes = new List<Sensor>();
        public static List<string> WeightParameters = new List<string> { "TD", "Dir", "Pirp", "Energy" };

        public static List<Packet> FinishedRoutedPackets = new List<Packet>(); // all the packets whatever dliverd or not.
        public static double ThresholdDistance  //Distance threshold ( unit m) 
        {
            get { return Math.Sqrt(Efs / Emp); }
        }


      //  public static double ControlPacketsPercentage { get { return 100 * (NumberofControlPackets / NumberofGeneratedPackets); } }
        public static double ControlPacketsEnergyConsmPercentage { get { return 100 * (EnergyComsumedForControlPackets / TotalEnergyConsumptionJoule); } }

        #region Number of generated Packets      
      
        public static long NumberofGeneratedDataPackets { get; set; }
    //Control Packets
        public static long NumberofGeneratedQueryPackets { get; set; }
        public static long NumberofGeneratedResponsePacket { get; set; }
        public static long NumberofGeneratedFollowUpPackets { get; set; }
        public static long NumberOfGeneratedRegionBeaconPacket { get; set; }
        public static long NumberofGeneratedControlPackets
        {
            get{ return  NumberofGeneratedFollowUpPackets + NumberofGeneratedQueryPackets + (NumberOfGeneratedRegionBeaconPacket * (networkCells.Count - 1)); }
        }
        public static long OverallGeneratedPackets { get; set; }
        #endregion

        #region number of delieverd packets
        public static long NumberOfDelieveredQueryPackets { get; set; }
        public static long NumberofDelieveredFollowUpPackets { get; set; }
        public static long NumberOfDelieveredDataPackets { get; set; }
        public static long NumberofDelivedRegBeaconPackets { get; set; }
        public static long NumberOfDeliveredControlPackets 
        { 
            get 
            {
                return NumberOfDelieveredQueryPackets + NumberofDelivedRegBeaconPackets + NumberofDelieveredFollowUpPackets;
            }
        }
        public static long OverallDelieverdPackets { get; set; }
        #endregion

        #region dropped Data Packets
        public static long NumberofDropedPackets { get; set; }
        public static long InQueuePackets
        {
            get{
                long x = OverallGeneratedPackets - (OverallDelieverdPackets + NumberofDropedPackets);
                if(x<0) {return x; }
                else {   return x; }                    
            }
        }

        public static List<Packet> DropedPacketsList = new List<Packet>();
        #endregion

        #region Energy Consumption

        public static double WastedEnergyPercentage { get { return 100 * (TotalWastedEnergyJoule / TotalEnergyConsumptionJoule); } } // idel listening energy percentage  
            public static double EnergyComsumedForControlPackets { get; set; }
            public static double EnergyConsumedForDataPackets { get; set; }
            public static double EnergyComsumedForRegAds { get; set; }

        #endregion 

            public static double ControlDelay { get; set; }
            public static double DataDelay { get; set; }
        public static double QueryDelay { get; set; }
        public static double AverageQueryDelay { get { return (QueryDelay + TotalWaitingTime * Settings.Default.QueueTime) / (NumberOfDelieveredQueryPackets); } }
        public static double AverageDataDelay { get {  return (DataDelay + TotalWaitingTime * Settings.Default.QueueTime ) / NumberOfDelieveredDataPackets; } }

        public static double AverageControlDelay
        {
            get
            {
                double average = (ControlDelay + QueryDelay) / Convert.ToDouble(NumberOfDeliveredControlPackets);
                return average;
            }
        }
        public static double AverageDelay
        {
            get
            {

                double QueueDelay = (TotalWaitingTime * Settings.Default.QueueTime);
                double delay = TotalDelayMs;

                double sumDelay = QueueDelay + delay;

                double average = sumDelay / Convert.ToDouble(OverallDelieverdPackets);
                return average;
            }
        }

        public static double AverageQueueDelay
        {
            get
            {
                double QueueDelay = (TotalWaitingTime * Settings.Default.QueueTime);
                double average = QueueDelay / Convert.ToDouble(OverallDelieverdPackets);
                return average;
            }
        }
        public static double AverageTransmissionDelay
        {
            get
            {
                double delay = TotalDelayMs;
                double average = delay / Convert.ToDouble(OverallDelieverdPackets);
                return average;
            }
        }


        public static double AverageWaitingTimes
        {
            get
            {
                double average = TotalWaitingTime / Convert.ToDouble(OverallDelieverdPackets);
                return average;
            }
        }       

            public static double NumberOfNodesDissemenating { get; set; }
        public static List<Color> RandomColors { get; set; }

        public static double SensingFeildArea
        {
            get; set;
        }
        public static int NumberofNodes
        {
            get; set;
        }

       

        public static double DeliveredRatio
        {
            get
            {
                //double totalGenerated = Convert.ToDouble(NumberofGeneratedDataPackets) + Convert.ToDouble(NumberofGeneratedControlPackets);
                //return 100 * (Convert.ToDouble(OverallDelieverdPackets) / totalGenerated);

                //double totalGenerated = Convert.ToDouble(OverallGeneratedPackets);

                return 100 - DropedRatio;
            }
        }
        public static double DropedRatio
        {
            get
            {
                //double totalGenerated = Convert.ToDouble(NumberofGeneratedDataPackets) + Convert.ToDouble(NumberofGeneratedControlPackets);
                return 100 * (Convert.ToDouble(NumberofDropedPackets) / Convert.ToDouble(OverallGeneratedPackets));
            }
        }

        public static MainWindow MainWindow { get; set; } 

        /// <summary>
        /// Each time when the node loses 5% of its energy, it shares new energy percentage with its neighbors. The neighbor nodes update their energy distributions according to the new percentage immediately as explained by Algorithm 2. 
        /// </summary>
        public static int UpdateLossPercentage
        {
            get
            {
                return Settings.Default.UpdateLossPercentage;
            }
            set
            {
                Settings.Default.UpdateLossPercentage = value;
            }
        }

        // lifetime paramerts:
        public static int NOS { get; set; } // NUMBER OF RANDOM SELECTED SOURCES
        public static int NOP { get; set; } // NUMBER OF PACKETS TO BE SEND.




        /// <summary>
        /// in sec.
        /// </summary>
        public static class Periods
        {
            public static double ActivePeriod { get { return Settings.Default.ActivePeriod; } } //  the node trun on and check for CheckPeriod seconds.// +1
            public static double SleepPeriod { get { return Settings.Default.SleepPeriod; } }  // the node trun off and sleep for SleepPeriod seconds.
        }



        /// <summary>
        /// When all forwarders are sleep, 
        /// the sender try agian until its formwarder is wake up. the sender try agian each 500 ms.
        /// when the sensor retry to send the back is it's forwarders are in sleep mode.
        /// </summary>
        public static TimeSpan QueueTime
        {
            get
            {
                return TimeSpan.FromSeconds(Settings.Default.QueueTime);
            }
        }

        /// <summary>
        /// the timer interval between 1 and 5 sec.
        /// </summary>
        public static int MacStartUp
        {
            get
            {
                return Settings.Default.MacStartUp;
            }
        }

        /// <summary>
        /// the runnunin time of simulator. in SEC
        /// </summary>
        public static int SimulationTime
        {
            get;set;
        }


        public static List<BatRange> getRanges()
        {
            List<BatRange> re = new List<BatRange>();

            int x = 100 / UpdateLossPercentage;
            for (int i = 1; i <= x; i++)
            {
                BatRange r = new Energy.BatRange();
                r.isUpdated = false;
                r.Rang[0] = (i - 1) * UpdateLossPercentage;
                r.Rang[1] = i * UpdateLossPercentage;
                r.ID = i;
                re.Add(r);
            }

            re[re.Count - 1].isUpdated = true;

            return re;
        }
    }
}
