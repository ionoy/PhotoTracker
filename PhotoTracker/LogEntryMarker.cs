using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.WindowsForms;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;

namespace PhotoTracker
{
    class LogEntryMarker : GMapMarker
    {
        private const double CameraHFov = 46.77696485798980517842728275484;
        private const double CameraVFov = 36.008323211826764472069247464708;

        private readonly FlightLogEntry _logEntry;
        private readonly GMapControl _map;
        private readonly Image _thumbnail;
        private readonly double _fovX;
        private readonly double _fovY;
        private FileInfo _photo;
        private int _index;

        public LogEntryMarker(FlightLogEntry logEntry, FileInfo photo, int index, GMapControl map)
            : base(new PointLatLng(logEntry.Lat, logEntry.Lon))
        {
            _index = index;
            _photo = photo;
            _logEntry = logEntry;
            _map = map;
            _fovX = (CameraHFov / 180.0) * Math.PI;
            _fovY = (CameraVFov / 180.0) * Math.PI;

            if (index > 200 && index < 300) {
                using (var bitmap = new Bitmap(photo.FullName)) {
                    _thumbnail = (Bitmap)bitmap.GetThumbnailImage(bitmap.Width/10, bitmap.Height/10, null, IntPtr.Zero).Clone();
                }

                Debug.WriteLine("Loaded #" + index);
            }
        }

        public override void OnRender(Graphics g)
        {
            g.DrawRectangle(new Pen(Color.Red), new Rectangle(LocalPosition.X - 5, LocalPosition.Y - 5, 10, 10));
            g.DrawString(_index.ToString(), new Font("Arial", 10), new SolidBrush(Color.Black), LocalPosition.X + 10, LocalPosition.Y);

            if (_thumbnail != null) {
                var oldMatrix = g.Transform;

                var widthInMeters = _logEntry.Alt * Math.Tan(_fovX/2) * 2.0;
                var heightInMeters = _logEntry.Alt * Math.Tan(_fovY/2) * 2.0;

                var groundResolution = _map.MapProvider.Projection.GetGroundResolution((int) _map.Zoom, _logEntry.Lat);
                
                g.TranslateTransform(LocalPosition.X, LocalPosition.Y);
                g.RotateTransform((float) _logEntry.Yaw);
                g.TranslateTransform(-LocalPosition.X, -LocalPosition.Y);

                var wInPixels = (int)(widthInMeters / groundResolution);
                var hInPixels = (int)(heightInMeters / groundResolution);

                g.DrawImage(_thumbnail, LocalPosition.X - wInPixels / 2, LocalPosition.Y - hInPixels / 2, wInPixels, hInPixels);

                g.Transform = oldMatrix;
            }
        }
    }
}