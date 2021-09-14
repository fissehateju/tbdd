using TBDD.Dataplane;
using TBDD.Dataplane.NOS;
using TBDD.Dataplane.PacketRouter;
using TBDD.Intilization;
using TBDD.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TBDD.Constructor;
using TBDD.NetAnimator;

namespace TBDD.Region.Routing
{

    class ObtainActiveRegInfo
    {
        private NetworkOverheadCounter counter;
        /// <summary>
        /// obtian the position for all sinks.
        /// </summary>
        /// <param name="sensor"></param>
        /// 
        public ObtainActiveRegInfo(Sensor sensor)
        {
            counter = new NetworkOverheadCounter();

        }
    }
}
