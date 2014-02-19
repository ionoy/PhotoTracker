using System.IO;

namespace PhotoTracker
{
    class PhotoObject
    {
        public FileInfo Photo { get; set; }

        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Alt { get; set; }

        public double Pitch { get; set; }
        public double Roll { get; set; }
        public double Yaw { get; set; }

        public bool IsActive { get; set; }

        public PhotoObject()
        {
            IsActive = true;
        }
    }
}