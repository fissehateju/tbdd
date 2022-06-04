using System.Windows;
using System.Windows.Controls;

namespace TBDD.ui.conts
{
    /// <summary>
    /// Interaction logic for ListControl.xaml
    /// </summary>
    public partial class ListControl : UserControl
    {
        public ListControl()
        {
            InitializeComponent();

            dg_date.MaxHeight = SystemParameters.FullPrimaryScreenHeight-10;
        }
    }
}
