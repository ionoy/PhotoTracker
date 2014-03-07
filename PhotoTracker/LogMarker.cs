using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GMap.NET;
using GMap.NET.WindowsForms;
using Microsoft.Xna.Framework;
using YLScsDrawing.Imaging.Filters;
using Color = System.Drawing.Color;
using Matrix = System.Drawing.Drawing2D.Matrix;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace PhotoTracker
{
    public class LogMarker : GMapMarker
    {
        private readonly MarkerController _controller;
        private readonly GMapControl _map;
        private Image _fullImage;

        public LogMarker(FlightLogEntry log, int index, FileInfo photo, GMapControl map, MarkerController controller)
            : base(new PointLatLng(log.Lat, log.Lon))
        {
            _controller = controller;
            Log = log;
            Index = index;
            _map = map;

            PhotoFilename = photo.Name;
            ThumbnailFullPath = new Uri(PhotoFilename.GetThumbnailPath(), UriKind.Absolute);
            ImageFullPath = new Uri(photo.FullName, UriKind.Absolute);

            Thumbnail = Image.FromFile(ThumbnailFullPath.AbsolutePath);
        }

        public FlightLogEntry Log { get; private set; }
        public int Index { get; private set; }
        public string PhotoFilename { get; private set; }

        public Uri ThumbnailFullPath { get; private set; }
        public Uri ImageFullPath { get; private set; }

        public Image Thumbnail { get; set; }
        public int DelayFromPrevious { get; set; }

        public override void OnRender(Graphics g)
        {
            if (_controller.EntryFilter(Log)) {
                Matrix oldMatrix = g.Transform;
                
                double groundResolution = _map.MapProvider.Projection.GetGroundResolution((int)_map.Zoom, Log.Lat);
                var matrix = new Matrix();
                
                float pitch = Log.Pitch + _controller.PitchOffset;
                float roll = Log.Roll + _controller.RollOffset;
                float yaw = Log.Yaw + _controller.YawOffset;

                var identityDir = new Vector3(0, -1, 0);
                var identityRight = new Vector3(1, 0, 0);

                //Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0, Angle.ToRadian(pitch), Angle.ToRadian(roll));
                Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0, Angle.ToRadian(pitch), Angle.ToRadian(roll));
                var lookDir = Vector3.Transform(identityDir, rotation);
                var lookRight = Vector3.Transform(identityRight, rotation);
                var lookUp = Vector3.Cross(lookDir, lookRight);

                var lookIntersection = GetIntersection(lookDir);
                g.DrawRectangle(new Pen(Color.Green, 2), (int)(LocalPosition.X + lookIntersection.X / groundResolution), (int)(LocalPosition.Y + lookIntersection.Z / groundResolution), 10, 10);
                
                var ur = GetCorner(lookDir, lookRight, lookUp, _controller.FovX / 2, _controller.FovY / 2, groundResolution);
                var ul = GetCorner(lookDir, lookRight, lookUp, -_controller.FovX / 2, _controller.FovY / 2, groundResolution);
                var lr = GetCorner(lookDir, lookRight, lookUp, _controller.FovX / 2, -_controller.FovY / 2, groundResolution);
                var ll = GetCorner(lookDir, lookRight, lookUp, -_controller.FovX / 2, -_controller.FovY / 2, groundResolution);

                var points = new[] { 
                    new PointF(ul.X, ul.Y), 
                    new PointF(ur.X, ur.Y), 
                    new PointF(lr.X, lr.Y), 
                    new PointF(ll.X, ll.Y) 
                };

                var freeTransform = new FreeTransform { FourCorners = points, Bitmap = (Bitmap)Thumbnail };
                var bmp = freeTransform.Bitmap;

                g.DrawImage(bmp, freeTransform.Rect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, GetTransparencyAttributes(_controller.Opacity));
                
                g.DrawRectangle(new Pen(Color.Red, 4), LocalPosition.X, LocalPosition.Y, 10, 10);

                g.Transform = oldMatrix;
            }
        }

        private Vector2 GetCorner(Vector3 lookDir, Vector3 lookRight, Vector3 lookUp, float fovx, float fovy, double groundResolution)
        {
            var yrotation = Quaternion.CreateFromAxisAngle(lookRight, fovy);

            var ur = Vector3.Transform(lookDir, yrotation);
            var newUp = Vector3.Transform(lookUp, yrotation);

            ur = Vector3.Transform(ur, Quaternion.CreateFromAxisAngle(newUp, fovx));
            var urIntersection = GetIntersection(ur);
            return new Vector2((float) (urIntersection.X / groundResolution) + LocalPosition.X, (float) (urIntersection.Z / groundResolution) + LocalPosition.Y);
        }

        private Vector3 GetIntersection(Vector3 lookDir)
        {
            var position = new Vector3(0, Log.Alt, 0);
            var plane = new Plane(new Vector3(0, 1, 0), 0);
            var ray = new Ray(position, lookDir);

            float? t = ray.Intersects(plane);

            if (t == null)
                return Vector3.Zero;

            var x = position.X + t.Value * lookDir.X;
            var y = position.Y + t.Value * lookDir.Y;
            var z = position.Z + t.Value * lookDir.Z;

            return new Vector3(x, y, z);
        }

        private ImageAttributes GetTransparencyAttributes(float opacity)
        {
            var colorMatrix = new ColorMatrix {Matrix33 = opacity};
            var imageAttributes = new ImageAttributes();
            imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            return imageAttributes;
        }
    }
}