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

namespace TBDD.Constructor
{
    /// <summary>
    /// Local partition / region object with its main properties
    /// </summary>

    public partial class LocalRegProp
    {
        public int Id { get; set; }
        public List<Point> BorderPoints = new List<Point>();
        public List<Sensor> MemberNodes { get; set; }
        public int numberOfNodes { 
            get 
            {
                if (this.MemberNodes != null) { return this.MemberNodes.Count; }
                else { return 0; }
            }
        }
        public List<CellGroup> Cells
        {
            get
            {
                List<CellGroup> C = new List<CellGroup>(); ;
                foreach (CellGroup clst in PublicParameters.networkCells)
                {
                    if (clst.clusterReg == this.Id)
                        C.Add(clst);
                }
                return C;
            }
        }
        public Point localCenteriod
        {
            get
            {
                if (this.MemberNodes != null)
                {
                    double memLenth = this.MemberNodes.Count;
                    double xsum = 0, ysum = 0;
                    foreach (Sensor mem in this.MemberNodes)
                    {
                        xsum += mem.CenterLocation.X;
                        ysum += mem.CenterLocation.Y;
                    }

                    return new Point(xsum / memLenth, ysum / memLenth);
                }
                else { return new Point(0,0); }
            }
        }
        public Sensor centerNode
        {
            get
            {
                if (this.MemberNodes != null)
                {
                    Sensor sen = this.MemberNodes[0];
                    double initial = Operations.DistanceBetweenTwoPoints(sen.CenterLocation, this.localCenteriod);
                    foreach (Sensor mem in this.MemberNodes)
                    {
                        double memdis = Operations.DistanceBetweenTwoPoints(mem.CenterLocation, this.localCenteriod);
                        if (memdis < initial)
                        {
                            initial = memdis;
                            sen = mem;
                        }
                    }
                    return sen;
                }
                else { return null; }
            }
        }
    }
}
