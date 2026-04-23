using System.Windows;
using System.Windows.Controls;
using AIFractureDetection.App.Models;
using AIFractureDetection.App.ViewModels;

namespace AIFractureDetection.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Drop zone aktif stiline geçmek için kullanılan DependencyProperty
    public static readonly DependencyProperty IsDraggingOverProperty =
        DependencyProperty.Register(nameof(IsDraggingOver), typeof(bool), typeof(MainWindow),
            new PropertyMetadata(false));

    public bool IsDraggingOver
    {
        get => (bool)GetValue(IsDraggingOverProperty);
        set => SetValue(IsDraggingOverProperty, value);
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        if (IsValidDrop(e))
        {
            e.Effects = DragDropEffects.Copy;
            IsDraggingOver = true;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_DragLeave(object sender, DragEventArgs e)
    {
        IsDraggingOver = false;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        IsDraggingOver = false;
        if (!IsValidDrop(e)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var first = Array.Find(files, f => MainViewModel.IsValidNifti(f));
        if (first is null) return;
        if (DataContext is MainViewModel vm)
        {
            await vm.LoadFileAsync(first);
        }
    }

    private static bool IsValidDrop(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        return Array.Exists(files, f => MainViewModel.IsValidNifti(f));
    }

    private void OrientationCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is not ComboBox cb) return;
        vm.Orientation = cb.SelectedIndex switch
        {
            1 => SliceOrientation.Coronal,
            2 => SliceOrientation.Sagittal,
            _ => SliceOrientation.Axial
        };
    }
}
