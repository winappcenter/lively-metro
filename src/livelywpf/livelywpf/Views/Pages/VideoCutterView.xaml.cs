using livelywpf.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YetAnotherLosslessCutter;

namespace livelywpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for VideoCutterView.xaml
    /// </summary>
    public partial class VideoCutterView : Page
    {
        bool loadedItems;
        readonly VideoCutterViewModel vm;
        public VideoCutterView()
        {
            vm = new VideoCutterViewModel(this);
            DataContext = vm;
            InitializeComponent();
            System.Windows.Application.Current.Exit += new ExitEventHandler(MainWindow_Closing);
            Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (loadedItems) return;
            loadedItems = true;

            var dirInfo = new DirectoryInfo(System.IO.Path.Combine(Settings.CurrentFolder, "Queue"));
            if (!dirInfo.Exists) return;
            var jsonOptions = new JsonSerializerOptions();
            //https://github.com/dotnet/corefx/issues/38641
            jsonOptions.Converters.Add(new TimeSpanConverter());
            foreach (var segmentFile in dirInfo.GetFiles("*.json"))
            {
                vm.ProcessingQueueList.Add(JsonSerializer.Deserialize<VideoSegment>(File.ReadAllText(segmentFile.FullName), jsonOptions));
            }

        }

        private void MainWindow_Closing(object sender, ExitEventArgs e)
        {
            //Save unfinished items
            if (vm.ProcessingQueueList.Count > 0)
            {
                var dirInfo = new DirectoryInfo(System.IO.Path.Combine(Settings.CurrentFolder, "Queue"));
                if (!dirInfo.Exists)
                    dirInfo.Create();
                for (int i = 0; i < vm.ProcessingQueueList.Count; i++)
                {
                    if (vm.ProcessingQueueList[i].Status == ProgressStatus.Finished) continue;
                    File.WriteAllText(System.IO.Path.Combine(dirInfo.FullName, System.IO.Path.GetFileNameWithoutExtension(vm.ProcessingQueueList[i].OutputFile) + ".json"),
                        JsonSerializer.Serialize(vm.ProcessingQueueList[i]));
                }
            }
            Settings.SaveSettings();
        }

        private void mediaPlayerElement_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length >= 1)
                    vm.LoadSourceFile(files[0]);
                e.Handled = true;
            }
        }

        private void mediaPlayerElement_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }


        private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (vm.SelectedSegment != null)
                vm.SelectedSegment.CurrentPosition = TimeSpan.FromMilliseconds(TimelineSlider.Value);
        }
    }
}
