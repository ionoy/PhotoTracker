using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhotoTracker
{
    public class FlightLog
    {
        public List<FlightLogEntry> Entries { get; private set; }
        public List<int> CameraTriggerTimes { get; private set; }

        private FlightLog(List<FlightLogEntry> entries, List<int> cameraTriggerTimes)
        {
            CameraTriggerTimes = cameraTriggerTimes;
            Entries = entries;
        }

        public static FlightLog FromArduLog(string path)
        {
            var entries = new List<FlightLogEntry>();
            var logLines = File.ReadAllLines(path)
                .Select(line => line.Split(' ').Select(s => s.Trim(',')).ToArray())
                .Where(splitted => splitted.Length > 0)
                .ToList();

            var tmpEntry = new FlightLogEntry();
            var cameraTriggerTimes = new List<int>();
            var cameraTriggered = false;
            var first = true;

            foreach (var line in logLines) {
                if (line[0] == "ATT") {
                    if(!first)
                        entries.Add(tmpEntry.Clone());
                    
                    tmpEntry.TimeMS = int.Parse(line[1]);
                    tmpEntry.Roll = float.Parse(line[2]);
                    tmpEntry.Pitch = float.Parse(line[3]);
                    tmpEntry.Yaw = float.Parse(line[4]);

                    if (cameraTriggered) {
                        cameraTriggerTimes.Add(tmpEntry.TimeMS);
                        cameraTriggered = false;
                    }

                    first = false;
                } else if (line[0] == "NTUN") {
                    tmpEntry.Alt = float.Parse(line[8]);
                } else if (line[0] == "GPS") {
                    tmpEntry.Lat = double.Parse(line[6]);
                    tmpEntry.Lon = double.Parse(line[7]);
                } else if (line[0] == "CAM") {
                    cameraTriggered = true;
                }
            }

            if (entries.Count > 0) {
                var start = entries[0].TimeMS;

                foreach (var logEntry in entries)
                    logEntry.TimeMS -= start;

                for (int i = 0; i < cameraTriggerTimes.Count; i++)
                    cameraTriggerTimes[i] -= start;
            }

            return new FlightLog(entries, cameraTriggerTimes);
        }

        public FlightLogEntry FindClosestEntry(int triggerTime)
        {
            foreach (var entry in Entries) {
                if (entry.TimeMS > triggerTime)
                    return entry;
            }
            return null;
        }
    }
}