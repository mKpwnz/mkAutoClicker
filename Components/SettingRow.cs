using System.Windows;
using System.Windows.Controls;

namespace mkAutoClicker.Components;

public class SettingRow : ContentControl {
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(SettingRow),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(SettingRow),
        new PropertyMetadata(string.Empty));

    static SettingRow() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SettingRow),
            new FrameworkPropertyMetadata(typeof(SettingRow)));
    }

    public string Label {
        get => (string)this.GetValue(LabelProperty);
        set => this.SetValue(LabelProperty, value);
    }

    public string Description {
        get => (string)this.GetValue(DescriptionProperty);
        set => this.SetValue(DescriptionProperty, value);
    }
}
