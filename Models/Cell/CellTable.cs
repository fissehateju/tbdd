using System.Windows;
using TBDD.Dataplane;

namespace TBDD.Models.Cell
{
    public class CellTable
    {
        public Sensor CellHeader { get; set; }

        public Point HeaderLocation { get; set; }

        public bool isRootCell = false;       

        public CellTable()
        {

        }
        public CellTable(Sensor header, Point x)
        {
            CellHeader = header;
            HeaderLocation = x;

        }

    }
}
