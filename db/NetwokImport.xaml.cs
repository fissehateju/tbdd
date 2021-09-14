using TBDD.Dataplane;
using TBDD.Properties;
using TBDD.ui;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace TBDD.db
{
    /// <summary>
    /// Interaction logic for NetwokImport.xaml
    /// </summary>
    public partial class NetwokImport : UserControl
    {
        public MainWindow MainWindow { set; get; }
        public List<ImportedSensor> ImportedSensorSensors = new List<ImportedSensor>();

        public UiImportTopology UiImportTopology { get; set; }
        public NetwokImport()
        {
            InitializeComponent();
        }

        private void brn_import_Click(object sender, RoutedEventArgs e)   // load the network on the sensing field
        {
            NetworkTopolgy.ImportNetwok(this);
            PublicParameters.NetworkName = lbl_network_name.Content.ToString();
            PublicParameters.SensingRangeRadius = ImportedSensorSensors[0].R;
            // now add them to feild.
            string[] arr = PublicParameters.NetworkName.Split('_');

            PublicParameters.MainWindow.Canvas_SensingFeild.Width = System.Convert.ToDouble(arr[1]);
            MainWindow.Canvas_SensingFeild.Width = System.Convert.ToDouble(arr[1]);

            PublicParameters.MainWindow.Canvas_SensingFeild.Height = System.Convert.ToDouble(arr[2]);
            MainWindow.Canvas_SensingFeild.Height = System.Convert.ToDouble(arr[2]);


            foreach (ImportedSensor imsensor in ImportedSensorSensors)
            {
                Sensor node = new Sensor(imsensor.NodeID);
                node.MainWindow = MainWindow;
                Point p = new Point(imsensor.Pox, imsensor.Poy);
                node.Position = p;
                node.VisualizedRadius = imsensor.R;
                MainWindow.myNetWork.Add(node);
                MainWindow.Canvas_SensingFeild.Children.Add(node);

                node.ShowSensingRange(Settings.Default.ShowSensingRange);
                node.ShowComunicationRange(Settings.Default.ShowComunicationRange);
                node.ShowBattery(Settings.Default.ShowBattry);
            }
           

            try
            {
                UiImportTopology.Close();
            }
            catch
            {

            }
            

           

        }
    }
}
