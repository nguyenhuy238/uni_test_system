using System.Windows.Controls;

namespace EmployeeSurvey.AdminApp.Views;

public partial class QuestionsView : UserControl
{
    public QuestionsView() => InitializeComponent();

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.QuestionsViewModel vm && vm.SelectedQuestion != null)
        {
            vm.EditQuestionCommand.Execute(vm.SelectedQuestion);
        }
    }
}
