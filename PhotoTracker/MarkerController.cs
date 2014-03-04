using System;

namespace PhotoTracker
{
    public class MarkerController
    {
        public const double FovX = (46.77696485798980517842728275484 / 180.0) * Math.PI;
        public const double FovY = (36.008323211826764472069247464708 / 180.0) * Math.PI;

        public Func<FlightLogEntry, bool> EntryFilter { get; private set; }
        public Func<LogEntryMarker, bool> IsSelected { get; private set; }
        public float Opacity { get { return _opacityGetter(); }}
        public float PitchOffset { get; set; }
        public float RollOffset { get; set; }
        public float YawOffset { get; set; }

        private readonly Func<float> _opacityGetter;

        public MarkerController(Func<FlightLogEntry, bool> entryFilter, Func<LogEntryMarker, bool> isSelected, Func<float> opacityGetter)
        {
            _opacityGetter = opacityGetter;
            EntryFilter = entryFilter;
            IsSelected = isSelected;
        }
    }
}