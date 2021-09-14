using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBDD.Dataplane.NOS;
using TBDD.Intilization;

namespace TBDD.Region.Routing
{
    class LoopChecker
    {
        private Packet _pck;
        public LoopChecker(Packet pck)
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
                        //Console.WriteLine("%%%>Packet:" + _pck.PID + " [ " + _pck.Path + " ] entered a loop.");
                        return true;
                    }
                }             

                return false;
            }
        }

        public bool isLongLoop
        {
            get
            {
                int count = 0;
                List<int> path = Operations.PacketPathToIDS(_pck.Path);

                foreach (int id in path)
                {
                    foreach (int idd in path)
                    {
                        if (id == idd)
                        {
                            count += 1;
                        }
                    }
                    if (count > 2 && !isLoop)
                    {
                        return true;
                    }
                    count = 0;
                }

                return false;
            }
        }
    }
}
