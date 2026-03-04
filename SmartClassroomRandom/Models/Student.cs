using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartClassroomRandom.Models
{
    // Kế thừa ObservableObject để UI tự động cập nhật khi dữ liệu thay đổi
    public partial class Student : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SpeakCount { get; set; } = 0;
        public int AbsenceCount { get; set; } = 0;
        public string Notes { get; set; } = "";

        // Thuộc tính này dùng để báo cho UI biết sinh viên này đang được highlight
        [ObservableProperty]
        private bool _isSelected = false;
    }
}