using System.Collections.ObjectModel;

namespace SmartClassroomRandom.Models
{
    public class ScheduleDay
    {
        public string DayName { get; set; } = string.Empty;

        // Danh sách tên các nhóm (VD: "Nhóm 1", "Nhóm 5") trong ngày đó
        public ObservableCollection<string> Groups { get; set; } = new ObservableCollection<string>();
    }
}