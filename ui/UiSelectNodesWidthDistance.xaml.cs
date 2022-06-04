﻿using TBDD.Dataplane;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TBDD.Intilization;
using TBDD.Forwarding;

namespace TBDD.ui
{
    /// <summary>
    /// Interaction logic for UiSelectNodesWidthDistance.xaml
    /// </summary>
    public partial class UiSelectNodesWidthDistance : Window
    {
        MainWindow mianWind;
        public UiSelectNodesWidthDistance(MainWindow _mianWind)
        {
            InitializeComponent();
            mianWind = _mianWind;

            for (int i = 50; i <= 250; i++)
            {
                com_distance.Items.Add(i);
            }

            for (int i = 1; i < mianWind.myNetWork.Count; i++)
            {
                com_nos.Items.Add(new ComboBoxItem() { Content = i.ToString() });
                com_nop.Items.Add(new ComboBoxItem() { Content = i.ToString() });
            }
        }


        private List<Sensor> SelectNodesWithinDistance(double distance)
        {
            List<Sensor> NodesWidDistance = new List<Sensor>();
            if (distance > (PublicParameters.SensingRangeRadius * 1.2))
            {
                double maxDi = distance + (PublicParameters.SensingRangeRadius / 2);
                double MinDi = distance - (PublicParameters.SensingRangeRadius / 2);

                foreach (Sensor s in mianWind.myNetWork)
                {
                    double senDis = Operations.DistanceBetweenTwoSensors(PublicParameters.SinkNode, s);
                    if (senDis >= MinDi && senDis <= maxDi)
                    {
                        NodesWidDistance.Add(s);
                    }
                }
                return NodesWidDistance;
            }
            else
            {

                return null;
            }
        }

        private void btn_compute_Click(object sender, RoutedEventArgs e)
        {
            PublicParameters.FinishedRoutedPackets.Clear();
           
            int NOS = Convert.ToInt16(com_nos.Text);
            int NOP = Convert.ToInt16(com_nop.Text);
            double dist = Convert.ToDouble(com_distance.Text);

            List<Sensor> SelectNodesWithinDistanceN = SelectNodesWithinDistance(dist);


            if (SelectNodesWithinDistanceN != null)
            {
                // selecte The Nodes:
                List<Sensor> SelectedSn = new List<Sensor>(NOS);
                for (int i = 0; i < NOS; i++)
                {
                    int ran = Convert.ToInt16(UnformRandomNumberGenerator.GetUniform(SelectNodesWithinDistanceN.Count - 1));
                    if (ran > 0)
                    {
                        SelectedSn.Add(SelectNodesWithinDistanceN[ran]);
                    }
                    else
                    {
                        SelectedSn.Add(SelectNodesWithinDistanceN[1]);
                    }
                }

                foreach (Sensor sen in SelectedSn)
                {

                    sen.GenerateMultipleDataPackets(NOP);
                }
            
            }
        }
    }
}






            
    

