using CommunityToolkit.Mvvm.ComponentModel;

namespace QualityControlSystem.WPF.Dtos;

public partial class SelectableLookupItemDto : ObservableObject
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
