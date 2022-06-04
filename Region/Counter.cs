using TBDD.Intilization;
using TBDD.Energy;
using TBDD.Dataplane;
using TBDD.Dataplane.NOS;
using TBDD.Dataplane.PacketRouter;
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
using static TBDD.Region.RandomvariableStream;
using static TBDD.Region.Sorters;

namespace TBDD.Region
{
    class Sorters
    {
        public class CoordinationEntrySorter : IComparer<CoordinationEntry>
        {
            public int Compare(CoordinationEntry y, CoordinationEntry x)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }
    }
    class RandomvariableStream
    {
        public static class Basics
        {
            private static uint m_w = 521288629;
            private static uint m_z = 362436069;
            public static double RandU01()
            {
                // 0 <= u < 2^32
                uint u = GetUint();
                // The magic number below is 1/(2^32 + 2).
                // The result is strictly between 0 and 1.
                return (u + 1.0) * 2.328306435454494e-10;
            }

            private static uint GetUint()
            {

                m_z = 36969 * (m_z & 65535) + (m_z >> 16);
                m_w = 18000 * (m_w & 65535) + (m_w >> 16);
                return (m_z << 16) + m_w;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public static class UniformRandomVariable
        {
            public static double GetDoubleValue(double min, double max)
            {
                double v = min + RandomeNumberGenerator.GetUniform() * (max - min);
                bool IsAntithetic = true; // check this.
                if (IsAntithetic)
                {
                    return min + (max - v);
                }
                else
                {
                    return v;
                }
            }

            public static int GetIntValue(int min, int max)
            {
                return Convert.ToInt32(GetDoubleValue(min, max));
            }
        }

        /// <summary>
        /// NormalRandomVariable:https://www.nsnam.org/doxygen/random-variable-stream_8cc_source.html
        /// </summary>
        public static class NormalRandomVariable
        {
            public static double GetValue(double m_mean, double m_variance)
            {
                return RandomeNumberGenerator.GetNormal(m_mean, m_variance);
            }
        }

    }

    public enum CoordinationType { Random, Maximal, Mixed }
    // MP.MergedPath.computing
    public enum EnergyConsumption { Transmit, Recive } // defualt is not used. i 

    public class NetworkOverheadCounter
    {
        public FirstOrderRadioModel EnergyModel;

        public NetworkOverheadCounter()
        {
            EnergyModel = new FirstOrderRadioModel(); // energy model.
        }

        private double ConvertToJoule(double UsedEnergy_Nanojoule) //the energy used for current operation
        {
            double _e9 = 1000000000; // 1*e^-9
            double _ONE = 1;
            double oNE_DIVIDE_e9 = _ONE / _e9;
            double re = UsedEnergy_Nanojoule * oNE_DIVIDE_e9;
            return re;
        }

        /// <summary>
        /// this counts the energy consumption, the delay and the hops.
        /// </summary>
        /// <param name="packt"></param>
        /// <param name="enCon"></param>
        /// <param name="sender"></param>
        /// <param name="Reciver"></param>
        public void ComputeOverhead(Packet packt, EnergyConsumption enCon, Sensor sender, Sensor reciver)
        {
            if (enCon == EnergyConsumption.Transmit)
            {
                // calculate the energy 
                double Distance_M = Operations.DistanceBetweenTwoSensors(sender, reciver);
                double UsedEnergy_Nanojoule = EnergyModel.Transmit(packt.PacketLength, Distance_M);
                double UsedEnergy_joule = reciver.ConvertToJoule(UsedEnergy_Nanojoule);
                sender.ResidualEnergy = sender.ResidualEnergy - UsedEnergy_joule;
                PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
                PublicParameters.EnergyComsumedForRegAds += UsedEnergy_joule;
                packt.UsedEnergy_Joule += UsedEnergy_joule;
                packt.RoutingDistance += Distance_M;
                packt.Hops += 1;
                double delay = DelayModel.DelayModel.Delay(sender, reciver, packt);
                packt.Delay += delay;
                PublicParameters.TotalDelayMs += delay;
                PublicParameters.TotalNumberOfHops += 1;
                PublicParameters.TotalRoutingDistance += Distance_M;

                if (Settings.Default.SaveRoutingLog)
                {
                    RoutingLog log = new RoutingLog();
                    log.PacketType = packt.PacketType;
                    log.IsSend = true;
                    log.NodeID = sender.ID;
                    log.Operation = "To:" + reciver.ID;
                    log.Time = DateTime.Now;
                    log.Distance_M = Distance_M;
                    log.UsedEnergy_Nanojoule = UsedEnergy_Nanojoule;
                    log.RemaimBatteryEnergy_Joule = sender.ResidualEnergy;
                    log.PID = packt.PID;
                    sender.Logs.Add(log);
                }

                switch (packt.PacketType)
                {
                    case PacketType.Data:
                        PublicParameters.EnergyConsumedForDataPackets += UsedEnergy_joule; // data packets.
                        break;
                    default:
                        PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule; // other packets.
                        break;
                }

            }
            else if (enCon == EnergyConsumption.Recive)
            {

                double UsedEnergy_Nanojoule = EnergyModel.Receive(packt.PacketLength);
                double UsedEnergy_joule = reciver.ConvertToJoule(UsedEnergy_Nanojoule);
                reciver.ResidualEnergy = reciver.ResidualEnergy - UsedEnergy_joule;
                packt.UsedEnergy_Joule += UsedEnergy_joule;
                PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
                PublicParameters.EnergyComsumedForRegAds += UsedEnergy_joule;

                switch (packt.PacketType)
                {
                    case PacketType.Data:
                        PublicParameters.EnergyConsumedForDataPackets += UsedEnergy_joule; // data packets.
                        break;
                    default:
                        PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule; // other packets.
                        break;
                }

            }

        }

        public void DropPacket(Packet packt, Sensor Reciver, PacketDropedReasons packetDropedReasons)
        {

            PublicParameters.NumberofDropedPackets += 1;
            packt.DroppedReason = packetDropedReasons;
            packt.isDelivered = false;
            Reciver.MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets.ToString();

            PublicParameters.DropedPacketsList.Add(packt);

            if (Settings.Default.SavePackets)
                PublicParameters.FinishedRoutedPackets.Add(packt);
            else
                packt.Dispose();
        }

        public void SuccessedDeliverdPacket(Packet packt)
        {
            packt.isDelivered = true;
            PublicParameters.NumberOfDelieveredDataPackets += 1;
            PublicParameters.MainWindow.lbl_Total_Delay.Content = PublicParameters.AverageDelay.ToString();

            if (Settings.Default.SavePackets)
                PublicParameters.FinishedRoutedPackets.Add(packt);
            else
                packt.Dispose();

        }

        public void Animate(Sensor sender, Sensor Reciver, Packet pck)
        {
            if (Settings.Default.ShowRoutingPaths)
            {
                sender.Animator.StartAnimate(Reciver.ID, pck.PacketType);
            }
        }

        public void IncreasePacketsCounter(Sensor packetSource, PacketType type)
        {
            packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_all_genpack.Content = PublicParameters.OverallGeneratedPackets, DispatcherPriority.Normal);

            switch (type)
            {
                case PacketType.Data:
                    PublicParameters.NumberofGeneratedDataPackets += 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_num_of_gen_packets.Content = PublicParameters.NumberofGeneratedDataPackets, DispatcherPriority.Normal);
                    break;
                case PacketType.QReq:
                    PublicParameters.NumberofGeneratedQueryPackets += 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_num_of_gen_query.Content = PublicParameters.NumberofGeneratedQueryPackets, DispatcherPriority.Normal);
                    break;
                case PacketType.QResp:
                    PublicParameters.NumberofGeneratedQueryPackets += 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_num_of_gen_query.Content = PublicParameters.NumberofGeneratedQueryPackets, DispatcherPriority.Normal);
                    break;
                case PacketType.RegBeacon:
                    PublicParameters.NumberOfGeneratedRegionBeaconPacket += 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_nymber_beacon.Content = PublicParameters.NumberOfGeneratedRegionBeaconPacket, DispatcherPriority.Normal);
                    break;
            }
        }

        public void DisplayRefreshAtReceivingPacket(Sensor Reciver)
        {
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_total_consumed_energy.Content = PublicParameters.TotalEnergyConsumptionJoule + " (JOULS)", DispatcherPriority.Send);
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_Number_of_Delivered_Packet.Content = PublicParameters.NumberOfDelieveredDataPackets, DispatcherPriority.Send);
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_sucess_ratio.Content = PublicParameters.DeliveredRatio, DispatcherPriority.Send);
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_nymber_inQueu.Content = PublicParameters.InQueuePackets.ToString());
        }

        public void SaveToQueue(Sensor sender, Packet packet)
        {
            sender.WaitingPacketsQueue.Enqueue(packet);

            if(sender.WaitingPacketsQueue.Count > 100)
            {
                //MessageBox.Show("more than 100 pkts waiting here: " + sender.ID.ToString());
            }

            if (!sender.QueuTimer.IsEnabled)
            {
                sender.QueuTimer.Start();
            }
            if (Settings.Default.ShowRadar) sender.Myradar.StartRadio();
            PublicParameters.MainWindow.Dispatcher.Invoke(() => sender.Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
            PublicParameters.MainWindow.Dispatcher.Invoke(() => sender.Ellipse_indicator.Visibility = Visibility.Visible);
        }

        public Sensor MaximalCoordinate(List<CoordinationEntry> coordinationEntries, Packet packet, double sum)
        {

            // normalized:
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
            }
            // sort:

            coordinationEntries.Sort(new CoordinationEntrySorter());

            // select forwarders:

            List<CoordinationEntry> Forwarders = new List<CoordinationEntry>();
            int n = coordinationEntries.Count;
            double average = 1 / Convert.ToDouble(n);
            int maxForwarders = Convert.ToInt16(Math.Floor(Math.Sqrt(Math.Sqrt(n)))) - 1;
            int MaxforwardersCount = 0;
            foreach (CoordinationEntry neiEntry in coordinationEntries)
            {
                if (neiEntry.Priority >= average && MaxforwardersCount <= maxForwarders)
                {
                    if (neiEntry.Sensor.ResidualEnergy > 0)
                    {
                        Forwarders.Add(neiEntry);
                        MaxforwardersCount++;
                    }
                }
            }

            // one forwarder:
            // forward:
            Sensor forwarder = null;

            foreach (CoordinationEntry neiEntry in Forwarders)
            {
                if (neiEntry.Sensor.CurrentSensorState == SensorState.Active)
                {
                    if (forwarder == null)
                    {
                        forwarder = neiEntry.Sensor;
                    }
                    else
                    {
                        neiEntry.Sensor.RedundantTransmisionCost(packet, neiEntry.Sensor);
                    }
                }
            }
            return forwarder;
        }

        public Sensor RandomCoordinate(List<CoordinationEntry> coordinationEntries, Packet packet, double sum)
        {
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
            }

            List<CoordinationEntry> Forwarders = new List<CoordinationEntry>();
            int n = coordinationEntries.Count;
            double average = 1 / Convert.ToDouble(n);
            average = 0;
            int maxForwarders = Convert.ToInt16(Math.Floor(Math.Sqrt(Math.Sqrt(n)))) - 1; ////-----
            int MaxforwardersCount = 0;

            foreach (CoordinationEntry neiEntry in coordinationEntries)
            {
                if (neiEntry.Priority >= average && neiEntry.Sensor.CurrentSensorState == SensorState.Active)
                {
                    if (neiEntry.Sensor.ResidualEnergy > 0)
                    {
                        if (MaxforwardersCount <= maxForwarders)
                        {
                            Forwarders.Add(neiEntry);
                            MaxforwardersCount++;
                        }
                    }
                }
            }

            double dsum = 0;
            for (int i = 0; i < Forwarders.Count; i++)
            {
                dsum += Forwarders[i].Priority;
            }

            foreach (CoordinationEntry neiEntry in Forwarders)
            {
                double val = neiEntry.Priority;
                double normalized = val / dsum;
                neiEntry.Priority = normalized;
            }



            // range:
            for (int i = 0; i < Forwarders.Count; i++)
            {
                if (i == 0)
                {
                    Forwarders[0].MinRange = 0;
                    Forwarders[0].MaxRange = Forwarders[i].Priority;
                }
                else
                {
                    Forwarders[i].MinRange = Forwarders[i - 1].MaxRange;// min
                    Forwarders[i].MaxRange = Forwarders[i - 1].MaxRange + Forwarders[i].Priority; // max
                }
            }

            // one forwarder:
            // forward:
            Sensor forwarder = null;

            double random = UniformRandomVariable.GetDoubleValue(0, 1);

            CoordinationEntry neiEntry1 = null;
            foreach (CoordinationEntry neiEntry in Forwarders)
            {
                if (random >= neiEntry.MinRange && random <= neiEntry.MaxRange)
                {
                    forwarder = neiEntry.Sensor;
                    neiEntry1 = neiEntry;

                    break;
                }
            }


            bool isRemoved = Forwarders.Remove(neiEntry1);
            if (isRemoved)
            {
                foreach (CoordinationEntry neiEntry in Forwarders)
                {
                    neiEntry.Sensor.RedundantTransmisionCost(packet, neiEntry.Sensor);
                }
            }


            return forwarder;
        }

    }
}
