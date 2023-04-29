using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FreedomClient.Controls
{
    /// <summary>
    /// Interaction logic for CyclingBackgroundImage.xaml
    /// </summary>
    public partial class CyclingBackgroundImage : UserControl
    {
        private Timer? _timer;
        private int _currentIndex;
        public ILogger<CyclingBackgroundImage>? Logger;

        public CyclingBackgroundImage()
        {
            InitializeComponent();
        }

        public static void UpdateVisualState(object? state)
        {
            var me = state as CyclingBackgroundImage;
            if (me == null)
            {
                return;
            }
            me.Dispatcher.Invoke(() =>
            {
                VisualStateManager.GoToState(me, "Cycling", true);
            });
            Thread.Sleep(2200);
            me.Dispatcher.Invoke(() =>
            {
                me._currentIndex = (me._currentIndex + 1) % me.ImagePaths.Count;
                me.Image1Source = me.SafeGetImageAtIndex(me._currentIndex);
                me.Image2Source = me.SafeGetImageAtIndex((me._currentIndex + 1) % me.ImagePaths.Count);
                VisualStateManager.GoToState(me, "Determinate", true);
            });
        }

        private BitmapImage SafeGetImageAtIndex(int index)
        {
            if (ImagePaths.Count == 0)
            {
                return new BitmapImage();
            }
            if (index >= ImagePaths.Count)
            {
                index = ImagePaths.Count - 1;
            }
            try
            {
                return new BitmapImage(new Uri(ImagePaths[index]));
            }
            // Corrupted File
            catch (NotSupportedException)
            {
                var filePath = ImagePaths[index];
                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(5000);
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch { }
                });
                ImagePaths.RemoveAt(index);
                return SafeGetImageAtIndex(index);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, null);
                return new BitmapImage();
            }
        }

        public static void OnImagePathsChange(DependencyObject dObject, DependencyPropertyChangedEventArgs e)
        {
            if (dObject is CyclingBackgroundImage me)
            {
                me._timer?.Dispose();
                if (me.ImagePaths.Count > 0)
                {
                    me.Image1Source = me.SafeGetImageAtIndex(0);
                }
                if (me.ImagePaths.Count > 1)
                {
                    me.Image2Source = me.SafeGetImageAtIndex(1);
                    me._currentIndex = 0;
                    me._timer = new Timer(new TimerCallback(UpdateVisualState), me, 10000, 10000);
                }
            }
        }

        public ObservableCollection<string> ImagePaths
        {
            get { return (ObservableCollection<string>)GetValue(ImagePathsProperty); }
            set { SetValue(ImagePathsProperty, value); }
        }

        private ImageSource Image1Source
        {
            get { return (ImageSource)GetValue(Image1SourceProperty); }
            set { SetValue(Image1SourceProperty, value); }
        }
        private ImageSource Image2Source
        {
            get { return (ImageSource)GetValue(Image2SourceProperty); }
            set { SetValue(Image2SourceProperty, value); }
        }

        public static readonly DependencyProperty Image1SourceProperty =
            DependencyProperty.Register(
                nameof(Image1Source),
                typeof(ImageSource),
                typeof(CyclingBackgroundImage));

        public static readonly DependencyProperty Image2SourceProperty =
            DependencyProperty.Register(
                nameof(Image2Source),
                typeof(ImageSource),
                typeof(CyclingBackgroundImage));

        public static readonly DependencyProperty ImagePathsProperty =
            DependencyProperty.Register(
                nameof(ImagePaths),
                typeof(ObservableCollection<string>),
                typeof(CyclingBackgroundImage),
                new PropertyMetadata(new PropertyChangedCallback(OnImagePathsChange)));
    }
}
