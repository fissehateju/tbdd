using TBDD.Dataplane;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TBDD.Models.Cell;
using TBDD.Region;
using TBDD.ui;
namespace TBDD.Constructor
{
    class NetworkConstruction
    {
        public NetworkConstruction(Canvas Canvase_SensingFeild)
        {
                buildFromZeroZero(Canvase_SensingFeild);
        }
        private int assignID { get; set; }

        private void addClustersToWindow(Canvas Canvas_SensingFeild)
        {

            Console.WriteLine("\nTotal Number for clusters {0}", PublicParameters.networkCells.Count());
            foreach (CellGroup cluster in PublicParameters.networkCells)
            {
                //   Console.WriteLine("Drawing cluster {0}", cluster.getID());
                cluster.getNodesCenter();
                cluster.setPositionOnWindow();
                Canvas_SensingFeild.Children.Add(cluster);
                Canvas_SensingFeild.Children.Add(cluster.centerOfCluster);

            }
        }
        

        //Assign cluster IDs here

        private void addIdsToSensorA(CellGroup cluster)
        {

            foreach (Sensor sensor in cluster.getClusterNodes())
            {
                sensor.inCell = cluster.getID();
                //Console.WriteLine("Sensor {0} is in {1}", sensor.ID, cluster.getID());
            }

        }
        private static void addIdsToSensorFinal()
        {
            foreach (Sensor sen in PublicParameters.myNetwork)
            {
                sen.inCell = -1;

            }
            foreach (CellGroup cluster in PublicParameters.networkCells)
            { 
                foreach (Sensor sensor in cluster.getClusterNodes())
                {

                    sensor.inCell = cluster.getID();
                    sensor.TBDDNodeTable.isEncapsulated = true;
                    sensor.TBDDNodeTable.myCellHeader = cluster.CellTable.CellHeader;
                    sensor.TBDDNodeTable.CellNumber = cluster.getID();

                }
            }
            CellFunctions.FillOutsideSensnors();
        }

        public static void populateClusterTables()
        {
            foreach (CellGroup cluster in PublicParameters.networkCells)
            {

                foreach (Sensor sensor in cluster.getClusterNodes())
                {
                    sensor.inCell = cluster.getID();
                    sensor.TBDDNodeTable.isEncapsulated = true;
                    sensor.TBDDNodeTable.myCellHeader = cluster.CellTable.CellHeader; ;

                }
            }
            CellFunctions.FillOutsideSensnors();
        }

        public static List<Sensor> subnet { get; set; }
        private void buildFromZeroZero(Canvas Canvase_SensingFeild)
        {
            double canvasHeight = Canvase_SensingFeild.ActualHeight;
            double canvasWidth = Canvase_SensingFeild.ActualWidth;

            double CRange = PublicParameters.cellDiameter / 4;

            Point startFrom = new Point(PublicParameters.mostleft + CRange, PublicParameters.mosttop + CRange); 
            double radius = PublicParameters.cellDiameter;
            double offset = (radius + (radius / 2));
            double xAxesCount = Math.Floor(canvasWidth / offset);
            double yAxesCount = Math.Floor(canvasHeight / offset);

            assignID = 1;

            for (int rightCount = 0; rightCount <= xAxesCount; rightCount++)
            {
                if(PublicParameters.mostright - startFrom.X < PublicParameters.cellDiameter)
                {
                    break;
                }
                buildFromDirection(startFrom, yAxesCount);
                startFrom.X += offset;

            }
            addIdsToSensorFinal();

            _ = new LocalRegion(Canvase_SensingFeild,  "RegInfoUpdating");
            //PrintingOutput.printing();

            addClustersToWindow(Canvase_SensingFeild);

            _ = new Tree(Canvase_SensingFeild);        
            initAssignHead();
            populateClusterTables();

            PublicParameters.SinkNode.sReg = CellGroup.getClusterWithID(Tree.rootClusterID).clusterReg;
            _ = new ActiveRegBeacon(CellGroup.getClusterWithID(Tree.rootClusterID).clusterReg);
        }

        private void initAssignHead()
        {
            foreach (CellGroup cluster in PublicParameters.networkCells)
            {
                CellFunctions.assignClusterHead(cluster);
                
            }
        }


        private void buildFromDirection(Point startFrom, double ycount)
        {

            for (int i = 0; i <= ycount; i++)
            {
                double rr = PublicParameters.cellDiameter;
                double y = (rr + (rr / 2));
                if (PublicParameters.mostbottom - (y * i + startFrom.Y) < rr)
                {
                    break;
                }

                CellGroup cluster = new CellGroup(startFrom, assignID);
                cluster.incDecPos(1, i);
                cluster.getClusterReg();
                cluster.findNearestSensor(true);
                if (cluster.isNotEmpty())
                {
                    cluster.findNearestSensor(false);
                    addIdsToSensorA(cluster);
                    assignID++;

                }
            }
        }


        public static void sendTrial(int count)
        {           
         
        }
    }
}
