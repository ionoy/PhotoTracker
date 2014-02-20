using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private List<FileInfo> _photoFiles = new List<FileInfo>();
        private int _maxRoll;
        private int _maxPitch;
        private int _minAlt;
        private FlightLog _flightLog;
        private int _offset;

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


                var progress = new Progress("Generating thumbnails");
                progress.Show();

                foreach (var photo in _photoFiles) {
                    var thumbnailPath = photo.Name.GetThumbnailPath();
                    var thumbnailDirectory = Path.GetDirectoryName(thumbnailPath);

                    if (!Directory.Exists(thumbnailDirectory))
                        Directory.CreateDirectory(thumbnailDirectory);

                    if (!File.Exists(thumbnailPath)) {
                        using (var bitmap = new Bitmap(photo.FullName)) {
                            var thumbnail = (Bitmap)bitmap.GetThumbnailImage(bitmap.Width / 20, bitmap.Height / 20, null, IntPtr.Zero);
                            thumbnail.Save(thumbnailPath);
                        }
                    }
                }

                progress.Hide();

                Invalidate();
            };

            GenerateOutput.Click += (sender, args) => WriteOutput();
            SetOffset.Click += (sender, args) => Invalidate(false);

            int.TryParse(MaxRoll.Text, out _maxRoll);
            int.TryParse(MaxPitch.Text, out _maxPitch);
            int.TryParse(MinAlt.Text, out _minAlt);
            
            MaxRoll.TextChanged += (sender, args) => {
                if (int.TryParse(MaxRoll.Text, out _maxRoll))
                    _map.Invalidate();
            };

            MaxPitch.TextChanged += (sender, args) => {
                if(int.TryParse(MaxPitch.Text, out _maxPitch))
                    _map.Invalidate();
            };

            MinAlt.TextChanged += (sender, args) => {
                if (int.TryParse(MinAlt.Text, out _minAlt))
                    _map.Invalidate();
            };
        }

        private void WriteOutput()
        {
            var saveFileDialog = new SaveFileDialog {
                RestoreDirectory = true,
                Filter = "Text files (*.txt)|*.txt"
            };
            
            var result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                var outputFilename = saveFileDialog.FileName;
                File.WriteAllText(outputFilename, "##  filename latitude longitude altitude pitch roll yaw" + Environment.NewLine);
                File.AppendAllText(outputFilename, string.Format("##  time offset: {0}ms max roll: {1} max pitch: {2} min alt: {3}", _offset, _maxRoll, _maxPitch, _minAlt) + Environment.NewLine);
                foreach (var obj in _overlay.Markers.OfType<LogEntryMarker>().Where(m => EntryFilter(m.LogEntry))) {
                    File.AppendAllText(@"c:\Users\Mihhail\Desktop\output.txt",
                        string.Format("{0} {1} {2} {3} {4} {5} {6}{7}",
                            obj.PhotoFilename,
                            obj.LogEntry.Lat.ToString("##.00000"),
                            obj.LogEntry.Lon.ToString("##.00000"),
                            obj.LogEntry.Alt.ToString("###.0"),
                            obj.LogEntry.Pitch.ToString("##0.00"),
                            obj.LogEntry.Roll.ToString("##0.00"),
                            obj.LogEntry.Yaw.ToString("##0.00"),
                            Environment.NewLine));
                }
            }
        }

        private void Invalidate(bool resetPosition = true)
        {
            Photos.ItemsSource = _photoFiles;

            _flightLog = FlightLog.FromArduLog(LogPath.Text);
            
            _overlay.Markers.Clear();

            if (_flightLog.Entries.Count == 0)
                return;
            
            if (!int.TryParse(Offset.Text, out _offset)) {
                System.Windows.MessageBox.Show("Incorrect offset format");
                return;
            }

            var markers = _flightLog.CameraTriggerTimes
                                   .Where((_, index) => index < _photoFiles.Count)
                                   .Select((trigger, index) => new LogEntryMarker(_flightLog.FindClosestEntry(trigger + _offset), _photoFiles[index], _map, EntryFilter));

            foreach (var marker in markers)
                _overlay.Markers.Add(marker);

            if (resetPosition) {
                _map.Position = new PointLatLng(_flightLog.Entries[0].Lat, _flightLog.Entries[0].Lon);
                _map.Zoom = 16;
            }
        }

        private bool EntryFilter(FlightLogEntry entry)
        {
            return entry.Alt > _minAlt && entry.Roll < _maxRoll && entry.Pitch < _maxPitch;
        }
    }
}