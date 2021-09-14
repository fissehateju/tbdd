using TBDD.ui;
using System.Windows;

namespace TBDD.db
{
    /// <summary>
    /// Interaction logic for UiImportTopology.xaml
    /// </summary>
    public partial class UiImportTopology : Window
    {

        public UiImportTopology(MainWindow MainWindow)
        {
            InitializeComponent();

           foreach( NetwokImport net in NetworkTopolgy.ImportNetworkNames(this))
           {
                net.MainWindow = MainWindow;
                stk_netwoks.Children.Add(net);  // stk_network is name of UiImportTopology.xaml frame 
           }
        }
    }
}
