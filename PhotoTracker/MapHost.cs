using System.Windows.Forms;
using System.Windows.Forms.Integration;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;

namespace PhotoTracker
{
    public class MapHost : WindowsFormsHost
    {
        public GMapControl Map { get; set; }

        public MapHost()
        {
            Map = new GMapControl {
                MapProvider = GMapProviders.GoogleHybridMap,
                MinZoom = 1,
                MaxZoom = 20,
                Zoom = 2,
                ShowCenter = false,
                ShowTileGridLines = false,
                MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter,
                DragButton = MouseButtons.Left,
                DisableFocusOnMouseEnter = true
            };

            Child = Map;
        }
    }
}
