using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using Caliburn.Micro;
using GMap.NET;
using GMap.NET.WindowsForms;
using PhotoTracker.Infrastructure;
using PropertyChanged;

namespace PhotoTracker 
{
    [ImplementPropertyChanged]
    public class MainViewModel : Screen, IShell
    {
        GMapControl _map = new GMapControl();
        
        private readonly GMapOverlay _photoOverlay = new GMapOverlay();
        //private readonly MarkerController _markerController;
        private readonly List<FileInfo> _photoFiles = new List<FileInfo>();
        private readonly MarkerController _markerController;

        public string PhotoPath { get; set; }
        public string LogPath { get; set; }
        public int TimeOffset { get; set; }

        public int MaxRoll { get; set; }
        public int MaxPitch { get; set; }
        public int MinAlt { get; set; }

        public int Opacity { get; set; }

        public int PitchOffset { get; set; }
        public int RollOffset { get; set; }
        public int YawOffset { get; set; }

        public string PitchOffsetLabel { get; set; }
        public string RollOffsetLabel { get; set; }
        public string YawOffsetLabel { get; set; }

        public int PhotoFilterFrom { get; set; }
        public int PhotoFilterTo { get; set; }
        
        public BindableCollection<LogMarker> PhotoList { get; set; }
        public LogMarker SelectedMarker { get; set; }
        public Visibility IsPhotoInfoVisible { get { return SelectedMarker != null ? Visibility.Visible : Visibility.Collapsed; } }

        public MainViewModel()
        {
            PhotoList = new BindableCollection<LogMarker>();
            PhotoPath = @"c:\Users\Mihhail\Downloads\104_PANA\";
            LogPath = @"c:\Users\Mihhail\Downloads\104_PANA\2014-02-15 12-37RVJETHaljavaIII.log";

            MaxRoll = 5;
            MaxPitch = 5;
            MinAlt = 0;
            Opacity = 50;

            _markerController = new MarkerController(this);

            PropertyChanged += (sender, args) => {
                PitchOffsetLabel = (PitchOffset / 10.0).ToString("##.00");
                RollOffsetLabel = (RollOffset / 10.0).ToString("##.00");
                YawOffsetLabel = (YawOffset / 10.0).ToString("##.00");

                _map.Invalidate();
            };
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var mapHost = (view as DependencyObject).FindVisualChild<MapHost>();
            
            _map = mapHost.Map;
            _map.Overlays.Add(_photoOverlay);
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
            var markers = _photoOverlay.Markers.OfType<LogMarker>().Where(m => _markerController.EntryFilter(m.Log));
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

        public void PhotoListDoubleClicked()
        {
            if (SelectedMarker != null)
                _map.Position = new PointLatLng(SelectedMarker.Log.Lat, SelectedMarker.Log.Lon);
        }

        private void ReloadMarkers(bool resetPosition = true)
        {
            PhotoList.Clear();

            var flightLog = FlightLog.FromArduLog(LogPath);
            
            if (flightLog.Entries.Count == 0 || _photoFiles.Count == 0)
                return;

            _photoOverlay.Markers.Clear();
            
            var markers = flightLog.CameraTriggerTimes
                                    .Where((_, index) => index < _photoFiles.Count)
                                    .Select((trigger, index) => new LogMarker(flightLog.FindClosestEntry(trigger + TimeOffset), index,
                                                                                   _photoFiles[index], _map, _markerController))
                                    .ToList();

            var prevTime = markers[0].Log.TimeMS;

            //foreach (var marker in markers.Skip(PhotoFilterFrom).Take(PhotoFilterTo - PhotoFilterFrom)) {
            foreach (var marker in markers.Skip(130).Take(1)) {
                marker.DelayFromPrevious = marker.Log.TimeMS - prevTime;
                _photoOverlay.Markers.Add(marker);
                PhotoList.Add(marker);

                prevTime = marker.Log.TimeMS;
            }

            if (resetPosition) {
                _map.Position = new PointLatLng(flightLog.Entries[0].Lat, flightLog.Entries[0].Lon);
                _map.Zoom = 16;
            }
        }
    }
}