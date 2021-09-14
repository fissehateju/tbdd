using TBDD.Dataplane;
using TBDD.Dataplane.PacketRouter;
using TBDD.Intilization;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TBDD.Constructor
{
    /// <summary>
    /// Interaction logic for Ring.xaml
    /// </summary>
    /// 
    public partial class RingNodeCandidate
    {
        public Sensor node { get; set; }
        public double difference { get; set; }
        public Sensor nextHop { get; set; }
        public List<RingNodeCandidate> NextHopCandidate = new List<RingNodeCandidate>();
        public bool alreadyRingNode = false;



        public RingNodeCandidate(Sensor sen, double dif)
        {
            difference = dif;
            node = sen;
        }
    }

    public partial class ConvexHullNodes
    {
        public Sensor node { get; set; }
        public double polarAngle { get; set; }
        public bool isInsideHull = false;
        public List<ConvexHullNodes> PotentialNextHop = new List<ConvexHullNodes>();
        public Queue<ConvexHullNodes> SortedConvexHullSet = new Queue<ConvexHullNodes>();
        public ConvexHullNodes(Sensor x, double y)
        {
            node = x;
            polarAngle = y;
        }

    }

    public partial class ConvexHullRing : UserControl
    {

        private static double InitialRadius { get; set; }
        private static double ThreshHold { get; set; }
        private Point NetworkCenter = PublicParameters.networkCenter;
        private static Canvas SensingField { get; set; }

        private List<RingNodeCandidate> RNodes = new List<RingNodeCandidate>();



        public ConvexHullRing()
        {
            InitializeComponent();
        }
        public static void setInitialParameters(double rad, double thresh, Canvas sensingField)
        {
            InitialRadius = rad;
            ThreshHold = thresh;
            SensingField = sensingField;
        }

        #region ConvexHull Method
        public static List<Sensor> SetofNodes = new List<Sensor>(); // All Nodes inside the area of intreset

        private static List<ConvexHullNodes> ConvexHullSet = new List<ConvexHullNodes>(); // All Nodes inside the area of intreset

        private static Stack<ConvexHullNodes> SubsetOfHull = new Stack<ConvexHullNodes>(); // Stack to go throw all the nodes

        public static List<Sensor> ConvexNodes = new List<Sensor>(); // Final Ring Nodes

        private static Queue<ConvexHullNodes> SortedSetOfConvexNodes = new Queue<ConvexHullNodes>();

        public static Sensor PointZero { get; set; }

        private static ConvexHullNodes PointZeroConvex { get; set; }

        private static void ConvexHullBuildMethod()
        {
            ConvexHullRing constructor = new ConvexHullRing();
            constructor.findTheSetofNodes();
            constructor.findPointZero();
            constructor.getPolarAngleToPointsFromAnchor(PointZero);
            ConvexHullNodes AnchorPoint = new ConvexHullNodes(PointZero, 0);   // ConvexHullNodes is second class in file Convex HullRing
            AnchorPoint.PotentialNextHop = ConvexHullSet;
            PointZeroConvex = AnchorPoint;
            constructor.sortConvexHullSet(PointZero);
            SubsetOfHull.Push(AnchorPoint);
            constructor.startBuilding();

        }

        private void findTheSetofNodes()
        {
            List<Sensor> NetworkNodes = PublicParameters.myNetwork;

            SetofNodes = PublicParameters.myNetwork;

        }

        private void findPointZero()
        {
            //O(n) where n = SetofNodes.Count();
            //P0 has the lowest y value , if two points have the same y value, we take the point that has the lowest x value amongst them
            List<Sensor> lowestYPoint = new List<Sensor>();
            double lowestYval = SetofNodes[0].CenterLocation.Y;
            Sensor holder = null;
            //(1) Find the lowest Y value
            foreach (Sensor p in SetofNodes)
            {
                if (p.CenterLocation.Y > lowestYval)
                {
                    lowestYval = p.CenterLocation.Y;
                }
            }
            //(2) Find the points with the lowest Y value, can be more than one
            foreach (Sensor p in SetofNodes)
            {
                if (p.CenterLocation.Y == lowestYval)
                {
                    lowestYPoint.Add(p);
                }
            }

            // Only One point 
            if (lowestYPoint.Count == 1)
            {
                PointZero = lowestYPoint[0];
            }
            else
            {
                //More than one point --> we check the lowest X value
                double lowestXVal = lowestYPoint[0].CenterLocation.X;
                foreach (Sensor p in lowestYPoint)
                {
                    if (p.CenterLocation.X <= lowestXVal)
                    {
                        holder = p;
                        lowestXVal = p.CenterLocation.X;
                    }
                }
                PointZero = holder;
            }

        }

        private void findPotentialPointsForAnchor(ConvexHullNodes anchor)
        {

            Point dest = new Point(anchor.node.CenterLocation.X, 10); // x-axis
            foreach (NeighborsTableEntry entry in anchor.node.NeighborsTable)
            {
                foreach (Sensor x in SetofNodes)
                {
                    if (ConvexNodes.Count > 3 && x == PointZero)
                    {
                        ConvexHullNodes nei = new ConvexHullNodes(x, 0);
                        anchor.PotentialNextHop.Add(nei);
                        return;
                    }
                    if (x.ID == entry.NeiNode.ID && !ConvexNodes.Contains(x))
                    {
                        double angle = Operations.GetDirectionAngle(anchor.node.CenterLocation, dest, x.CenterLocation);
                        ConvexHullNodes nei = new ConvexHullNodes(x, angle);
                        anchor.PotentialNextHop.Add(nei);
                        //anchor.PotentialNextHop.Add(x);
                    }
                }
            }
            SubsetOfHull.Push(anchor);
        }

        private bool isClockwise(ConvexHullNodes one, ConvexHullNodes two, ConvexHullNodes three)
        {
            Point a = one.node.CenterLocation;
            Point b = two.node.CenterLocation;
            Point c = three.node.CenterLocation;

            double value = ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));

            if (value >= 0)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        private void getPolarAngleToPointsFromAnchor(Sensor anchor)
        {
            // according to the direction they make with P0 and the X-Axis
            Point dest = new Point(anchor.CenterLocation.X + InitialRadius + ThreshHold, anchor.CenterLocation.Y); // x-axis
            foreach (Sensor px in SetofNodes)
            {
                if (px.ID != anchor.ID)
                {
                    double angle = Operations.GetDirectionAngle(anchor.CenterLocation, dest, px.CenterLocation);
                    ConvexHullNodes node = new ConvexHullNodes(px, angle);
                    ConvexHullSet.Add(node);
                    //Console.WriteLine("Node {0} , has Angle {1}", node.node.ID, node.polarAngle);
                    // Console.WriteLine("Node {0} , has Angle {1}", px.ID, angle);
                }
            }
        }

        private ConvexHullNodes getLowestAngle(List<ConvexHullNodes> set)
        {
            double lowest = 10;
            ConvexHullNodes holder = null;

            foreach (ConvexHullNodes compare in set)
            {
                if (compare.polarAngle < lowest)
                {
                    lowest = compare.polarAngle;
                    holder = compare;
                }
            }

            return holder;

        }

        private void sortConvexHullSet(Sensor anchor)
        {
            List<ConvexHullNodes> beforeSort = ConvexHullSet;
            ConvexHullNodes small = null;
            do
            {
                try
                {
                    small = getLowestAngle(beforeSort);
                    SortedSetOfConvexNodes.Enqueue(small);
                    beforeSort.Remove(small);
                    //Console.WriteLine("Node {0} has angle {1}", small.node.ID, small.polarAngle);
                }
                catch
                {
                    small = null;
                    MessageBox.Show("Just returned a null");
                }


            } while (beforeSort.Count > 0);

        }

        private void sortMyPotentialNextHop(ConvexHullNodes from)
        {
            List<ConvexHullNodes> beforeSort = from.PotentialNextHop;

            ConvexHullNodes small = null;
            do
            {
                try
                {
                    small = getLowestAngle(beforeSort);
                    from.SortedConvexHullSet.Enqueue(small);
                    beforeSort.Remove(small);
                   // Console.WriteLine("Node {0} has angle {1}", small.node.ID, small.polarAngle);
                }
                catch
                {
                    small = null;
                    MessageBox.Show("Just returned a null");
                }

            } while (beforeSort.Count > 0);

        }

        private static bool isMyNeighbor(Sensor s1, Sensor s2)
        {
            bool isNeighbor = false;
            foreach (NeighborsTableEntry entry in s1.NeighborsTable)
            {
                if (entry.NeiNode.ID == s2.ID)
                {
                    isNeighbor = true;
                    return true;
                }
            }
            return isNeighbor;
        }

        private void startBuilding()
        {
            bool takeNextPoint = true;
            SubsetOfHull.Push(SortedSetOfConvexNodes.Dequeue());
            do
            {
                if (takeNextPoint)
                {
                    SubsetOfHull.Push(SortedSetOfConvexNodes.Dequeue());
                }

                ConvexHullNodes PointThree = SubsetOfHull.Pop();
                ConvexHullNodes PointTwo = SubsetOfHull.Pop();
                ConvexHullNodes PointOne = SubsetOfHull.Pop();

                if (isClockwise(PointOne, PointTwo, PointThree))
                {
                    SubsetOfHull.Push(PointOne);
                    SubsetOfHull.Push(PointTwo);
                    SubsetOfHull.Push(PointThree);
                    takeNextPoint = true;
                }
                else
                {
                    SubsetOfHull.Push(PointOne);
                    SubsetOfHull.Push(PointThree);
                    if (SubsetOfHull.Count >= 3)
                    {
                        takeNextPoint = false;
                    }
                }

            } while (SortedSetOfConvexNodes.Count > 0);

          //  Console.WriteLine("Ending ****");
            int c = SubsetOfHull.Count;
            do
            {
                ConvexHullNodes x = SubsetOfHull.Pop();
                ConvexNodes.Add(x.node);
                PublicParameters.BorderNodes.Add(x.node);

                //   x.node.ShowComunicationRange(true);
              //  Console.WriteLine(x.node.ID);
            } while (SubsetOfHull.Count > 0);
   
            getBorderNodes();
        }

        #endregion

        private void getBorderNodes()
        {
            Sensor smallestX = PublicParameters.BorderNodes[0];
            Sensor smallestY = PublicParameters.BorderNodes[0];
            Sensor biggestX = PublicParameters.BorderNodes[0];
            Sensor biggestY = PublicParameters.BorderNodes[0];

            foreach (Sensor sen in PublicParameters.BorderNodes)
            {

                if (sen.Position.X > biggestX.Position.X)
                {
                    biggestX = sen;
                }
                if (sen.Position.X < smallestX.Position.X)
                {
                    smallestX = sen;
                }
                if (sen.Position.Y > biggestY.Position.Y)
                {
                    biggestY = sen;
                }
                if (sen.Position.Y < biggestX.Position.Y)
                {
                    smallestY = sen;
                }
            }

            PublicParameters.BorderNodes.Clear();
            PublicParameters.BorderNodes.Add(smallestY);
            PublicParameters.BorderNodes.Add(smallestX);
            PublicParameters.BorderNodes.Add(biggestY);
            PublicParameters.BorderNodes.Add(biggestX);

        }

        private void drawVirtualLine()
        {
            int start = 0;
            for (int counter = ConvexNodes.Count - 1; counter >= 0; counter--)
            {
                Sensor from = ConvexNodes[start];
                Sensor to = ConvexNodes[counter];
                Line lineBetweenTwo = new Line();
                lineBetweenTwo.Fill = Brushes.Black;
                lineBetweenTwo.Stroke = Brushes.Black;
                lineBetweenTwo.X1 = from.CenterLocation.X;
                lineBetweenTwo.Y1 = from.CenterLocation.Y;
                lineBetweenTwo.X2 = to.CenterLocation.X;
                lineBetweenTwo.Y2 = to.CenterLocation.Y;
                SensingField.Children.Add(lineBetweenTwo);
                start = counter;
            }
        }

        public static void startRingConstruction()
        {
            ConvexHullBuildMethod();

        }


    }

    
}
