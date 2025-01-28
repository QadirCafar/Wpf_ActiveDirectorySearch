using ActiveDirectorySearch.Model;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.WebRequestMethods;


namespace ActiveDirectorySearch
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPaused = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadDrives();
        }

        private void LoadDrives()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.Name).ToList();
            DriveSelector.ItemsSource = drives;
            if (drives.Any())
                DriveSelector.SelectedIndex = 0;
        }


        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (DriveSelector.SelectedItem == null)
            {
                MessageBox.Show("Choose a disc.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            string selectedDrive = DriveSelector.SelectedItem.ToString();
            ResultsGrid.ItemsSource = null;

            await SearchDirectoriesAsync(selectedDrive, _cancellationTokenSource.Token);

        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = true;
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = false;
        }

        private async Task SearchDirectoriesAsync(string path, CancellationToken cancellationToken)
        {
            var results = new List<SearchResultModel>();
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
                            results.Add(
                                new SearchResultModel
                                {
                                    Path = directory,
                                    FileName = "  Unauthorized folder."
                                });
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

                            Application.Current.Dispatcher.Invoke(() => results.Add(result));
                        }
                    });
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Search stopped.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            ResultsGrid.ItemsSource = results;
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