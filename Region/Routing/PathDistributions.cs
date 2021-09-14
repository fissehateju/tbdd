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
using Point = System.Windows.Point;

namespace TBDD.Region.Routing
{
    class PathDistributions
    {

        public static double EnergyDistribution(double CurentEn, double intialEnergy)
        {
            if (CurentEn > 0)
            {
                double σ = CurentEn / intialEnergy;
                double γ = 1.0069, ε = 0.70848, ϑ = 17.80843;
                double re = γ / (1 + Math.Exp(-ϑ * (σ - ε)));
                return re;
            }
            else
                return 0;
        }

    }


    class LoopMechanizimAvoidance
    {
        private Packet _pck;
        public LoopMechanizimAvoidance(Packet pck)
        {
            _pck = pck;
        }


        /// <summary>
        /// return true if loop is discovred.
        /// </summary>
        public bool isLoop
        {
            get
            {


                string[] spliter = _pck.Path.Split('>');
                if (spliter.Length >= 4)
                {
                    string last1 = spliter[spliter.Length - 1];
                    string last2 = spliter[spliter.Length - 2];
                    string last3 = spliter[spliter.Length - 3];
                    string last4 = spliter[spliter.Length - 4];

                    if (last1 == last3 && last4 == last2)
                    {
                        Console.WriteLine("%%%>Packet:" + _pck.PID + " [ " + _pck.Path + " ] entered a loop.");
                        return true;
                    }

                }

                return false;
            }
        }


    }

}
