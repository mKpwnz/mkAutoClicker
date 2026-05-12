using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace mkAutoClicker.Components;

[ContentProperty(nameof(CardContent))]
public partial class SectionCard : UserControl {
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(SectionCard),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(SectionCard),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CardContentProperty = DependencyProperty.Register(
        nameof(CardContent),
        typeof(object),
        typeof(SectionCard),
        new PropertyMetadata(null));


    public SectionCard() {
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this)) {
            Title = "Section";
            Description = "Description";
        }
    }

    public string Title {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? CardContent {
        get => GetValue(CardContentProperty);
        set => SetValue(CardContentProperty, value);
    }
}