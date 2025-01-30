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


namespace ActiveDirectorySearch.View
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPaused = false;

        public MainWindow()
        {
            InitializeComponent();
        }

    }
}