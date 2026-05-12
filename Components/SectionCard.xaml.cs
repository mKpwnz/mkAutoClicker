using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
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

    public string Title {
        get => (string)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    public string Description {
        get => (string)this.GetValue(DescriptionProperty);
        set => this.SetValue(DescriptionProperty, value);
    }

    public object? CardContent {
        get => this.GetValue(CardContentProperty);
        set => this.SetValue(CardContentProperty, value);
    }

    public SectionCard() {
        this.InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this)) {
            this.Title = "Section";
            this.Description = "Beschreibung";
        }
    }
}
