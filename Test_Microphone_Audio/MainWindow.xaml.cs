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
    private bool _grow = true;
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

        _circle = new Ellipse { Width = 100, Height = 100, Fill = Brushes.Red };
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
            Height = 100,
            Fill = Brushes.Blue
        };
        Canvas.SetRight(_innerRectangle, 20);
        Canvas.SetBottom(_innerRectangle, 50); // Positioning it on top of the outerRectangle
        canvas.Children.Add(_innerRectangle);

        UpdateLoop.Instance.Subscribe(SetInnerRectangleSize);
    }

    private void SetInnerRectangleSize()
    {
        // increase to max 100 and decrease to min 0 over time
        var tick = (int)_innerRectangle.Height;
        var max = 300;
        var min = 0;

        if (tick < max && _grow)
        {
            tick++;
            _innerRectangle.Height = tick;
        }
        else if (tick > min && !_grow)
        {
            tick--;
            _innerRectangle.Height = tick;
        }
        else
        {
            _grow = !_grow;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        UpdateLoop.Instance.Unsubscribe(SetInnerRectangleSize);
    }
}