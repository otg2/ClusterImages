using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterImagesClipper
{
    public static class Config
    {
        public static string Prefix = "";
        public static int NumberOfRuns = 1;
        public static bool DrawLinesOnInterface = true;
        public static bool ConsoleOutput = true;

        public static bool ScaleImages = true;
        public static bool ScaleUseFactoring = false;
        public static int ScaleFactor = 4;
        // TODO: Add scaletarget width to the averagewidth of lowest width?
        public static int ScaleTargetWidth = 300;

        public static string ExtensionType = "png";
        public static string SourceFolder = "images";
        public static string TargetFolder = "preprocessed";
        public static string ExpandedTargetFolder = "expanded";


        public static int BoundarBoxAccuracy = 8;
        public static int MaxRotation = 20;


        public static double IntersectionErrorModifier = 2.5;
        public static double BoundaryErrorModifier = 0.5;
        public static double ClusteringErrorModifier = 2; // TODO: Implement error given too much space between objects
    }
}
