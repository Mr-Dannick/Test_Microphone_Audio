using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Test_Microphone_Audio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Rectangle _innerRectangle;
        private Canvas _colorWheelCanvas;
        private TextBlock _frequencyDisplay;
        private TextBlock _colorFreqRangeDisplay;
        private Ellipse _centerCircle;
        private List<Path> _colorSegments = new List<Path>();
        private List<TextBlock> _colorLabels = new List<TextBlock>();
        private const int MAX_HEIGHT = 300;

        public MainWindow()
        {
            InitializeComponent();

            // Set window size to prevent content crowding
            Width = 800;
            Height = 700;

            var canvas = new Canvas();
            Content = canvas;

            var title = new TextBlock
            {
                Text = "Hieronder staat een cirkel die van kleur moet veranderen met toonhoogte en een balk die meer of minder gevuld wordt aan de hand van volume",
                FontSize = 20,
                Margin = new Thickness(10, 10, 10, 10),
                TextWrapping = TextWrapping.Wrap,
                Width = 600
            };
            canvas.Children.Add(title);

            // Add frequency display with proper spacing
            _frequencyDisplay = new TextBlock
            {
                Text = "Frequency: 0 Hz",
                FontSize = 16,
                Margin = new Thickness(450, 20, 10, 10)
            };
            canvas.Children.Add(_frequencyDisplay);

            // Add frequency range display with proper spacing
            _colorFreqRangeDisplay = new TextBlock
            {
                Text = "Frequency Range: N/A",
                FontSize = 16,
                Margin = new Thickness(450, 50, 10, 10)
            };
            canvas.Children.Add(_colorFreqRangeDisplay);

            // Create the color wheel
            CreateColorWheel(canvas);

            _innerRectangle = new Rectangle
            {
                Width = 30,
                Height = 0, // Start at zero height
                Fill = Brushes.Blue
            };
            Canvas.SetRight(_innerRectangle, 20);
            Canvas.SetBottom(_innerRectangle, 50); // Positioning it on top of the outerRectangle
            canvas.Children.Add(_innerRectangle);

            var outerRectangle = new Rectangle
            {
                Width = 30,
                Height = 300,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            Canvas.SetRight(outerRectangle, 20);
            Canvas.SetBottom(outerRectangle, 50);
            canvas.Children.Add(outerRectangle);

            // Subscribe to volume and frequency updates
            UpdateLoop.Instance.SubscribeWithVolumeAndFrequency(UpdateVolumeAndFrequencyVisualization);
        }

        private void CreateColorWheel(Canvas canvas)
        {
            // Create a canvas for the color wheel - make it bigger
            _colorWheelCanvas = new Canvas { Width = 300, Height = 300 };
            Canvas.SetTop(_colorWheelCanvas, 150);
            Canvas.SetLeft(_colorWheelCanvas, 150);
            canvas.Children.Add(_colorWheelCanvas);

            double centerX = 150;
            double centerY = 150;
            double outerRadius = 150;
            double innerRadius = 50; // Bigger inner circle
            
            // Define our color segments with their frequency ranges - adjusted for vocal/singing range
            var colorSegments = new[]
            {
                (Color: Colors.Red, Name: "Red", MinFreq: 700, MaxFreq: 1100),         // Soprano
                (Color: Colors.Orange, Name: "Orange", MinFreq: 500, MaxFreq: 700),     // Alto/Mezzo
                (Color: Colors.Yellow, Name: "Yellow", MinFreq: 300, MaxFreq: 500),     // Tenor
                (Color: Colors.Green, Name: "Green", MinFreq: 180, MaxFreq: 300),       // Baritone
                (Color: Colors.Cyan, Name: "Cyan", MinFreq: 80, MaxFreq: 180),          // Bass
                (Color: Colors.Blue, Name: "Indigo", MinFreq: 40, MaxFreq: 80),         // Below singing
                (Color: Colors.BlueViolet, Name: "Blue-Violet", MinFreq: 1100, MaxFreq: 2000), // Above singing
                (Color: Colors.Magenta, Name: "Magenta", MinFreq: 2000, MaxFreq: 5000)  // Well above singing
            };
            
            int segmentCount = colorSegments.Length;
            double anglePerSegment = 360.0 / segmentCount;
            
            for (int i = 0; i < segmentCount; i++)
            {
                double startAngle = i * anglePerSegment;
                double endAngle = (i + 1) * anglePerSegment;
                
                var segment = CreateSegment(centerX, centerY, innerRadius, outerRadius, startAngle, endAngle);
                segment.Fill = new SolidColorBrush(colorSegments[i].Color);
                segment.Stroke = Brushes.Black;
                segment.StrokeThickness = 1;
                segment.Tag = colorSegments[i];
                
                _colorSegments.Add(segment);
                _colorWheelCanvas.Children.Add(segment);
                
                // Calculate better position for centered text
                double midAngle = (startAngle + endAngle) / 2 * Math.PI / 180;
                double textRadius = (innerRadius + outerRadius) / 2 * 0.75; // Position at 75% from center to better center text
                
                // Create a Canvas for the label to allow rotation
                var labelCanvas = new Canvas();
                _colorWheelCanvas.Children.Add(labelCanvas);
                
                // Position the label canvas
                double labelX = centerX + textRadius * Math.Cos(midAngle);
                double labelY = centerY + textRadius * Math.Sin(midAngle);
                Canvas.SetLeft(labelCanvas, labelX);
                Canvas.SetTop(labelCanvas, labelY);
                
                // Create label with color name and frequency range
                var label = new TextBlock
                {
                    Text = $"{colorSegments[i].Name}\n{colorSegments[i].MinFreq}-{colorSegments[i].MaxFreq} Hz",
                    FontSize = 11,
                    Foreground = Brushes.Black,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                // Add the label to its canvas
                labelCanvas.Children.Add(label);
                
                // Center the label on its canvas
                Canvas.SetLeft(label, -label.ActualWidth / 2);
                Canvas.SetTop(label, -label.ActualHeight / 2);
                
                // Add a second text rendering pass to position correctly after layout
                _colorWheelCanvas.Loaded += (s, e) =>
                {
                    Canvas.SetLeft(label, -label.ActualWidth / 2);
                    Canvas.SetTop(label, -label.ActualHeight / 2);
                };
                
                _colorLabels.Add(label);
            }
            
            // Add white center circle
            _centerCircle = new Ellipse
            {
                Width = innerRadius * 2,
                Height = innerRadius * 2,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            
            Canvas.SetLeft(_centerCircle, centerX - innerRadius);
            Canvas.SetTop(_centerCircle, centerY - innerRadius);
            _colorWheelCanvas.Children.Add(_centerCircle);
        }
        
        private Path CreateSegment(double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle)
        {
            startAngle = startAngle * Math.PI / 180;
            endAngle = endAngle * Math.PI / 180;
            
            bool isLargeArc = endAngle - startAngle > Math.PI;
            
            Point innerStart = new Point(
                centerX + innerRadius * Math.Cos(startAngle),
                centerY + innerRadius * Math.Sin(startAngle));
            
            Point innerEnd = new Point(
                centerX + innerRadius * Math.Cos(endAngle),
                centerY + innerRadius * Math.Sin(endAngle));
            
            Point outerStart = new Point(
                centerX + outerRadius * Math.Cos(startAngle),
                centerY + outerRadius * Math.Sin(startAngle));
            
            Point outerEnd = new Point(
                centerX + outerRadius * Math.Cos(endAngle),
                centerY + outerRadius * Math.Sin(endAngle));
            
            var geometry = new PathGeometry();
            var figure = new PathFigure { IsClosed = true, StartPoint = innerStart };
            
            figure.Segments.Add(new LineSegment(outerStart, true));
            figure.Segments.Add(new ArcSegment(
                outerEnd,
                new Size(outerRadius, outerRadius),
                0,
                isLargeArc,
                SweepDirection.Clockwise,
                true));
            
            figure.Segments.Add(new LineSegment(innerEnd, true));
            figure.Segments.Add(new ArcSegment(
                innerStart,
                new Size(innerRadius, innerRadius),
                0,
                isLargeArc,
                SweepDirection.Counterclockwise,
                true));
            
            geometry.Figures.Add(figure);
            
            return new Path { Data = geometry };
        }

        private void UpdateVolumeAndFrequencyVisualization(float volume, float frequency)
        {
            // Update the UI on the dispatcher thread
            Dispatcher.Invoke(() => 
            {
                // Amplify the volume to make it more sensitive
                float amplifiedVolume = Math.Min((float)Math.Sqrt(volume) * 10.0f, 1.0f);
                
                // Scale the amplified volume to the height of the rectangle
                double newHeight = amplifiedVolume * MAX_HEIGHT;
                
                // Add smoothing for more natural movement
                newHeight = _innerRectangle.Height * 0.3 + newHeight * 0.7;
                
                // Set the new height
                _innerRectangle.Height = newHeight;
                
                // Update the frequency display
                _frequencyDisplay.Text = $"Frequency: {frequency:F0} Hz";
                
                // Reset all segments to normal state
                foreach (var segment in _colorSegments)
                {
                    segment.Opacity = 0.7;
                    segment.Effect = null;
                }
                
                // Highlight the segment based on frequency
                if (frequency > 0)
                {
                    foreach (var segment in _colorSegments)
                    {
                        if (segment.Tag is ValueTuple<Color, string, int, int> colorInfo)
                        {
                            var (_, name, minFreq, maxFreq) = colorInfo;
                            
                            if (frequency >= minFreq && frequency < maxFreq)
                            {
                                // Highlight this segment
                                segment.Opacity = 1.0;
                                segment.Effect = new DropShadowEffect
                                {
                                    Color = Colors.White,
                                    ShadowDepth = 0,
                                    BlurRadius = 15
                                };
                                
                                // Update the frequency range display
                                _colorFreqRangeDisplay.Text = $"Frequency Range: {name} ({minFreq}-{maxFreq} Hz)";
                                break;
                            }
                        }
                    }
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Unsubscribe from volume and frequency updates
            UpdateLoop.Instance.UnsubscribeWithVolumeAndFrequency(UpdateVolumeAndFrequencyVisualization);
            
            // Dispose of the UpdateLoop resources
            UpdateLoop.Instance.Dispose();
        }
    }
}