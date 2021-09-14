using TBDD.Dataplane;
using TBDD.Dataplane.PacketRouter;
using TBDD.Intilization;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TBDD.Region
{
    /// <summary>
    /// Interaction logic for Region.xaml
    /// </summary>
    public partial class Region : UserControl
    {
        public Region()
        {
            InitializeComponent();
        }

        //public static List<Sensor> bordernodes = new List<Sensor>();
        private double smallx { get; set; }
        private double smally { get; set; }
        private double bigx { get; set; }
        private double bigy { get; set; }

        private void BuildingRegion(Canvas Canvas_SensingFeild)
        {
            
        }

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
                    bigx = sen.Position.X;
                }
                if (sen.Position.X < smallestX.Position.X)
                {
                    smallestX = sen;
                    smallx = sen.Position.X;
                }
                if (sen.Position.Y > biggestY.Position.Y)
                {
                    biggestY = sen;
                    bigy = sen.Position.Y;
                }
                if (sen.Position.Y < biggestX.Position.Y)
                {
                    smallestY = sen;
                    smally = sen.Position.Y;
                }
            }

        }

        private void drawVirtualLine()
        {

            Line border = new Line();
            Canvas SensingField = new Canvas();

            border.Fill = Brushes.Red;
            border.Stroke = Brushes.Red;
            border.X1 = bigx / 2;
            border.Y1 = smally;
            border.X2 = bigx / 2;
            border.Y2 = bigy;
            SensingField.Children.Add(border);
        }
    }
}
