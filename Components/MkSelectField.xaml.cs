using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace mkAutoClicker.Components;

public partial class MkSelectField : UserControl {
    public IEnumerable? ItemsSource {
        get => this.InnerComboBox.ItemsSource;
        set => this.InnerComboBox.ItemsSource = value;
    }

    public string DisplayMemberPath {
        get => this.InnerComboBox.DisplayMemberPath;
        set => this.InnerComboBox.DisplayMemberPath = value;
    }

    public object? SelectedItem {
        get => this.InnerComboBox.SelectedItem;
        set => this.InnerComboBox.SelectedItem = value;
    }

    public int SelectedIndex {
        get => this.InnerComboBox.SelectedIndex;
        set => this.InnerComboBox.SelectedIndex = value;
    }

    public event SelectionChangedEventHandler SelectionChanged {
        add => this.InnerComboBox.SelectionChanged += value;
        remove => this.InnerComboBox.SelectionChanged -= value;
    }

    public MkSelectField() {
        this.InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this)) {
            this.InnerComboBox.ItemsSource = new[] { "Option 1", "Option 2", "Option 3" };
            this.InnerComboBox.SelectedIndex = 0;
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);

        if (e.Property == IsEnabledProperty) {
            this.InnerComboBox.IsEnabled = this.IsEnabled;
        }
    }
}