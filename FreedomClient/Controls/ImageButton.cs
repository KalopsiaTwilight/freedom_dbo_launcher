using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;

namespace FreedomClient.Controls
{
    public class ImageButton: Button
    {
        public Brush Brush 
        { 
            get { return (Brush)GetValue(BrushProperty); } 
            set { SetValue(BrushProperty, value); }
        }
        public Geometry Geometry 
        { 
            get { return (Geometry)GetValue(GeometryProperty); }
            set { SetValue(GeometryProperty, value); }
        }

        public static readonly DependencyProperty BrushProperty =
            DependencyProperty.Register(
                "Brush",
                typeof(Brush),
                typeof(ImageButton), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 255, 255))));

        public static readonly DependencyProperty GeometryProperty =
            DependencyProperty.Register(
                "Geometry",
                typeof(Geometry),
                typeof(ImageButton), new PropertyMetadata(new RectangleGeometry()));
    }
}
