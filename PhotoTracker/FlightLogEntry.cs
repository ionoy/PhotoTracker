namespace PhotoTracker
{
    public class FlightLogEntry
    {
        public int TimeMS { get; set; }

        public float Pitch { get; set; }
        public float Roll { get; set; }
        public float Yaw { get; set; }

        public double Lat { get; set; }
        public double Lon { get; set; }
        public float Alt { get; set; }

        public FlightLogEntry Clone()
        {
            return new FlightLogEntry {
                TimeMS = TimeMS,
                Pitch = Pitch,
                Roll = Roll,
                Yaw = Yaw,
                Lat = Lat,
                Lon = Lon,
                Alt = Alt,
            };
        }
    }
}