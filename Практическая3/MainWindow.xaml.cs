using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Практическая3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string dir;
        List<string> sounds;
        int current;
        bool isPlaying;
        bool isRepeat;
        bool isRandom;
        Thread updateThread = null;

        public MainWindow()
        {
            InitializeComponent();
            sounds = new List<string>();
            current = 0;
            dir = "";
            isPlaying = false;
            isRepeat = false;
            isRandom = false;
        }

        private void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true};
            CommonFileDialogResult result =  dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                LoadSounds(dialog.FileName);
                if (sounds.Count > 0)
                {
                    UpdateListBox();
                    current = 0;
                    PlayCurrent();
                }
            }
        }

        private void LoadSounds(string path)
        {
            dir = path;
            string[] files = Directory.GetFiles(path);
            sounds = new List<string>();
            foreach (string file in files)
            {
                if (file.EndsWith(".mp3") || file.EndsWith(".m4a") || file.EndsWith(".wav"))
                {
                    sounds.Add(file);
                }
            }
        }

        private void PlayCurrent()
        {
            Media.Source = new Uri(sounds[current]);
            Media.Play();
            isPlaying = true;
        }

        private void Pause()
        {
            if (Media.Source == null)
            {
                return;
            }

            if (isPlaying)
            {
                Media.Pause();
                isPlaying = false;
            }
            else
            {
                Media.Play();
                isPlaying = true;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (sounds.Count > 0)
            {
                current++;
                if (current >= sounds.Count)
                {
                    current = 0;
                }
                Sounds.SelectedIndex = current;
                PlayCurrent();
            }
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (sounds.Count > 0)
            {
                current--;
                if (current < 0)
                {
                    current = sounds.Count - 1;
                }
                Sounds.SelectedIndex = current;
                PlayCurrent();
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            isRepeat = !isRepeat;
        }

        private void Random_Click(object sender, RoutedEventArgs e)
        {
            string sound = sounds[current];

            if (isRandom)
            {
                LoadSounds(dir);
                isRandom = false;
            }
            else
            {
                Random r = new Random();
                sounds = sounds.OrderBy(x => r.Next()).ToList();
                isRandom = true;
            }

            current = sounds.FindIndex(x => x == sound);
            UpdateListBox();
        }

        private void UpdateListBox()
        {
            List<string> names = new List<string>();
            foreach (string s in sounds)
            {
                FileInfo fileInfo = new FileInfo(s);
                names.Add(fileInfo.Name);
            }

            Sounds.ItemsSource = names.ToArray();
            Sounds.SelectedIndex = current;
        }

        private void Sounds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Sounds.SelectedIndex >= 0 && current != Sounds.SelectedIndex)
            {
                current = Sounds.SelectedIndex;
                PlayCurrent();
            }
        }

        private void Media_MediaOpened(object sender, RoutedEventArgs e)
        {
            TimeSpan ts = Media.NaturalDuration.TimeSpan;
            CurrentTime.Text = $"0:00";
            string seconds = ts.Seconds < 10 ? $"0{ts.Seconds}" : $"{ts.Seconds}";
            EndTime.Text = $"{ts.Minutes}:{seconds}";
            Timer.Maximum = Media.NaturalDuration.TimeSpan.Ticks;
            Timer.Value = 0;
            AbortUpdate();
            updateThread = new Thread(new ThreadStart(UpdateTime));
            updateThread.Start();
        }

        public void UpdateTime()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (isPlaying)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TimeSpan ts = Media.Position;
                        Timer.Value = ts.Ticks;
                        string seconds = ts.Seconds < 10 ? $"0{ts.Seconds}" : $"{ts.Seconds}";
                        CurrentTime.Text = $"{ts.Minutes}:{seconds}";
                    });
                }
            }
        }

        private void Timer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan ts = new TimeSpan(Convert.ToInt64(Timer.Value));
            Media.Position = ts;
            string seconds = ts.Seconds < 10 ? $"0{ts.Seconds}" : $"{ts.Seconds}";
            CurrentTime.Text = $"{ts.Minutes}:{seconds}";
        }

        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (!isRepeat)
            {
                AbortUpdate();
                current++;
                if (current >= sounds.Count)
                {
                    current = 0;
                }
                Sounds.SelectedIndex = current;
            }
            PlayCurrent();
        }

        private void AbortUpdate()
        {
            if (updateThread != null && updateThread.IsAlive)
            {
                updateThread.Abort();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            AbortUpdate();
        }
    }
}
