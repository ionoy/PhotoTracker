using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;

namespace PhotoTracker
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GMapControl _map = new GMapControl();
        private readonly GMapOverlay _overlay = new GMapOverlay();
        private List<string[]> _logLines = new List<string[]>();
        private List<FileInfo> _photoFiles = new List<FileInfo>();
        private List<PhotoObject> _photoObjects = new List<PhotoObject>();
        
        public MainWindow()
        {
            InitializeComponent();

            Photos.DisplayMemberPath = "Name";

            PhotoPath.Text = @"c:\Users\Mihhail\Downloads\104_PANA\";
            LogPath.Text = @"c:\Users\Mihhail\Downloads\104_PANA\2014-02-15 12-37RVJETHaljavaIII.log";

            WinFormsHost.Child = _map;

            _map.MapProvider = GMapProviders.GoogleHybridMap;
            _map.MinZoom = 1;
            _map.MaxZoom = 20;
            _map.Zoom = 2;
            _map.ShowCenter = false;
            _map.ShowTileGridLines = false;
            _map.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            _map.DragButton = MouseButtons.Left;

            _map.Overlays.Add(_overlay);

            Load.Click += (sender, args) => {
                _photoFiles = Directory.EnumerateFiles(PhotoPath.Text)
                    .Where(f => f.ToUpper().EndsWith(".JPG"))
                    .Select(f => new FileInfo(f))
                    .OrderBy(f => f.LastWriteTime)
                    .ToList();

                _logLines = File.ReadAllLines(LogPath.Text)
                    .Select(line => line.Split(' ').Select(s => s.Trim(',')).ToArray())
                    .Where(splitted => splitted.Length > 0)
                    .Where(splitted => splitted[0] == "CAM")
                    .ToList();

                InvalidatePhotoObjects();
            };

            GenerateOutput.Click += (sender, args) => WriteOutput();
            SetOffset.Click += (sender, args) => InvalidatePhotoObjects();
        }

        private void WriteOutput()
        {
            File.WriteAllText(@"c:\Users\Mihhail\Desktop\output.txt",
                "##  filename latitude longitude altitude pitch roll yaw" + Environment.NewLine);
            foreach (PhotoObject obj in _photoObjects) {
                File.AppendAllText(@"c:\Users\Mihhail\Desktop\output.txt",
                    string.Format("{0} {1} {2} {3} {4} {5} {6}{7}",
                        obj.Photo.Name,
                        obj.Lat.ToString("##.00000"),
                        obj.Lon.ToString("##.00000"),
                        obj.Alt.ToString("###.0"),
                        obj.Pitch.ToString("##0.00"),
                        obj.Roll.ToString("##0.00"),
                        obj.Yaw.ToString("##0.00"),
                        Environment.NewLine));
            }
        }

        private void InvalidatePhotoObjects()
        {
            Photos.ItemsSource = _photoFiles;

            var flightLog = FlightLog.FromArduLog(LogPath.Text);

            _overlay.Markers.Clear();

            int offset;
            if (!int.TryParse(Offset.Text, out offset)) {
                System.Windows.MessageBox.Show("Incorrect offset format");
                return;
            }

            var markers = flightLog.CameraTriggerTimes
                                   .Where((_, index) => index < _photoFiles.Count)
                                   .Select((trigger, index) => new LogEntryMarker(flightLog.FindClosestEntry(trigger + offset), _photoFiles[index], index, _map));

            foreach (var marker in markers)
                _overlay.Markers.Add(marker);

            CenterAroundFirstPhoto();
        }

        private void CenterAroundFirstPhoto()
        {
            if (_photoObjects.Count > 0) {
                _map.Position = new PointLatLng(_photoObjects[0].Lat, _photoObjects[0].Lon);
                _map.Zoom = 16;
            }
        }
    }
}