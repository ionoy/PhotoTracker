using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Caliburn.Micro;
using GMap.NET;
using GMap.NET.WindowsForms;
using Microsoft.Xna.Framework;
using PhotoTracker.Infrastructure;
using PropertyChanged;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Screen = Caliburn.Micro.Screen;

namespace PhotoTracker 
{
    [ImplementPropertyChanged]
    public class MainViewModel : Screen, IShell
    {
        GMapControl _map = new GMapControl();
        
        private readonly GMapOverlay _photoOverlay = new GMapOverlay();
        private readonly List<FileInfo> _photoFiles = new List<FileInfo>();
        private readonly MarkerInfo _markerInfo;
        private int _pitchOffset;
        private int _rollOffset;
        private int _yawOffset;
        private LogMarker _currentMarker;

        public string PhotoPath { get; set; }
        public string LogPath { get; set; }
        public int TimeOffset { get; set; }

        public int MaxRoll { get; set; }
        public int MaxPitch { get; set; }
        public int MinAlt { get; set; }

        public int Opacity { get; set; }

        public int PitchOffset
        {
            get { return _pitchOffset; }
            set { _pitchOffset = value; PitchOffsetLabel = (PitchOffset / 10.0).ToString("##.00"); }
        }

        public int RollOffset
        {
            get { return _rollOffset; }
            set { _rollOffset = value; RollOffsetLabel = (RollOffset / 10.0).ToString("##.00"); }
        }

        public int YawOffset
        {
            get { return _yawOffset; }
            set { _yawOffset = value; YawOffsetLabel = (YawOffset / 10.0).ToString("##.00"); }
        }

        public string PitchOffsetLabel { get; set; }
        public string RollOffsetLabel { get; set; }
        public string YawOffsetLabel { get; set; }

        public int PhotoFilterFrom { get; set; }
        public int PhotoFilterTo { get; set; }
        
        public BindableCollection<LogMarker> Markers { get; set; }
        public BindableCollection<LogMarker> SelectedMarkers { get; set; }
        public Dictionary<int, LogMarker> SelectedMarkersByIndex { get; set; } 
        public Dictionary<int, LogMarker> ManuallyFilteredOutByIndex { get; set; }

        public LogMarker CurrentMarker
        {
            get { return _currentMarker; }
            set
            {
                _currentMarker = value;

                if (value != null) {
                    _photoOverlay.Markers.Remove(_currentMarker);
                    _photoOverlay.Markers.Add(_currentMarker);
                    _map.Invalidate();    
                }
            }
        }

        public Visibility IsPhotoInfoVisible { get { return CurrentMarker != null ? Visibility.Visible : Visibility.Collapsed; } }

        public MainViewModel()
        {
            Markers = new BindableCollection<LogMarker>();
            SelectedMarkers = new BindableCollection<LogMarker>();

            SelectedMarkers.CollectionChanged += (sender, args) => {
                if(args.NewItems != null)
                    foreach (var newItem in args.NewItems.OfType<LogMarker>())
                        SelectedMarkersByIndex[newItem.Index] = newItem;
                if (args.OldItems != null)
                    foreach (var oldItem in args.OldItems.OfType<LogMarker>())
                        if(SelectedMarkersByIndex.ContainsKey(oldItem.Index))
                            SelectedMarkersByIndex.Remove(oldItem.Index);

                _map.Invalidate();
            };

            SelectedMarkersByIndex = new Dictionary<int, LogMarker>();
            ManuallyFilteredOutByIndex = new Dictionary<int, LogMarker>();

            PhotoPath = @"c:\Users\Mihhail\Downloads\104_PANA\";
            LogPath = @"c:\Users\Mihhail\Downloads\104_PANA\2014-02-15 12-37RVJETHaljavaIII.log";

            MaxRoll = 5;
            MaxPitch = 5;
            MinAlt = 0;
            Opacity = 50;

            _markerInfo = new MarkerInfo(this);
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var mapHost = (view as DependencyObject).FindVisualChild<MapHost>();
            
            _map = mapHost.Map;
            _map.Overlays.Add(_photoOverlay);
            _map.MouseMove += MouseMove;
        }

        public void Load()
        {
            _photoFiles.Clear();
            _photoFiles.AddRange(Directory.EnumerateFiles(PhotoPath)
                                          .Where(f => f.ToUpper().EndsWith(".JPG"))
                                          .Select(f => new FileInfo(f))
                                          .OrderBy(f => f.LastWriteTime)
                                          .ToList());

            PhotoFilterFrom = 0;
            PhotoFilterTo = _photoFiles.Count;

            var progress = new Progress("Generating thumbnails");
            progress.Show();

            ThumnailGenerator.Start(TaskPoolScheduler.Default, _photoFiles)
                             .ObserveOn(SynchronizationContext.Current)
                             .Subscribe(i => progress.Percentage = i, _ => progress.Hide(), () => {
                                 progress.Hide();
                                 ReloadMarkers();
                             });
        }

        public void ExportLog()
        {
            var markers = _photoOverlay.Markers.OfType<LogMarker>().Where(m => _markerInfo.ValuesOutOfRange(m.Log));
            LogExporter.WriteOutput(YawOffset / 10.0f, markers, TimeOffset, MaxRoll, MaxPitch, MinAlt);
        }

        public void SetOffset()
        {
            ReloadMarkers(false);
        }

        public void SetPhotoFilterRange()
        {
            ReloadMarkers(false);
        }
        
        public void MouseMove(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Right) {
                var mouseLatLon = _map.FromLocalToLatLng(args.X, args.Y);
                if (Markers.Count > 0) {
                    LogMarker closest = null;
                    float closestDist = float.MaxValue;

                    if (Keyboard.Modifiers == ModifierKeys.Control) {
                        foreach (var marker in Markers.Where(m => m.IsSelected)) {
                            var dist = Vector2.Distance(new Vector2(marker.Log.Lat, marker.Log.Lon),
                                new Vector2((float)mouseLatLon.Lat, (float)mouseLatLon.Lng));

                            if (dist < closestDist && dist < 0.001) {
                                closest = marker;
                                closestDist = dist;
                            }
                        }

                        if (closest != null)
                            SelectedMarkers.Remove(closest);
                    } else {
                        foreach (var marker in Markers.Where(m => !m.IsSelected)) {
                            var dist = Vector2.Distance(new Vector2(marker.Log.Lat, marker.Log.Lon),
                                new Vector2((float)mouseLatLon.Lat, (float)mouseLatLon.Lng));

                            if (dist < closestDist && dist < 0.001) {
                                closest = marker;
                                closestDist = dist;
                            }
                        }

                        if (closest != null)
                            SelectedMarkers.Add(closest);
                    }
                }
            }
        }
        
        public void KeyDown(object sender, System.Windows.Input.KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
                SelectedMarkers.Clear();
            else if (args.Key == Key.Delete && CurrentMarker != null)
                SelectedMarkers.Remove(CurrentMarker);
        }

        public void Update(bool recalc = true)
        {
            if(recalc)
                foreach (var marker in Markers)
                    marker.NeedRecalc = true;
            _map.Invalidate();
        }

        private void ReloadMarkers(bool resetPosition = true)
        {
            Markers.Clear();

            var flightLog = FlightLog.FromArduLog(LogPath);
            
            if (flightLog.Entries.Count == 0 || _photoFiles.Count == 0)
                return;

            _photoOverlay.Markers.Clear();
            
            var markers = flightLog.CameraTriggerTimes
                                   .Where((_, index) => index < _photoFiles.Count)
                                   .Select((trigger, index) => new LogMarker(flightLog.FindClosestEntry(trigger + TimeOffset), index,
                                                                                  _photoFiles[index], _map, _markerInfo))
                                   .ToList();

            var prevTime = markers[0].Log.TimeMS;

            foreach (var marker in markers) {
                marker.DelayFromPrevious = marker.Log.TimeMS - prevTime;
                _photoOverlay.Markers.Add(marker);
                Markers.Add(marker);

                prevTime = marker.Log.TimeMS;
            }

            if (resetPosition) {
                _map.Position = new PointLatLng(flightLog.Entries[0].Lat, flightLog.Entries[0].Lon);
                _map.Zoom = 16;
            }
        }
    }
}