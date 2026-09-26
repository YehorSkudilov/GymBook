using GymBook.ViewModels;

namespace GymBook.Views;

public partial class CalendarPage : BasePage
{
    public CalendarPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
