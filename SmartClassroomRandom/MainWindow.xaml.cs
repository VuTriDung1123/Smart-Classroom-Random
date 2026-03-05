using System.Windows;
using SmartClassroomRandom.ViewModels; // Thêm dòng này

namespace SmartClassroomRandom
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Gán DataContext để nối kết XAML và ViewModel
            this.DataContext = new MainViewModel();
        }
    }
}