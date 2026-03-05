using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartClassroomRandom.Models
{
    public partial class BombWire : ObservableObject
    {
        [ObservableProperty] private string _wireColor = "#FFFFFF";
        [ObservableProperty] private string _wireName = "Dây";

        [ObservableProperty] private bool _isCut = false;
        [ObservableProperty] private bool _isBomb = false;
        [ObservableProperty] private bool _isSafe = false;
    }
}