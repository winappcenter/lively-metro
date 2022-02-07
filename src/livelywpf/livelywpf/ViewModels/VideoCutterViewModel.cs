using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using livelywpf.Views.Pages;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using YetAnotherLosslessCutter;
using YetAnotherLosslessCutter.FFProbe;
using YetAnotherLosslessCutter.MVVM;

namespace livelywpf.ViewModels
{
    sealed class VideoCutterViewModel : ViewModelBase
    {
        readonly FfprobeUtil ffprobe = new FfprobeUtil();
        readonly VideoCutterView host;

        Track TimeLineTrack => host.TimelineSlider.Template.FindName("PART_Track", host.TimelineSlider) as Track;

        private readonly MetroDialogSettings dialogSettings = new MetroDialogSettings
        {
            AnimateHide = false,
            AnimateShow = false
        };
        public string Title => $"Video Cutter for Lively Wallpaper";
        MediaInfo SourceInfo;

        public VideoCutterViewModel(VideoCutterView mainWindow)
        {
            host = mainWindow;
            host.InputBindings.Add(new KeyBinding(Jump1FrameForward, new KeyGesture(Key.Right, ModifierKeys.None)));
            host.InputBindings.Add(new KeyBinding(JumpXSecondForward, new KeyGesture(Key.Right, ModifierKeys.Control)) { CommandParameter = 1 });
            host.InputBindings.Add(new KeyBinding(JumpXSecondForward, new KeyGesture(Key.Right, ModifierKeys.Shift)) { CommandParameter = 10 });
            host.InputBindings.Add(new KeyBinding(JumpXSecondForward, new KeyGesture(Key.Right, ModifierKeys.Shift | ModifierKeys.Control)) { CommandParameter = 60 });

            host.InputBindings.Add(new KeyBinding(Jump1FrameBackward, new KeyGesture(Key.Left, ModifierKeys.None)));
            host.InputBindings.Add(new KeyBinding(JumpXSecondBackward, new KeyGesture(Key.Left, ModifierKeys.Control)) { CommandParameter = 1 });
            host.InputBindings.Add(new KeyBinding(JumpXSecondBackward, new KeyGesture(Key.Left, ModifierKeys.Shift)) { CommandParameter = 1 });
            host.InputBindings.Add(new KeyBinding(JumpXSecondBackward, new KeyGesture(Key.Left, ModifierKeys.Shift | ModifierKeys.Control)) { CommandParameter = 60 });
        }


        readonly ConcurrentQueue<VideoSegment> ProcessingQueue = new ConcurrentQueue<VideoSegment>();

        public ObservableCollection<VideoSegment> ProcessingQueueList { get; } =
            new ObservableCollection<VideoSegment>();

        public ObservableCollection<VideoSegment> ProjectSegmentList { get; } =
            new ObservableCollection<VideoSegment>();

        VideoSegment _SelectedSegment;

        public VideoSegment SelectedSegment
        {
            get => _SelectedSegment;
            set
            {
                if (!Set(ref _SelectedSegment, value)) return;
                if (value == null)
                {
                    if (ProjectSegmentList.Count == 0)
                    {
                        //Clear everything
                        SourceFile = string.Empty;
                        return;
                    }

                    //Select previous segment
                    SelectedSegment = ProjectSegmentList[^1];
                    return;
                }

                //Do nothing if this is the first segment
                if (ProjectSegmentList.Count == 1) return;
                //Otherwise, update cut draw area
                host.CutMarker.X1 = 0d;
                host.CutMarker.X2 = 0d;
                host.TimelineSlider.Value = value.CutFrom.TotalMilliseconds;
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                    new Action(() => { })).Wait();
                MarkerTimeline(0);
                host.TimelineSlider.Value = value.CutTo.TotalMilliseconds;
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                    new Action(() => { })).Wait();
                MarkerTimeline(1);
            }
        }



        string _SourceFileName;

        public string SourceFileName
        {
            get => _SourceFileName;
            private set => Set(ref _SourceFileName, value);
        }

        string _SourceFile;

        public string SourceFile
        {
            get => _SourceFile;
            set
            {
                if (!Set(ref _SourceFile, value)) return;
                if (string.IsNullOrEmpty(value))
                {
                    host.MediaElement1.Stop();
                    host.MediaElement1.Close();
                    host.MediaElement1.Source = null;
                    ProjectSegmentList.Clear();
                    SourceFileName = string.Empty;
                    host.CutMarker.X1 = 0d;
                    host.CutMarker.X2 = 0d;
                    host.txtRemind.Visibility = Visibility.Visible;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                SourceFileName = Path.GetFileName(_SourceFile);
                SourceInfo = ffprobe.GetInfos(_SourceFile);
                var project = new VideoSegment(host) { SourceFile = _SourceFile, MaxDuration = SourceInfo.Duration };
                host.TimelineSlider.Maximum = SourceInfo.Duration.TotalMilliseconds;
                host.TimelineSlider.Value = 0;
                ProjectSegmentList.Add(project);
                SelectedSegment = project;
                OnPropertyChanged(nameof(Title));
                host.MediaElement1.Source = new Uri(_SourceFile);
                host.MediaElement1.Play();
                host.MediaElement1.Pause();
                host.txtRemind.Visibility = Visibility.Collapsed;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public DelegateCommand SetLeftPosition => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CutFrom = host.MediaElement1.Position;
            MarkerTimeline(0);
        });

        public DelegateCommand SetRightPosition => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CutTo = host.MediaElement1.Position;
            MarkerTimeline(1);

        });

        public DelegateCommand AddNewSegment => new DelegateCommand(() =>
        {
            var project = new VideoSegment(host)
            {
                SourceFile = SourceFile,
                MaxDuration = SourceInfo.Duration,
                CutTo = SourceInfo.Duration,
                CutFrom = SelectedSegment?.CutTo ?? TimeSpan.Zero,
            };
            ProjectSegmentList.Add(project);
            SelectedSegment = project;
            host.TimelineSlider.Value = project.CutFrom.TotalMilliseconds;
        });

        public  void DeleteSegment(VideoSegment segment)
        {
            if (segment == null) return;
            MessageBoxResult result = MessageBox.Show("Remove segment from list?", "Confirmation", MessageBoxButton.OKCancel);
            
            if (result != MessageBoxResult.OK) return;
            if (segment == SelectedSegment)
                SelectedSegment = null;
            ProjectSegmentList.Remove(segment);
        }
        public void DeleteSegmentFromQueue(VideoSegment segment)
        {
            if (segment == null) return;
            MessageBoxResult result = MessageBox.Show("Remove segment from queue?", "Confirmation",  MessageBoxButton.OKCancel);

            if (result != MessageBoxResult.OK) return;
           
            segment.MarkedForDeletion = true;
            ProcessingQueueList.Remove(segment);
        }

        public DelegateCommand RemoveAllSegments => new DelegateCommand(async () =>
        {
            MessageBoxResult result = MessageBox.Show("Clear segment list?", "Confirmation", MessageBoxButton.OKCancel);

            if (result != MessageBoxResult.OK) return;
           
            SelectedSegment = null;
            ProjectSegmentList.Clear();
        });

        public DelegateCommand<int> JumpXSecondForward => new DelegateCommand<int>((i) =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            if (SelectedSegment.CurrentPosition + TimeSpan.FromSeconds(i) >
                SourceInfo.Duration)
                SelectedSegment.CurrentPosition = SourceInfo.Duration;
            else
                SelectedSegment.CurrentPosition += TimeSpan.FromSeconds(i);
        });
        public DelegateCommand Jump1FrameForward => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            if (SelectedSegment.CurrentPosition + TimeSpan.FromMilliseconds(SourceInfo.Streams[0].FrameRate) > SourceInfo.Duration)
                SelectedSegment.CurrentPosition = SourceInfo.Duration;
            else
                SelectedSegment.CurrentPosition += TimeSpan.FromMilliseconds(SourceInfo.Streams[0].FrameRate);
        });
        public DelegateCommand Jump1FrameBackward => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            if (SelectedSegment.CurrentPosition - TimeSpan.FromMilliseconds(SourceInfo.Streams[0].FrameRate) < TimeSpan.Zero)
                SelectedSegment.CurrentPosition = TimeSpan.Zero;
            else
                SelectedSegment.CurrentPosition -= TimeSpan.FromMilliseconds(SourceInfo.Streams[0].FrameRate);
        });

        public DelegateCommand<int> JumpXSecondBackward => new DelegateCommand<int>((i) =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            if (SelectedSegment.CurrentPosition - TimeSpan.FromSeconds(i) < TimeSpan.Zero)
                SelectedSegment.CurrentPosition = TimeSpan.Zero;
            else
                SelectedSegment.CurrentPosition -= TimeSpan.FromSeconds(i);
        });

        public DelegateCommand PlayVideo => new DelegateCommand(() => host.MediaElement1.Play());
        public DelegateCommand PauseVideo => new DelegateCommand(() => host.MediaElement1.Pause());
        public DelegateCommand CreateGIF => new DelegateCommand(async () =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            var sfd = new System.Windows.Forms.SaveFileDialog { DefaultExt = "gif", AddExtension = true, Filter = "GIF|*.gif" };
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            await FfmpegUtil.CreateGIF(SourceFile, sfd.FileName, SelectedSegment.CutFrom, SelectedSegment.CutTo, -1);
        });
        public DelegateCommand ReloadVideo => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            var currentPos = host.MediaElement1.Position;
            host.MediaElement1.Close();
            host.MediaElement1.Source = null;
            host.MediaElement1.Source = new Uri(SourceFile);
            host.MediaElement1.Play();
            host.MediaElement1.Pause();
            host.MediaElement1.Position = currentPos;
        });

        public DelegateCommand CheckForUpdate => new DelegateCommand(async () =>
        {
           
        });

        public DelegateCommand CutVideo => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            while (ProjectSegmentList.Count > 0)
            {
                ProjectSegmentList[0].Status = ProgressStatus.Waiting;
                ProcessingQueueList.Add(ProjectSegmentList[0]);
                ProcessingQueue.Enqueue(ProjectSegmentList[0]);
                ProjectSegmentList.RemoveAt(0);
            }
            if (Settings.Instance.AutoStartQueue)
                StartQueue.Execute();
        });
        public DelegateCommand OpenVideo => new DelegateCommand(() =>
        {
            var openFileDlg = new OpenFileDialog
            {
                Title = Properties.Resources.TitleAddWallpaper,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
                Filter = "(.wmv)|*.wmv|(.avi)|*.avi|(.flv)|*.flv|(.m4v)|*.m4v|(.mkv)|*.mkv|(.mov)|*.mov|(.mp4)|*.mp4|(.mpeg4)|*.mpeg4|(.mpg)|*.mpg|(.webm)|*.webm|(.ogm)|*.ogm|(.ogv)|*.ogv|(.ogx)|*.ogx"
           
            };

            if (openFileDlg.ShowDialog() == true)
            {
                if (openFileDlg.FileNames.Length > 0)
                {
                    LoadSourceFile(openFileDlg.FileNames[0]);
                }
            }
        });

        //public DelegateCommand DeleteSource => new DelegateCommand(async () =>
        //{
        //    var result = await host.ShowMessageAsync("Confirmation", $"Delete {SourceFile}?",
        //        MessageDialogStyle.AffirmativeAndNegative);
        //    if (result != MessageDialogResult.Affirmative) return;
        //    host.MediaElement1.Stop();
        //    host.MediaElement1.Close();
        //    host.MediaElement1.Source = null;

        //});

        public DelegateCommand PickOutputDirectory => new DelegateCommand(() =>
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            Settings.Instance.OutputDirectory = dialog.SelectedPath;
        });

        public DelegateCommand ClearFinishedQueue => new DelegateCommand(() =>
        {
            for (int i = ProcessingQueueList.Count - 1; i >= 0; i--)
            {
                if (ProcessingQueueList[i].Status == ProgressStatus.Finished)
                    ProcessingQueueList.RemoveAt(i);
            }
        });

        static T Do<T>(Func<T> action, TimeSpan retryInterval, int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }

        public void LoadSourceFile(string file)
        {
            if (!File.Exists(file)) return;

            SourceFile = null;
            SourceFile = file;
        }

        void MarkerTimeline(int pos)
        {
            if (string.IsNullOrEmpty(SourceFile) == false && host.MediaElement1.NaturalDuration.HasTimeSpan)
            {

                Point relativePoint = TimeLineTrack.Thumb.TransformToAncestor(host.TimelineGrid)
                    .Transform(new Point(0, 0));
                if (pos == 0 & (host.CutMarker.X2 == 0d || relativePoint.X > host.CutMarker.X2))
                    host.CutMarker.X2 = host.TimelineSlider.ActualWidth;
                if (pos == 0 && relativePoint.X <= host.CutMarker.X2)
                {
                    host.CutMarker.X1 = relativePoint.X;
                }
                else if (pos == 1 && relativePoint.X >= host.CutMarker.X1)
                    host.CutMarker.X2 = relativePoint.X;

            }
        }

        private bool queueIsBusy;
        public DelegateCommand StartQueue => new DelegateCommand(() =>
        {
            //Re-add failed items
            for (int i = 0; i < ProcessingQueueList.Count; i++)
            {
                if (ProcessingQueueList[i].Status != ProgressStatus.Failed) continue;
                ProcessingQueueList[i].Status = ProgressStatus.Waiting;
                ProcessingQueue.Enqueue(ProcessingQueueList[i]);
            }
            if (queueIsBusy)
                return;
            queueIsBusy = true;

            StartQueueInternal();
        });

        async void StartQueueInternal()
        {
            var fileList = new List<VideoSegment>();

            while (ProcessingQueue.TryDequeue(out var videoSegment))
            {
                if (videoSegment.MarkedForDeletion) continue;
                
                var result = await videoSegment.Cut();
                if (result.Success == false)
                {
                    MessageBoxResult mresult = MessageBox.Show(result.Error.ToString(), "Error", MessageBoxButton.OK);

                    
                }
                else
                {
                    //See if this is a new source file
                    bool isNewSourceFile = ProcessingQueue.IsEmpty || ProcessingQueue.TryPeek(out var nextSegment) &&
                                           !videoSegment.SourceFile.Equals(nextSegment.SourceFile);

                    if (!Settings.Instance.MergeSegments && Settings.Instance.RemoveFinishedSegments)
                        ProcessingQueueList.Remove(videoSegment);
                    else if (Settings.Instance.MergeSegments && fileList.Count > 0)
                    {
                        if (isNewSourceFile)
                        {
                            //We cutted the last one, so lets merge
                            

                            var ouputFilename = Settings.Instance.SaveToSourceFolder
                                ? Path.ChangeExtension(SourceFile, $"_merged{Path.GetExtension(SourceFile)}")
                                : Path.Combine(Settings.Instance.OutputDirectory,
                                    $"{Path.GetFileNameWithoutExtension(SourceFile)}_merged{Path.GetExtension(SourceFile)}");
                            await FfmpegUtil.Merge(ouputFilename, fileList);
                            foreach (var file in fileList)
                                try
                                {
                                    File.Delete(file.OutputFile);
                                }
                                catch
                                {
                                }

                            if (Settings.Instance.RemoveFinishedSegments)
                            {
                                for (int i = 0; i < fileList.Count; i++)
                                    ProcessingQueueList.Remove(fileList[i]);
                            }

                            fileList.Clear();
                        }

                    }

                    if (isNewSourceFile && Settings.Instance.DeleteSourceFileAfterDone)
                    {
                        try
                        {
                            var success = Do(() =>
                            {
                                File.Delete(videoSegment.SourceFile);
                                return true;
                            }, TimeSpan.FromSeconds(0.5));
                            if (!success)
                                throw new TimeoutException($"Failed to delete {SourceFile}");
                        }
                        catch (Exception ex)
                        {
                            MessageBoxResult mresult = MessageBox.Show(ex.ToString(), "Failed to delete file", MessageBoxButton.OK);
                            
                        }
                    }
                    fileList.Add(videoSegment);
                }

            }
           
            queueIsBusy = false;
        }
    }
}
