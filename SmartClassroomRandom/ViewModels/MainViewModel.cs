using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SmartClassroomRandom.Models;
using SmartClassroomRandom.Services;
using SmartClassroomRandom.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data;
using System.Threading.Tasks;

namespace SmartClassroomRandom.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // ================= 1. CÁC BIẾN TOÀN CỤC (TOP BAR) =================
        [ObservableProperty] private int _totalStudents = 0;
        [ObservableProperty] private int _attendCount = 0;
        [ObservableProperty] private int _bannedCount = 0;
        [ObservableProperty] private string _currentFileName = "Chưa tải file";
        [ObservableProperty] private DataTable _excelData = new DataTable();
        [ObservableProperty] private string _bestStudentName = "Chưa có";
        [ObservableProperty] private string _bestStudentPoints = "0";

        // Biến này quyết định xem màn hình bên phải đang hiển thị trang nào
        [ObservableProperty] private object? _currentView;

        // Danh sách gốc (Tất cả sinh viên từ Excel)
        [ObservableProperty] private ObservableCollection<Student> _students = new();


        // ================= BIẾN CHO PHẦN GIỌNG NÓI =================
        // Dữ liệu cho 2 ComboBox
        public List<string> AudienceList { get; } = new List<string> { "sinh viên", "học sinh" };
        public List<string> ActionList { get; } = new List<string> { "trả bài", "trả lời", "kiểm tra bài tập về nhà" };

        [ObservableProperty] private string _selectedAudience = "sinh viên";
        [ObservableProperty] private string _selectedAction = "trả bài";

        // ================= 2. CÁC BIẾN CHO TỪNG TRANG =================
        // Trang Random 1 người
        [ObservableProperty] private Student? _selectedSingleStudent;

        // CÁC BIẾN CHO GACHA NHÂN PHẨM
        [ObservableProperty] private string _gachaTitle = string.Empty;
        [ObservableProperty] private string _gachaDescription = string.Empty;
        [ObservableProperty] private string _gachaColor = "Transparent";
        [ObservableProperty] private string _gachaRarity = string.Empty;
        [ObservableProperty] private bool _isGachaRevealed = false;

        // Trang Random N người
        [ObservableProperty] private int _generateCount = 10;
        [ObservableProperty] private ObservableCollection<Student> _selectedStudents = new();

        // Trang Xếp lịch
        [ObservableProperty] private int _totalGroups = 15;
        [ObservableProperty] private int _totalDays = 3;
        [ObservableProperty] private ObservableCollection<ScheduleDay> _scheduleDays = new();

        // Cache các trang để chuyển đổi không bị lag
        private readonly ExcelListView _excelView = new();
        private readonly RandomOneView _randomOneView = new();
        private readonly RandomMultipleView _randomMultipleView = new();
        private readonly GroupScheduleView _groupScheduleView = new();

        public MainViewModel()
        {
            // Mặc định mở app lên là trang danh sách Excel
            CurrentView = _excelView;
        }

        // ================= 3. COMMAND: ĐIỀU HƯỚNG CHUYỂN TRANG =================
        [RelayCommand] private void NavigateExcel() => CurrentView = _excelView;
        [RelayCommand] private void NavigateRandomOne() => CurrentView = _randomOneView;
        [RelayCommand] private void NavigateRandomN() => CurrentView = _randomMultipleView;
        [RelayCommand] private void NavigateGroup() => CurrentView = _groupScheduleView;

        // ================= 4. COMMAND: TẢI FILE EXCEL =================
        [RelayCommand]
        private void SyncExcel()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Chọn danh sách sinh viên"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Gọi Service đọc file (Lấy cả Bảng động và Danh sách Student)
                var result = ExcelService.ImportDynamicExcel(openFileDialog.FileName);

                // Cập nhật Bảng cho màn hình Xem danh sách
                ExcelData = result.table;

                // Cập nhật danh sách Model cho các màn hình Random
                Students.Clear();
                foreach (var st in result.students)
                {
                    Students.Add(st);
                }

                TotalStudents = Students.Count;
                AttendCount = Students.Count;
                CurrentFileName = System.IO.Path.GetFileName(openFileDialog.FileName);

                // Thêm đoạn này vào gần cuối hàm SyncExcel(), ngay trên NavigateExcel();
                var best = Students.OrderByDescending(s => s.DiemCong).FirstOrDefault();
                if (best != null)
                {
                    BestStudentName = best.Name;
                    BestStudentPoints = best.DiemCong.ToString();
                }

                NavigateExcel(); // Đã có sẵn

                NavigateExcel();
            }
        }

        // ================= 5. COMMAND: RANDOM 1 NGƯỜI (CÓ TRỌNG SỐ & GIỌNG NÓI) =================
        [RelayCommand]
        private async Task RandomOneAsync()
        {
            if (Students.Count == 0) return;
            SelectedSingleStudent = null;
            IsGachaRevealed = false;

            string intro = $"Chọn ngẫu nhiên 1 {SelectedAudience} {SelectedAction}";
            await VoiceService.SpeakAsync(intro);

            var random = new Random();
            Student? pickedStudent = null;

            // NẾU LÀ "TRẢ LỜI" -> Ưu tiên người ít phát biểu
            if (SelectedAction == "trả lời")
            {
                int maxPhatBieu = Students.Max(s => s.PhatBieu);
                var weightedStudents = Students.Select(s => new { Student = s, Weight = (maxPhatBieu - s.PhatBieu) + 1 }).ToList();
                int totalWeight = weightedStudents.Sum(x => x.Weight);
                int randomNumber = random.Next(0, totalWeight);
                int currentSum = 0;
                foreach (var item in weightedStudents)
                {
                    currentSum += item.Weight;
                    if (randomNumber < currentSum) { pickedStudent = item.Student; break; }
                }
            }
            // NẾU LÀ KHÁC (Kiểm tra bài, Trả bài) -> Random đều, tỉ lệ bằng nhau
            else
            {
                pickedStudent = Students[random.Next(Students.Count)];
            }

            SelectedSingleStudent = pickedStudent;
            if (pickedStudent != null) await VoiceService.SpeakAsync(pickedStudent.Name);
        }

        // ================= 6. COMMAND: RANDOM N NGƯỜI (ĐỌC GIỌNG NÓI) =================
        [RelayCommand]
        private async Task GenerateMultipleAsync()
        {
            if (Students.Count == 0 || GenerateCount <= 0) return;
            SelectedStudents.Clear();

            string intro = $"Chọn ngẫu nhiên {GenerateCount} {SelectedAudience} {SelectedAction}";
            await VoiceService.SpeakAsync(intro);

            int takeCount = Math.Min(GenerateCount, Students.Count);
            var random = new Random();
            var randomizedList = Students.OrderBy(x => random.Next()).Take(takeCount).ToList();

            // Tính trung bình phát biểu của cả lớp
            double avgPhatBieu = Students.Count > 0 ? Students.Average(s => s.PhatBieu) : 0;

            foreach (var st in randomizedList)
            {
                // Xét màu theo tiêu chí
                if (st.PhatBieu == 0 || st.PhatBieu < avgPhatBieu * 0.5) st.CardColor = "#E24A4A"; // Màu Đỏ (Ít/Không)
                else if (st.PhatBieu > avgPhatBieu * 1.2) st.CardColor = "#4CAF50"; // Xanh Lá (Nhiều)
                else st.CardColor = "#4A90E2"; // Xanh Dương (Trung bình)

                SelectedStudents.Add(st);
                await VoiceService.SpeakAsync(st.Name);
            }
        }

        // ================= LỆNH MỚI: RÚT THẺ GACHA =================
        [RelayCommand]
        private async Task RollGachaAsync()
        {
            // Bắt buộc phải bốc trúng sinh viên rồi mới cho rút thẻ
            if (SelectedSingleStudent == null) return;

            IsGachaRevealed = false; // Reset hiệu ứng
            await Task.Delay(100);   // Chờ 0.1s để UI kịp reset

            var rnd = new Random();
            int roll = rnd.Next(1, 101); // Quay số từ 1 đến 100

            if (roll <= 5) // 5% SSR
            {
                GachaRarity = "SSR";
                GachaTitle = "KIM BÀI MIỄN TỬ";
                GachaDescription = "Miễn trả bài hôm nay! Hệ thống tự động vinh danh và cộng 3 điểm.";
                GachaColor = "#FFD700"; // Vàng ánh kim (Gold)
            }
            else if (roll <= 20) // 15% SR (Từ 6 đến 20) 
            {
                GachaRarity = "SR";
                GachaTitle = "TRIỆU HỒI HỘ GIÁ";
                GachaDescription = "Chỉ định 1 đồng đội trả lời hộ. Đúng cùng hưởng 1 điểm cộng, sai cùng chịu 1 điểm trừ!";
                GachaColor = "#B465DA"; // Tím rực rỡ
            }
            else if (roll <= 50) // 30% R (Từ 21 đến 50)
            {
                GachaRarity = "R";
                GachaTitle = "NHÂN ĐÔI SÁT THƯƠNG";
                GachaDescription = "Trả lời đúng được x2 điểm cộng. Trả lời sai bị 2 điểm trừ!";
                GachaColor = "#4A90E2"; // Xanh dương
            }
            else // 50% N (Từ 51 đến 100)
            {
                GachaRarity = "N";
                GachaTitle = "LỜI NGUYỀN CÂM LẶNG";
                GachaDescription = "Áp lực thời gian: Chỉ có đúng 15 giây để suy nghĩ và trả lời! Hết giờ là trừ 1 điểm.";
                GachaColor = "#607D8B"; // Xám đen
            }

            IsGachaRevealed = true; // Hiện thẻ lên

            // Chị Google đọc kết quả
            await VoiceService.SpeakAsync($"Thẻ hiệu ứng: {GachaTitle}. {GachaDescription}");
        }

        // ================= 7. COMMAND: XẾP LỊCH BÁO CÁO NHÓM (CÓ ĐỌC GIỌNG NÓI) =================
        [RelayCommand]
        private async Task GenerateScheduleAsync()
        {
            if (TotalGroups <= 0 || TotalDays <= 0) return;

            // Xóa danh sách cũ trên màn hình
            ScheduleDays.Clear();

            // 1. Chị Google đọc câu mở đầu
            string intro = $"Bắt đầu xếp lịch báo cáo cho {TotalGroups} nhóm, trong {TotalDays} ngày.";
            await VoiceService.SpeakAsync(intro);

            var random = new Random();

            // Tạo danh sách các nhóm (1, 2, 3...) và xáo trộn ngẫu nhiên
            var allGroups = Enumerable.Range(1, TotalGroups).OrderBy(x => random.Next()).ToList();

            int currentGroupIndex = 0;
            int groupsPerDay = (int)Math.Ceiling((double)TotalGroups / TotalDays);

            for (int i = 1; i <= TotalDays; i++)
            {
                var day = new ScheduleDay { DayName = $"Ngày {i}" };

                // Lắp nhóm vào ngày
                for (int j = 0; j < groupsPerDay && currentGroupIndex < allGroups.Count; j++)
                {
                    day.Groups.Add($"Nhóm {allGroups[currentGroupIndex]}");
                    currentGroupIndex++;
                }

                if (day.Groups.Count > 0)
                {
                    // Đưa ngày này lên giao diện (giao diện sẽ tự động chạy animation rớt thẻ xuống)
                    ScheduleDays.Add(day);

                    // Tạo câu văn cho chị Google đọc ngày hôm đó
                    // Ví dụ: "Ngày 1 gồm: Nhóm 5, Nhóm 9, Nhóm 2"
                    string groupNames = string.Join(", ", day.Groups);
                    string textToRead = $"{day.DayName} gồm: {groupNames}";

                    // Đợi chị Google đọc xong danh sách nhóm của ngày này rồi mới tạo ngày tiếp theo
                    await VoiceService.SpeakAsync(textToRead);
                }
            }

            // Đọc câu chốt khi xong việc
            await VoiceService.SpeakAsync("Đã xếp lịch xong. Chúc các nhóm chuẩn bị bài tốt!");
        }
    }
}