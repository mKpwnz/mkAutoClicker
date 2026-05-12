using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace mkAutoClicker.Components;

public partial class MkSelectField : UserControl {
    public MkSelectField() {
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this)) {
            InnerComboBox.ItemsSource = new[] { "Option 1", "Option 2", "Option 3" };
            InnerComboBox.SelectedIndex = 0;
        }
    }

    public IEnumerable? ItemsSource {
        get => InnerComboBox.ItemsSource;
        set => InnerComboBox.ItemsSource = value;
    }

    public string DisplayMemberPath {
        get => InnerComboBox.DisplayMemberPath;
        set => InnerComboBox.DisplayMemberPath = value;
    }

    public object? SelectedItem {
        get => InnerComboBox.SelectedItem;
        set => InnerComboBox.SelectedItem = value;
    }

    public int SelectedIndex {
        get => InnerComboBox.SelectedIndex;
        set => InnerComboBox.SelectedIndex = value;
    }

    public event SelectionChangedEventHandler SelectionChanged {
        add => InnerComboBox.SelectionChanged += value;
        remove => InnerComboBox.SelectionChanged -= value;
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);

        if (e.Property == IsEnabledProperty) InnerComboBox.IsEnabled = IsEnabled;
    }
}