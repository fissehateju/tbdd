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
    /// <summary>
    /// Interaction logic for Cluster.xaml
    /// </summary>
    public partial class CellGroup : UserControl
    {

        private double clusterHeight = PublicParameters.cellDiameter;
        private double clusterWidth = PublicParameters.cellDiameter;
        public List<Sensor> clusterNodes = new List<Sensor>();
        //Location variables for the center and the actual location of cluster
        public Point clusterLocMargin { get; set; }
        public Point clusterCenterComputed { get; set; }
        public Point clusterCenterMargin { get; set; }
        public Point clusterActualCenter { get; set; }
      
        public CellCenter centerOfCluster { get; set; }

        public int clusterDepth { get; set; }
        public int id { set; get; }
        public int clusterReg { get; set; }  // new property

        public int buildClustersunderTop { get; set; }
        public Link clusterLinks = new Link();
        public Link clusterBeaconLinks = new Link();

        private List<int> neighborClusters = new List<int>();
        List<Sensor> myNetwork = PublicParameters.myNetwork;

        private static int assignID { set; get; }
        public static Point ptrail = new Point();
        public static List<CellGroup> changePosClus = new List<CellGroup>();

        //Tree heirarchry variables
        public CellGroup parentCluster;
        public List<CellGroup> childrenClusters = new List<CellGroup>();
        public bool isLeafNode = false;
        public bool isVisited = false;
        public int clusterLevel { get; set; }
        public Point treeParentNodePos { get; set; }
        public Point treeNodePos { get; set; }
        public int xValue { get; set; }

        private List<Sensor> subnet { get; set; }

        public CellTable CellTable = new CellTable();
        //Cluster header
       // public ClusterHeaderTable clusterHeader = new ClusterHeaderTable();

        public CellGroup()
        {

        }
        public CellGroup(Point locatio, int id)
        {
            InitializeComponent();

            this.id = id;
            clusterLocMargin = locatio;
        }

        public void setPositionOnWindow()
        {
            //Setting the height and widths of each cluster and its container
            ell_clust.Height = clusterHeight;
            ell_clust.Width = clusterWidth;
            canv_cluster.Height = clusterHeight;
            canv_cluster.Width = clusterWidth;

            //Giving a margin for each cluster container
            Thickness clusterMargin = canv_cluster.Margin;
            clusterMargin.Top = this.clusterLocMargin.Y;
            clusterMargin.Left = this.clusterLocMargin.X;
            canv_cluster.Margin = clusterMargin;
            //Giving a margin for each cluster center (Margin inside the container)

        }

        public bool isNotEmpty()
        {
            if (this.getClusterNodes().Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int getID()
        {
            return this.id;
        }

        public List<Sensor> getClusterNodes()
        {
            return clusterNodes;
        }

        public bool isNear(Point p1)
        {
            double offset = PublicParameters.cellDiameter / 2;
            Point p2 = new Point(this.clusterLocMargin.X + offset, this.clusterLocMargin.Y + offset);
            double x = Operations.DistanceBetweenTwoPoints(p1, p2);
           
            if (x <= offset)
            {
                
                return true;
            }
            else
            {
                return false;
            }
        }



        //This function find the nearest sensor to the point needed 
        public void findNearestSensor(bool isReCheck)
        {
            double radius = PublicParameters.cellDiameter;
            List<Sensor> nearestSen = new List<Sensor>();
            bool clusterDouble = false;
            foreach (Sensor sensor in myNetwork)
            {
                Point p = new Point(sensor.CenterLocation.X, sensor.CenterLocation.Y);

                if (sensor.ID != PublicParameters.SinkNode.ID)
                {
                    if (isNear(p))
                    {
                        if (sensor.inCell != -1)
                        {
                            clusterDouble = true;
                            break;
                        }

                        nearestSen.Add(sensor);
                    }
                }
            }
            this.clusterNodes = nearestSen;
            if (nearestSen.Count > 0 && !isReCheck && !clusterDouble)
            {  
                PublicParameters.networkCells.Add(this);
            }

        }


        public void getNodesCenter()
        {
            double sensorX = 0;
            double sensorY = 0;
            //double halfRad = PublicParameters.cellDiameter / 2;
            double clusterX = this.clusterActualCenter.X;
            double clusterY = this.clusterActualCenter.Y;
            //this.clusterActualCenter = new Point(clusterX, clusterY);


            double sumX = 0;
            double sumY = 0;
            double n = clusterNodes.Count;

            foreach (Sensor sensor in this.clusterNodes)
            {
                sensorX += sensor.CenterLocation.X;
                sensorY += sensor.CenterLocation.Y;

            }


            sumX = (double)clusterX + (sensorX / n);
            sumY = (double)clusterY + (sensorY / n);

            sumX = sumX / 2;
            sumY = sumY / 2;

            //double marginTop = Math.Floor(clusterY - this.clusterLocMargin.Y) - label_clustercenter.Height/2;
            // double marginLeft =Math.Floor( clusterX - this.clusterLocMargin.X) - label_clustercenter.Width/2;


            clusterCenterMargin = new Point(sumX, sumY);
            CellCenter center = new CellCenter(clusterCenterMargin, this.getID());
            this.centerOfCluster = center;

            clusterCenterComputed = new Point(sumX, sumY);


        }

        public static double getAverageSensors()
        {
            double sum = 0;
            double clusterCount = PublicParameters.networkCells.Count();
            foreach (CellGroup cluster in PublicParameters.networkCells)
            {
                sum += cluster.clusterNodes.Count();
                Console.WriteLine("cluster {0}: location {1}", cluster.id, cluster.clusterReg);
            }
            Console.WriteLine("AVG {0}", (sum / clusterCount));
            return Math.Floor(sum / clusterCount);
        }


        public void incDecPos(int direction, double multiply)
        {
            double radius = PublicParameters.cellDiameter;
            double sRange = PublicParameters.SensingRangeRadius;
            double distance = (radius + (radius / 2));;
            Point moveTo = new Point(this.clusterLocMargin.X, this.clusterLocMargin.Y);
            switch (direction)
            {
                case 1:
                    moveTo.Y += distance * multiply;
                    break;
                case 2:
                    moveTo.X += distance * multiply;
                    break;                
            }
            this.clusterLocMargin = moveTo;

        }

        public static CellGroup getClusterWithID(int findID)
        {
            CellGroup getCluster = new CellGroup();
            foreach (CellGroup findCluster in PublicParameters.networkCells)
            {
                if (findCluster.getID() == findID)
                {
                    //Console.WriteLine("Searching in {0}", findCluster.getID());
                    getCluster = findCluster;
                    //Console.WriteLine("Returning");
                    return findCluster;
                }
            }
            return getCluster;
        }
        public void getClusterReg()
        {
            //int clusId = 1;
            double halfRad = PublicParameters.cellDiameter / 2;
            double clusterX = this.clusterLocMargin.X + halfRad;
            double clusterY = this.clusterLocMargin.Y + halfRad;
            this.clusterActualCenter = new Point(clusterX, clusterY);

            double x1, x2, y1, y2, cCenterX, cCenterY;
            foreach (LocalRegProp reg in PublicParameters.listOfRegs)
            {
                x1 = reg.BorderPoints[0].X;
                x2 = reg.BorderPoints[3].X;
                y1 = reg.BorderPoints[0].Y;
                y2 = reg.BorderPoints[3].Y;
                cCenterX = this.clusterActualCenter.X;
                cCenterY = this.clusterActualCenter.Y;
                if (x1 <= cCenterX && cCenterX < x2 && y1 <= cCenterY && cCenterY <= y2)
                {
                    this.clusterReg = reg.Id;
                    //return clusId;
                }
            }
            //return clusId;
        }
        /// <summary>
        /// Used to set or change the cluster head
        /// </summary>
        /// <param name="isRechange">isReachange = false if it's used to set for the first time</param>
        



    }





}
