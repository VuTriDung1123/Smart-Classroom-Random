using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartClassroomRandom.Models
{
    public partial class MatrixCard : ObservableObject
    {
        [ObservableProperty] private int _cardNumber; // Số thứ tự hiện trên mặt úp
        [ObservableProperty] private bool _isFlipped = false; // Trạng thái lật

        [ObservableProperty] private string _cardType = ""; // Normal, Safe, Bonus, Bomb
        [ObservableProperty] private string _cardTitle = "";
        [ObservableProperty] private string _cardIcon = "";
        [ObservableProperty] private string _cardColor = "#1E1E24";
        [ObservableProperty] private string _ttsMessage = "";
    }
}