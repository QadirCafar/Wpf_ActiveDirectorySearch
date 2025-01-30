using ActiveDirectorySearch.Command;
using ActiveDirectorySearch.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ActiveDirectorySearch.ViewModel
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPaused;
        private ObservableCollection<SearchResultModel> _searchResults;
        private string _selectedDrive;
        private ICommand _startSearchCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SearchResultModel> SearchResults
        {
            get { return _searchResults; }
            set
            {
                _searchResults = value;
                OnPropertyChanged(nameof(SearchResults));
            }
        }

        public string SelectedDrive
        {
            get { return _selectedDrive; }
            set
            {
                _selectedDrive = value;
                OnPropertyChanged(nameof(SelectedDrive));
            }
        }

        public ICommand StartSearchCommand
        {
            get
            {
                if (_startSearchCommand == null)
                {
                    _startSearchCommand = new RelayCommand(async () => await StartSearchAsync(), CanStartSearch);
                }
                return _startSearchCommand;
            }
        }

        public MainViewModel()
        {
            _searchResults = new ObservableCollection<SearchResultModel>();
            LoadDrives();
        }

        public void LoadDrives()
        {
            var drives = DriveInfo.GetDrives()
                                  .Where(d => d.IsReady)
                                  .Select(d => d.Name)
                                  .ToList();

            // 'drives' siyahısını ObservableCollection-ə çeviririk
            Drives = new ObservableCollection<string>(drives);

            if (drives.Any())
                SelectedDrive = drives[0];
        }

        public ObservableCollection<string> Drives { get; set; }

        private bool CanStartSearch()
        {
            return !string.IsNullOrEmpty(SelectedDrive); // Əgər seçilmiş disk varsa, axtarışı başlatmaq olar
        }

        public async Task StartSearchAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            SearchResults.Clear();
            await SearchDirectoriesAsync(SelectedDrive, _cancellationTokenSource.Token);
        }

        private async Task SearchDirectoriesAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(Directory.EnumerateDirectories(path), new ParallelOptions { CancellationToken = cancellationToken }, (directory) =>
                    {
                        if (_isPaused)
                            Task.Delay(100).Wait(); // Gözləmə rejimi


                        // Qovluğa giriş icazəsini yoxlayırıq
                        if (!HasAccess(directory))
                        {
                            var result = new SearchResultModel
                            {
                                Path = directory + " -- Unauthorized folder.",
                            };
                            Application.Current.Dispatcher.Invoke(() => SearchResults.Add(result));
                            return; // Əgər icazə yoxdursa, bu qovluğu keç
                        }

                        var files = Directory.EnumerateFiles(directory)
                                              .Select(f => new FileInfo(f))
                                              .Where(f => f.Length > 10 * 1024 * 1024); // 10 MB-dan böyük fayllar

                        if (files.Any())
                        {
                            var result = new SearchResultModel
                            {
                                Path = directory,
                                FileCount = files.Count(),
                                TotalSizeMB = files.Sum(f => f.Length) / (1024 * 1024) // MB cinsindən ölçü
                            };

                            Application.Current.Dispatcher.Invoke(() => SearchResults.Add(result));
                        }
                    });
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Search stopped.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private bool HasAccess(string path)
        {
            try
            {
                // Qovluğa giriş yoxlaması
                var accessCheck = Directory.GetDirectories(path);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
