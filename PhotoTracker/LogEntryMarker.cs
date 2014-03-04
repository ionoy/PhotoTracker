using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Documents;
using GMap.NET;
using GMap.NET.WindowsForms;
using Microsoft.Maps.MapControl.WPF;

namespace PhotoTracker
{
    public class LogEntryMarker : GMapMarker
    {
        public FlightLogEntry LogEntry { get; private set; }
        public int Index { get; private set; }
        public string PhotoFilename { get; private set; }

        public Uri ThumbnailFullPath { get; private set; }
        public Uri ImageFullPath { get; private set; }

        public Image Thumbnail { get; set; }
        public int DTime { get; set; }

        private readonly GMapControl _map;
        private readonly MarkerController _controller;
        private Image _fullImage;

        public LogEntryMarker(FlightLogEntry logEntry, int index, FileInfo photo, GMapControl map, MarkerController controller)
            : base(new PointLatLng(logEntry.Lat, logEntry.Lon))
        {
            _controller = controller;
            LogEntry = logEntry;
            Index = index;
            _map = map;

            PhotoFilename = photo.Name;
            ThumbnailFullPath = new Uri(PhotoFilename.GetThumbnailPath(), UriKind.Absolute);
            ImageFullPath = new Uri(photo.FullName, UriKind.Absolute);

            Thumbnail = Image.FromFile(ThumbnailFullPath.AbsolutePath);
        }

        public override void OnRender(Graphics g)
        {
            if (_controller.EntryFilter(LogEntry)) {
                var oldMatrix = g.Transform;

                var pitch = LogEntry.Pitch + _controller.PitchOffset;
                var roll = -LogEntry.Roll + _controller.RollOffset;
                var yaw = LogEntry.Yaw + _controller.YawOffset;

                var ll = Tuple.Create(Angle.ToRadian(roll) - MarkerController.FovX / 2, Angle.ToRadian(pitch) - MarkerController.FovY / 2);
                var lr = Tuple.Create(Angle.ToRadian(roll) + MarkerController.FovX / 2, Angle.ToRadian(pitch) - MarkerController.FovY / 2);
                var ur = Tuple.Create(Angle.ToRadian(roll) + MarkerController.FovX / 2, Angle.ToRadian(pitch) + MarkerController.FovY / 2);
                var ul = Tuple.Create(Angle.ToRadian(roll) - MarkerController.FovX / 2, Angle.ToRadian(pitch) + MarkerController.FovY / 2);

                var groundResolution = _map.MapProvider.Projection.GetGroundResolution((int)_map.Zoom, LogEntry.Lat);
                var llPoint = PointFromAngles(ll.Item1, ll.Item2, groundResolution);
                var lrPoint = PointFromAngles(lr.Item1, lr.Item2, groundResolution);
                var urPoint = PointFromAngles(ur.Item1, ur.Item2, groundResolution);
                var ulPoint = PointFromAngles(ul.Item1, ul.Item2, groundResolution);

                var points = new[] { ulPoint, urPoint, llPoint };

                g.TranslateTransform(LocalPosition.X, LocalPosition.Y);
                g.RotateTransform(yaw);
                g.TranslateTransform(-LocalPosition.X, -LocalPosition.Y);

                if (_controller.IsSelected(this)) {
                    //                    if (_fullImage == null)
                    //                        _fullImage = Image.FromFile(ImageFullPath.AbsolutePath);

                    //g.DrawImage(Thumbnail, screenX, screenY, screenWidth, screenHeight);
                    g.DrawImage(Thumbnail, points, new Rectangle(0, 0, Thumbnail.Width, Thumbnail.Height), GraphicsUnit.Pixel, GetTransparencyAttributes(_controller.Opacity));
                } else {
                    if (_fullImage != null) {
                        _fullImage.Dispose();
                        _fullImage = null;
                    }

                    try {
                        g.DrawImage(Thumbnail, points, new Rectangle(0, 0, Thumbnail.Width, Thumbnail.Height), GraphicsUnit.Pixel, GetTransparencyAttributes(_controller.Opacity));
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                }
                g.DrawRectangle(new Pen(Color.Red, 4), LocalPosition.X, LocalPosition.Y, 10, 10);

                g.Transform = oldMatrix;
            }
        }

        private Point PointFromAngles(double roll, double pitch, double groundResolution)
        {
            var dx = Math.Tan(roll) * LogEntry.Alt;
            var dy = Math.Tan(pitch) * LogEntry.Alt;
            return new Point((int)(LocalPosition.X + dx / groundResolution), (int)(LocalPosition.Y - dy / groundResolution));
        }
        
        ImageAttributes GetTransparencyAttributes(float opacity)
        {
            var colorMatrix = new ColorMatrix { Matrix33 = opacity };
            var imageAttributes = new ImageAttributes();
            imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            return imageAttributes;
        }
    }
}