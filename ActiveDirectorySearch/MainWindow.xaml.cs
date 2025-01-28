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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
            // await SearchFilesAsync(selectedDrive, _cancellationTokenSource.Token);

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
        //private async Task SearchFilesAsync(string path, CancellationToken cancellationToken)
        //{
        //    var results = new List<SearchResultModel>(); // Fayl adlarını saxlayacaq siyahı
        //    try
        //    {
        //        await Task.Run(() =>
        //        {
        //            ProcessDirectory(path, results, cancellationToken); // Başlanğıc qovluğunu təhlil et

        //            // Qovluqdakı altqovluqları tapın və onlarda axtarış aparın
        //            var subDirectories = Directory.EnumerateDirectories(path);
        //            Parallel.ForEach(subDirectories, new ParallelOptions { CancellationToken = cancellationToken }, (subDir) =>
        //            {
        //                if (cancellationToken.IsCancellationRequested)
        //                    return;

        //                // Hər bir altqovluqda faylları tapırıq
        //                ProcessDirectory(subDir, results, cancellationToken);
        //            });
        //        }, cancellationToken);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        MessageBox.Show("Axtarış dayandırıldı.", "Məlumat", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }

        //    // Siyahını interfeysdə göstərmək
        //    Application.Current.Dispatcher.Invoke(() => ResultsGrid.ItemsSource = results);
        //}

        //private void ProcessDirectory(string directory, List<SearchResultModel> results, CancellationToken cancellationToken)
        //{
        //    // Qovluğa giriş icazəsini yoxlayırıq
        //    if (!HasAccess(directory))
        //    {
        //        results.Add(
        //            new SearchResultModel
        //            {
        //                Path = directory,
        //                FileName = "  Giriş hüququ olmayan qovluq."
        //            });
        //        return; // Əgər icazə yoxdursa, bu qovluğu keç
        //    }

        //    try
        //    {
        //        var files = Directory.EnumerateFiles(directory)
        //                              .Select(f => new FileInfo(f))
        //                              .Where(f => f.Length > 10 * 1024 * 1024) // 10 MB-dan böyük fayllar
        //                              .ToList();

        //        if (files.Any())
        //        {
        //            // Fayl adlarını siyahıya əlavə et
        //            foreach (var file in files)
        //            {
        //                results.Add(new SearchResultModel
        //                {
        //                    Path = directory,
        //                    TotalSizeMB = file.Length / (1024 * 1024), // Fayl ölçüsünü MB cinsindən
        //                    FileName = file.Name
        //                });
        //            }
        //        }
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        // İcazə problemi varsa, sadəcə keçin
        //    }
        //    catch (Exception ex)
        //    {
        //        // Hər hansı bir başqa xəta varsa
        //        MessageBox.Show($"Xəta: {ex.Message}");
        //    }
        //}


        ////private async Task SearchFilesAsync(string path, CancellationToken cancellationToken)
        ////{
        ////    var results = new List<SearchResultModel>(); // Fayl adlarını saxlayacaq siyahı
        ////    try
        ////    {
        ////        await Task.Run(() =>
        ////        {
        ////            Parallel.ForEach(Directory.EnumerateDirectories(path), new ParallelOptions { CancellationToken = cancellationToken }, (directory) =>
        ////            {
        ////               // MessageBox.Show(directory);
        ////                if (cancellationToken.IsCancellationRequested)
        ////                    return;
        ////                if (!HasAccess(directory))
        ////                {
        ////                    results.Add(
        ////                            new SearchResultModel
        ////                            {
        ////                                Path = directory,
        ////                                //TotalSizeMB = files.Sum(f => f.Length) / (1024 * 1024),
        ////                                FileName =  "  Giriş hüququ olmayan qovluq."
        ////                            });
        ////                    return; // Əgər icazə yoxdursa, bu qovluğu keç
        ////                }

        ////                try
        ////                {
        ////                    var files = Directory.EnumerateFiles(directory)
        ////                                          .Select(f => new FileInfo(f))
        ////                                          .Where(f => f.Length > 10 * 1024 * 1024) // 10 MB-dan böyük fayllar
        ////                                          .ToList();

        ////                    if (files.Any())
        ////                    {

        ////                        // Fayl adlarını siyahıya əlavə et
        ////                        foreach (var file in files)
        ////                        {
        ////                            results.Add(new SearchResultModel
        ////                            {
        ////                                Path = directory,
        ////                                //FileCount = files.Count(),
        ////                                TotalSizeMB = file.Length/ (1024 * 1024), 
        ////                                 FileName= file.Name
        ////                            }); // Tam yol ilə fayl adı

        ////                           // Application.Current.Dispatcher.Invoke(() => results.Add(result));
        ////                        }
        ////                    }
        ////                }
        ////                catch (UnauthorizedAccessException ex)
        ////                {
        ////                    //MessageBox.Show($"Xəta: {ex.Message}");
        ////                    // İcazə problemi varsa, sadəcə keçin
        ////                }
        ////                catch (Exception ex)
        ////                {
        ////                    // Hər hansı bir başqa xəta varsa
        ////                    MessageBox.Show($"Xəta: {ex.Message}");
        ////                }
        ////            });
        ////        }, cancellationToken);
        ////    }
        ////    catch (OperationCanceledException)
        ////    {
        ////        MessageBox.Show("Axtarış dayandırıldı.", "Məlumat", MessageBoxButton.OK, MessageBoxImage.Information);
        ////    }

        ////    // Siyahını interfeysdə göstərmək
        ////    Application.Current.Dispatcher.Invoke(() => ResultsGrid.ItemsSource = results);
        ////}
        //private async Task SearchDirectoriesAsync(string path, CancellationToken cancellationToken)
        //{
        //    var results = new List<SearchResultModel>();
        //    try
        //    {
        //        await Task.Run(() =>
        //        {
        //            Parallel.ForEach(Directory.EnumerateDirectories(path), new ParallelOptions { CancellationToken = cancellationToken }, (directory) =>
        //            {
        //                if (_isPaused)
        //                    Task.Delay(100).Wait(); // Gözləmə rejimi

        //                if (!HasAccess(directory))
        //                    return; // Əgər icazə yoxdursa, bu qovluğu keç
        //                try
        //                {
        //                    var files = Directory.EnumerateFiles(directory)
        //                                         .Select(f => new FileInfo(f))
        //                                         .Where(f => f.Length > 10 * 1024 * 1024); // 10 MB-dan böyük fayllar

        //                    if (files.Any())
        //                    {
        //                        var result = new SearchResultModel
        //                        {
        //                            Path = directory,
        //                            FileCount = files.Count(),
        //                            TotalSizeMB = files.Sum(f => f.Length) / (1024 * 1024)
        //                        };

        //                        Application.Current.Dispatcher.Invoke(() => results.Add(result));
        //                    }
        //                }
        //                catch (UnauthorizedAccessException)
        //                {
        //                    MessageBox.Show($"Giriş hüququ olmayan qovluq");
        //                    // Giriş hüququ olmayan qovluq 
        //                }
        //                catch (Exception ex)
        //                {
        //                    MessageBox.Show($"Qovluqda xəta: {ex.Message}");
        //                }
        //            });
        //        }, cancellationToken);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        MessageBox.Show("Axtarış dayandırıldı.", "Məlumat", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //    ResultsGrid.ItemsSource = results;
        //}



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