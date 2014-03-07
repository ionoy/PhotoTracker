using System;

namespace PhotoTracker
{
    public class MarkerController
    {
        public readonly float FovX;
        public readonly float FovY;

        public Func<FlightLogEntry, bool> EntryFilter { get; private set; }
        public Func<LogMarker, bool> IsSelected { get; private set; }

        public float Opacity { get { return _model.Opacity / 100.0f; }}
        public float PitchOffset { get { return _model.PitchOffset / 2.0f; } }
        public float RollOffset { get { return _model.RollOffset / 2.0f; } }
        public float YawOffset { get { return _model.YawOffset / 2.0f; } }

        private readonly MainViewModel _model;
        
        public MarkerController(MainViewModel model)
        {
            _model = model;

            EntryFilter = e => e.Alt > model.MinAlt && 
                               Math.Abs(e.Roll) <= model.MaxRoll && 
                               Math.Abs(e.Pitch) <= model.MaxPitch;

            IsSelected = e => e == model.SelectedMarker;

            FovX = Angle.ToRadian(46.7769f);
            FovY = Angle.ToRadian(36.0083f);
        }
    }
}