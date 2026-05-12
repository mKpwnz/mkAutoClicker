using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace mkAutoClicker.Components;

public partial class MkTextBox : UserControl {
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MkTextBox),
        new FrameworkPropertyMetadata(
            "Text",
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnTextPropertyChanged));

    private bool _isFocused;
    private bool _isHovered;

    private bool _isSynchronizingText;

    public MkTextBox() {
        InitializeComponent();
        InnerTextBox.TextChanged += InnerTextBox_TextChanged;
        MouseEnter += Root_MouseEnter;
        MouseLeave += Root_MouseLeave;
        InnerTextBox.GotKeyboardFocus += InnerTextBox_GotKeyboardFocus;
        InnerTextBox.LostKeyboardFocus += InnerTextBox_LostKeyboardFocus;

        if (DesignerProperties.GetIsInDesignMode(this)) Text = "Text";

        SynchronizeInnerText(Text);
        ApplyBorderState();
    }

    public string Text {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public event TextChangedEventHandler? TextChanged;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);

        if (e.Property == IsEnabledProperty) {
            InnerTextBox.IsEnabled = IsEnabled;
            ApplyBorderState();
        }
    }

    private void InnerTextBox_TextChanged(object sender, TextChangedEventArgs e) {
        _ = sender;

        if (_isSynchronizingText) return;

        _isSynchronizingText = true;
        SetCurrentValue(TextProperty, InnerTextBox.Text);
        _isSynchronizingText = false;
        TextChanged?.Invoke(this, e);
    }

    private static void OnTextPropertyChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args) {
        if (dependencyObject is MkTextBox control) {
            var text = args.NewValue as string ?? string.Empty;
            control.SynchronizeInnerText(text);
        }
    }

    private void SynchronizeInnerText(string text) {
        if (_isSynchronizingText) return;

        var safeText = text ?? string.Empty;
        if (InnerTextBox.Text == safeText) return;

        _isSynchronizingText = true;
        InnerTextBox.Text = safeText;
        _isSynchronizingText = false;
    }

    private void Root_MouseEnter(object sender, MouseEventArgs e) {
        _ = sender;
        _ = e;
        _isHovered = true;
        ApplyBorderState();
    }

    private void Root_MouseLeave(object sender, MouseEventArgs e) {
        _ = sender;
        _ = e;
        _isHovered = false;
        ApplyBorderState();
    }

    private void InnerTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
        _ = sender;
        _ = e;
        _isFocused = true;
        ApplyBorderState();
    }

    private void InnerTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
        _ = sender;
        _ = e;
        _isFocused = false;
        ApplyBorderState();
    }

    private void ApplyBorderState() {
        var defaultBorder = TryFindResource("Brush.AppBorder") as Brush ?? Brushes.Gray;
        var accentBorder = TryFindResource("Brush.Accent") as Brush ?? Brushes.DodgerBlue;
        var fixedBorderThickness = new Thickness(1);

        if (!IsEnabled) {
            InputBorder.BorderBrush = defaultBorder;
            InputBorder.BorderThickness = fixedBorderThickness;
            InputBorder.Opacity = 0.55;
            FocusRingBorder.Opacity = 0.0;
            return;
        }

        InputBorder.Opacity = 1.0;
        InputBorder.BorderThickness = fixedBorderThickness;

        if (_isHovered || _isFocused)
            InputBorder.BorderBrush = accentBorder;
        else
            InputBorder.BorderBrush = defaultBorder;

        FocusRingBorder.Opacity = _isFocused ? 0.35 : 0.0;
    }
}