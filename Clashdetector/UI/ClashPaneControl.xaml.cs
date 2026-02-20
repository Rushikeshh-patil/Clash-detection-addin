using System.Windows;
using System.Windows.Controls;
using Clashdetector.Revit;
using Microsoft.Win32;

namespace Clashdetector.UI;

public partial class ClashPaneControl : UserControl
{
    private readonly ClashPaneViewModel _viewModel;
    private readonly ClashAddinContext _context;

    public ClashPaneControl(ClashPaneViewModel viewModel, ClashAddinContext context)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _context = context;
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _context.QueueRequest(new RevitRequest
        {
            Type = RevitRequestType.RefreshCatalogs,
        });
    }

    private void RunNowButton_Click(object sender, RoutedEventArgs e)
    {
        _context.QueueRequest(new RevitRequest
        {
            Type = RevitRequestType.RunManual,
        });
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveSettings();
        _viewModel.SetStatus("Saved settings.");
    }

    private void RefreshCatalogsButton_Click(object sender, RoutedEventArgs e)
    {
        _context.QueueRequest(new RevitRequest
        {
            Type = RevitRequestType.RefreshCatalogs,
        });
    }

    private void NewConfigButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CreateConfig();
    }

    private void DuplicateConfigButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DuplicateSelectedConfig();
    }

    private void DeleteConfigButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DeleteSelectedConfig();
    }

    private void AddCategoryPairButton_Click(object sender, RoutedEventArgs e)
    {
        var categoryA = CategoryACombo.SelectedItem?.ToString() ?? string.Empty;
        var categoryB = CategoryBCombo.SelectedItem?.ToString() ?? string.Empty;
        _viewModel.AddCategoryPair(categoryA, categoryB);
    }

    private void RemoveCategoryPairButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RemoveSelectedCategoryPair();
    }

    private void FocusButton_Click(object sender, RoutedEventArgs e)
    {
        _context.QueueRequest(new RevitRequest
        {
            Type = RevitRequestType.FocusSelected,
        });
    }

    private void IsolateButton_Click(object sender, RoutedEventArgs e)
    {
        _context.QueueRequest(new RevitRequest
        {
            Type = RevitRequestType.IsolateSelected,
        });
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Results.Count == 0)
        {
            _viewModel.SetStatus("No clash results available to export.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export Clash Results",
            Filter = "CSV File (*.csv)|*.csv",
            AddExtension = true,
            FileName = $"clash_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportResults(dialog.FileName);
        }
    }

    private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearResults();
    }
}
