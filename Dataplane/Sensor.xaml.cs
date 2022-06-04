using TBDD.Intilization;
using TBDD.Energy;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TBDD.ui;
using TBDD.Properties;
using System.Windows.Threading;
using System.Threading;
using TBDD.ControlPlane.NOS;
using TBDD.ui.conts;
using TBDD.ControlPlane.NOS.FlowEngin;
using TBDD.Forwarding;
using TBDD.Dataplane.PacketRouter;
using TBDD.Dataplane.NOS;
using TBDD.Models.MobileSink;
using System.Diagnostics;
using TBDD.Models.MobileModel;
using TBDD.ControlPlane.DistributionWeights;
using TBDD.Models.Energy;
using TBDD.Models.Cell;
using TBDD.Region;
using TBDD.NetAnimator;
using TBDD.Region.Routing;

namespace TBDD.Dataplane
{
    public enum SensorState { initalized, Active, Sleep } // defualt is not used. i 
    //public enum EnergyConsumption { Transmit, Recive } // defualt is not used. i 


    /// <summary>
    /// Interaction logic for Node.xaml
    /// </summary>
    public partial class Sensor : UserControl
    {
        #region Common parameters.
        private NetworkOverheadCounter counter = new NetworkOverheadCounter();

        public Radar Myradar;
        public List<Arrow> MyArrows = new List<Arrow>();
        public MainWindow MainWindow { get; set; } // the mian window where the sensor deployed.
        public MyNetAnimator Animator;
        public static double SR { get; set; } // the radios of SENSING range.
        public double SensingRangeRadius { get { return SR; } }
        public static double CR { get; set; }  // the radios of COMUNICATION range. double OF SENSING RANGE
        public double ComunicationRangeRadius { get { return CR; } }
        public double BatteryIntialEnergy; // jouls // value will not be changed
        private double _ResidualEnergy; //// jouls this value will be changed according to useage of battery
        public List<int> DutyCycleString = new List<int>(); // return the first letter of each state.
        public BoXMAC Mac { get; set; } // the mac protocol for the node.
        public SensorState CurrentSensorState { get; set; } // state of node.
        public List<RoutingLog> Logs = new List<RoutingLog>();
        public List<NeighborsTableEntry> NeighborsTable = null; // neighboring table.
        public List<Sensor> myNeighborsTable = null; // neighboring table, new update for extra functionality

        public List<FlowTableEntry> TBDDFlowTable = new List<FlowTableEntry>(); //flow table.
        private BatteryLevelThresh BT = new BatteryLevelThresh();
        public int NumberofPacketsGeneratedByMe = 0; // the number of packets sent by this packet.
        public FirstOrderRadioModel EnergyModel = new FirstOrderRadioModel(); // energy model.
        public int ID { get; set; } // the ID of sensor.       
        public bool trun { get; set; }// this will be true if the node is already sent the beacon packet for discovering the number of hops to the sink.
        private DispatcherTimer SendPacketTimer = new DispatcherTimer();// 
        public DispatcherTimer QueuTimer = new DispatcherTimer();// to check the packets in the queue right now.
        public Queue<Packet> WaitingPacketsQueue = new Queue<Packet>(); // packets queue.
        public DispatcherTimer OldAgentTimer = new DispatcherTimer();
        public List<BatRange> BatRangesList = new List<Energy.BatRange>();

        public CaluclateWeights CW = new CaluclateWeights();

        public int inCell = -1;
        
        public CellNode TBDDNodeTable = new CellNode();

        public Agent AgentNode = new Agent();
        public bool isSinkAgent = false;
        public Sensor SinkAdversary { get; set; }
        public Point SinkPosition { get; set; }
        public bool CanRecievePacket { get { return this.ResidualEnergy > 0; } }
        private Stopwatch QueryDelayStopwatch { get; set; }
        public int agentBufferCount { get {
            if (this.isSinkAgent)
            {
                return this.AgentNode.AgentBuffer.Count;
            }
            else
            {
                return 0;
            }
            } }
        public int cellHeaderBufferCount
        {
            get
            {
                if (this.inCell == -1)
                {
                    return 0;
                }
                else
                {
                    if (this.TBDDNodeTable.myCellHeader.ID != this.ID)
                    {
                        return 0;
                    }
                    else
                    {

                        return this.TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count;
                     
                    }
                    
                }
                
            }
        }

        public int sReg { get; set; }       // new region property
        public int activeRegId { get; set; }
        public bool isRingnode = false;
        public Sensor ringFollower { get; set; }
        public List<Sensor> atteptList = new List<Sensor>();
        public double CellHeaderProbability { get; set; }

        public Stopwatch DelayStopWatch = new Stopwatch(); 
        /// <summary>
        /// CONFROM FROM NANO NO JOUL
        /// </summary>
        /// <param name="UsedEnergy_Nanojoule"></param>
        /// <returns></returns>
        public double ConvertToJoule(double UsedEnergy_Nanojoule) //the energy used for current operation
        {
            double _e9 = 1000000000; // 1*e^-9
            double _ONE = 1;
            double oNE_DIVIDE_e9 = _ONE / _e9;
            double re = UsedEnergy_Nanojoule * oNE_DIVIDE_e9;
            return re;
        }

        /// <summary>
        /// in JOULE
        /// </summary>
        public double ResidualEnergy // jouls this value will be changed according to useage of battery
        {
            get { return _ResidualEnergy; }
            set
            {
                _ResidualEnergy = value;
                Prog_batteryCapacityNotation.Value = _ResidualEnergy;
            }
        } //@unit(JOULS);


        /// <summary>
        /// 0%-100%
        /// </summary>
        public double ResidualEnergyPercentage
        {
            get { return (ResidualEnergy / BatteryIntialEnergy) * 100; }
        }
        /// <summary>
        /// visualized sensing range and comuinication range
        /// </summary>
        public double VisualizedRadius
        {
            get { return Ellipse_Sensing_range.Width / 2; }
            set
            {
                // sensing range:
                Ellipse_Sensing_range.Height = value * 2; // heigh= sen rad*2;
                Ellipse_Sensing_range.Width = value * 2; // Width= sen rad*2;
                SR = VisualizedRadius;
                CR = SR * 2; // comunication rad= sensing rad *2;

                // device:
                Device_Sensor.Width = value * 4; // device = sen rad*4;
                Device_Sensor.Height = value * 4;
                // communication range
                Ellipse_Communication_range.Height = value * 4; // com rang= sen rad *4;
                Ellipse_Communication_range.Width = value * 4;

                // battery:
                Prog_batteryCapacityNotation.Width = 8;
                Prog_batteryCapacityNotation.Height = 2;
            }
        }

        /// <summary>
        /// Real postion of object.
        /// </summary>
        public Point Position
        {
            get
            {
                double x = Device_Sensor.Margin.Left;
                double y = Device_Sensor.Margin.Top;
                Point p = new Point(x, y);
                return p;
            }
            set
            {
                Point p = value;
                Device_Sensor.Margin = new Thickness(p.X, p.Y, 0, 0);
            }
        }

        /// <summary>
        /// center location of node.
        /// </summary>
        public Point CenterLocation
        {
            get
            {
                double x = Device_Sensor.Margin.Left;
                double y = Device_Sensor.Margin.Top;
                Point p = new Point(x + CR, y + CR);
                return p;
            }
        }

        bool StartMove = false; // mouse start move.
        private void Device_Sensor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    System.Windows.Point P = e.GetPosition(MainWindow.Canvas_SensingFeild);
                    P.X = P.X - CR;
                    P.Y = P.Y - CR;
                    Position = P;
                    StartMove = true;
                }
            }
        }

        private void Device_Sensor_MouseMove(object sender, MouseEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                if (StartMove)
                {
                    System.Windows.Point P = e.GetPosition(MainWindow.Canvas_SensingFeild);
                    P.X = P.X - CR;
                    P.Y = P.Y - CR;
                    this.Position = P;
                }
            }
        }
        
        private void Device_Sensor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StartMove = false;
        }

        private void Prog_batteryCapacityNotation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
            double val = ResidualEnergyPercentage;
            if (val <= 0)
            {
                MainWindow.RandomSelectSourceNodesTimer.Stop();
                
                // dead certificate:
                ExpermentsResults.Lifetime.DeadNodesRecord recod = new ExpermentsResults.Lifetime.DeadNodesRecord();
                recod.DeadAfterPackets = PublicParameters.NumberofGeneratedDataPackets;
                recod.DeadOrder = PublicParameters.DeadNodeList.Count + 1;
                recod.Rounds = PublicParameters.Rounds + 1;
                recod.DeadNodeID = ID;
                recod.NOS = PublicParameters.NOS;
                recod.NOP = PublicParameters.NOP;
                PublicParameters.DeadNodeList.Add(recod);

                Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));
                Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));


                if (Settings.Default.StopeWhenFirstNodeDeid)
                {
                    MainWindow.TimerCounter.Stop();
                    MainWindow.RandomSelectSourceNodesTimer.Stop();
                    MainWindow.stopSimlationWhen = PublicParameters.SimulationTime;
                    MainWindow.top_menu.IsEnabled = true;
                    MobileModel.StopSinkMovement();
                }
                Mac.SwichToSleep();
                Mac.SwichOnTimer.Stop();
                Mac.ActiveSleepTimer.Stop();
                if (this.ResidualEnergy <= 0)
                {
                    while (this.WaitingPacketsQueue.Count > 0)
                    {
                        //PublicParameters.NumberofDropedPackets += 1;
                        Packet pack = WaitingPacketsQueue.Dequeue();
                        pack.isDelivered = false;
                       // PublicParameters.FinishedRoutedPackets.Add(pack);
                        Console.WriteLine("PID:" + pack.PID + " has been droped.");
                        pack.DroppedReason = PacketDropedReasons.DeadNode;
                        this.updateStates(pack);
                        MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);

                    }
                    this.QueuTimer.Stop();
                    foreach(Sensor sen in PublicParameters.myNetwork)
                    {
                        if(sen.WaitingPacketsQueue.Count > 0)
                        {
                            while (sen.WaitingPacketsQueue.Count > 0)
                            {
                                Packet pkt = sen.WaitingPacketsQueue.Dequeue();
                                pkt.isDelivered = false;
                                pkt.DroppedReason = PacketDropedReasons.DeadNode;
                                this.updateStates(pkt);
                            }
                        }
                        if (sen.TBDDNodeTable.CellHeaderTable.isHeader)
                        {
                            if (sen.TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
                            {
                                while(sen.TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
                                {
                                    Packet pkt = sen.TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
                                    pkt.isDelivered = false;
                                    pkt.DroppedReason = PacketDropedReasons.DeadNode;
                                    this.updateStates(pkt);
                                }
                            }
                        }else if(sen.agentBufferCount > 0)
                        {
                            if (sen.AgentNode.AgentBuffer.Count > 0)
                            {
                                while (sen.AgentNode.AgentBuffer.Count > 0)
                                {
                                    Packet pkt = sen.AgentNode.AgentBuffer.Dequeue();
                                    pkt.isDelivered = false;
                                    pkt.DroppedReason = PacketDropedReasons.DeadNode;
                                    this.updateStates(pkt);
                                }
                            }
                        }
                    }
                    if (Settings.Default.ShowRadar) Myradar.StopRadio();
                    QueuTimer.Stop();
                    Console.WriteLine("NID:" + this.ID + ". Queu Timer is stoped.");
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);
                    return;
                }
                return;


            }
            if (val >= 1 && val <= 9)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
               Dispatcher.Invoke(()=> Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
            }

            if (val >= 10 && val <= 19)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19)));
            }

            if (val >= 20 && val <= 29)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29)));
                Dispatcher.Invoke(() => Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29))));
            }

            // full:
            if (val >= 30 && val <= 39)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39)));
            }
            // full:
            if (val >= 40 && val <= 49)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49)));
            }
            // full:
            if (val >= 50 && val <= 59)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59)));
            }
            // full:
            if (val >= 60 && val <= 69)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69)));
            }
            // full:
            if (val >= 70 && val <= 79)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79)));
            }
            // full:
            if (val >= 80 && val <= 89)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89)));
            }
            // full:
            if (val >= 90 && val <= 100)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100)));
            }


            /*
            // update the battery distrubtion.
            int battper = Convert.ToInt16(val);
            if (battper > PublicParamerters.UpdateLossPercentage)
            {
                int rangeIndex = battper / PublicParamerters.UpdateLossPercentage;
                if (rangeIndex >= 1)
                {
                    if (BatRangesList.Count > 0)
                    {
                        BatRange range = BatRangesList[rangeIndex - 1];
                        if (battper >= range.Rang[0] && battper <= range.Rang[1])
                        {
                            if (range.isUpdated == false)
                            {
                                range.isUpdated = true;
                                // update the uplink.
                                UplinkRouting.UpdateUplinkFlowEnery(this,);

                            }
                        }
                    }
                }
            }*/
        }


        /// <summary>
        /// show or hide the arrow in seperated thread.
        /// </summary>
        /// <param name="id"></param>
        public void ShowOrHideArrow(int id) 
        {
            Thread thread = new Thread(() =>
            
            {
                lock (MyArrows)
                {
                    Arrow ar = GetArrow(id);
                    if (ar != null)
                    {
                        lock (ar)
                        {
                            if (ar.Visibility == Visibility.Visible)
                            {
                                Action action = () => ar.Visibility = Visibility.Hidden;
                                Dispatcher.Invoke(action);
                            }
                            else
                            {
                                Action action = () => ar.Visibility = Visibility.Visible;
                                Dispatcher.Invoke(action);
                            }
                        }
                    }
                }
            }
            );
            thread.Name = "Arrow for " + id;
            thread.Start();
        }


        // get arrow by ID.
        private Arrow GetArrow(int EndPointID)
        {
            foreach (Arrow arr in MyArrows) { if (arr.To.ID == EndPointID) return arr; }
            return null;
        }



       

        #endregion
/////////////////////////////////////////////////////////////////////////////////////////
///           

        /// <summary>
        /// 
        /// </summary>
        public void SwichToActive()
        {
            Mac.SwichToActive();

        }

        /// <summary>
        /// 
        /// </summary>
        private void SwichToSleep()
        {
            Mac.SwichToSleep();
        }
       
        public Sensor(int nodeID)
        {
            InitializeComponent();
            //: sink is diffrent:
            if (nodeID == 0) BatteryIntialEnergy = PublicParameters.BatteryIntialEnergyForSink; // the value will not be change
            else
                BatteryIntialEnergy = PublicParameters.BatteryIntialEnergy;
           
            
            ResidualEnergy = BatteryIntialEnergy;// joules. intializing.
            Prog_batteryCapacityNotation.Value = BatteryIntialEnergy;
            Prog_batteryCapacityNotation.Maximum = BatteryIntialEnergy;
            lbl_Sensing_ID.Content = nodeID;
            ID = nodeID;
            QueuTimer.Interval = PublicParameters.QueueTime;
            QueuTimer.Tick += DeliveerPacketsInQueuTimer_Tick;
            OldAgentTimer.Interval = TimeSpan.FromSeconds(3);
            OldAgentTimer.Tick += RemoveOldAgentTimer_Tick;
            //:

            SendPacketTimer.Interval = TimeSpan.FromSeconds(1);
            Animator = new MyNetAnimator(this);

        }


        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            

        }

        /// <summary>
        /// hide all arrows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            /*
            Vertex ver = MainWindow.MyGraph[ID];
            foreach(Vertex v in ver.Candidates)
            {
                MainWindow.myNetWork[v.ID].lbl_Sensing_ID.Background = Brushes.Black;
            }*/
         
        }

        

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           
        }

       

        public int ComputeMaxHopsUplink
        {
            get
            {
                double  DIS= Operations.DistanceBetweenTwoSensors(PublicParameters.SinkNode, this);
                return Convert.ToInt16(Math.Ceiling((Math.Sqrt(PublicParameters.Density) * (DIS / ComunicationRangeRadius))));
            }
        }

        public int ComputeMaxHopsDownlink(Sensor endNode)
        {
            double DIS = Operations.DistanceBetweenTwoSensors(PublicParameters.SinkNode, endNode);
            return Convert.ToInt16(Math.Ceiling((Math.Sqrt(PublicParameters.Density) * (DIS / ComunicationRangeRadius))));
        }

        #region Old Sending Data ///
      
        /// <summary>
        ///  data or control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reciver"></param>
        /// <param name="packt"></param>
        
       
        
        public void IdentifySourceNode(Sensor source)
        {
            if (Settings.Default.ShowAnimation && source.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => source.Ellipse_indicator.Visibility = Visibility.Visible;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => source.Ellipse_indicator.Fill = Brushes.Yellow;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void UnIdentifySourceNode(Sensor source)
        {
            if (Settings.Default.ShowAnimation && source.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => source.Ellipse_indicator.Visibility = Visibility.Hidden;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => source.Ellipse_indicator.Fill = Brushes.Transparent;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void GenerateDataPacket()
        {
            if (Settings.Default.IsIntialized && this.ResidualEnergy > 0)
            {
                this.DissemenateData();

            }
        }

        public void GenerateMultipleDataPackets(int numOfPackets)
        {
            for (int i = 0; i < numOfPackets; i++)
            {
                GenerateDataPacket();
                //  Thread.Sleep(50);
            }
        }

        public void GenerateControlPacket(Sensor endNode)
        {
            if (Settings.Default.IsIntialized && this.ResidualEnergy > 0)
            {

                

            }
        }
        /// <summary>
        /// to the same endnode.
        /// </summary>
        /// <param name="numOfPackets"></param>
        /// <param name="endone"></param>
        public void GenerateMultipleControlPackets(int numOfPackets, Sensor endone)
        {
            for (int i = 0; i < numOfPackets; i++)
            {
                GenerateControlPacket(endone);
            }
        }

        public void IdentifyEndNode(Sensor endNode)
        {
            if (Settings.Default.ShowAnimation && endNode.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => endNode.Ellipse_indicator.Visibility = Visibility.Visible;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => endNode.Ellipse_indicator.Fill = Brushes.DarkOrange;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void UnIdentifyEndNode(Sensor endNode)
        {
            if (Settings.Default.ShowAnimation && endNode.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => endNode.Ellipse_indicator.Visibility = Visibility.Hidden;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => endNode.Ellipse_indicator.Fill = Brushes.Transparent;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void btn_send_packet_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lbl_title = sender as Label;
            switch (lbl_title.Name)
            {
                case "btn_send_1_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(1);
                        }
                        else
                        {
                            RandomSelectEndNodes(1);
                        }

                        break;
                    }
                case "btn_send_10_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(10);
                        }
                        else
                        {
                            RandomSelectEndNodes(10);
                        }
                        break;
                    }

                case "btn_send_100_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(100);
                        }
                        else
                        {
                            RandomSelectEndNodes(100);
                        }
                        break;
                    }

                case "btn_send_300_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(300);
                        }
                        else
                        {
                            RandomSelectEndNodes(300);
                        }
                        break;
                    }

                case "btn_send_1000_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(1000);
                        }
                        else
                        {
                            RandomSelectEndNodes(1000);
                        }
                        break;
                    }

                case "btn_send_5000_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(5000);
                        }
                        else
                        {
                            // DOWN
                            RandomSelectEndNodes(5000);
                        }
                        break;
                    }
            }
        }

        private void OpenChanel(int reciverID, PacketType packtype)
        {
            Thread thread = new Thread(() =>
            {
                lock (MyArrows)
                {
                    
                    Arrow ar = GetArrow(reciverID);
                    if (ar != null)
                    {
                        lock (ar)
                        {
                            if (ar.Visibility == Visibility.Hidden)
                            {
                                if (Settings.Default.ShowAnimation)
                                {
                                    Action actionx = () => ar.BeginAnimation(packtype);
                                    Dispatcher.Invoke(actionx);
                                    Action action1 = () => ar.Visibility = Visibility.Visible;
                                    Dispatcher.Invoke(action1);
                                }
                                else
                                {
                                    Action action1 = () => ar.Visibility = Visibility.Visible;
                                    Dispatcher.Invoke(action1);
                                    Dispatcher.Invoke(() => ar.Stroke = new SolidColorBrush(Colors.Black));
                                    Dispatcher.Invoke(() => ar.StrokeThickness = 1);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                            }
                            else
                            {
                                if (Settings.Default.ShowAnimation)
                                {
                                    Action actionx = () => ar.BeginAnimation(packtype);
                                    Dispatcher.Invoke(actionx);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                                else
                                {
                                    Dispatcher.Invoke(() => ar.Stroke = new SolidColorBrush(Colors.Black));
                                    Dispatcher.Invoke(() => ar.StrokeThickness = 1);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                            }
                        }
                    }
                }
            }
           );
            thread.Name = "OpenChannel thread " + reciverID + "packtype:" + packtype;
            thread.Start();
            thread.Priority = ThreadPriority.Highest;
        }

        #endregion


        #region send data: /////////////////////////////////////////////////////////////////////////////

       
        public bool isQreqGoingIn(Packet QReq)
        {
            double distance = Operations.DistanceBetweenTwoPoints(this.CenterLocation, QReq.DestinationAddress);
            if (distance <= PublicParameters.cellDiameter)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    
        public int maxHopsForDestination( Point sourcepoint, Point destinationPoint)
        {
            double DIS = Operations.DistanceBetweenTwoPoints(sourcepoint, destinationPoint) * 1.5;
            return PublicParameters.HopsErrorRange + Convert.ToInt16(Math.Ceiling(DIS / (ComunicationRangeRadius/3)));         
        }

        public int maxHopsForQuery(Sensor sourceNode)
        {
            if (sourceNode.inCell != -1)
            {
                double DIS = Operations.DistanceBetweenTwoPoints(sourceNode.CenterLocation, sourceNode.TBDDNodeTable.myCellHeader.CenterLocation) ;
                return Convert.ToInt16( DIS / (ComunicationRangeRadius / 3) + 3);
            }
            else
            {
                double smallDistance = Operations.DistanceBetweenTwoPoints(sourceNode.CenterLocation, sourceNode.TBDDNodeTable.NearestCellCenter);
                double DIS = smallDistance + PublicParameters.SensingRangeRadius;

                return Convert.ToInt16(DIS / (ComunicationRangeRadius / 3) + 3);
            }
        
        }

        //**************Generating Packets and Data Dissemenation

        public void DissemenateData()
        {

            PublicParameters.NumberOfNodesDissemenating += 1;

            if (this.ID == 0 || this.isSinkAgent)
            {
                //Directly send to the sink
                this.GenerateDataToSink(PublicParameters.SinkNode);
            }
            else if (this.inCell != -1)
            {
                if (TBDDNodeTable.CellHeaderTable.isHeader)
                {
                    if (TBDDNodeTable.CellHeaderTable.isRootHeader)
                    {
                        if (TBDDNodeTable.CellHeaderTable.hasSinkPosition)
                        {
                            this.GenerateDataToSink(TBDDNodeTable.CellHeaderTable.SinkAgent);
                        }
                        else
                        {
                            Console.WriteLine("I am RootHeader but have no Sink position info.");
                            PublicParameters.NumberOfNodesDissemenating -= 1;
                            return;
                        }                       
                    }
                    else
                    {
                        generateDataToActiveReg(this, TBDDNodeTable.CellHeaderTable.activeRegID);
                    }
                }
                else
                {
                    GenerateQueryRequest(this);
                }
            }
            else
            {
                GenerateQueryRequest(this);
            }
 
        }
        
        public void generateDataToActiveReg(Sensor sender, int actRegId)
        {
            PublicParameters.OverallGeneratedPackets += 1;
            Packet packet = new Packet();
            packet.Source = sender;
            packet.PacketLength = PublicParameters.RoutingDataLength;
            packet.PacketType = PacketType.Data;
            packet.PID = PublicParameters.OverallGeneratedPackets;
            packet.Path = "" + sender.ID;
            packet.prevSenId = sender.ID;
            if (actRegId > 0)
            {
                packet.activeRegId = actRegId;
            }
            else
            {
                packet.activeRegId = sender.sReg;
            }           

            packet.DestinationAddress = PublicParameters.listOfRegs[packet.activeRegId - 1].localCenteriod;

            packet.TimeToLive = maxHopsForDestination(sender.CenterLocation, packet.DestinationAddress) + 3;
            IdentifySourceNode(sender);
            counter.IncreasePacketsCounter(sender, PacketType.Data);
            MlCDDDataPackForwader(sender, packet);
        }

        public void GenerateDataToSink(Sensor destSensor)
        {
            PublicParameters.OverallGeneratedPackets += 1;
            Packet packet = new Packet();                
            packet.Source = this;
            packet.PacketLength = PublicParameters.RoutingDataLength;
            packet.PacketType = PacketType.Data;
            packet.PID = PublicParameters.OverallGeneratedPackets;
            packet.Path = "" + this.ID;
            packet.Destination = destSensor;
            packet.DestinationAddress = packet.Destination.CenterLocation;
            packet.activeRegId = this.sReg;
            packet.TimeToLive = this.maxHopsForDestination(this.CenterLocation, destSensor.CenterLocation);
            IdentifySourceNode(this);
            counter.IncreasePacketsCounter(this, PacketType.Data);
            this.sendDataPack(packet);
            return;                
        }
        
        public void GenerateQueryRequest(Sensor sender)
        {
            PublicParameters.OverallGeneratedPackets += 1;
            Packet QReq = new Packet();
            QReq.Path = "" + sender.ID;
            QReq.TimeToLive = maxHopsForQuery(sender);
            QReq.Source = sender;
            QReq.PacketLength = PublicParameters.ControlDataLength;
            QReq.PacketType = PacketType.QReq;
            QReq.PID = PublicParameters.OverallGeneratedPackets;
            QReq.prevSenId = sender.ID;

            if (sender.inCell == -1)
            {
                QReq.DestinationAddress = sender.TBDDNodeTable.NearestCellCenter;
                QReq.Destination = null;
            }
            else
            {  
                QReq.Destination = sender.TBDDNodeTable.myCellHeader;
                QReq.DestinationAddress = sender.TBDDNodeTable.myCellHeader.CenterLocation;

            }
            IdentifySourceNode(sender);
            counter.IncreasePacketsCounter(sender, PacketType.QReq);
            SendQReq(sender, QReq);         
        }

        public void GenerateQueryResponse(Sensor sender, Sensor destination)
        {
            Packet QResp = new Packet();
            PublicParameters.OverallGeneratedPackets += 1;
            QResp.Source = sender;
            QResp.Path = "" + sender.ID;
            QResp.PacketLength = PublicParameters.ControlDataLength;
            QResp.PacketType = PacketType.QResp;
            QResp.activeRegId = sender.TBDDNodeTable.CellHeaderTable.activeRegID;
            QResp.isDelivered = false;
            QResp.Destination = destination;
            QResp.DestinationAddress = destination.CenterLocation;
            QResp.TimeToLive = maxHopsForDestination(sender.CenterLocation, destination.CenterLocation);               
            QResp.PID = PublicParameters.OverallGeneratedPackets;
            QResp.prevSenId = sender.ID;

            IdentifySourceNode(sender);
            counter.IncreasePacketsCounter(sender, PacketType.QResp);
            SendQResponse(sender, QResp);
                                             
        }

        public void GenerateAS(Sensor oldAgent,Sensor newAgent,Sensor rootClusterHeader, Point rootCellCenter)
        {
            Packet ASNewAgent = new Packet();
            Packet ASOldAgent = new Packet();
            PublicParameters.NumberofGeneratedFollowUpPackets += 1;
            PublicParameters.OverallGeneratedPackets += 1;
            ASNewAgent.Source = this;
            ASNewAgent.Root = rootClusterHeader;
            ASNewAgent.OldAgent = oldAgent;
            ASNewAgent.PacketLength = PublicParameters.ControlDataLength;
            ASNewAgent.PacketType = PacketType.AS;
            ASNewAgent.PID = PublicParameters.OverallGeneratedPackets;
            ASNewAgent.Path = "" + this.ID;
            ASNewAgent.Destination = newAgent;
            ASNewAgent.PossibleDest = rootCellCenter;
            ASNewAgent.TimeToLive = this.maxHopsForDestination(this.CenterLocation, rootClusterHeader.CenterLocation) ;
           
            IdentifySourceNode(this);
            MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
            this.SendAS(ASNewAgent);         
        }

        public void GenerateFM(Sensor OldAgent,Sensor newAgent)
        {
            //..WriteLine("Sending From {0} to old agent {1}", this.ID, OldAgent.ID);
            PublicParameters.NumberofGeneratedFollowUpPackets++;
            PublicParameters.OverallGeneratedPackets += 1;
            Packet FM = new Packet();
            FM.Source = this;
            try
            {
                FM.Destination = OldAgent;
                FM.PacketLength = PublicParameters.ControlDataLength;
                FM.PID = PublicParameters.OverallGeneratedPackets;
                FM.PacketType = PacketType.FM;
                FM.Path = "" + this.ID;               
                FM.TimeToLive = this.maxHopsForDestination(this.CenterLocation, OldAgent.CenterLocation);               
                FM.SinkAgent = newAgent;
                IdentifySourceNode(this);
                MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
                this.sendFM(FM);
            }
            catch(NullReferenceException e)
            {
                Console.WriteLine(e.Message + " from generate FM agent null");
            }
            
           
        }
        public void GenerateFSA(Point RootCellCenter)
        {
             try
            {
                                  
                Packet FSA = new Packet();
                PublicParameters.NumberofGeneratedFollowUpPackets += 1;
                PublicParameters.OverallGeneratedPackets += 1;                
                FSA.PacketLength = PublicParameters.ControlDataLength;
                FSA.PID = PublicParameters.OverallGeneratedPackets;
                FSA.PacketType = PacketType.FSA;
                FSA.Path = "" + this.ID;
                FSA.Source = this;
                FSA.Destination = null;
                FSA.DestinationAddress = RootCellCenter;
                FSA.TimeToLive = 3 + Convert.ToInt16((Operations.DistanceBetweenTwoPoints(this.CenterLocation, FSA.DestinationAddress) / (PublicParameters.CommunicationRangeRadius / 3)));
                IdentifySourceNode(this);   
                MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
                this.sendFSA(FSA);
             }
            catch(NullReferenceException e)
            {
                Console.WriteLine("FSA returning a null reference "+e.Message);
            }
            
        }
        
        public Sensor selectQueryForwader(Sensor ni, Packet packet)
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            double sum = 0;
            Sensor sj = null;
            if (packet.PacketType == PacketType.QReq && ni.inCell != -1)
            {
                packet.Destination = ni.TBDDNodeTable.myCellHeader;
                packet.DestinationAddress = ni.TBDDNodeTable.myCellHeader.CenterLocation;
            }

            if (packet.Destination != null && Operations.isInMyComunicationRange(ni, packet.Destination))
            {
                sj = packet.Destination;
                return sj;
            }

            foreach (Sensor nj in ni.myNeighborsTable)
            {               
                if (nj.ResidualEnergyPercentage > 0)
                {
                    if (packet.Destination != null && nj.ID == packet.Destination.ID)
                    {
                        return nj;
                    }

                    double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
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

            sj = counter.RandomCoordinate(coordinationEntries, packet, sum);


            return sj;
        }
        
        //********************Sending

        public void ForwardDataUsingTree(Sensor sender, Packet pck)
        {
            sender.SwichToActive();
            pck.isDataStartUsingTree = true;          

            Sensor Reciver;

            if (sender.ID == PublicParameters.SinkNode.ID)
            {
                pck.Destination = PublicParameters.SinkNode;
                pck.isDelivered = true;
                sender.updateStates(pck);
                return;
            }
            if (sender.isSinkAgent)
            {
                pck.Destination = PublicParameters.SinkNode;
                sender.sendDataPack(pck);
                return;
            }
            if (Operations.isInMyComunicationRange(sender, PublicParameters.SinkNode))
            {
                pck.Destination = PublicParameters.SinkNode;
                pck.DestinationAddress = PublicParameters.SinkNode.CenterLocation;
                sender.sendDataPack(pck);
                return;
            }
            
            Packet pack = updateDestinationAdress(sender, pck);          

            if (pack != null)
            {
                if (pack.Destination != null)
                {
                    if (pack.Destination.ID == sender.ID)
                    {
                        counter.SaveToQueue(sender, pck);
                        return;
                    }
                    if (Operations.isInMyComunicationRange(sender, pack.Destination))
                    {
                        Reciver = pack.Destination;
                        Reciver.ComputeOverhead(pck, EnergyConsumption.Transmit, Reciver);
                        Reciver.ReciverToForwardDataUsingTree(pck);
                        return;
                    }
                }
                else
                {
                    if (pack.Destination == null && pack.isDataInsideCell && 
                        Operations.DistanceBetweenTwoPoints(sender.CenterLocation, pack.DestinationAddress) <= (PublicParameters.cellDiameter / 2))
                    {
                        // wait to get header
                        pack.isRedlightON = false;
                        counter.SaveToQueue(this, pack);
                        return;
                    }
                }

                lock (sender.TBDDFlowTable)
                {
                    LinkRouting.GetD_Distribution(sender, pck);
                    FlowTableEntry FlowEntry = MatchFlow(pck);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;

                        if (Reciver.ID == sender.ID)
                        {
                            MessageBox.Show("sending to myself ooo: " + sender.ID.ToString());
                            return;
                        }

                        Reciver.ComputeOverhead(pck, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.ReciverToForwardDataUsingTree(pck);
                    }
                    else
                    {
                        counter.SaveToQueue(sender, pck);
                    }
                }

                //Reciver = selectDataForwader(sender, pack);
                //if (Reciver != null)
                //{
                //    // overhead:
                //    sender.ComputeOverhead(pack, EnergyConsumption.Transmit, Reciver);
                //    Reciver.ReciverToForwardDataUsingTree(pack);
                //}
                //else
                //{
                //    counter.SaveToQueue(sender, pack); // save in the queue.
                //}
            }
            else
            {
                return;
            }
        }

        public void ReciverToForwardDataUsingTree(Packet pack)
        {           
            pack.Path += ">" + ID;           

            if (this.ID == PublicParameters.SinkNode.ID)
            {
                pack.Destination = PublicParameters.SinkNode;
                pack.isDelivered = true;
                this.updateStates(pack);
                return;
            }
            if (this.isSinkAgent)
            {
                pack.Destination = PublicParameters.SinkNode;
                pack.DestinationAddress = PublicParameters.SinkNode.CenterLocation;
                this.sendDataPack(pack);
                return;
            }
            if (Operations.isInMyComunicationRange(this, PublicParameters.SinkNode))
            {
                pack.Destination = PublicParameters.SinkNode;
                pack.DestinationAddress = PublicParameters.SinkNode.CenterLocation;
                this.sendDataPack(pack);
                return;
            }          

            if (new LoopChecker(pack).isLoop)
            {
                pack.IsLooped = true;
                pack.isDelivered = false;
                pack.aDroppedReason = "Loooooop in side tree";
                this.updateStates(pack);
                return;
            }

            if (new LoopChecker(pack).isLongLoop)
            {
                //MessageBox.Show("it rotate around");
                pack.IsLooped = true;
                pack.isDelivered = false;
                pack.aDroppedReason = "rotate in side tree";
                this.updateStates(pack);
                return;
            }

            pack.ReTransmissionTry = 0;
            if (pack.Hops <= pack.TimeToLive)
            {
                this.ComputeOverhead(pack, EnergyConsumption.Recive, null);
                ForwardDataUsingTree(this, pack);
            }
            else
            {
                pack.isDelivered = false;
                pack.DroppedReason = PacketDropedReasons.TimeToLive;
                pack.aDroppedReason = "TimeTolive at forwarderdatausingtree";
                this.updateStates(pack);
            }
            
        }

        private Sensor selectDataForwader(Sensor ni, Packet packet)
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            double sum = 0;
            Sensor sj = null;
            if (packet.Destination != null && packet.Destination.ID == ni.ID) packet.Destination = null;
            if (packet.Destination != null && Operations.isInMyComunicationRange(ni, packet.Destination))
            {
                sj = packet.Destination;
                return sj;
            }

            foreach (Sensor nj in ni.myNeighborsTable)
            {
                if (nj.ResidualEnergyPercentage > 0)
                {

                    if (packet.Destination != null)
                    {
                        if (nj.ID == packet.Destination.ID) return nj;
                    }
                    double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
                    if (Double.IsNaN(Norangle) || Norangle == 0 || Norangle < 0.0001)
                    {
                        return nj;
                    }
                    else
                    {
                        if (Norangle < 0.5)
                        {
                            double ij_ψ = Operations.PerpendicularDistanceDistribution(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
                            double ij_ω = Operations.ProximityToBranchEndPoint(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
                            double ij_σ = Operations.EnergyDistribution(nj.ResidualEnergyPercentage, 100);
                            double ij_d = Operations.TransDistDistribution(nj.CenterLocation, nj.CenterLocation);

                            double defual = ij_ω * (ij_ψ + ij_d + ij_σ);

                            double aggregatedValue = defual;
                            sum += aggregatedValue;
                            coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj });
                        }
                    }
                }
            }

            //sj = counter.RandomCoordinate(coordinationEntries, packet, sum);
            sj = counter.MaximalCoordinate(coordinationEntries, packet, sum);
            return sj;

        }

        public Packet updateDestinationAdress(Sensor sender, Packet pckt)
        {
            Packet pck = pckt;

            if (sender.inCell != -1)
            {
                pck.isDataInsideCell = true;
                if (sender.TBDDNodeTable.myCellHeader.TBDDNodeTable.CellHeaderTable.isRootHeader && sender.isRingnode)
                {                  
                    if (sender.TBDDNodeTable.SinkAgent != null)
                    {
                        pck.isDataArrivedAtRootheader = true;
                        pck.isRedlightON = true;
                        pck.Destination = sender.TBDDNodeTable.SinkAgent;
                        pck.DestinationAddress = pck.Destination.CenterLocation;
                        sender.sendDataPack(pck);
                    }
                    else
                    {
                        pck.isDelivered = false;
                        pck.aDroppedReason = "Noooooooo sink agent found !!!";
                        sender.updateStates(pck);                       
                    }
                    return null;
                }
                else if (sender.TBDDNodeTable.CellHeaderTable.isHeader && !sender.TBDDNodeTable.CellHeaderTable.isRootHeader)
                {
                    pck.isRedlightON = true;
                    pck.isDataArrivedAtRootheader = false;
                    pck.Destination = null;
                    pck.DestinationAddress = sender.TBDDNodeTable.CellHeaderTable.ParentCellCenter;
                    return pck;                 
                }
                else
                {
                    if (!pck.isRedlightON)
                    {
                        pck.Destination = sender.TBDDNodeTable.myCellHeader;
                        pck.DestinationAddress = sender.TBDDNodeTable.myCellHeader.CenterLocation;
                    }
                    return pck;
                }
            }
            else
            {
                pck.isDataInsideCell = false;
                if (sender.TBDDNodeTable.NearestCellCenter == pck.DestinationAddress)
                {
                    pck.isRedlightON = false;
                }
                return pck;
            }
        }

        private Sensor DataForwaderForFSA(Sensor ni, Packet packet)
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            double sum = 0;
            Sensor sj = null;
            if (packet.Destination != null && packet.Destination.ID == ni.ID) packet.Destination = null;
            if (packet.Destination != null && Operations.isInMyComunicationRange(ni, packet.Destination))
            {
                sj = packet.Destination;
                return sj;
            }

            foreach (Sensor nj in ni.myNeighborsTable)
            {
                if (nj.ResidualEnergyPercentage > 0)
                {

                    if (packet.Destination != null)
                    {
                        if (nj.ID == packet.Destination.ID) return nj;
                    }
                    if (nj.isRingnode && Operations.DistanceBetweenTwoPoints(nj.CenterLocation, packet.PossibleDest) <= PublicParameters.cellDiameter)
                    {
                        return nj;
                    }

                    double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
                    if (Double.IsNaN(Norangle) || Norangle == 0 || Norangle < 0.0001)
                    {
                        return nj;
                    }
                    else
                    {
                        if (Norangle < 0.5)
                        {
                            double ij_ψ = Operations.PerpendicularDistanceDistribution(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
                            double ij_ω = Operations.ProximityToBranchEndPoint(ni.CenterLocation, nj.CenterLocation, packet.DestinationAddress);
                            double ij_σ = Operations.EnergyDistribution(nj.ResidualEnergyPercentage, 100);
                            double ij_d = Operations.TransDistDistribution(nj.CenterLocation, nj.CenterLocation);

                            double defual = ij_ω * (ij_ψ + ij_d + ij_σ);

                            if (nj.isRingnode) defual *= 100;

                            double aggregatedValue = defual;
                            sum += aggregatedValue;
                            coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj });
                        }
                    }
                }
            }

            //sj = counter.RandomCoordinate(coordinationEntries, packet, sum);
            sj = counter.MaximalCoordinate(coordinationEntries, packet, sum);
            return sj;

        }

        public void MlCDDDataPackForwader(Sensor sender, Packet pck)
        {
            sender.SwichToActive();
            lock (sender.TBDDFlowTable)
            {
                if (sender.sReg == pck.activeRegId)
                {
                    double multiplier = PublicParameters.networkCells.Count / PublicParameters.listOfRegs.Count;
                    double D = Math.Sqrt(Math.Pow(PublicParameters.CommunicationRangeRadius, 2) + Math.Pow(PublicParameters.CommunicationRangeRadius, 2)) * multiplier;
                    Point p = new Point(sender.CenterLocation.X + D, sender.CenterLocation.Y + D);

                    pck.DestinationAddress = sender.TBDDNodeTable.NearestCellCenter;
                    pck.TimeToLive += maxHopsForDestination(sender.CenterLocation, p) + 6;
                    ForwardDataUsingTree(sender, pck);
                    return;
                }
                else
                {
                    Sensor Reciver;
                    LinkRouting.GetD_Distribution(sender, pck);
                    FlowTableEntry FlowEntry = MatchFlow(pck);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                        sender.ComputeOverhead(pck, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.MlCDDDataPackReciever(pck);
                    }
                    else
                    {
                        counter.SaveToQueue(sender, pck);
                    }
                }
            }
        }

        #region //Recieving Data Packet
        public void MlCDDDataPackReciever(Packet packt)
        {
            packt.Path += ">" + this.ID;
            packt.ReTransmissionTry = 0;

            if (new LoopChecker(packt).isLoop)
            {
                packt.IsLooped = true;
                packt.isDelivered = false;
                packt.aDroppedReason = "Loooooop inside MLCDD";
                this.updateStates(packt);
                return;
            }

            if (this.ID == PublicParameters.SinkNode.ID)
            {
                packt.Destination = PublicParameters.SinkNode;
                packt.isDelivered = true;
                this.updateStates(packt);
                return;
            }
            else if (this.isSinkAgent)
            {
                //if (this.AgentNode.isSinkInRange())
                //{
                    packt.Destination = PublicParameters.SinkNode;
                    packt.DestinationAddress = PublicParameters.SinkNode.CenterLocation;
                    this.sendDataPack(packt);
                    return;
                //}
            }               

            else
            {                
                if (packt.Hops <= packt.TimeToLive)
                {
                    this.ComputeOverhead(packt, EnergyConsumption.Recive, null);
                    MlCDDDataPackForwader(this, packt);
                }
                else
                {
                    packt.isDelivered = false;
                    packt.DroppedReason = PacketDropedReasons.TimeToLive;
                    packt.aDroppedReason = "TimeToLive at MLCDD recive function";
                    this.updateStates(packt);
                }
            }
        }

         

        #endregion

        #region original sendDataPack recieveDatPack codes
        public void sendDataPack(Packet packet)
        {
            lock (TBDDFlowTable)
            {
                packet.isInSendData = true;
                Sensor Reciver;

                if (this.ID == PublicParameters.SinkNode.ID)
                {
                    packet.isDelivered = true;
                    this.updateStates(packet);
                    return;
                }
                if (isSinkAgent) 
                {
                    packet.Destination = PublicParameters.SinkNode;
                    Reciver = packet.Destination;

                    Reciver.ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                    Reciver.RecieveDataPack(packet);
                    return;                    
                }
                if (Operations.isInMyComunicationRange(this, PublicParameters.SinkNode))
                {
                    packet.Destination = PublicParameters.SinkNode;                
                    Reciver = packet.Destination;

                    Reciver.ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                    Reciver.RecieveDataPack(packet);
                    return;
                }
                if (packet.Destination != null && packet.Destination == PublicParameters.SinkNode)
                {
                    Reciver = packet.Destination;

                    Reciver.ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                    Reciver.RecieveDataPack(packet);
                    return;
                }
                else
                {
                    LinkRouting.GetD_Distribution(this, packet);
                    if (this.TBDDFlowTable.Count == 0)
                    {
                        packet.isDelivered = false;
                        packet.DroppedReason = PacketDropedReasons.NoForwarderAvail;
                        packet.aDroppedReason = "TBDDflowtable is null at sendDatapack function";
                        this.updateStates(packet);
                        return;
                    }
                    else
                    {
                        FlowTableEntry FlowEntry = MatchFlow(packet);
                        if (FlowEntry != null)
                        {
                            Reciver = FlowEntry.NeighborEntry.NeiNode;

                            Reciver.ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                            FlowEntry.DownLinkStatistics += 1;
                            Reciver.RecieveDataPack(packet);
                        }
                        else
                        {
                            counter.SaveToQueue(this, packet);
                            return;
                        }
                    }

                }

            }
        }

        public void RecieveDataPack(Packet packet)
        {
            if (packet.Destination == null)
            {
                packet.isDelivered = false;
                packet.DroppedReason = PacketDropedReasons.NOdestination;
                packet.aDroppedReason = "Empty agent/destination ";
                this.updateStates(packet);
                return;
            }
            packet.ReTransmissionTry = 0;

            packet.Path += ">" + this.ID;

            if (new LoopChecker(packet).isLoop)
            {
                packet.IsLooped = true;
                packet.isDelivered = false;
                packet.aDroppedReason = "Loooooop inside SendData function ";
                this.updateStates(packet);
                return;
            }

            if (this.ID == PublicParameters.SinkNode.ID)
            {
                packet.isDelivered = true;
                this.updateStates(packet);
                return;
            }
            if (packet.Destination.ID == this.ID)
            {
                
                if (this.isSinkAgent)
                {
                    if (this.AgentNode.isSinkInRange())
                    {
                        packet.Destination = PublicParameters.SinkNode;
                        packet.DestinationAddress = packet.Destination.CenterLocation;
                        this.ComputeOverhead(packet, EnergyConsumption.Recive, null);
                        this.sendDataPack(packet);
                        return;
                    }
                    else
                    {
                        this.AgentNode.AgentStorePacket(packet);
                        return;
                    }

                }
                else
                {
                    //Old Agent Follow Up Mechanisim          
                    if (this.AgentNode.NewAgent != null)
                    {
                        packet.Destination = this.AgentNode.NewAgent;
                        packet.DestinationAddress = packet.Destination.CenterLocation;
                        this.ComputeOverhead(packet, EnergyConsumption.Recive, null);
                        this.sendDataPack(packet);
                        return;
                    }
                    else
                    {
                        packet.Destination = PublicParameters.SinkNode;
                        packet.DestinationAddress = packet.Destination.CenterLocation;
                        this.ComputeOverhead(packet, EnergyConsumption.Recive, null);
                        this.sendDataPack(packet);
                        return;

                    }

                }
            }
            else
            {
                if (packet.Hops > packet.TimeToLive)
                {
                    // drop the paket.
                    packet.isDelivered = false;
                    packet.DroppedReason = PacketDropedReasons.TimeToLive;
                    packet.aDroppedReason = "TimeTolive at sendDatapack function";
                    this.updateStates(packet);
                }
                else
                {
                    this.ComputeOverhead(packet, EnergyConsumption.Recive, null);
                    this.sendDataPack(packet);
                }
            }
        }
        #endregion
        public void SendQReq(Sensor sender, Packet QReq)
        {
            //NextHopSelector NXT = new NextHopSelector();
            sender.SwichToActive();
            //Sensor Reciver = selectQueryForwader(sender, QReq);           

            lock (sender.TBDDFlowTable)
            {
                if (sender.inCell != -1 && sender.TBDDNodeTable.CellHeaderTable.isHeader) // sender may get header role after packet queued
                {
                    QReq.Destination = sender;
                    QReq.DestinationAddress = QReq.Destination.CenterLocation;
                    QReq.isDelivered = true;
                    this.updateStates(QReq);                    
                    GenerateQueryResponse(this, QReq.Source);

                    this.CellHeaderRecvQReq(QReq);
                    return;
                }

                if (sender.inCell != -1)
                {
                    QReq.Destination = sender.TBDDNodeTable.myCellHeader;
                    QReq.DestinationAddress = sender.TBDDNodeTable.myCellHeader.CenterLocation;
                }

                if (QReq.Destination != null && !QReq.Destination.TBDDNodeTable.CellHeaderTable.isHeader)
                {
                    counter.SaveToQueue(sender, QReq);
                    return;
                }

                Sensor Reciver;
                if (QReq.Destination != null && Operations.isInMyComunicationRange(sender, QReq.Destination))
                {
                    Reciver = QReq.Destination;
                    sender.ComputeOverhead(QReq, EnergyConsumption.Transmit, Reciver);

                    Reciver.RecvQReq(QReq);
                    return;
                }

                LinkRouting.GetD_Distribution(sender, QReq);
                if (TBDDFlowTable.Count == 0)
                {
                    QReq.isDelivered = false;
                    QReq.aDroppedReason = "No Flows Matched";
                    this.updateStates(QReq);
                    return;
                }
                else
                {
                    FlowTableEntry FlowEntry = MatchFlow(QReq);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                        if (Reciver.ID == ID)
                        {
                            Console.WriteLine("Same id");
                        }
                        sender.ComputeOverhead(QReq, EnergyConsumption.Transmit, Reciver);
                        Reciver.RecvQReq(QReq);
                    }
                    else
                    {
                        counter.SaveToQueue(sender, QReq);
                    }
                }

            }

        }
         public void SendQResponse(Sensor sender, Packet QResp)
         {

            sender.SwichToActive();

            lock (sender.TBDDFlowTable)
            {
                Sensor Reciver;
                if (Operations.isInMyComunicationRange(sender, QResp.Destination))
                {
                    Reciver = QResp.Destination;
                    if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                    {
                        sender.ComputeOverhead(QResp, EnergyConsumption.Transmit, Reciver);
                        Reciver.RecvQueryResponse(QResp);
                    }
                    else
                    {
                        counter.SaveToQueue(sender, QResp);
                    }
                }
                else
                {
                    LinkRouting.GetD_Distribution(sender, QResp);
                    if (TBDDFlowTable.Count == 0)
                    {
                        QResp.isDelivered = false;
                        QResp.aDroppedReason = "No Flows Matched";
                        this.updateStates(QResp);
                        return;
                    }
                    else
                    {
                        FlowTableEntry FlowEntry = MatchFlow(QResp);
                        if (FlowEntry != null)
                        {
                            Reciver = FlowEntry.NeighborEntry.NeiNode;
                            if (Reciver.ID == ID)
                            {
                                Console.WriteLine("Same id");
                            }
                            sender.ComputeOverhead(QResp, EnergyConsumption.Transmit, Reciver);
                            Reciver.RecvQueryResponse(QResp);
                        }
                        else
                        {
                            counter.SaveToQueue(sender, QResp);
                        }
                    }

                }
            }
         }

        public void SendAS(Packet AS)
        {
            lock (TBDDFlowTable) {

                Sensor Reciver = AS.Destination;
                if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                {
                    this.ComputeOverhead(AS, EnergyConsumption.Transmit, Reciver);
                   // Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + AS.PID);
                    Reciver.RecieveAS(AS);
                }
                else
                {
                    counter.SaveToQueue(this, AS);
                }
            }
           
        }

        public void sendFM(Packet FM)
        {
            Sensor Reciver;
            if (Operations.isInMyComunicationRange(this, FM.Destination))
            {
                Reciver = FM.Destination;
                if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                {
                    Reciver.ComputeOverhead(FM, EnergyConsumption.Transmit, Reciver);
                    Reciver.RecieveFM(FM);
                }
                else
                {
                    counter.SaveToQueue(this, FM);
                }

            }
            else
            {
                LinkRouting.GetD_Distribution(this, FM);
                FlowTableEntry FlowEntry = MatchFlow(FM);
                if (FlowEntry != null)
                {
                    Reciver = FlowEntry.NeighborEntry.NeiNode;
                    Reciver.ComputeOverhead(FM, EnergyConsumption.Transmit, Reciver);
                    FlowEntry.DownLinkStatistics += 1;
                    Reciver.RecieveFM(FM);

                }
                else
                {
                    counter.SaveToQueue(this, FM);
                }
            }
           
        }

        public void sendFSA(Packet FSA)
        {
            Sensor Reciver;
            if (this.isRingnode)
            {
                FSA.Destination = this;
                this.RecieveFSA(FSA);
            }
            else
            {
                Reciver = DataForwaderForFSA(this, FSA);
                if (Reciver != null)
                {
                    if (Reciver.isRingnode)
                    {
                        FSA.Destination = Reciver;
                    }
                    // overhead:
                    this.ComputeOverhead(FSA, EnergyConsumption.Transmit, Reciver);
                    Reciver.RecieveFSA(FSA);
                }
                else
                {
                    counter.SaveToQueue(this, FSA); // save in the queue.
                }
            }

        }

        //*******************Recieving 

        #region //Recieving a follow Up
        //Inform Old Agent
        public void RecieveFM(Packet FM)
        {
            FM.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                if (this.ID == FM.Destination.ID )
                {
                        if (this.AgentNode.hasStoredPackets)
                        {
                            this.AgentDelieverStoredPackets();
                        }
                        FM.isDelivered = true;
                        this.updateStates(FM);
                }
                else
                {
                    if (FM.Hops > FM.TimeToLive)
                    {
                        FM.isDelivered = false;
                        FM.DroppedReason = PacketDropedReasons.TimeToLive;
                        this.updateStates(FM);
                    }
                    else
                    {
                        this.ComputeOverhead(FM, EnergyConsumption.Recive, null);
                        this.sendFM(FM);
                    }
                }
            }
            else
            {
                FM.isDelivered = false;
                FM.DroppedReason = PacketDropedReasons.DeadNode;
                this.updateStates(FM);
            }
        }

        //Inform Root
        public void RecieveFSA(Packet FSA)
        {
            FSA.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                FSA.Path += ">" + this.ID;
                if (FSA.Destination != null && FSA.Destination.ID == this.ID)
                {
                    this.TBDDNodeTable.SinkAgent = FSA.Source;
                    FSA.isDelivered = true;
                    this.updateStates(FSA);
                    FSA.ringSourceID = this.ID;
                    this.sharingFSA(FSA);
                }
                else if (FSA.Hops > FSA.TimeToLive)
                {
                    FSA.isDelivered = false;
                    FSA.DroppedReason = PacketDropedReasons.TimeToLive;
                    this.updateStates(FSA);
                    return;
                }
                else
                {
                    this.ComputeOverhead(FSA, EnergyConsumption.Recive, null);
                    this.sendFSA(FSA);
                }
            }
            else
            {
                FSA.isDelivered = false;
                FSA.DroppedReason = PacketDropedReasons.DeadNode;
                this.updateStates(FSA);
            }
           
        }

        public void sharingFSA(Packet FSA)
        {
            if (FSA.ringSourceID != this.ringFollower.ID)
            {
                // sending
                this.ComputeOverhead(FSA, EnergyConsumption.Transmit, this.ringFollower);

                // recieving 
                this.ringFollower.ComputeOverhead(FSA, EnergyConsumption.Recive, null);
                this.ringFollower.TBDDNodeTable.SinkAgent = FSA.Source;
                this.ringFollower.sharingFSA(FSA);
            }
        }

        //Agent Selection
        public void RecieveAS(Packet AS)
        {
            AS.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                AS.Path += ">" + this.ID;
                if (this.ID == AS.Destination.ID) {
                        //recieve by new agent 
                     AS.isDelivered = true;
                     this.updateStates(AS);
                    //this.GenerateFSA(AS.Root);    
                     //this.GenerateFSA(AS.PossibleDest);

                    if (this.AgentNode.hasStoredPackets)
                    {
                        this.AgentDelieverStoredPackets();
                    }
            }
                else
                {
                    if (AS.Hops > AS.TimeToLive)
                    {
                        // drop the paket.
                        AS.isDelivered = false;
                        AS.DroppedReason = PacketDropedReasons.TimeToLive;
                        this.updateStates(AS);
                    }
                    else
                    {
                        this.ComputeOverhead(AS, EnergyConsumption.Recive, null);
                        this.SendAS(AS);
                    }
                }
                
            }
            else
            {
                AS.isDelivered = false;
                AS.DroppedReason = PacketDropedReasons.DeadNode;
                this.updateStates(AS);
            }
           
        }
        #endregion


        public void SendTreeChange(Packet packet)
        {
            Sensor Reciver;
            if (Operations.isInMyComunicationRange(this, packet.Destination))
            {
                Reciver = packet.Destination;
                if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                {
                    Reciver.ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                    Reciver.RecvTreeChange(packet);
                }
                else
                {
                    counter.SaveToQueue(this, packet);
                }

            }
            else
            {
                LinkRouting.GetD_Distribution(this, packet);
                FlowTableEntry FlowEntry = MatchFlow(packet);
                if (FlowEntry != null)
                {
                    Reciver = FlowEntry.NeighborEntry.NeiNode;
                    Reciver.ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                    Reciver.RecvTreeChange(packet);

                }
                else
                {
                    counter.SaveToQueue(this, packet);
                }
            }
        }
        public void RecvTreeChange(Packet packet)
        {
            packet.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                packet.Path += ">" + this.ID;
                if (packet.Destination.ID == this.ID)
                {
                    packet.isDelivered = true;
                    this.updateStates(packet);
                    return;
                }
                else
                {
                    if (packet.Hops > packet.TimeToLive)
                    {
                        // drop the paket.
                        packet.DroppedReason = PacketDropedReasons.TimeToLive;
                        packet.isDelivered = false;
                        this.updateStates(packet);
                        return;
                    }
                    else
                    {
                        // forward the packet.
                        this.ComputeOverhead(packet, EnergyConsumption.Recive, null);
                        this.SendTreeChange(packet);
                    }
                }
            }
            else
            {
                packet.isDelivered = false;
                packet.DroppedReason = PacketDropedReasons.DeadNode;
                this.updateStates(packet);
            }
           
        }

        #region//Recieving Query
        public void  RecvQueryResponse(Packet QResp)
        {
            
            QResp.Path += ">" + this.ID;

            if (new LoopChecker(QResp).isLoop)
            {
                // drop the packet:
                QResp.IsLooped = true;
                //counter.DropPacket(packt, Reciver, PacketDropedReasons.Loop);
                QResp.isDelivered = false;
                QResp.aDroppedReason = "Loooooop in RecvQueryResponse";
                this.updateStates(QResp);
                return;
            }

            QResp.ReTransmissionTry = 0;
            if (this.ID == QResp.Destination.ID)
            {
                QResp.isDelivered = true;
                this.updateStates(QResp);
                //reciver.Ellipse_nodeTypeIndicator.Fill = Brushes.Transparent;
                generateDataToActiveReg(this, QResp.activeRegId);
            }
            else
            {
                if (QResp.Hops <= QResp.TimeToLive)
                {
                    this.ComputeOverhead(QResp, EnergyConsumption.Recive, null);
                    SendQResponse(this, QResp);
                }
                else
                {
                    QResp.isDelivered = false;
                    QResp.aDroppedReason = "TimeToLive";
                    this.updateStates(QResp);
                }
            }
           
        }

     
        public static bool needtoCheck = false;
     

        public void CellHeaderRecvQReq(Packet DataPck)
        {
           if(BT.threshReached(this.ResidualEnergyPercentage)){
               CellFunctions.ChangeCellHeader(this);
           }
        }

        public void RecvQReq(Packet QReq)
        {
            QReq.Path += ">" + this.ID;

            if (new LoopChecker(QReq).isLoop)
            {
                //if (QReq.LoopCnt < 1)
                //{
                //    QReq.LoopCnt += 1;
                //    counter.SaveToQueue(this, QReq);
                //    return;
                //}
                // drop the packet:
                QReq.IsLooped = true;
                //counter.DropPacket(packt, Reciver, PacketDropedReasons.Loop);
                QReq.isDelivered = false;
                QReq.aDroppedReason = "Loooooop in RecvQReq";
                this.updateStates(QReq);
                return;
            }

            QReq.ReTransmissionTry = 0;
            if (this.inCell != -1)
            {
                if (this.TBDDNodeTable.CellHeaderTable.isHeader)
                {
                    QReq.Destination = this;
                    QReq.isDelivered = true;
                    this.updateStates(QReq);
                    this.CellHeaderRecvQReq(QReq);

                    GenerateQueryResponse(this, QReq.Source);

                }
                else
                {
                    QReq.DestinationAddress = this.TBDDNodeTable.myCellHeader.CenterLocation;
                    QReq.Destination = this.TBDDNodeTable.myCellHeader;

                    this.ComputeOverhead(QReq, EnergyConsumption.Recive, null);
                    SendQReq(this, QReq);
                }
            }
            else
            {
                if (QReq.Hops <= QReq.TimeToLive)
                {
                    this.ComputeOverhead(QReq, EnergyConsumption.Recive, null);
                    SendQReq(this, QReq);
                }
                else
                {
                    QReq.isDelivered = false;
                    //QReq.DroppedReason = "TTL>Hops";
                    QReq.DroppedReason = PacketDropedReasons.TimeToLive;
                    QReq.aDroppedReason = "TimeToLive";
                    this.updateStates(QReq);
                }
            }
        }

        #endregion

        public void AgentDelieverStoredPackets()
        {
            do
            {
                Console.WriteLine("Agent Deliever stored packets");
                Packet packet = this.AgentNode.AgentBuffer.Dequeue();
                if (this.isSinkAgent)
                {
                    if (this.AgentNode.isSinkInRange())
                    {
                      //  Console.WriteLine("Sending to the sink directly");
                        packet.Destination = PublicParameters.SinkNode;
                        packet.DestinationAddress = packet.Destination.CenterLocation;
                        packet.TimeToLive += maxHopsForDestination(this.CenterLocation, packet.Destination.CenterLocation);
                        this.sendDataPack(packet);
                    }
                }
                else if (this.AgentNode.NewAgent != null)
                {
                   // Console.WriteLine("Sending to the new agent, packet {0}", packet.PID);
                    packet.Destination = this.AgentNode.NewAgent;
                    packet.DestinationAddress = packet.Destination.CenterLocation;
                    packet.TimeToLive += maxHopsForDestination(this.CenterLocation, packet.Destination.CenterLocation);
                    PIDE = packet.PID;
                    this.sendDataPack(packet);
                }
                else if (this.ID == PublicParameters.SinkNode.ID)
                {
                    packet.isDelivered = true;
                    this.updateStates(packet);
                }
                else if (Operations.isInMyComunicationRange(this, PublicParameters.SinkNode))
                {
                    packet.Destination = PublicParameters.SinkNode;
                    packet.DestinationAddress = packet.Destination.CenterLocation;
                    this.sendDataPack(packet);
                    return;
                }

                else
                {
                    if (this.inCell == -1) packet.DestinationAddress = this.TBDDNodeTable.NearestCellCenter;
                    packet.TimeToLive += 10;
                    packet.isRedlightON = false;
                    packet.isInSendData = false;
                    ForwardDataUsingTree(this, packet);
                    return;

                    //packet.isDelivered = false;
                    //packet.DroppedReason = PacketDropedReasons.RecoveryNoNewAgentFound;
                    //packet.aDroppedReason = "NoNewAgentFound";
                    //this.updateStates(packet);
                }
            } while (this.AgentNode.AgentBuffer.Count > 0);
            
        }


        public static long PIDE = -1;

        public void updateStates(Packet packet)
        {
            if (packet.isDelivered)
            {
               
                if (packet.PacketType == PacketType.FM || packet.PacketType == PacketType.FSA || packet.PacketType == PacketType.AS || packet.PacketType==PacketType.Control)
                {
                    PublicParameters.NumberofDelieveredFollowUpPackets += 1;
                }
                else if (packet.PacketType == PacketType.QReq || packet.PacketType == PacketType.QResp)
                {
                    PublicParameters.NumberOfDelieveredQueryPackets += 1;
                    //packet.ComputeDelay();
                    //PublicParameters.QueryDelay += packet.Delay;
                }
                else if (packet.PacketType == PacketType.RegBeacon)
                {
                    PublicParameters.NumberofDelivedRegBeaconPackets += 1;
                    //packet.ComputeDelay();
                    //PublicParameters.QueryDelay += packet.Delay;
                }
                else
                {
                    PublicParameters.NumberOfDelieveredDataPackets += 1;
                    //packet.ComputeDelay();
                    //PublicParameters.DataDelay += packet.Delay;
                }


                PublicParameters.OverallDelieverdPackets += 1;
                // Console.WriteLine("{2} Packet: {0} with Path: {1} delievered",packet.PID,packet.Path,packet.PacketType);
                PublicParameters.FinishedRoutedPackets.Add(packet);

                this.ComputeOverhead(packet, EnergyConsumption.Recive, null);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_total_consumed_energy.Content = PublicParameters.TotalEnergyConsumptionJoule + " (JOULS)", DispatcherPriority.Send);

                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Delivered_TPacket.Content = PublicParameters.OverallDelieverdPackets, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Delivered_Packet.Content = PublicParameters.NumberOfDelieveredDataPackets, DispatcherPriority.Send);

                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_sucess_ratio.Content = PublicParameters.DeliveredRatio, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_nymber_inQueu.Content = PublicParameters.InQueuePackets.ToString());
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_num_of_disseminatingNodes.Content = PublicParameters.NumberOfNodesDissemenating.ToString());
                //MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Average_QDelay.Content = PublicParameters.AverageQueryDelay.ToString());
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Total_Delay.Content = PublicParameters.AverageDelay.ToString());

                UnIdentifySourceNode(packet.Source);
                // Console.WriteLine("PID:" + packet.PID + " has been delivered.");
            }
            else
            {
               
                Console.WriteLine("Failed {2} PID: {0} Reason: {1} ({6}), source region: {3}, ID : {4}, forwarder : {5}", packet.PID, packet.aDroppedReason,packet.PacketType, packet.Source.sReg, packet.Source.ID, this.ID, packet.DroppedReason);
                PublicParameters.NumberofDropedPackets += 1;
                PublicParameters.DropedPacketsList.Add(packet);
                //Console.WriteLine("PID:" + packet.PID + " has been droped.");

                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_nymber_inQueu.Content = PublicParameters.InQueuePackets.ToString());
                if (Settings.Default.SavePackets)
                    PublicParameters.FinishedRoutedPackets.Add(packet);
                else
                    packet.Dispose();
            }
        }

        private void DeliveerPacketsInQueuTimer_Tick(object sender, EventArgs e)
        {
            if(WaitingPacketsQueue.Count > 0)
            {
                Packet toppacket = WaitingPacketsQueue.Dequeue();
                toppacket.WaitingTimes += 1;
                toppacket.ReTransmissionTry += 1;
                PublicParameters.TotalWaitingTime += 1; // total;
                int bound = 3;
                int count = 2;
                //if (PublicParameters.listOfRegs.Count == 4 || PublicParameters.listOfRegs.Count == 2 || PublicParameters.listOfRegs.Count == 3)
                //{
                //    bound = 10;
                //}
                //else if (PublicParameters.listOfRegs.Count > 4)
                //{
                //    bound = 14;
                //}

                if (toppacket.ReTransmissionTry <= bound)
                {
                    if (toppacket.PacketType == PacketType.QResp)
                    {
                        SendQResponse(this, toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.QReq)
                    {
                        SendQReq(this, toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.FSA)
                    {
                        this.sendFSA(toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.AS)
                    {
                        this.SendAS(toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.Data)
                    {
                        if (toppacket.isInSendData)
                        {
                            this.sendDataPack(toppacket);
                        }
                        else if (toppacket.isDataStartUsingTree)
                        {
                            //if (toppacket.Destination == null && this.inCell != -1 && 
                            //    Operations.DistanceBetweenTwoPoints(this.CenterLocation, toppacket.DestinationAddress) <= PublicParameters.cellDiameter / 2)
                            //{
                            //    toppacket.isRedlightON = false;
                            //}
                            //if (toppacket.Destination != null && !toppacket.Destination.isSinkAgent &&
                            //    toppacket.Destination.ID != PublicParameters.SinkNode.ID &&
                            //    !toppacket.Destination.TBDDNodeTable.CellHeaderTable.isHeader)
                            //{
                            //    toppacket.isRedlightON = false;
                            //}

                            ForwardDataUsingTree(this, toppacket);
                        }
                        else
                        {                       
                            MlCDDDataPackForwader(this, toppacket);
                        }
                        
                    }
                    else if (toppacket.PacketType == PacketType.FM)
                    {
                        this.sendFM(toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.Control)
                    {
                        this.SendTreeChange(toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.RegBeacon)
                    {
                        ActiveRegBeacon act = new ActiveRegBeacon();
                        act.sendRegbeacon(toppacket, this);
                        if (toppacket.ReTransmissionTry == bound && !toppacket.isDelivered && count > 0)
                        {
                            toppacket.ReTransmissionTry = 1;
                            count -= 1;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Unknown in the Queue");
                    }
                }
                else
                {
                    // PublicParameters.NumberofDropedPackets += 1;
                    toppacket.isDelivered = false;
                    toppacket.DroppedReason = PacketDropedReasons.WaitingTime;
                    toppacket.aDroppedReason = "waitingtime > " + bound.ToString() + " at DeliveerPacketsInQueuTimer_Tick function";
                    this.updateStates(toppacket);
                    //  Console.WriteLine("Waiting times more for packet {0}", toppacket.PID);
                    // PublicParameters.FinishedRoutedPackets.Add(toppacket);
                    //    MessageBox.Show("PID:" + toppacket.PID + " has been droped. Packet Type = "+toppacket.PacketType);
                    MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);
                }
                if (WaitingPacketsQueue.Count == 0)
                {
                    if (Settings.Default.ShowRadar) Myradar.StopRadio();
                    QueuTimer.Stop();
                    // Console.WriteLine("NID:" + this.ID + ". Queu Timer is stoped.");
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);
                }
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_nymber_inQueu.Content = PublicParameters.InQueuePackets.ToString());
            }
            else
            {
                if (Settings.Default.ShowRadar) Myradar.StopRadio();
                QueuTimer.Stop();
                // Console.WriteLine("NID:" + this.ID + ". Queu Timer is stoped.");
                MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);
                SwichToSleep();
            }
            
            
        }

        private void RemoveOldAgentTimer_Tick(object sender, EventArgs e) {
            OldAgentTimer.Stop();
            this.AgentNode = new Agent();
        }

               
        public static int CountRedun =0;
        public void RedundantTransmisionCost(Packet pacekt, Sensor reciverNode)
        {
            // logs.
            PublicParameters.TotalReduntantTransmission += 1;       
            double UsedEnergy_Nanojoule = EnergyModel.Receive(PublicParameters.PreamblePacketLength); // preamble packet length.
            double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
            reciverNode.ResidualEnergy = reciverNode.ResidualEnergy - UsedEnergy_joule;
            pacekt.UsedEnergy_Joule += UsedEnergy_joule;
            PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
            PublicParameters.TotalWastedEnergyJoule += UsedEnergy_joule;
            MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Redundant_packets.Content = PublicParameters.TotalReduntantTransmission);
            MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Wasted_Energy_percentage.Content = PublicParameters.WastedEnergyPercentage);
        }

        /// <summary>
        /// the node which is active will send preample packet and will be selected.
        /// match the packet.
        /// </summary>
        public FlowTableEntry MatchFlow(Packet pacekt)
        {

            FlowTableEntry ret = null;
            try
            {
               
                if (TBDDFlowTable.Count > 0)
                {
                  
                    foreach (FlowTableEntry selectedflow in TBDDFlowTable)
                    {
                        if (selectedflow.NID != PublicParameters.SinkNode.ID)
                        {
                            if (pacekt.PacketType == PacketType.RegBeacon)
                            {
                                selectedflow.NeighborEntry.NeiNode.Mac.SwichToActive();
                            }
                            if (selectedflow.SensorState == SensorState.Active && selectedflow.DownLinkAction == FlowAction.Forward && selectedflow.SensorBufferHasSpace)
                            {
                                if (ret == null)
                                {
                                    ret = selectedflow;
                                }
                                else
                                {
                                    RedundantTransmisionCost(pacekt, selectedflow.NeighborEntry.NeiNode);
                                }
                            }
                        }
                        
                    }
                }
                else
                {
                    //MessageBox.Show("No Flow!!!. muach flow!");
                    return null;
                }
            }
            catch
            {
                ret = null;
                MessageBox.Show(" Null Match.!");
            }

            return ret;
        }

        // When the sensor open the channel to transmit the data.

        public void ComputeOverhead(Packet packt, EnergyConsumption enCon, Sensor Reciver)
        {
            if (enCon == EnergyConsumption.Transmit)
            {
                if (ID != PublicParameters.SinkNode.ID)
                {
                    // calculate the energy 
                    double Distance_M = Operations.DistanceBetweenTwoSensors(this, Reciver);
                    double UsedEnergy_Nanojoule = EnergyModel.Transmit(packt.PacketLength, Distance_M);
                    double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
                    ResidualEnergy -= UsedEnergy_joule;
                    PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
                    packt.UsedEnergy_Joule += UsedEnergy_joule;
                    packt.RoutingDistance += Distance_M;
                    packt.Hops += 1;
                    double delay = DelayModel.DelayModel.Delay(this, Reciver, packt);
                    packt.Delay += delay;

                    PublicParameters.TotalDelayMs += delay;
                    PublicParameters.TotalNumberOfHops += 1;
                    PublicParameters.TotalRoutingDistance += Distance_M;

                    if (Settings.Default.SaveRoutingLog)
                    {
                        RoutingLog log = new RoutingLog();
                        log.PacketType = packt.PacketType;
                        log.IsSend = true;
                        log.NodeID = ID;
                        log.Operation = "To:" + ID;
                        log.Time = DateTime.Now;
                        log.Distance_M = Distance_M;
                        log.UsedEnergy_Nanojoule = UsedEnergy_Nanojoule;
                        log.RemaimBatteryEnergy_Joule = ResidualEnergy;
                        log.PID = packt.PID;
                        Logs.Add(log);
                    }

                    switch (packt.PacketType)
                    {
                        case PacketType.Data:
                            PublicParameters.EnergyConsumedForDataPackets += UsedEnergy_joule; // data packets.
                            PublicParameters.DataDelay += delay;
                            PublicParameters.TotalNumberOfHopsData += 1;
                            break;
                        case PacketType.QReq:
                            PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule; // other packets.
                            PublicParameters.QueryDelay += delay;
                            PublicParameters.TotalNumberOfHopsControl += 1;
                            break;
                        case PacketType.QResp:
                            PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule; // other packets.
                            PublicParameters.QueryDelay += delay;
                            PublicParameters.TotalNumberOfHopsControl += 1;
                            break;
                        default:
                            PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule; // other packets.
                            PublicParameters.ControlDelay += delay;
                            PublicParameters.TotalNumberOfHopsControl += 1;
                            break;
                    }
                }

                if (Settings.Default.ShowRoutingPaths)
                {
                    OpenChanel(Reciver.ID, packt.PacketType);
                }

            }
            else if (enCon == EnergyConsumption.Recive)
            {

                double UsedEnergy_Nanojoule = EnergyModel.Receive(packt.PacketLength);
                double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
                ResidualEnergy = ResidualEnergy - UsedEnergy_joule;
                packt.UsedEnergy_Joule += UsedEnergy_joule;
                PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;

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

     
        #endregion

        #region Buffer



        public void ReRoutePacketsInCellHeaderBuffer()
        {
            while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
            {
                Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
                ForwardDataUsingTree(this, pkt);
            } 
        }

        public void ClearCellHeaderBuffer()
        {
            if (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
            {
                Console.WriteLine("Clearing cell header");
                //Regular Cell Node
                if (!TBDDNodeTable.CellHeaderTable.isHeader)
                {
                    while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
                    {
                        Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
                        pkt.TimeToLive += 3;
                        ForwardDataUsingTree(this, pkt);
                    }
                }               
                else if (TBDDNodeTable.CellHeaderTable.isRootHeader)
                {
                    while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
                    {
                        Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();

                        ForwardDataUsingTree(this, pkt);
                    } 
                }
                //Regular CellHeader
                else if (TBDDNodeTable.CellHeaderTable.isHeader)
                {
                    while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
                    {
                        Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();

                        pkt.TimeToLive += maxHopsForDestination(this.CenterLocation, pkt.DestinationAddress);                          
                        ForwardDataUsingTree(this, pkt);
                    } 

                }
                else
                {
                    do
                    {
                        Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
                        pkt.isDelivered = false;
                        pkt.DroppedReason = PacketDropedReasons.Unknow;
                        pkt.aDroppedReason = "clear cellheaderbuffer at function,, from oldroot to new root";
                        this.updateStates(pkt);
                    } while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0);
                }
            }
        }

        //public void ClearCellHeaderBuffer()
        //{
        //    if (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
        //    {
        //        Console.WriteLine("Clearing cell header");
        //        //Regular Cell Node
        //        if (!TBDDNodeTable.CellHeaderTable.isHeader)
        //        {
        //            do
        //            {
        //                Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
        //                if (TBDDNodeTable.myCellHeader.ID != this.ID)
        //                {                            
        //                    pkt.Destination = TBDDNodeTable.myCellHeader;
        //                    pkt.DestinationAddress = TBDDNodeTable.myCellHeader.CenterLocation;
        //                }
        //                else
        //                {
        //                    Console.WriteLine("From Buffer");
        //                }
        //                pkt.TimeToLive += maxHopsForQuery(this);
        //                ForwardDataUsingTree(this, pkt);
        //            } while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0);
        //        }
        //        //Regular CellHeader
        //        else if (!TBDDNodeTable.CellHeaderTable.isRootHeader)
        //        {
        //            do
        //            {
        //                Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
        //                if (TBDDNodeTable.CellHeaderTable.ParentCellCenter != null)
        //                {
        //                    pkt.DestinationAddress = TBDDNodeTable.CellHeaderTable.ParentCellCenter;
        //                    pkt.Destination = null;

        //                }
        //                else
        //                {
        //                    Console.WriteLine("From Buffer 2");
        //                }
        //                pkt.TimeToLive += maxHopsForQuery(this);
        //                ForwardDataUsingTree(this, pkt);
        //            } while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0);

        //        }
        //        else if (TBDDNodeTable.CellHeaderTable.isRootHeader)
        //        {
        //            do
        //            {
        //                Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
        //                if (TBDDNodeTable.CellHeaderTable.hasSinkPosition)
        //                {
        //                    ForwardDataUsingTree(this, pkt);
        //                }
        //                else
        //                {
        //                    pkt.isDelivered = false;
        //                    pkt.aDroppedReason = "Max # tries dnt havesink pos";
        //                    this.updateStates(pkt);
        //                }


        //            } while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0);
        //        }
        //        else
        //        {
        //            do
        //            {
        //                Packet pkt = TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
        //                pkt.isDelivered = false;
        //                pkt.aDroppedReason = "Couldn't find destination";
        //                this.updateStates(pkt);
        //            } while (TBDDNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0);
        //        }


        //    }


        //}
        #endregion






        private void lbl_MouseEnter(object sender, MouseEventArgs e)
        {
            ToolTip = new Label() { Content = "("+ID + ") [ " + ResidualEnergyPercentage + "% ] [ " + ResidualEnergy + " J ]" };
        }

        private void btn_show_routing_log_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(Logs.Count>0)
            {
                UiShowRelativityForAnode re = new ui.UiShowRelativityForAnode();
                re.dg_relative_shortlist.ItemsSource = Logs;
                re.Show();
            }
        }

        private void btn_draw_random_numbers_MouseDown(object sender, MouseButtonEventArgs e)
        {
            List<KeyValuePair<int, double>> rands = new List<KeyValuePair<int, double>>();
            int index = 0;
            foreach (RoutingLog log in Logs )
            {
                if(log.IsSend)
                {
                    index++;
                    rands.Add(new KeyValuePair<int, double>(index, log.ForwardingRandomNumber));
                }
            }
            UiRandomNumberGeneration wndsow = new ui.UiRandomNumberGeneration();
            wndsow.chart_x.DataContext = rands;
            wndsow.Show();
        }

        private void Ellipse_center_MouseEnter(object sender, MouseEventArgs e)
        {
            
        }

        private void btn_show_my_duytcycling_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
        }

        private void btn_draw_paths_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NetworkVisualization.UpLinksDrawPaths(this);
        }

       
         
        private void btn_show_my_flows_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
            ListControl ConMini = new ui.conts.ListControl();
            ConMini.lbl_title.Content = "Mini-Flow-Table";
            ConMini.dg_date.ItemsSource = TBDDFlowTable;


            ListControl ConNei = new ui.conts.ListControl();
            ConNei.lbl_title.Content = "Neighbors-Table";
            ConNei.dg_date.ItemsSource = NeighborsTable;

            UiShowLists win = new UiShowLists();
            win.stack_items.Children.Add(ConMini);
            win.stack_items.Children.Add(ConNei);
            win.Title = "Tables of Node " + ID;
            win.Show();
            win.WindowState = WindowState.Maximized;
        }

        private void btn_send_1_p_each1sec_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SendPacketTimer.Start();
            SendPacketTimer.Tick += SendPacketTimer_Random; // redfine th trigger.
        }



        public void RandomSelectEndNodes(int numOFpACKETS)
        {
            if (PublicParameters.SimulationTime > PublicParameters.MacStartUp)
            {
                int index = 1 + Convert.ToInt16(UnformRandomNumberGenerator.GetUniform(PublicParameters.NumberofNodes - 2));
                if (index != PublicParameters.SinkNode.ID)
                {
                    Sensor endNode = MainWindow.myNetWork[index];
                    GenerateMultipleControlPackets(numOFpACKETS, endNode);
                }
            }
        }

        private void SendPacketTimer_Random(object sender, EventArgs e)
        {
            if (ID != PublicParameters.SinkNode.ID)
            {
                // uplink:
                GenerateMultipleDataPackets(1);
            }
            else
            { //
                RandomSelectEndNodes(1);
            }
        }

        /// <summary>
        /// i am slected as end node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_select_me_as_end_node_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lbl_title = sender as Label;
            switch (lbl_title.Name)
            {
                case "Btn_select_me_as_end_node_1":
                    {
                       PublicParameters.SinkNode.GenerateMultipleControlPackets(1, this);

                        break;
                    }
                case "Btn_select_me_as_end_node_10":
                    {
                        PublicParameters.SinkNode.GenerateMultipleControlPackets(10, this);
                        break;
                    }
                //Btn_select_me_as_end_node_1_5sec

                case "Btn_select_me_as_end_node_1_5sec":
                    {
                        PublicParameters.SinkNode.SendPacketTimer.Start();
                        PublicParameters.SinkNode.SendPacketTimer.Tick += SelectMeAsEndNodeAndSendonepacketPer5s_Tick;
                        break;
                    }
            }
        }

        
        
        public void SelectMeAsEndNodeAndSendonepacketPer5s_Tick(object sender, EventArgs e)
        {
            PublicParameters.SinkNode.GenerateMultipleControlPackets(1, this);
        }


        /*** Vistualize****/

        public void ShowSensingRange(bool isVis)
        {
            if (isVis) Ellipse_Sensing_range.Visibility = Visibility.Visible;
            else Ellipse_Sensing_range.Visibility = Visibility.Hidden;
        }

        public void ShowComunicationRange(bool isVis)
        {
            if (isVis) Ellipse_Communication_range.Visibility = Visibility.Visible;
            else Ellipse_Communication_range.Visibility = Visibility.Hidden;
        }

        public void ShowBattery(bool isVis) 
        {
            if (isVis) Prog_batteryCapacityNotation.Visibility = Visibility.Visible;
            else Prog_batteryCapacityNotation.Visibility = Visibility.Hidden;
        }

        private void btn_update_mini_flow_MouseDown(object sender, MouseButtonEventArgs e)
        {
          
        }
    }
}
