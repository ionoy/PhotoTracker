using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;

namespace PhotoTracker
{
    public partial class MainWindow : Window
    {
        public LogEntryMarker SelectedMarker { get; set; }

        private readonly GMapControl _map = new GMapControl();
        private readonly GMapOverlay _overlay = new GMapOverlay();
        private readonly MarkerController _markerController;
        private readonly List<FileInfo> _photoFiles = new List<FileInfo>();

        private int _maxRoll;
        private int _maxPitch;
        private int _minAlt;
        private FlightLog _flightLog;
        private int _offset;
        private double _opacity;

        private IDisposable _invalidateSubscription = Observable.Never<long>().Subscribe(_ => { });
        private int _filterFrom;
        private int _filterTo;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
   
            PhotoPath.Text = @"c:\Users\Mihhail\Downloads\104_PANA\";
            LogPath.Text = @"c:\Users\Mihhail\Downloads\104_PANA\2014-02-15 12-37RVJETHaljavaIII.log";

            _map = new GMapControl {
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

            WinFormsHost.Child = _map;

            _map.Overlays.Add(_overlay);
            _markerController = new MarkerController(e => e.Alt > _minAlt && Math.Abs(e.Roll) <= _maxRoll && Math.Abs(e.Pitch) <= _maxPitch,
                                                     m => m == SelectedMarker,
                                                     () => (float)_opacity / 100.0f);
            
            int.TryParse(MaxRoll.Text, out _maxRoll);
            int.TryParse(MaxPitch.Text, out _maxPitch);
            int.TryParse(MinAlt.Text, out _minAlt);

            PhotoList.PreviewMouseWheel += (sender, args) => {
                var scrollViewer = FindVisualChild<ScrollViewer>(PhotoList);

                if(args.Delta < 0) scrollViewer.LineRight();
                else scrollViewer.LineLeft();
            };

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

            PhotoList.SelectionChanged += (sender, args) => {
                SelectedMarker = args.AddedItems.Count > 0 ? (LogEntryMarker) args.AddedItems[0] : null;
                SelectMarker(SelectedMarker);
            };
            
            _map.MouseClick += (sender, args) => {
                var clickLatLon = _map.FromLocalToLatLng(args.X, args.Y);
                var closestMarker = _overlay.Markers.OfType<LogEntryMarker>()
                                            .OrderBy(m => (clickLatLon.Lat - m.LogEntry.Lat) * (clickLatLon.Lat - m.LogEntry.Lat) +
                                                          (clickLatLon.Lng - m.LogEntry.Lon) * (clickLatLon.Lng - m.LogEntry.Lon))
                                            .FirstOrDefault();

                if (closestMarker != null) {
                    PhotoList.SelectedItem = closestMarker;
                    PhotoList.ScrollIntoView(closestMarker);

                    SelectMarker(closestMarker);
                }
            };

            Closed += (sender, args) => Process.GetCurrentProcess().Kill();
        }

        private void SelectMarker(LogEntryMarker marker)
        {
            if (marker != null) {
                _overlay.Markers.Remove(marker);
                _overlay.Markers.Add(marker);
                PhotoInfo.Content = new PhotoInfoPanel(marker);
            } else {
                PhotoInfo.Content = null;
            }
        }

        private void UpdateMarkers(bool resetPosition = true)
        {
            _flightLog = FlightLog.FromArduLog(LogPath.Text);

            if (_flightLog.Entries.Count == 0 || _photoFiles.Count == 0)
                return;

            _overlay.Markers.Clear();

            if (!int.TryParse(Offset.Text, out _offset)) {
                System.Windows.MessageBox.Show("Incorrect offset format");
                return;
            }

            var markers = _flightLog.CameraTriggerTimes
                                    .Where((_, index) => index < _photoFiles.Count)
                                    .Select((trigger, index) => new LogEntryMarker(_flightLog.FindClosestEntry(trigger + _offset), index,
                                                                                   _photoFiles[index], _map, _markerController))
                                    .ToList();

            var prevTime = markers[0].LogEntry.TimeMS;

            foreach (var marker in markers.Skip(_filterFrom).Take(_filterTo - _filterFrom)) {
                marker.DTime = marker.LogEntry.TimeMS - prevTime;
                _overlay.Markers.Add(marker);
                PhotoList.Items.Add(marker);
                prevTime = marker.LogEntry.TimeMS;
            }

            if (resetPosition) {
                _map.Position = new PointLatLng(_flightLog.Entries[0].Lat, _flightLog.Entries[0].Lon);
                _map.Zoom = 16;
            }
        }

        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++) {
                var child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is T)
                    return (T)child;

                else {
                    var childOfChild = FindVisualChild<T>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }

        private void OpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _opacity = e.NewValue;
            _map.Invalidate();
        }
        
        private void PhotoListDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if(SelectedMarker != null)
                _map.Position = new PointLatLng(SelectedMarker.LogEntry.Lat, SelectedMarker.LogEntry.Lon);
        }

        private void PitchOffsetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _markerController.PitchOffset = (float)(e.NewValue / 10.0f);
            PitchOffset.Text = _markerController.PitchOffset.ToString("##.00");
            _map.Invalidate();
        }

        private void RollOffsetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _markerController.RollOffset = (float)(e.NewValue / 10.0f);
            RollOffset.Text = _markerController.RollOffset.ToString("##.00");
            _map.Invalidate();
        }

        private void YawOffsetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _markerController.YawOffset = (float)(e.NewValue / 10.0f);
            YawOffset.Text = _markerController.YawOffset.ToString("##.00");
            _map.Invalidate();
        }

        private void FilterClick(object sender, RoutedEventArgs e)
        {
            try {
                InvalidateFilter();
                UpdateMarkers();
            }
            catch (Exception exception) {
                System.Windows.MessageBox.Show(exception.Message);
            }
        }

        private void InvalidateFilter()
        {
            _filterFrom = int.Parse(From.Text);
            _filterTo = int.Parse(To.Text);
        }
    }
}