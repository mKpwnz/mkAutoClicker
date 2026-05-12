using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace mkAutoClicker.Components;

public partial class MkBadge : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(MkBadge),
        new PropertyMetadata("Label:"));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(MkBadge),
        new PropertyMetadata("Value"));

    public static readonly DependencyProperty ValueForegroundProperty = DependencyProperty.Register(
        nameof(ValueForeground),
        typeof(Brush),
        typeof(MkBadge),
        new PropertyMetadata(Brushes.White));

    public string Label
    {
        get => (string)this.GetValue(LabelProperty);
        set => this.SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)this.GetValue(ValueProperty);
        set => this.SetValue(ValueProperty, value);
    }

    public Brush ValueForeground
    {
        get => (Brush)this.GetValue(ValueForegroundProperty);
        set => this.SetValue(ValueForegroundProperty, value);
    }

    public MkBadge()
    {
        this.InitializeComponent();
    }
}
