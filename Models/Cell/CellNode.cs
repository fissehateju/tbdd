using System.Windows;
using TBDD.Dataplane;

namespace TBDD.Models.Cell
{
    public class CellNode
    {
        public bool isEncapsulated = false;
        public int CellNumber { get; set; }
        public Point CellCenter { get; set; }
        public Sensor myCellHeader { get; set; }
        public Sensor SinkAgent { get; set; }
        public bool rootCellNodes { get; set; }

        public CellHeader CellHeaderTable = new CellHeader();


        //Regular Nodes
        public Point NearestCellCenter { get; set; }
        public int NearestCellId { get; set; }

    }
}
