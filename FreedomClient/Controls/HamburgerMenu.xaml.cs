using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FreedomClient.Controls
{
    /// <summary>
    /// Interaction logic for HamburgerMenu.xaml
    /// </summary>
    public partial class HamburgerMenu : UserControl
    {
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(HamburgerMenu),
                new PropertyMetadata(false, OnIsOpenPropertyChanged));

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        public static readonly DependencyProperty OpenCloseDurationProperty =
            DependencyProperty.Register("OpenCloseDuration", typeof(Duration), typeof(HamburgerMenu),
                new PropertyMetadata(Duration.Automatic));

        public Duration OpenCloseDuration
        {
            get { return (Duration)GetValue(OpenCloseDurationProperty); }
            set { SetValue(OpenCloseDurationProperty, value); }
        }

        public static readonly DependencyProperty FallbackOpenWidthProperty =
            DependencyProperty.Register("FallbackOpenWidth", typeof(double), typeof(HamburgerMenu),
                new PropertyMetadata(100.0));

        public double FallbackOpenWidth
        {
            get { return (double)GetValue(FallbackOpenWidthProperty); }
            set { SetValue(FallbackOpenWidthProperty, value); }
        }

        public event EventHandler<MouseEventArgs>? OnClickOutsideOfMenu;

        
        static HamburgerMenu()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HamburgerMenu), new FrameworkPropertyMetadata(typeof(HamburgerMenu)));
        }

        public HamburgerMenu()
        {
            InitializeComponent();
            this.Loaded += OnComponentLoaded;
            Width = 0;
        }

        private void OnComponentLoaded(object sender, RoutedEventArgs e)
        {
            AddHandler(Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickOutsideOfControl), true);
        }

        private void OnClickOutsideOfControl(object sender, MouseEventArgs e)
        {
            OnClickOutsideOfMenu?.Invoke(this, e);
        }

        private void OnClickInControl(object sender, MouseEventArgs e)
        {
            Mouse.Capture(this, CaptureMode.SubTree);
        }

        private static void OnIsOpenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HamburgerMenu hamburgerMenu)
            {
                hamburgerMenu.OnIsOpenPropertyChanged();
            }
        }

        private void OnIsOpenPropertyChanged()
        {
            if (IsOpen)
            {
                OpenMenuAnimated();
                Mouse.Capture(this, CaptureMode.SubTree);
            }
            else
            {
                CloseMenuAnimated();
                ReleaseMouseCapture();
            }
        }

        private void OpenMenuAnimated()
        {
            double contentWidth = GetDesiredContentWidth();
            if (contentWidth <= FallbackOpenWidth)
            {
                contentWidth = FallbackOpenWidth;
            }

            DoubleAnimation openingAnimation = new DoubleAnimation(contentWidth, OpenCloseDuration);
            BeginAnimation(WidthProperty, openingAnimation);
        }

        private double GetDesiredContentWidth()
        {
            if (Content is not FrameworkElement elem)
            {
                return FallbackOpenWidth;
            }

            elem.Measure(new Size(MaxWidth, MaxHeight));

            return elem.DesiredSize.Width;
        }

        private void CloseMenuAnimated()
        {
            DoubleAnimation closingAnimation = new DoubleAnimation(0, OpenCloseDuration);
            BeginAnimation(WidthProperty, closingAnimation);
        }
    }
}
