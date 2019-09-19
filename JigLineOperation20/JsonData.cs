using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigLineOperation20
{
   public class JsonData
    {
    }

    public class Point
    {
        public double X { get; set; }

        public double Y { get; set; }
        
    }

    public class Size
    {

        public double W { get; set; }

        public double H { get; set; }

    }

    public class Wall
    {
        public string Name { get; set; }

        public Point Pos { get; set; }

        public Point Dir { get; set; }

        public Size Size { get; set; }

        public string Hatch { get; set; }

        public string[] Tags { get; set; }
    }
}
