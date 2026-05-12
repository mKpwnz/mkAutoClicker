using System.Windows;
using System.Windows.Controls;

namespace mkAutoClicker.Components;

public partial class MkCheckBox : UserControl {
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

    private bool _isSynchronizing;

    public MkCheckBox() {
        InitializeComponent();
        InnerCheckBox.Checked += InnerCheckBox_Checked;
        InnerCheckBox.Unchecked += InnerCheckBox_Unchecked;

        if (string.IsNullOrWhiteSpace(Text)) Text = "Enable";

        SynchronizeInnerCheckState(IsChecked);
    }

    public string Text {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool? IsChecked {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public event RoutedEventHandler? Checked;

    public event RoutedEventHandler? Unchecked;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);

        if (e.Property == IsEnabledProperty) InnerCheckBox.IsEnabled = IsEnabled;
    }

    private void InnerCheckBox_Checked(object sender, RoutedEventArgs e) {
        _ = sender;
        UpdateCheckState(true);
        Checked?.Invoke(this, e);
    }

    private void InnerCheckBox_Unchecked(object sender, RoutedEventArgs e) {
        _ = sender;
        UpdateCheckState(false);
        Unchecked?.Invoke(this, e);
    }

    private void UpdateCheckState(bool value) {
        if (_isSynchronizing) return;

        _isSynchronizing = true;
        SetCurrentValue(IsCheckedProperty, value);
        _isSynchronizing = false;
    }

    private void SynchronizeInnerCheckState(bool? value) {
        if (_isSynchronizing) return;

        var normalizedValue = value == true;
        if (InnerCheckBox.IsChecked == normalizedValue) return;

        _isSynchronizing = true;
        InnerCheckBox.IsChecked = normalizedValue;
        _isSynchronizing = false;
    }

    private static void OnIsCheckedPropertyChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args) {
        if (dependencyObject is MkCheckBox control) {
            bool? isChecked = args.NewValue is bool value ? value : null;
            control.SynchronizeInnerCheckState(isChecked);
        }
    }
}