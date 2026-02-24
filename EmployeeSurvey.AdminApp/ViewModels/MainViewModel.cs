using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EmployeeSurvey.AdminApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private Test? _selectedTest;
        private User? _selectedUser;
        private string _statusMessage = "Ready";

        public MainViewModel(ApiService apiService)
        {
            _apiService = apiService;
            Tests = new ObservableCollection<Test>();
            Users = new ObservableCollection<User>();
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            SaveTestCommand = new RelayCommand(async () => await SaveTestAsync(), () => SelectedTest != null);
            DeleteTestCommand = new RelayCommand(async () => await DeleteTestAsync(), () => SelectedTest != null);
            SaveUserCommand = new RelayCommand(async () => await SaveUserAsync(), () => SelectedUser != null);
            DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => SelectedUser != null);
        }

        public ObservableCollection<Test> Tests { get; }
        public ObservableCollection<User> Users { get; }

        public Test? SelectedTest
        {
            get => _selectedTest;
            set
            {
                _selectedTest = value;
                OnPropertyChanged();
            }
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand LoadDataCommand { get; }
        public RelayCommand SaveTestCommand { get; }
        public RelayCommand DeleteTestCommand { get; }
        public RelayCommand SaveUserCommand { get; }
        public RelayCommand DeleteUserCommand { get; }

        private async Task LoadDataAsync()
        {
            try
            {
                StatusMessage = "Loading data...";
                
                var tests = await _apiService.GetTestsAsync();
                if (tests != null)
                {
                    Tests.Clear();
                    foreach (var test in tests)
                    {
                        Tests.Add(test);
                    }
                }

                var users = await _apiService.GetUsersAsync();
                if (users != null)
                {
                    Users.Clear();
                    foreach (var user in users)
                    {
                        Users.Add(user);
                    }
                }

                StatusMessage = $"Loaded {Tests?.Count ?? 0} tests, {Users?.Count ?? 0} users";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task SaveTestAsync()
        {
            if (SelectedTest == null) return;

            try
            {
                StatusMessage = "Saving test...";
                
                if (string.IsNullOrEmpty(SelectedTest.Id))
                {
                    await _apiService.CreateTestAsync(SelectedTest);
                    StatusMessage = "Test created successfully";
                }
                else
                {
                    await _apiService.UpdateTestAsync(SelectedTest.Id, SelectedTest);
                    StatusMessage = "Test updated successfully";
                }
                
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task DeleteTestAsync()
        {
            if (SelectedTest == null || string.IsNullOrEmpty(SelectedTest.Id)) return;

            try
            {
                StatusMessage = "Deleting test...";
                await _apiService.DeleteTestAsync(SelectedTest.Id);
                StatusMessage = "Test deleted successfully";
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task SaveUserAsync()
        {
            if (SelectedUser == null) return;

            try
            {
                StatusMessage = "Saving user...";
                
                if (string.IsNullOrEmpty(SelectedUser.Id))
                {
                    await _apiService.CreateUserAsync(SelectedUser, "Password123!");
                    StatusMessage = "User created successfully";
                }
                else
                {
                    await _apiService.UpdateUserAsync(SelectedUser.Id, SelectedUser);
                    StatusMessage = "User updated successfully";
                }
                
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null || string.IsNullOrEmpty(SelectedUser.Id)) return;

            try
            {
                StatusMessage = "Deleting user...";
                await _apiService.DeleteUserAsync(SelectedUser.Id);
                StatusMessage = "User deleted successfully";
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object? parameter) => await _executeAsync();
    }
}
