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

        // ================= 7. COMMAND: XẾP LỊCH BÁO CÁO NHÓM =================
        [RelayCommand]
        private void GenerateSchedule()
        {
            if (TotalGroups <= 0 || TotalDays <= 0) return;

            var random = new Random();

            // Tạo danh sách các nhóm (1, 2, 3...) và xáo trộn ngẫu nhiên
            var allGroups = Enumerable.Range(1, TotalGroups).OrderBy(x => random.Next()).ToList();

            ScheduleDays.Clear();
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
                    ScheduleDays.Add(day);
                }
            }
        }
    }
}