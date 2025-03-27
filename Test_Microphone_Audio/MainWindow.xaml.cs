using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Timer = System.Timers.Timer;

namespace Test_Microphone_Audio;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Rectangle _innerRectangle;
    private Ellipse _circle;
    private TextBlock _frequencyDisplay;
    private Color _lastColor = Colors.Blue;
    private const int MAX_HEIGHT = 300;
    
    public MainWindow()
    {
        InitializeComponent();

        var canvas = new Canvas();
        Content = canvas;

        var title = new TextBlock
        {
            Text = "Hieronder staat een cirkel die van kleur moet veranderen met toonhoogte en \n een balk die meer of minder gevuld wordt aan de hand van volume",
            FontSize = 20,
            Margin = new Thickness(10, 10, 10, 10)
        };
        canvas.Children.Add(title);

        // Add frequency display
        _frequencyDisplay = new TextBlock
        {
            Text = "Frequency: 0 Hz",
            FontSize = 16,
            Margin = new Thickness(10, 70, 10, 10)
        };
        canvas.Children.Add(_frequencyDisplay);

        _circle = new Ellipse { Width = 100, Height = 100, Fill = Brushes.Blue };
        Canvas.SetTop(_circle, 150);
        Canvas.SetLeft(_circle, 250);
        canvas.Children.Add(_circle);

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

        _innerRectangle = new Rectangle
        {
            Width = 30,
            Height = 0, // Start at zero height
            Fill = Brushes.Blue
        };
        Canvas.SetRight(_innerRectangle, 20);
        Canvas.SetBottom(_innerRectangle, 50); // Positioning it on top of the outerRectangle
        canvas.Children.Add(_innerRectangle);

        // Subscribe to volume and frequency updates
        UpdateLoop.Instance.SubscribeWithVolumeAndFrequency(UpdateVolumeAndFrequencyVisualization);
    }

    private void UpdateVolumeAndFrequencyVisualization(float volume, float frequency)
    {
        // Update the UI on the dispatcher thread
        Dispatcher.Invoke(() => 
        {
            // Amplify the volume to make it more sensitive, with a maximum of 1.0
            // Using a higher multiplier (10.0) and taking the square root for more linear sensitivity
            float amplifiedVolume = Math.Min((float)Math.Sqrt(volume) * 10.0f, 1.0f);
            
            // Scale the amplified volume to the height of the rectangle
            double newHeight = amplifiedVolume * MAX_HEIGHT;
            
            // Add smoothing for more natural movement
            newHeight = _innerRectangle.Height * 0.3 + newHeight * 0.7;
            
            // Set the new height
            _innerRectangle.Height = newHeight;
            
            // Update the frequency display - always display the frequency (held or current)
            _frequencyDisplay.Text = $"Frequency: {frequency:F0} Hz";
            
            // Update circle color based on frequency if we have a valid frequency
            if (frequency > 0)
            {
                // Map frequency to color (20Hz-4000Hz)
                // Low frequencies = red, high frequencies = blue
                float normalizedFreq = Math.Min(Math.Max(frequency - 20, 0) / 3980, 1.0f);
                
                byte red = (byte)(255 * (1 - normalizedFreq));
                byte blue = (byte)(255 * normalizedFreq);
                
                _lastColor = Color.FromRgb(red, 0, blue);
                _circle.Fill = new SolidColorBrush(_lastColor);
            }
            // The color is already held by keeping _lastColor
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