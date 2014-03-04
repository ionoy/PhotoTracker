using System;

namespace PhotoTracker
{
    public class Angle
    {
        public static double ToRadian(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}