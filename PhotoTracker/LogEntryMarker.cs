using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.WindowsForms;

namespace PhotoTracker
{
    class LogEntryMarker : GMapMarker
    {
        private const double CameraHFov = 46.77696485798980517842728275484;
        private const double CameraVFov = 36.008323211826764472069247464708;

        public FlightLogEntry LogEntry { get; private set; }
        public string PhotoFilename { get; private set; }

        private readonly GMapControl _map;
        private readonly Image _thumbnail;
        private readonly double _fovX;
        private readonly double _fovY;
        private readonly Func<FlightLogEntry, bool> _entryFilter;
        
        public LogEntryMarker(FlightLogEntry logEntry, FileInfo photo, GMapControl map, Func<FlightLogEntry, bool> entryFilter)
            : base(new PointLatLng(logEntry.Lat, logEntry.Lon))
        {
            _entryFilter = entryFilter;
            LogEntry = logEntry;
            _map = map;
            _fovX = (CameraHFov / 180.0) * Math.PI;
            _fovY = (CameraVFov / 180.0) * Math.PI;

            PhotoFilename = photo.Name;

            _thumbnail = Image.FromFile(PhotoFilename.GetThumbnailPath());
        }

        public override void OnRender(Graphics g)
        {
            if (_entryFilter(LogEntry)) {
                var oldMatrix = g.Transform;

                var widthInMeters = LogEntry.Alt * Math.Tan(_fovX/2) * 2.0;
                var heightInMeters = LogEntry.Alt * Math.Tan(_fovY/2) * 2.0;

                var groundResolution = _map.MapProvider.Projection.GetGroundResolution((int) _map.Zoom, LogEntry.Lat);
                
                g.TranslateTransform(LocalPosition.X, LocalPosition.Y);
                g.RotateTransform((float) LogEntry.Yaw);
                g.TranslateTransform(-LocalPosition.X, -LocalPosition.Y);

                var wInPixels = (int)(widthInMeters / groundResolution);
                var hInPixels = (int)(heightInMeters / groundResolution);

                g.DrawImage(_thumbnail, LocalPosition.X - wInPixels / 2, LocalPosition.Y - hInPixels / 2, wInPixels, hInPixels);

                g.Transform = oldMatrix;
            }
        }
    }
}