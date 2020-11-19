using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterImagesClipper.Objects
{
    public enum ShapeDrawingOrder
    {
        None = 0,
        LeftToRight = 1,
        RightToLeft = 2,
        DownToUp = 3,
        UpToDown = 4,
        Custom = 5 // Implement this somehow, like center/outwards, rgb values, sizes, etc
    }
}
