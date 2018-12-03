using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TeensySharp;

namespace _03_FirmwareUploadWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TeensyWatcher _watcher;

        public ObservableCollection<USB_Device> Boards { get; set; } = new ObservableCollection<USB_Device>();
        
        public MainWindow()
        {
            InitializeComponent();
            _watcher = new TeensyWatcher();
            foreach (var device in _watcher.ConnectedDevices)
            {
                Boards.Add(device);
            }
            _watcher.ConnectionChanged += Watcher_ConnectionChanged;
        }

        private void Watcher_ConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
            if (e.changedDevice == null)
                return;
            switch (e.changeType)
            {
                case TeensyWatcher.ChangeType.add:
                    {
                        Application.Current.Dispatcher.Invoke(delegate 
                        {
                            Boards.Add(e.changedDevice);
                        });
                    }
                    break;
                case TeensyWatcher.ChangeType.remove:
                    if (Boards.Contains(e.changedDevice))
                    {
                        Application.Current.Dispatcher.Invoke(delegate 
                        {
                            Boards.Remove(e.changedDevice);
                        });   
                    }
                    break;

            }
            
            // RaisePropertyChanged("Level2MenuItems");
        }

        private string firmareToUpload = @"C:\Users\sgmk2\AppData\Local\Temp\arduino_build_540532\TeensyVoyage.ino.hex";

        private void Upload(USB_Device board)
        {
            var Board = PJRC_Board.Teensy_35;
            var FlashImage = SharpUploader.GetEmptyFlashImage(Board);
            var parsedResult = SharpHexParser.ParseStream(File.OpenText(firmareToUpload), FlashImage);
            if (!parsedResult)
            {
                MessageBox.Show("Error parsing firmware file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // USB_Device Teensy = Watcher.ConnectedDevices.FirstOrDefault();
            var mode = SharpUploader.StartHalfKay(board.Serialnumber);
            if (!mode)
            {
                MessageBox.Show("Error setting mode on board.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int result = SharpUploader.Upload(FlashImage, Board, board.Serialnumber, reboot: true);
            switch (result)
            {
                case 0:
                    MessageBox.Show("Success", "Ok", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case 1:
                    MessageBox.Show("Board not found", "Problem uploading", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                case 2:
                    MessageBox.Show("Error during firmware updated", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selected = Teensies.SelectedItem as USB_Device;
            if (selected == null)
            {
                MessageBox.Show("Select a board from the list first.", "Action needed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Upload(selected);
        }
    }
}
