using System.Windows;
using System.Windows.Controls;

namespace PhotoTracker
{
    public class ExSlider : Slider
    {
        public double FinalValue
        {
            get { return (double)GetValue(FinalValueProperty); }
            set { SetValue(FinalValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FinalValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FinalValueProperty =
            DependencyProperty.Register("FinalValue", typeof(double), typeof(ExSlider), new PropertyMetadata(0.0));

        protected override void OnThumbDragCompleted(System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            base.OnThumbDragCompleted(e);
            FinalValue = Value;
        }
    }
}