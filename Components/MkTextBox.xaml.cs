using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;

namespace mkAutoClicker.Components;

public partial class MkTextBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MkTextBox),
        new FrameworkPropertyMetadata(
            "Text",
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnTextPropertyChanged));

    private bool isSynchronizingText;
    private bool isHovered;
    private bool isFocused;

    public string Text
    {
        get => (string)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    public event TextChangedEventHandler? TextChanged;

    public MkTextBox()
    {
        this.InitializeComponent();
        this.InnerTextBox.TextChanged += this.InnerTextBox_TextChanged;
        this.MouseEnter += this.Root_MouseEnter;
        this.MouseLeave += this.Root_MouseLeave;
        this.InnerTextBox.GotKeyboardFocus += this.InnerTextBox_GotKeyboardFocus;
        this.InnerTextBox.LostKeyboardFocus += this.InnerTextBox_LostKeyboardFocus;

        if (DesignerProperties.GetIsInDesignMode(this))
        {
            this.Text = "Text";
        }

        this.SynchronizeInnerText(this.Text);
        this.ApplyBorderState();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == IsEnabledProperty)
        {
            this.InnerTextBox.IsEnabled = this.IsEnabled;
            this.ApplyBorderState();
        }
    }

    private void InnerTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _ = sender;

        if (this.isSynchronizingText)
        {
            return;
        }

        this.isSynchronizingText = true;
        this.SetCurrentValue(TextProperty, this.InnerTextBox.Text);
        this.isSynchronizingText = false;
        this.TextChanged?.Invoke(this, e);
    }

    private static void OnTextPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is MkTextBox control)
        {
            string text = args.NewValue as string ?? string.Empty;
            control.SynchronizeInnerText(text);
        }
    }

    private void SynchronizeInnerText(string text)
    {
        if (this.isSynchronizingText)
        {
            return;
        }

        string safeText = text ?? string.Empty;
        if (this.InnerTextBox.Text == safeText)
        {
            return;
        }

        this.isSynchronizingText = true;
        this.InnerTextBox.Text = safeText;
        this.isSynchronizingText = false;
    }

    private void Root_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _ = sender;
        _ = e;
        this.isHovered = true;
        this.ApplyBorderState();
    }

    private void Root_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _ = sender;
        _ = e;
        this.isHovered = false;
        this.ApplyBorderState();
    }

    private void InnerTextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        this.isFocused = true;
        this.ApplyBorderState();
    }

    private void InnerTextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        this.isFocused = false;
        this.ApplyBorderState();
    }

    private void ApplyBorderState()
    {
        Brush defaultBorder = this.TryFindResource("Brush.AppBorder") as Brush ?? Brushes.Gray;
        Brush accentBorder = this.TryFindResource("Brush.Accent") as Brush ?? Brushes.DodgerBlue;
        Thickness fixedBorderThickness = new Thickness(1);

        if (!this.IsEnabled)
        {
            this.InputBorder.BorderBrush = defaultBorder;
            this.InputBorder.BorderThickness = fixedBorderThickness;
            this.InputBorder.Opacity = 0.55;
            this.FocusRingBorder.Opacity = 0.0;
            return;
        }

        this.InputBorder.Opacity = 1.0;
        this.InputBorder.BorderThickness = fixedBorderThickness;

        if (this.isHovered || this.isFocused)
        {
            this.InputBorder.BorderBrush = accentBorder;
        }
        else
        {
            this.InputBorder.BorderBrush = defaultBorder;
        }

        this.FocusRingBorder.Opacity = this.isFocused ? 0.35 : 0.0;
    }
}
