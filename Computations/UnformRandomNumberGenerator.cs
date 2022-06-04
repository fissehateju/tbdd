using System;
using TBDD.Intilization;
using System.Threading;

namespace TBDD.Forwarding
{
    /// <summary>
    /// generate anumber between 0- max:
    /// </summary>
    public static class UnformRandomNumberGenerator
    {
        public static double GetUniform(double max) 
        {
            return max * RandomeNumberGenerator.GetUniform();
        }

        public static double GetUniformSleepSec(double max)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            return max * RandomeNumberGenerator.GetUniform();
        }
    }
}
