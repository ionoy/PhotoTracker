using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace PhotoTracker
{
    class LogExporter
    {
        public static void WriteOutput(float yawCorrection, IEnumerable<LogEntryMarker> markers, int offset, int maxRoll, int maxPitch, int minAlt)
        {
            var saveFileDialog = new SaveFileDialog {
                RestoreDirectory = true,
                Filter = "Text files (*.txt)|*.txt"
            };
            
            Func<float, float> normalizeYawAngle = (yaw) => {
                if (yaw < 0) return yaw + 360;
                if (yaw > 360) return yaw - 360;
                return yaw;
            };

            var result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK) {
                var outputFilename = saveFileDialog.FileName;
                File.WriteAllText(outputFilename, "##  filename latitude longitude altitude pitch roll yaw" + Environment.NewLine);
                File.AppendAllText(outputFilename, string.Format("##  time offset: {0}ms max roll: {1} max pitch: {2} min alt: {3}", offset, maxRoll, maxPitch, minAlt) + Environment.NewLine);

                foreach (var obj in markers) {
                    File.AppendAllText(@"c:\Users\Mihhail\Desktop\output.txt",
                        string.Format("{0} {1} {2} {3} {4} {5} {6}{7}",
                            obj.PhotoFilename,
                            obj.LogEntry.Lat.ToString("##.00000"),
                            obj.LogEntry.Lon.ToString("##.00000"),
                            obj.LogEntry.Alt.ToString("###.0"),
                            obj.LogEntry.Pitch.ToString("##0.00"),
                            obj.LogEntry.Roll.ToString("##0.00"),
                            normalizeYawAngle(obj.LogEntry.Yaw + yawCorrection).ToString("##0.00"),
                            Environment.NewLine));
                }
            }
        }
    }
}