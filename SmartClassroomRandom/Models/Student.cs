using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartClassroomRandom.Models
{
    public partial class Student : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // 4 chỉ số hiển thị trên Card
        public int PhatBieu { get; set; } = 0;
        public int KhongTraLoiDuoc { get; set; } = 0;
        public int KhongDiHoc { get; set; } = 0;
        public string GhiChu { get; set; } = "";

        // Nếu muốn một vài card có màu đỏ như bạn "Trần Việt Hiếu" trong ảnh, 
        // ta có thể dùng biến này để binding màu (ví dụ: điểm kém thì thẻ màu đỏ)
        [ObservableProperty]
        private bool _isWarning = false;
        public int DiemCong { get; set; } = 0; // Để tìm người Best
        [ObservableProperty] private string _cardColor = "#4A90E2"; // Màu nền của thẻ (đổi động)
    }
}