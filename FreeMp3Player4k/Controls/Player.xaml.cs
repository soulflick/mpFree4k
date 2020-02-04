using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WMPLib;
using System.ComponentModel;
using MpFree4k.Classes;
using System.Timers;
using Classes;
using MpFree4k.ViewModels;
using System.Windows.Threading;
using MpFree4k.Enums;
using MpFree4k.Layers;
using System.Linq;
using System.Threading;

namespace MpFree4k.Controls
{
    public class ValueChangedEvent : EventArgs
    {
        public string Key;
        public string Value;
    }

    public enum RemainingMode
    {
        Elapsed,
        Remaining
    }

    public enum Playmode
    {
        Play,
        Unplay
    }

    /// <summary>
    /// Interaktionslogik für Controls.xaml
    /// </summary>
    public partial class Player : UserControl, INotifyPropertyChanged
    {
        private PlaylistViewModel _playlistVM = null;
        public PlaylistViewModel PlayListVM
        {
            get { return _playlistVM; }
            set
            {
                if (_playlistVM != null)
                    _playlistVM.PropertyChanged -= _playlistVM_PropertyChanged;
                _playlistVM = value;
                _playlistVM.PropertyChanged += _playlistVM_PropertyChanged;
            }
        }

        public double ButtonSize
        {
            get { return (double)UserConfig.ControlSize; }
        }

        private void _playlistVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Play")
            {
                current_track = PlayListVM.GetCurrent();
                Play(current_track);
            }
            else if (e.PropertyName == "PlayFromStart")
            {
                current_track = PlayListVM.GetCurrent();
                Play(current_track, true);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { return; };
        public event EventHandler<ValueChangedEvent> ValueChanged;

        public void NotifyPropertyChanged(String info)
        {
            if (info == "ButtonSize" || info == "FontSize")
            {
                double h = ButtonSize + 36;
                this.Height = h + (6.5 * (Math.Max((int)UserConfig.FontSize - 3,0)));
            }

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        private bool _isCheckedState = false;
        public bool IsCheckedState
        {
            get { return _isCheckedState; }
            set { _isCheckedState = value; NotifyPropertyChanged("IsCheckedState"); }
        }
        public WMPLib.WindowsMediaPlayer MediaPlayer = null;

        private System.Timers.Timer CheckSongTimer;
        public bool SongEnded = true;
        bool manualPlayStateChanged = false;

        int muteVolumne = 50;
        List<bool> PlayStates = new List<bool>();

        Dispatcher dispatcher = null;
        private static Player _singleton = null;
        public Player()
        {
            dispatcher = this.Dispatcher;
            CreatePlayerControl();
            InitializeComponent();


            MediaPlayer.settings.autoStart = true;
            btnMute.DataContext = this;

            CheckSongTimer = new System.Timers.Timer(300);
            CheckSongTimer.AutoReset = true;
            CheckSongTimer.Elapsed += CheckSongTimer_Elapsed;

            this.MediaPlayer.PlayStateChange +=
                new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(wplayer_PlayStateChange);

            _singleton = this;

            this.Loaded += Player_Loaded;
        }

        private void Player_Loaded(object sender, RoutedEventArgs e)
        {
            lblTrack.Content = "...";
            lblArtist.Content = "...";
            lblAlbum.Text = "...";
            lblYear.Text = "-";

            this.Height = (85 - 34) + ButtonSize - 34;
            NotifyPropertyChanged("ButtonSize");
        }

        private void CheckSongTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckSong();
            ValueChanged(null, new ValueChangedEvent() { Key = "PlayTimeUpdate", Value = MediaPlayer.controls.currentPosition.ToString() });
        }

        public void CreatePlayerControl()
        {
            if (MediaPlayer != null)
                MediaPlayer.controls.stop();

            MediaPlayer = new WMPLib.WindowsMediaPlayer();

            //try
            //{
            //    WMPLib.WMPEqualizerSettingsCtrl ctrl = new WMPEqualizerSettingsCtrl();
            //}
            //catch (Exception exc)
            //{
            //}
            //ctrl.gainLevel2 = 0.0f;
        }

        private void sldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer.settings.volume = (int)sldVolume.Value;
        }

        void UpdatePositionMarker(double position)
        {
            sldTrackSlider.ValueChanged -= sldTrackSlider_ValueChanged;
            sldTrackSlider.Value = position;
            sldTrackSlider.ValueChanged += sldTrackSlider_ValueChanged;
        }

        void InitPositionMarker(int duration)
        {
            sldTrackSlider.Maximum = duration;
        }

        void ShowWindowTitleByFile(PlaylistItem item)
        {
            SetTrackInfo(item);

            string trackName = "";

            if (item != null)
            {
                item.Path.Substring(item.Path.LastIndexOf(@"\") + 1);

                FileViewInfo info = new FileViewInfo(item.Path);
                info.CreateFileHandle();

                if (info._Handle != null)
                    trackName = info.Mp3Fields.Title + " - " + info.Mp3Fields.AlbumArtists;
            }
            ShowWindowTitle(trackName);

        }

        void SetTrackInfo(PlaylistItem itm)
        {
            lblTrack.Content = "";
            lblArtist.Content = "";
            lblAlbum.Text = "";
            lblYear.Text = "";

            if (itm == null)
                return;

            string title = itm.Title;
            if (string.IsNullOrWhiteSpace(title))
                title = itm.Path;

            lblTrack.Content = title;
            lblArtist.Content = itm.Artists;
            lblAlbum.Text = itm.Album;
            lblYear.Text = itm.Year;

            trackLength = DurationStringToSeconds(itm.Duration);
        }

        double DurationStringToSeconds(string duration)
        {
            double secs = 0;
            int idx = duration.IndexOf(':');

            if (idx <= 0)
            {
                Double.TryParse(duration, out secs);
                return secs;
            }
            int idx2;
            string s_hrs, s_mins, s_secs;
            if (duration.Count(x => x == ':') > 1)
            {
                
                idx2 = duration.LastIndexOf(':');
                s_hrs = duration.Substring(0, idx);
                s_mins = duration.Substring(idx + 1, idx2 - idx - 1);
                s_secs = duration.Substring(idx2 + 1);
            }
            else
            {
                s_hrs = "0";
                s_mins = duration.Substring(0, idx);
                s_secs = duration.Substring(idx + 1);
            }

            double d_hrs = Convert.ToDouble(s_hrs) * 3600;
            double d_mins = Convert.ToDouble(s_mins) * 60;
            double d_secs = Convert.ToDouble(s_secs);

            return d_hrs + d_mins + d_secs;
        }

        string secondsToDuration(double secs)
        {
            double d_hrs = Math.Floor(secs / 3600);
            double d_mins = Math.Floor((secs - (d_hrs * 3600)) / 60);
            double d_seconds = Math.Floor(secs - (d_hrs * 3600) - (d_mins * 60));

            string dur = "";
            if (d_hrs > 0)
                dur += d_hrs.ToString() + ":";
            if (d_mins < 10)
                dur += "0";
            dur += d_mins.ToString() + ":";
            if (d_seconds < 10)
                dur += "0";
            dur += d_seconds.ToString();

            return dur;
        }

        double trackLength = 0;

        void ShowWindowTitle(string songName)
        {
            string cstr = string.Format("{0} - MpFree4k - soulflick", songName);
            (Window.GetWindow(this) as MainWindow).Title = cstr;
        }

        Thread tPlayReverse = new Thread(() => reverse());
        Playmode playmode = Playmode.Play;
        private void Unplay(PlaylistItem pItm)
        {
            playmode = Playmode.Unplay;
            //MediaPlayer.controls.stop();
            //tPlayReverse.Abort();
            //MediaPlayer.controls.currentPosition = 0;
            //IWMPMedia media = MediaPlayer.newMedia(pItm.Path);
            //double end = media.duration;
            //MediaPlayer.controls.currentPosition = end - 4;
            //UpdatePositionMarker(end - 4);
            tPlayReverse = new Thread(() => reverse());
            tPlayReverse.Start();
        }

        bool stopreverse = false;
        static void reverse()
        {
            double position = _singleton.MediaPlayer.controls.currentPosition;
            double end = position;
            _singleton.MediaPlayer.controls.play();
            while (!_singleton.stopreverse && position > 0.1)
            {
                Thread.Sleep(100);
                _singleton.MediaPlayer.controls.currentPosition -= 0.2;
                position = _singleton.MediaPlayer.controls.currentPosition;
                _singleton.Dispatcher.BeginInvoke(new Action(() => _singleton.UpdatePositionMarker(position)));
            }

            _singleton.MediaPlayer.controls.currentPosition = end - 1;
            return;
        }


        PlaylistItem current_track = null;
        public void Play(PlaylistItem fileInfo, bool from_start = false)
        {
            if (fileInfo == null)
            {
                Stop();
                return;
            }

            bool paused = MediaPlayer.playState == WMPPlayState.wmppsPaused;

            SongEnded = false;
            CheckSongTimer.Start();
            current_track = fileInfo;

            MediaPlayer.controls.stop();
            MediaPlayer.controls.currentPosition = 0;
            MediaPlayer.URL = fileInfo.Path;
            lblTrack.Content = fileInfo.Path;

            IWMPMedia media = MediaPlayer.newMedia(fileInfo.Path);

            if (media.duration == 0)
            {
                if (PlayListVM.RepeatMode == RepeatMode.GoThrough ||
                    PlayListVM.RepeatMode == RepeatMode.Loop ||
                    PlayListVM.RepeatMode == RepeatMode.Shuffle)
                {
                    Next();
                    return;
                }
                return;
            }


            InitPositionMarker((int)media.duration);
            lblTotalTrackDuration.Content = media.durationString; // GetDurationString((int)media.duration);
            setProgressSliderTicks(media.duration);
            media = null;

            ShowWindowTitleByFile(fileInfo);


            //FileViewInfo fInfo = new FileViewInfo(URL);
            //this.FileProperties.SetInfo(fInfo);



            if (MediaPlayer.playState != WMPPlayState.wmppsPaused)
            {
                MediaPlayer.controls.stop();
                MediaPlayer.controls.currentPosition = 0;
            }

            try
            {
                if (MediaPlayer.playState != WMPPlayState.wmppsPaused)
                {
                    MediaPlayer.URL = current_track.Path;
                }

                if (playmode == Playmode.Play)
                    MediaPlayer.controls.play();
                else
                {
                    Unplay(fileInfo);
                    return;
                }

                RememberTrack(current_track);
                
                if (paused && !from_start)
                    MediaPlayer.controls.currentPosition = pause_position;

                try
                {
                    DisplayTrackImage(fileInfo);
                }
                catch
                {
                    TrackImage.Source = null;
                    //TrackImageBig.Source = null;
                }

                IWMPMedia __media = MediaPlayer.newMedia(MediaPlayer.URL);
                InitPositionMarker((int)__media.duration);
                lblTotalTrackDuration.Content = __media.durationString; // GetDurationString((int)media.duration);
                setProgressSliderTicks(__media.duration);
                __media = null;

                MediaPlayer.PlayStateChange +=
                new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(wplayer_PlayStateChange);

                NotifyPropertyChanged("Play");

            }
            catch (Exception exc)
            {
                if (PlayListVM.RepeatMode != RepeatMode.Once ||
                    PlayListVM.RepeatMode != RepeatMode.RepeatOne)
                    Next();
            }
        }

        void RememberTrack(PlaylistItem t)
        {
            Library._singleton.connector.SetTrack(t.Path);
            string[] tracks = Library._singleton.Current.Files.Where(f => f.Mp3Fields.Year.ToString() == t.Year && f.Mp3Fields.Album == t.Album).Select(y => y.Path).ToArray();
            Library._singleton.connector.SetAlbum(t.Album, t.Year.ToString(), tracks);
        }

        void CheckSong()
        {

            double progress = MediaPlayer.controls.currentPosition;
            string posStr = MediaPlayer.controls.currentPositionString;
            string posStrRemaining = secondsToDuration(trackLength - progress);
            this.dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, (Action)(() =>
            {
                lblProgress.Content = (String.IsNullOrEmpty(posStr)) ? "00:00" : (remainingMode == RemainingMode.Elapsed) ? posStr : posStrRemaining;
                UpdatePositionMarker(progress);
            }));


            RepeatMode repeatMode = PlayListVM.RepeatMode;
            if (SongEnded)
            {
                SongEnded = false;
                if (repeatMode == RepeatMode.RepeatOne)
                    Play(current_track);
                else if (repeatMode == RepeatMode.Once)
                    Stop();
                else if (repeatMode == RepeatMode.GoThrough)
                    Next();
                else if (repeatMode == RepeatMode.Loop)
                    Next();
                else if (repeatMode == RepeatMode.Shuffle)
                    Shuffle();
            }
        }

        void setProgressSliderTicks(double duration)
        {
            sldTrackSlider.TickFrequency = 10; // (int)(duration / 30);
            System.Windows.Media.DoubleCollection dbls = new System.Windows.Media.DoubleCollection();
            for (int i = 0; i <= (int)(duration / 30); i++)
                dbls.Add(i * 30);
            sldTrackSlider.Ticks = dbls;
        }

        void DisplayTrackImage(PlaylistItem itm)
        {
            if (itm.Image != null)
            {
                TrackImage.Source = itm.Image;
                //(Window.GetWindow(this) as MainWindow).Player.TrackImageBig.Source = itm.Image;
                return;
            }

            try
            {
                TagLib.File _tmp = TagLib.File.Create(itm.Path);
                TrackImage.Source = TagLibConvertPicture.GetImageFromTag(_tmp.Tag.Pictures);
                //(Window.GetWindow(this) as MainWindow).Player.TrackImageBig.Source = TrackImage.Source;
            }
            catch
            {
                TrackImage.Source = null;
                //(Window.GetWindow(this) as MainWindow).Player.TrackImageBig.Source = null;
            }

            if (TrackImage.Source == null)
            {
                showDefaultPicture();
            }
        }

        string GetDurationString(int duration)
        {
            int seconds, minutes, hours;
            hours = duration / 3600;
            minutes = (duration - (hours * 3600)) / 60;
            seconds = duration - (hours * 3600) - minutes * 60;

            string ret = "";
            if (hours > 0) ret = hours.ToString() + ":";
            if (minutes < 10) ret += "0";
            ret += minutes.ToString() + ":";
            if (seconds < 10) ret += "0";
            ret += seconds.ToString();

            return ret;
        }

        double pause_position = 0;
        public void Pause()
        {
            if (MediaPlayer.playState == WMPPlayState.wmppsPaused)
            {
                Play(current_track);
            }
            else
            {
                pause_position = MediaPlayer.controls.currentPosition;
                CheckSongTimer.Stop();
                MediaPlayer.controls.pause();
            }
        }
        public void Shutdown()
        {
            Stop();
            MediaPlayer.controls.stop();
            MediaPlayer.close();
            MediaPlayer = null;
        }

        public void Stop()
        {
            tPlayReverse.Abort();


            //(Window.GetWindow(this) as MainWindow).fileListView.UnsetPlayStatus();
            for (int i = 0; i < PlayStates.Count; i++)
                PlayStates[i] = false;

            MediaPlayer.controls.stop();

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                sldTrackSlider.Value = 0;
                lblProgress.Content = "00:00";
            }));
            CheckSongTimer.Stop();
        }


        void wplayer_PlayStateChange(int NewState)
        {
            switch ((WMPPlayState)NewState)
            {
                case WMPPlayState.wmppsUndefined:
                    SongEnded = true;
                    //CheckSongTimer.Start();
                    break;

                case WMPLib.WMPPlayState.wmppsMediaEnded:
                    SongEnded = true;
                    //CheckSongTimer.Start();
                    break;

                case WMPLib.WMPPlayState.wmppsPlaying:
                    SongEnded = false;
                    //CheckSongTimer.Start();
                    break;

                default:
                    break;
            }
        }

        private void sldTrackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaPlayer == null) return;
            if (MediaPlayer.currentMedia == null) return;

            MediaPlayer.controls.currentPosition = sldTrackSlider.Value;
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Previous();
        }

        void Next()
        {
            current_track = PlayListVM.GetNext();
            dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, (Action)(() =>
            {
                Play(current_track);
                ShowWindowTitleByFile(current_track);
            }));

        }

        void Shuffle()
        {
            dispatcher.BeginInvoke(new Action(() =>
            {
                current_track = PlayListVM.GetShuffle();
                ShowWindowTitleByFile(current_track);
                //if (MediaPlayer.playState == WMPPlayState.wmppsPlaying)
                    Play(current_track);
            }));
        
        }

        void Previous()
        {
            current_track = PlayListVM.GetPrevious();
            ShowWindowTitleByFile(current_track);
            if (MediaPlayer.playState == WMPPlayState.wmppsPlaying)
                Play(current_track);
        }

        private void btnRewind_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer.controls.currentPosition > 0)
                MediaPlayer.controls.currentPosition -= 5;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (playmode == Playmode.Unplay)
            {
                tPlayReverse.Abort();
                MediaPlayer.controls.play();
                playmode = Playmode.Play;
                return;
            }
            playmode = Playmode.Play;
            current_track = PlayListVM.GetCurrent();
            Play(current_track);
        }

        private void btnReverse_Click(object sender, RoutedEventArgs e)
        {
            current_track = PlayListVM.GetCurrent();
            Unplay(current_track);
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void btnForward_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.controls.currentPosition += 5;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void btnMute_Checked(object sender, RoutedEventArgs e)
        {
            muteVolumne = (int)sldVolume.Value;
            MediaPlayer.settings.volume = 0;
            sldVolume.Value = 0;
            IsCheckedState = btnMute.IsChecked == true;
        }

        private void btnMute_Unchecked(object sender, RoutedEventArgs e)
        {
            MediaPlayer.settings.volume = muteVolumne;
            sldVolume.Value = muteVolumne;
            IsCheckedState = btnMute.IsChecked == true;
        }

        public void showDefaultPicture()
        {
            TrackImage.Source = new System.Windows.Media.Imaging.BitmapImage(
new System.Uri(@"pack://application:,,,/" +
System.Reflection.Assembly.GetCallingAssembly().GetName().Name +
";component/" + "Images/no_album_cover.jpg", System.UriKind.Absolute));

            //(Window.GetWindow(this) as MainWindow).Player.TrackImageBig.Source = TrackImage.Source;
        }

        private void sldTrackSlider_StylusMove(object sender, System.Windows.Input.StylusEventArgs e)
        {
            MediaPlayer.settings.volume = 0;
        }

        private void sldTrackSlider_StylusDown(object sender, System.Windows.Input.StylusDownEventArgs e)
        {
            MediaPlayer.settings.volume = muteVolumne;
        }

        private RemainingMode remainingMode = RemainingMode.Elapsed;
        private void lblProgress_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            remainingMode = remainingMode == RemainingMode.Elapsed ? RemainingMode.Remaining : RemainingMode.Elapsed;
        }
    }
}
