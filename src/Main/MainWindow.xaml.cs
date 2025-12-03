using System;
using System.Windows;
using System.Windows.Input;

using ControlFileManager.Core.Models;
using ControlFileManager.Core.Services;
using ControlFileManager.UI.ViewModels;

namespace ControlFileManager.Main
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm, IFileSystemService fileService)
        {
            InitializeComponent();
            DataContext = vm;
            vm.StatusTb = StatusTb;
        }
    }
}