using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Test_Microphone_Audio;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
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

        var circle = new Ellipse { Width = 100, Height = 100, Fill = Brushes.Red };
        Canvas.SetTop(circle, 150);
        Canvas.SetLeft(circle, 250);
        canvas.Children.Add(circle);

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

        var innerRectangle = new Rectangle
        {
            Width = 30,
            Height = 100,
            Fill = Brushes.Blue
        };
        Canvas.SetRight(innerRectangle, 20);
        Canvas.SetBottom(innerRectangle, 50); // Positioning it on top of the outerRectangle
        canvas.Children.Add(innerRectangle);
        

    }
}