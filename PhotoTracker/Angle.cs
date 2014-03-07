using System;

namespace PhotoTracker
{
    public static class Angle
    {
        public static float ToRadian(float degrees)
        {
            return degrees * (float)Math.PI / 180.0f;
        }
    }
}