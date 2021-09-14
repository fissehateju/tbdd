using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using TBDD.Dataplane;
using TBDD.Dataplane.NOS;

namespace TBDD.Models.Cell
{
    public class CellHeader
    {
        //Cell Header Main Variables
        public bool isHeader = false;
        public Point ParentCellCenter { get; set; }        
        public bool hasSinkPosition = false;
        public Sensor SinkAgent { get; set; }
        public int atTreeDepth { get; set; }
        public double DistanceFromRoot { get { return PublicParameters.cellDiameter * atTreeDepth; } }
        public bool isRootHeader = false;
        public int activeRegID { get; set; }
        private Sensor me { get; set; }
        public bool isNewHeaderAvail = false;
        //private DispatcherTimer OldHeaderTimer;

        //Cell Header Buffer
        public Queue<Packet> CellHeaderBuffer = new Queue<Packet>();
        public void StoreInCellHeaderBuffer(Packet packet)
        {
            CellHeaderBuffer.Enqueue(packet);
        }

        public void DidChangeHeader(Sensor m)
        {
            isNewHeaderAvail = true;
           // OldHeaderTimer = new DispatcherTimer();
           // OldHeaderTimer.Interval = TimeSpan.FromSeconds(3);
          //  OldHeaderTimer.Start();
            me = m;
           // OldHeaderTimer.Tick += OldHeaderTimer_Tick;
            hasSinkPosition = false;
            if (CellHeaderBuffer.Count > 0)
            {
                me.ReRoutePacketsInCellHeaderBuffer();
            }
            ClearData();
            
        }
       private void ClearData(){
             isHeader = false;     
             hasSinkPosition = false;
             SinkAgent =null;
             isRootHeader = false;
        }
        



    }
}
