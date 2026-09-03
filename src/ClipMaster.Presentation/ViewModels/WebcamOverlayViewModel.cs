using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipMaster.Presentation.ViewModels;

public partial class WebcamOverlayViewModel : ObservableObject
{
    [ObservableProperty] private double _positionX = 50;
    [ObservableProperty] private double _positionY = 50;
    [ObservableProperty] private int _size = 200;
    [ObservableProperty] private int _borderThickness = 3;
    [ObservableProperty] private string _borderColor = "#FF00FF00";
    [ObservableProperty] private bool _isVisible = true;
}
