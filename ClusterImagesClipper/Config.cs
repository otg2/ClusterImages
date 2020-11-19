using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterImagesClipper
{
    public static class Config
    {
        public static string Prefix = "parts";
        public static int RoughCalculate = 20000;
        public static bool DrawLinesOnInterface = false;
        public static bool ConsoleOutput = false;


        public static string ExtensionType = "png";
        public static string SourceFolder = "images";
        public static string TargetFolder = "preprocessed";
        public static string ExpandedTargetFolder = "expanded";


        public static int BoundarBoxAccuracy = 8;
        public static int MaxRotation = 10;


        public static double IntersectionErrorModifier = 0.5;
        public static double BoundaryErrorModifier = 2;
        public static double ClusteringErrorModifier = 2;
    }
}
