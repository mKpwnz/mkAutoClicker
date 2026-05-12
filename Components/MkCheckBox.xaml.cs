using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace mkAutoClicker.Components;

public partial class MkCheckBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MkCheckBox),
        new PropertyMetadata("Enable"));

    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
        nameof(IsChecked),
        typeof(bool?),
        typeof(MkCheckBox),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnIsCheckedPropertyChanged));

    private bool isSynchronizing;

    public string Text
    {
        get => (string)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    public bool? IsChecked
    {
        get => (bool?)this.GetValue(IsCheckedProperty);
        set => this.SetValue(IsCheckedProperty, value);
    }

    public event RoutedEventHandler? Checked;

    public event RoutedEventHandler? Unchecked;

    public MkCheckBox()
    {
        this.InitializeComponent();
        this.InnerCheckBox.Checked += this.InnerCheckBox_Checked;
        this.InnerCheckBox.Unchecked += this.InnerCheckBox_Unchecked;

        if (string.IsNullOrWhiteSpace(this.Text))
        {
            this.Text = "Enable";
        }

        this.SynchronizeInnerCheckState(this.IsChecked);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == IsEnabledProperty)
        {
            this.InnerCheckBox.IsEnabled = this.IsEnabled;
        }
    }

    private void InnerCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        this.UpdateCheckState(true);
        this.Checked?.Invoke(this, e);
    }

    private void InnerCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        _ = sender;
        this.UpdateCheckState(false);
        this.Unchecked?.Invoke(this, e);
    }

    private void UpdateCheckState(bool value)
    {
        if (this.isSynchronizing)
        {
            return;
        }

        this.isSynchronizing = true;
        this.SetCurrentValue(IsCheckedProperty, value);
        this.isSynchronizing = false;
    }

    private void SynchronizeInnerCheckState(bool? value)
    {
        if (this.isSynchronizing)
        {
            return;
        }

        bool normalizedValue = value == true;
        if (this.InnerCheckBox.IsChecked == normalizedValue)
        {
            return;
        }

        this.isSynchronizing = true;
        this.InnerCheckBox.IsChecked = normalizedValue;
        this.isSynchronizing = false;
    }

    private static void OnIsCheckedPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is MkCheckBox control)
        {
            bool? isChecked = args.NewValue is bool value ? value : null;
            control.SynchronizeInnerCheckState(isChecked);
        }
    }
}
