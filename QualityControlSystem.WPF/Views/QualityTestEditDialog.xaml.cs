using CommunityToolkit.Mvvm.ComponentModel;
using QualityControlSystem.WPF.Dtos;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace QualityControlSystem.WPF.Views;

public partial class QualityTestEditDialog : Window
{
    private const string AllSidesFilter = "Все стороны";

    private readonly QualityTestDto _test;
    private readonly ObservableCollection<SelectableTemplateDto> _templateRows = new();
    private readonly ICollectionView _templateRowsView;

    public QualityTestEditDialog(
        QualityTestDto test,
        IEnumerable<LookupItemDto> frames,
        IEnumerable<TemplateDto> templates,
        bool isEdit)
    {
        InitializeComponent();

        _test = test;
        Title = isEdit ? "Редактирование теста" : "Добавление теста";
        TitleText.Text = Title;

        FrameComboBox.ItemsSource = frames.ToList();

        var templateList = templates.ToList();
        foreach (var template in templateList)
        {
            _templateRows.Add(new SelectableTemplateDto
            {
                Template = template,
                IsSelected = _test.TemplateIds.Contains(template.Id)
            });
        }

        _templateRowsView = CollectionViewSource.GetDefaultView(_templateRows);
        _templateRowsView.Filter = FilterTemplateRow;
        TemplatesDataGrid.ItemsSource = _templateRowsView;

        var sides = templateList
            .Select(template => template.Side)
            .Where(side => !string.IsNullOrWhiteSpace(side))
            .Distinct()
            .OrderBy(side => side)
            .Prepend(AllSidesFilter)
            .ToList();
        TemplateSideFilterComboBox.ItemsSource = sides;
        TemplateSideFilterComboBox.SelectedItem = AllSidesFilter;

        NameTextBox.Text = _test.Name;
        DescriptionTextBox.Text = _test.Description;
        FrameComboBox.SelectedValue = _test.FrameId > 0 ? _test.FrameId : null;
    }

    private void TemplateFilter_Changed(object sender, RoutedEventArgs e)
    {
        _templateRowsView.Refresh();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ShowValidationMessage("Укажите название теста.");
            NameTextBox.Focus();
            return;
        }

        if (FrameComboBox.SelectedValue is not int frameId)
        {
            ShowValidationMessage("Выберите модель каркаса.");
            FrameComboBox.Focus();
            return;
        }

        var templateIds = _templateRows
            .Where(row => row.IsSelected)
            .Select(row => row.Template.Id)
            .ToList();

        if (templateIds.Count == 0)
        {
            ShowValidationMessage("Привяжите хотя бы один шаблон.");
            TemplatesDataGrid.Focus();
            return;
        }

        _test.Name = NameTextBox.Text.Trim();
        _test.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim();
        _test.FrameId = frameId;
        _test.TemplateIds = templateIds;

        DialogResult = true;
    }

    private bool FilterTemplateRow(object item)
    {
        if (item is not SelectableTemplateDto row)
            return false;

        var search = TemplateSearchTextBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(search)
            && !row.Template.Name.Contains(search, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TemplateSideFilterComboBox.SelectedItem is string side
            && side != AllSidesFilter
            && !string.Equals(row.Template.Side, side, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static void ShowValidationMessage(string message)
    {
        MessageBox.Show(message, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private partial class SelectableTemplateDto : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public TemplateDto Template { get; set; } = new();
    }
}
