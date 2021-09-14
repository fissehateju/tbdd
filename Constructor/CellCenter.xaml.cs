using System;
using System.Windows;
using System.Windows.Controls;

namespace TBDD.Constructor
{
    /// <summary>
    /// Interaction logic for ClusterCenter.xaml
    /// </summary>
    public partial class CellCenter : UserControl
    {
        public CellCenter(Point p, int clusterID)
        {

            InitializeComponent();
            setPosition(p);
            //center_label_text.Text = "";
            center_label_text.Text = clusterID.ToString();
        }
        public void setPosition(Point p)
        {
            try
            {
                double height = center_label.Height / 2;
                double width = center_label.Width / 2;
                Thickness margin = center_label.Margin;
                margin.Top = p.Y - height;
                margin.Left = p.X - width;
                center_label.Margin = margin;



            }
            catch
            {
                Console.WriteLine("adding the center has failed");
            }
        }

    }
}
