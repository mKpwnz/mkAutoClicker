using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace mkAutoClicker.Components;

public enum MkButtonVariant {
    Default = 0,
    Primary = 1,
    Outline = 2,
    Destructive = 3,
    TitleBar = 4,
    TitleBarClose = 5
}

public partial class MkButton : UserControl {
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MkButton),
        new PropertyMetadata("Button"));

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant),
        typeof(MkButtonVariant),
        typeof(MkButton),
        new PropertyMetadata(MkButtonVariant.Default));

    public static readonly DependencyProperty ButtonBackgroundProperty = DependencyProperty.Register(
        nameof(ButtonBackground),
        typeof(Brush),
        typeof(MkButton),
        new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty ButtonBorderBrushProperty = DependencyProperty.Register(
        nameof(ButtonBorderBrush),
        typeof(Brush),
        typeof(MkButton),
        new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty ButtonForegroundProperty = DependencyProperty.Register(
        nameof(ButtonForeground),
        typeof(Brush),
        typeof(MkButton),
        new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty ButtonHoverBackgroundProperty = DependencyProperty.Register(
        nameof(ButtonHoverBackground),
        typeof(Brush),
        typeof(MkButton),
        new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty ButtonHoverBorderBrushProperty = DependencyProperty.Register(
        nameof(ButtonHoverBorderBrush),
        typeof(Brush),
        typeof(MkButton),
        new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty IconPathDataProperty = DependencyProperty.Register(
        nameof(IconPathData),
        typeof(Geometry),
        typeof(MkButton),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize),
        typeof(double),
        typeof(MkButton),
        new PropertyMetadata(14.0));

    public static readonly DependencyProperty IconStrokeThicknessProperty = DependencyProperty.Register(
        nameof(IconStrokeThickness),
        typeof(double),
        typeof(MkButton),
        new PropertyMetadata(1.5));

    public MkButton() {
        InitializeComponent();
        Loaded += MkButton_Loaded;
        InnerButton.Click += InnerButton_Click;
        InnerButton.MouseEnter += InnerButton_MouseEnter;
        InnerButton.MouseLeave += InnerButton_MouseLeave;
        InnerButton.PreviewMouseLeftButtonUp += InnerButton_PreviewMouseLeftButtonUp;
        InnerButton.LostMouseCapture += InnerButton_LostMouseCapture;

        if (string.IsNullOrWhiteSpace(Text)) Text = "Button";
    }

    public string Text {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MkButtonVariant Variant {
        get => (MkButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public Brush ButtonBackground {
        get => (Brush)GetValue(ButtonBackgroundProperty);
        set => SetValue(ButtonBackgroundProperty, value);
    }

    public Brush ButtonBorderBrush {
        get => (Brush)GetValue(ButtonBorderBrushProperty);
        set => SetValue(ButtonBorderBrushProperty, value);
    }

    public Brush ButtonForeground {
        get => (Brush)GetValue(ButtonForegroundProperty);
        set => SetValue(ButtonForegroundProperty, value);
    }

    public Brush ButtonHoverBackground {
        get => (Brush)GetValue(ButtonHoverBackgroundProperty);
        set => SetValue(ButtonHoverBackgroundProperty, value);
    }

    public Brush ButtonHoverBorderBrush {
        get => (Brush)GetValue(ButtonHoverBorderBrushProperty);
        set => SetValue(ButtonHoverBorderBrushProperty, value);
    }

    public Geometry? IconPathData {
        get => GetValue(IconPathDataProperty) as Geometry;
        set => SetValue(IconPathDataProperty, value);
    }

    public double IconSize {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public double IconStrokeThickness {
        get => (double)GetValue(IconStrokeThicknessProperty);
        set => SetValue(IconStrokeThicknessProperty, value);
    }

    public event RoutedEventHandler? Click;

    private void MkButton_Loaded(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        ApplyVariant();
        ApplyContentState();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);

        if (e.Property == VariantProperty) {
            ApplyVariant();
        } else if (e.Property == TextProperty || e.Property == IconPathDataProperty) {
            ApplyContentState();
        } else if (e.Property == IsEnabledProperty) {
            InnerButton.IsEnabled = IsEnabled;
            ApplyNormalState();
        }
    }

    private void InnerButton_Click(object sender, RoutedEventArgs e) {
        _ = sender;
        Click?.Invoke(this, e);
    }

    private void ApplyVariant() {
        var accent = FindBrush("Brush.Accent", new SolidColorBrush(Color.FromRgb(59, 130, 246)));
        var accentHover = FindBrush("Brush.AccentHover", new SolidColorBrush(Color.FromRgb(37, 99, 235)));
        var accentText = FindBrush("Brush.AccentText", Brushes.White);
        var border = FindBrush("Brush.AppBorder", new SolidColorBrush(Color.FromRgb(64, 64, 70)));
        var surface = FindBrush("Brush.AppSurfaceSoft", new SolidColorBrush(Color.FromRgb(39, 39, 42)));
        var surfaceHover = FindBrush("Brush.AppSurfaceElevated", new SolidColorBrush(Color.FromRgb(47, 47, 53)));
        var textPrimary = FindBrush("Brush.TextPrimary", Brushes.White);
        var warning = FindBrush("Brush.Warning", new SolidColorBrush(Color.FromRgb(248, 113, 113)));
        var warningSoft = FindBrush("Brush.WarningSoft", new SolidColorBrush(Color.FromRgb(63, 26, 26)));
        var textSecondary = FindBrush("Brush.TextSecondary", new SolidColorBrush(Color.FromRgb(212, 212, 216)));

        switch (Variant) {
            case MkButtonVariant.Primary:
                ButtonBackground = accent;
                ButtonBorderBrush = accent;
                ButtonForeground = accentText;
                ButtonHoverBackground = accentHover;
                ButtonHoverBorderBrush = accentHover;
                break;
            case MkButtonVariant.Destructive:
                ButtonBackground = warningSoft;
                ButtonBorderBrush = warning;
                ButtonForeground = textPrimary;
                ButtonHoverBackground = FindBrush("Brush.AccentSoft", warningSoft);
                ButtonHoverBorderBrush = warning;
                break;
            case MkButtonVariant.Outline:
                ButtonBackground = surface;
                ButtonBorderBrush = border;
                ButtonForeground = textPrimary;
                ButtonHoverBackground = surfaceHover;
                ButtonHoverBorderBrush = FindBrush("Brush.AppBorderHover", border);
                break;
            case MkButtonVariant.TitleBar:
                ButtonBackground = Brushes.Transparent;
                ButtonBorderBrush = Brushes.Transparent;
                ButtonForeground = textSecondary;
                ButtonHoverBackground = surface;
                ButtonHoverBorderBrush = border;
                break;
            case MkButtonVariant.TitleBarClose:
                ButtonBackground = Brushes.Transparent;
                ButtonBorderBrush = Brushes.Transparent;
                ButtonForeground = textSecondary;
                ButtonHoverBackground = warningSoft;
                ButtonHoverBorderBrush = warning;
                break;
            default:
                ButtonBackground = accent;
                ButtonBorderBrush = accent;
                ButtonForeground = accentText;
                ButtonHoverBackground = accentHover;
                ButtonHoverBorderBrush = accentHover;
                break;
        }

        ApplyNormalState();
    }

    private Brush FindBrush(string key, Brush fallback) {
        var resource = TryFindResource(key) ?? Application.Current.TryFindResource(key) ?? fallback;
        return resource as Brush ?? fallback;
    }

    private void InnerButton_MouseEnter(object sender, MouseEventArgs e) {
        _ = sender;
        _ = e;

        InnerButton.Background = ButtonHoverBackground;
        InnerButton.BorderBrush = ButtonHoverBorderBrush;
    }

    private void InnerButton_MouseLeave(object sender, MouseEventArgs e) {
        _ = sender;
        _ = e;
        ApplyNormalState();
    }

    private void InnerButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        _ = sender;
        _ = e;

        if (InnerButton.IsMouseOver) {
            InnerButton.Background = ButtonHoverBackground;
            InnerButton.BorderBrush = ButtonHoverBorderBrush;
        }
    }

    private void InnerButton_LostMouseCapture(object sender, MouseEventArgs e) {
        _ = sender;
        _ = e;
        ApplyNormalState();
    }

    private void ApplyNormalState() {
        InnerButton.Background = ButtonBackground;
        InnerButton.BorderBrush = ButtonBorderBrush;
        InnerButton.Foreground = ButtonForeground;
        TextElement.Foreground = ButtonForeground;
        IconPathElement.Stroke = ButtonForeground;
    }

    private void ApplyContentState() {
        var hasIcon = IconPathData is not null;
        var hasText = !string.IsNullOrWhiteSpace(Text);

        IconPathElement.Visibility = hasIcon ? Visibility.Visible : Visibility.Collapsed;
        TextElement.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

        if (!hasIcon && !hasText) TextElement.Visibility = Visibility.Visible;
    }
}