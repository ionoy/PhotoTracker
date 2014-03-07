using System;

namespace PhotoTracker
{
    public class MarkerInfo
    {
        public readonly float FovX;
        public readonly float FovY;

        public Func<FlightLogEntry, bool> ValuesOutOfRange { get; private set; }
        public Func<LogMarker, bool> IsSelected { get; private set; }

        public float Opacity { get { return _model.Opacity / 100.0f; }}
        public float PitchOffset { get { return _model.PitchOffset / 10.0f; } }
        public float RollOffset { get { return _model.RollOffset / 10.0f; } }
        public float YawOffset { get { return _model.YawOffset / 10.0f; } }

        private readonly MainViewModel _model;
        
        public MarkerInfo(MainViewModel model)
        {
            _model = model;

            ValuesOutOfRange = e => e.Alt < model.MinAlt || 
                                    Math.Abs(e.Roll) > model.MaxRoll ||
                                    Math.Abs(e.Pitch) > model.MaxPitch;

            IsSelected = e => model.SelectedMarkersByIndex.ContainsKey(e.Index);

            FovX = Angle.ToRadian(46.7769f);
            FovY = Angle.ToRadian(36.0083f);
        }

        public bool IsManuallyFilteredOut(LogMarker logMarker)
        {
            return _model.ManuallyFilteredOutByIndex.ContainsKey(logMarker.Index);
        }

        public void SetManualFilter(LogMarker logMarker, bool value)
        {
            if (value) {
                _model.ManuallyFilteredOutByIndex[logMarker.Index] = logMarker;
            } else {
                if(_model.ManuallyFilteredOutByIndex.ContainsKey(logMarker.Index))
                    _model.ManuallyFilteredOutByIndex.Remove(logMarker.Index);
            }

            _model.Update(false);
        }

        public bool IsCurrentMarker(LogMarker marker)
        {
            return marker == _model.CurrentMarker;
        }
    }
}