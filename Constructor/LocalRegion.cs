using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBDD.Dataplane;
using System.Windows;
using System.Windows.Controls;
using TBDD.Models.Cell;
using TBDD.Dataplane.PacketRouter;
using TBDD.Intilization;
using System.Windows.Media;
using System.Windows.Shapes;
using TBDD.ui;

namespace TBDD.Constructor
{
 
    public partial class LocalRegion
    {
        private List<double> xy = new List<double>();      
        private Canvas SensingField;
        private double canvasHeight { get; set; }
        private double canvasWidth { get; set; }
        private double CellRadius { get; set; }
        private double minRegSize { get; set; }

        private Point TopLeft = new Point();
        private Point TopRight = new Point();
        private Point BottomLeft = new Point();
        private Point BottomRight = new Point();
        private static List<Line> RegBorders = new List<Line>();

        public LocalRegion()
        {

        }
        public LocalRegion(Canvas Sfield, string toUpdate)
        {
            SensingField = Sfield;
            partitionUpdate();
            drawNewBorder();
            CellNodeLinkage();
        }
        public LocalRegion(Canvas Senfield, double diameter)
        {
            SensingField = Senfield;
            canvasHeight = SensingField.ActualHeight;
            canvasWidth = SensingField.ActualWidth;
            CellRadius = diameter;
            minRegSize = CellRadius * 3;

            getBorderNodes();
            Partitioning_Network_area();
        }


        //private List<Sensor> centerNodes;
        private Queue<Sensor> nextSensor;
        private Stack<Sensor> ordering;
        private static Line lineBetweenTwo;
        private void CellNodeLinkage()
        {
            foreach (CellGroup cell in PublicParameters.networkCells)
            {
                //centerNodes = new List<Sensor>();
                //RingNodes = new List<Sensor>();
                ordering = new Stack<Sensor>();
                nextSensor = new Queue<Sensor>();

                rotationPath(cell, cell.clusterNodes);

                foreach (Sensor s in cell.clusterNodes)
                {
                    lineBetweenTwo = new Line();
                    lineBetweenTwo.Fill = Brushes.Black;
                    lineBetweenTwo.Stroke = Brushes.Black;
                    lineBetweenTwo.X1 = s.CenterLocation.X;
                    lineBetweenTwo.Y1 = s.CenterLocation.Y;
                    lineBetweenTwo.X2 = s.ringFollower.CenterLocation.X;
                    lineBetweenTwo.Y2 = s.ringFollower.CenterLocation.Y;
                    SensingField.Children.Add(lineBetweenTwo);
                }

            }
        }

        private void rotationPath(CellGroup reg, List<Sensor> ringNs)
        {
            nextSensor.Enqueue(ringNs[0]);
            ringNs[0].isRingnode = true;
            Sensor me = null, nextRn = null;
            bool finish = false;

            while (!finish)
            {
                if (nextSensor.Count > 0) me = nextSensor.Dequeue();

                nextRn = myfollower(reg, ringNs, me);
                if (nextRn != null)
                {
                    nextRn.isRingnode = true;
                    me.ringFollower = nextRn;
                    nextSensor.Enqueue(nextRn);

                    ordering.Push(me);
                    nextRn.atteptList.Clear();
                    //if (exceptList.Contains(me)) exceptList.Remove(me);
                }
                else if (ordering.Count == ringNs.Count)
                {
                    finish = true;
                }
                else if (ordering.Count == ringNs.Count - 1)
                {
                    me.ringFollower = ringNs[0];
                    ordering.Push(me);
                    finish = true;
                }
                else 
                {
                    me.isRingnode = false;

                    Sensor goback = ordering.Pop();
                    goback.atteptList.Add(me);
                    nextSensor.Enqueue(goback);
                }

                if (ordering.Count >= ringNs.Count - 1) ringNs[0].isRingnode = false;
            }
            ringNs[0].isRingnode = true;
        }
        private Sensor myfollower(CellGroup reg, List<Sensor> rNodes, Sensor me)
        {
            Sensor ss = null;
            double min = 100;
            foreach (Sensor rnN in me.myNeighborsTable)
            {
                if (rnN.ID != me.ID && rNodes.Contains(rnN) && !me.atteptList.Contains(rnN))
                {
                    double D_ = Operations.DistanceBetweenTwoPoints(me.CenterLocation, rnN.CenterLocation);
                    if (D_ <= min && !rnN.isRingnode)
                    {
                        min = D_;
                        ss = rnN;
                    }
                }
            }
            return ss;
        }

        private List<Sensor> Collectmembers(int id, Point TLeft, Point TRight, Point BLeft, Point BRight)
        {
            List<Sensor> members = new List<Sensor>();
            List<Sensor> AllSen = PublicParameters.myNetwork;
            foreach (Sensor sen in AllSen)
            {
                if (sen.CenterLocation.X > TLeft.X && sen.CenterLocation.X <= TRight.X && sen.CenterLocation.Y > TLeft.Y && sen.CenterLocation.Y <= BLeft.Y)
                {
                    sen.sReg = id;
                    members.Add(sen);
                }
            }
            return members;
        }
        private void Partitioning_Network_area()
        {
            double numOfReghorizon = canvasWidth / minRegSize;
            double numOfRegvert = canvasHeight / minRegSize;

            LocalRegProp part;
            int id = 1;
            for (int i = 0; i < Math.Round(numOfReghorizon); i++)
            {
                for (int j = 0; j < Math.Round(numOfRegvert); j++)
                {
                    part = new LocalRegProp();
                    TopLeft = new Point(i * (canvasWidth / numOfReghorizon), j * (canvasHeight / numOfRegvert));
                    TopRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), j * (canvasHeight / numOfRegvert));
                    BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), (j + 1) * (canvasHeight / numOfRegvert));
                    BottomRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), (j + 1) * (canvasHeight / numOfRegvert));                   

                    if (i == Math.Round(numOfReghorizon) - 1)
                    {
                        TopRight = new Point(PublicParameters.mostright + 10, j * (canvasHeight / numOfRegvert));
                        BottomRight = new Point(PublicParameters.mostright + 10, (j + 1) * (canvasHeight / numOfRegvert));
                    }
                    if (j == Math.Round(numOfRegvert) - 1)
                    {
                        BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), PublicParameters.mostbottom + 10);
                        BottomRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), PublicParameters.mostbottom + 10);
                    }
                    if (i == Math.Round(numOfReghorizon) - 1 && j == Math.Round(numOfRegvert) - 1)
                    {
                        TopRight = new Point(PublicParameters.mostright + 10, j * (canvasHeight / numOfRegvert));
                        BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), PublicParameters.mostbottom + 10);
                        BottomRight = new Point(PublicParameters.mostright + 10, PublicParameters.mostbottom + 10);
                    }

                    part.BorderPoints.Add(TopLeft); part.BorderPoints.Add(TopRight); part.BorderPoints.Add(BottomLeft); part.BorderPoints.Add(BottomRight);
                    part.Id = id;
                    id += 1;

                    PublicParameters.listOfRegs.Add(part);
                }
            }

        }

        private void getBorderNodes()
        {
            double smallx = PublicParameters.SinkNode.CenterLocation.X;
            double smally = PublicParameters.SinkNode.CenterLocation.Y;
            double bigx = PublicParameters.SinkNode.CenterLocation.X;
            double bigy = PublicParameters.SinkNode.CenterLocation.Y;
            foreach (Sensor sen in PublicParameters.myNetwork)
            {
                if (sen.ID != PublicParameters.SinkNode.ID)
                {
                    if (sen.CenterLocation.X >= bigx)
                    {
                        bigx = sen.CenterLocation.X;
                    }
                    else if (sen.CenterLocation.X <= smallx)
                    {
                        smallx = sen.CenterLocation.X;
                    }
                    if (sen.CenterLocation.Y >= bigy)
                    {
                        bigy = sen.CenterLocation.Y;
                    }
                    else if (sen.CenterLocation.Y <= smally)
                    {
                        smally = sen.CenterLocation.Y;
                    }
                }
            }

            xy.Add(smallx); xy.Add(smally); xy.Add(bigx); xy.Add(bigy);  // was + 70 was bigs

            PublicParameters.SmallAndBigxy = xy;

            PublicParameters.mostleft = smallx;
            PublicParameters.mostright = bigx;
            PublicParameters.mosttop = smally;
            PublicParameters.mostbottom = bigy;
            
        }
        public static void partitionUpdate()
        {

            foreach (Sensor sen in PublicParameters.myNetwork)
            {
                if (sen.inCell == -1)
                { 
                    sen.sReg = CellGroup.getClusterWithID(sen.TBDDNodeTable.NearestCellId).clusterReg;
                }
                else
                {
                    sen.sReg = CellGroup.getClusterWithID(sen.TBDDNodeTable.CellNumber).clusterReg;
                }
            }

            List<Sensor> localsen;
            for (int RegId = 1; RegId <= PublicParameters.listOfRegs.Count(); RegId++)
            {
                localsen = new List<Sensor>();
                foreach (Sensor sen in PublicParameters.myNetwork)
                {
                    if (sen.sReg == RegId) 
                    { 
                        localsen.Add(sen); 
                    }
                }
                PublicParameters.listOfRegs[RegId - 1].MemberNodes = localsen;
                borderPointUpdate(localsen, RegId);
            }            
        }
        private static void borderPointUpdate(List<Sensor> regsensors, int RegId)
        {
            double x1 = double.MaxValue, x2 = double.MinValue, y1 = double.MaxValue, y2 = double.MinValue;
            foreach (Sensor sen in regsensors)
            {  
                if (sen.CenterLocation.X < x1)
                {
                    x1 = sen.CenterLocation.X;
                }
                if (sen.CenterLocation.X > x2)
                {
                    x2 = sen.CenterLocation.X;
                }
                if (sen.CenterLocation.Y < y1)
                {
                    y1 = sen.CenterLocation.Y;
                }
                if (sen.CenterLocation.Y > y2)
                {
                    y2 = sen.CenterLocation.Y;
                }
                
            }
            PublicParameters.listOfRegs[RegId-1].BorderPoints.Clear();
            PublicParameters.listOfRegs[RegId-1].BorderPoints.Add(new Point(x1, y1));
            PublicParameters.listOfRegs[RegId-1].BorderPoints.Add(new Point(x2, y1));
            PublicParameters.listOfRegs[RegId-1].BorderPoints.Add(new Point(x1, y2));
            PublicParameters.listOfRegs[RegId-1].BorderPoints.Add(new Point(x2, y2));
        }

        private Line linee;
        private void drawNewBorder()
        {

            int n = PublicParameters.listOfRegs.Count();
            int ctr = n % 3;
            int bWeight;
            foreach (LocalRegProp regin in PublicParameters.listOfRegs)
            {
                if (regin.Id > 4)
                {
                    bWeight = regin.Id % 4 + 1;
                }               
                else 
                { 
                    bWeight = regin.Id; 
                }
              
                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.BorderPoints[0].X;
                linee.Y1 = regin.BorderPoints[0].Y;
                linee.X2 = regin.BorderPoints[1].X;
                linee.Y2 = regin.BorderPoints[1].Y;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.BorderPoints[0].X;
                linee.Y1 = regin.BorderPoints[0].Y;
                linee.X2 = regin.BorderPoints[2].X;
                linee.Y2 = regin.BorderPoints[2].Y;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.BorderPoints[1].X;
                linee.Y1 = regin.BorderPoints[1].Y;
                linee.X2 = regin.BorderPoints[3].X;
                linee.Y2 = regin.BorderPoints[3].Y;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.BorderPoints[2].X;
                linee.Y1 = regin.BorderPoints[2].Y;
                linee.X2 = regin.BorderPoints[3].X;
                linee.Y2 = regin.BorderPoints[3].Y;
                SensingField.Children.Add(linee);
            }
        }
    }

}
