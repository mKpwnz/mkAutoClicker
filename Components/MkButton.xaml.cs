using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace mkAutoClicker.Components;

public enum MkButtonVariant
{
    Default = 0,
    Primary = 1,
    Outline = 2,
    Destructive = 3,
    TitleBar = 4,
    TitleBarClose = 5
}

public partial class MkButton : UserControl
{
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

    public string Text
    {
        get => (string)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    public MkButtonVariant Variant
    {
        get => (MkButtonVariant)this.GetValue(VariantProperty);
        set => this.SetValue(VariantProperty, value);
    }

    public Brush ButtonBackground
    {
        get => (Brush)this.GetValue(ButtonBackgroundProperty);
        set => this.SetValue(ButtonBackgroundProperty, value);
    }

    public Brush ButtonBorderBrush
    {
        get => (Brush)this.GetValue(ButtonBorderBrushProperty);
        set => this.SetValue(ButtonBorderBrushProperty, value);
    }

    public Brush ButtonForeground
    {
        get => (Brush)this.GetValue(ButtonForegroundProperty);
        set => this.SetValue(ButtonForegroundProperty, value);
    }

    public Brush ButtonHoverBackground
    {
        get => (Brush)this.GetValue(ButtonHoverBackgroundProperty);
        set => this.SetValue(ButtonHoverBackgroundProperty, value);
    }

    public Brush ButtonHoverBorderBrush
    {
        get => (Brush)this.GetValue(ButtonHoverBorderBrushProperty);
        set => this.SetValue(ButtonHoverBorderBrushProperty, value);
    }

    public Geometry? IconPathData
    {
        get => this.GetValue(IconPathDataProperty) as Geometry;
        set => this.SetValue(IconPathDataProperty, value);
    }

    public double IconSize
    {
        get => (double)this.GetValue(IconSizeProperty);
        set => this.SetValue(IconSizeProperty, value);
    }

    public double IconStrokeThickness
    {
        get => (double)this.GetValue(IconStrokeThicknessProperty);
        set => this.SetValue(IconStrokeThicknessProperty, value);
    }

    public event RoutedEventHandler? Click;

    public MkButton()
    {
        this.InitializeComponent();
        this.Loaded += this.MkButton_Loaded;
        this.InnerButton.Click += this.InnerButton_Click;
        this.InnerButton.MouseEnter += this.InnerButton_MouseEnter;
        this.InnerButton.MouseLeave += this.InnerButton_MouseLeave;
        this.InnerButton.PreviewMouseLeftButtonUp += this.InnerButton_PreviewMouseLeftButtonUp;
        this.InnerButton.LostMouseCapture += this.InnerButton_LostMouseCapture;

        if (string.IsNullOrWhiteSpace(this.Text))
        {
            this.Text = "Button";
        }
    }

    private void MkButton_Loaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        this.ApplyVariant();
        this.ApplyContentState();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == VariantProperty)
        {
            this.ApplyVariant();
        }
        else if (e.Property == TextProperty || e.Property == IconPathDataProperty)
        {
            this.ApplyContentState();
        }
        else if (e.Property == IsEnabledProperty)
        {
            this.InnerButton.IsEnabled = this.IsEnabled;
            this.ApplyNormalState();
        }
    }

    private void InnerButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        this.Click?.Invoke(this, e);
    }

    private void ApplyVariant()
    {
        Brush accent = this.FindBrush("Brush.Accent", new SolidColorBrush(Color.FromRgb(59, 130, 246)));
        Brush accentHover = this.FindBrush("Brush.AccentHover", new SolidColorBrush(Color.FromRgb(37, 99, 235)));
        Brush accentText = this.FindBrush("Brush.AccentText", Brushes.White);
        Brush border = this.FindBrush("Brush.AppBorder", new SolidColorBrush(Color.FromRgb(64, 64, 70)));
        Brush surface = this.FindBrush("Brush.AppSurfaceSoft", new SolidColorBrush(Color.FromRgb(39, 39, 42)));
        Brush surfaceHover = this.FindBrush("Brush.AppSurfaceElevated", new SolidColorBrush(Color.FromRgb(47, 47, 53)));
        Brush textPrimary = this.FindBrush("Brush.TextPrimary", Brushes.White);
        Brush warning = this.FindBrush("Brush.Warning", new SolidColorBrush(Color.FromRgb(248, 113, 113)));
        Brush warningSoft = this.FindBrush("Brush.WarningSoft", new SolidColorBrush(Color.FromRgb(63, 26, 26)));
        Brush textSecondary = this.FindBrush("Brush.TextSecondary", new SolidColorBrush(Color.FromRgb(212, 212, 216)));

        switch (this.Variant)
        {
            case MkButtonVariant.Primary:
                this.ButtonBackground = accent;
                this.ButtonBorderBrush = accent;
                this.ButtonForeground = accentText;
                this.ButtonHoverBackground = accentHover;
                this.ButtonHoverBorderBrush = accentHover;
                break;
            case MkButtonVariant.Destructive:
                this.ButtonBackground = warningSoft;
                this.ButtonBorderBrush = warning;
                this.ButtonForeground = textPrimary;
                this.ButtonHoverBackground = this.FindBrush("Brush.AccentSoft", warningSoft);
                this.ButtonHoverBorderBrush = warning;
                break;
            case MkButtonVariant.Outline:
                this.ButtonBackground = surface;
                this.ButtonBorderBrush = border;
                this.ButtonForeground = textPrimary;
                this.ButtonHoverBackground = surfaceHover;
                this.ButtonHoverBorderBrush = this.FindBrush("Brush.AppBorderHover", border);
                break;
            case MkButtonVariant.TitleBar:
                this.ButtonBackground = Brushes.Transparent;
                this.ButtonBorderBrush = Brushes.Transparent;
                this.ButtonForeground = textSecondary;
                this.ButtonHoverBackground = surface;
                this.ButtonHoverBorderBrush = border;
                break;
            case MkButtonVariant.TitleBarClose:
                this.ButtonBackground = Brushes.Transparent;
                this.ButtonBorderBrush = Brushes.Transparent;
                this.ButtonForeground = textSecondary;
                this.ButtonHoverBackground = warningSoft;
                this.ButtonHoverBorderBrush = warning;
                break;
            default:
                this.ButtonBackground = accent;
                this.ButtonBorderBrush = accent;
                this.ButtonForeground = accentText;
                this.ButtonHoverBackground = accentHover;
                this.ButtonHoverBorderBrush = accentHover;
                break;
        }

        this.ApplyNormalState();
    }

    private Brush FindBrush(string key, Brush fallback)
    {
        object resource = this.TryFindResource(key) ?? Application.Current.TryFindResource(key) ?? fallback;
        return resource as Brush ?? fallback;
    }

    private void InnerButton_MouseEnter(object sender, MouseEventArgs e)
    {
        _ = sender;
        _ = e;

        this.InnerButton.Background = this.ButtonHoverBackground;
        this.InnerButton.BorderBrush = this.ButtonHoverBorderBrush;
    }

    private void InnerButton_MouseLeave(object sender, MouseEventArgs e)
    {
        _ = sender;
        _ = e;
        this.ApplyNormalState();
    }

    private void InnerButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _ = sender;
        _ = e;

        if (this.InnerButton.IsMouseOver)
        {
            this.InnerButton.Background = this.ButtonHoverBackground;
            this.InnerButton.BorderBrush = this.ButtonHoverBorderBrush;
        }
    }

    private void InnerButton_LostMouseCapture(object sender, MouseEventArgs e)
    {
        _ = sender;
        _ = e;
        this.ApplyNormalState();
    }

    private void ApplyNormalState()
    {
        this.InnerButton.Background = this.ButtonBackground;
        this.InnerButton.BorderBrush = this.ButtonBorderBrush;
        this.InnerButton.Foreground = this.ButtonForeground;
        this.TextElement.Foreground = this.ButtonForeground;
        this.IconPathElement.Stroke = this.ButtonForeground;
    }

    private void ApplyContentState()
    {
        bool hasIcon = this.IconPathData is not null;
        bool hasText = !string.IsNullOrWhiteSpace(this.Text);

        this.IconPathElement.Visibility = hasIcon ? Visibility.Visible : Visibility.Collapsed;
        this.TextElement.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

        if (!hasIcon && !hasText)
        {
            this.TextElement.Visibility = Visibility.Visible;
        }
    }
}
