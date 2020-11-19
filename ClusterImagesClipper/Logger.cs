using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterImagesClipper
{
    public static class Logger
    {
        public static void SimpleDebug(string debugText)
        {
            if (Config.ConsoleOutput)
                Debug.WriteLine(debugText);
        }
    }
}
