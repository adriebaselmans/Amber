using Grabber;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Threading;
using HueInterface;
using Color = Vortice.Mathematics.Color;

namespace GUI
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private DesktopDuplicator _desktopDuplicator;
        private DispatcherTimer _timer;
        private Brush? _leftPanelBackground;
        private Brush? _rightPanelBackground;
        private Color _leftColor;
        private Color _rightColor;
        private Bridge _hueInterface;

        public Brush? LeftPanelBackground
        {
            get => _leftPanelBackground;
            set
            {
                if (_leftPanelBackground != value)
                {
                    _leftPanelBackground = value;
                    OnPropertyChanged();
                }
            }
        }

        public Brush? RightPanelBackground
        {
            get => _rightPanelBackground;
            set
            {
                if (_rightPanelBackground != value)
                {
                    _rightPanelBackground = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel()
        {
            LeftPanelBackground = new SolidColorBrush(Colors.Black);
            RightPanelBackground = new SolidColorBrush(Colors.Black);
                       
            _desktopDuplicator = new DesktopDuplicator(0);

            _hueInterface = new Bridge("192.168.1.190", "sKDNmehonjs7SU8gmkECh25YjXDZZFqbNzmxHccu");
       
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000 / 20.0);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
             _desktopDuplicator.GetLatestFrame(ref _leftColor, ref _rightColor);

            var newColorLeftWpf = System.Windows.Media.Color.FromArgb(255, _leftColor.R, _leftColor.G, _leftColor.B);
            LeftPanelBackground = new SolidColorBrush(newColorLeftWpf);

            var newColorRightWpf = System.Windows.Media.Color.FromArgb(255, _rightColor.R, _rightColor.G, _rightColor.B);
            RightPanelBackground = new SolidColorBrush(newColorRightWpf);
 
            _hueInterface.UpdateLight(_leftColor, _rightColor);
        }
        
        public void Dispose()
        {
            _hueInterface?.Shutdown();
        }
    }
}