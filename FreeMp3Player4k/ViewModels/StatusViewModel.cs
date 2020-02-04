using MpFree4k.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MpFree4k.ViewModels
{
    public class StatusViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { return; };

        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        public void Update(double progress = -999)
        {
            if (pvm.Tracks.Count <= 0)
            {
                NumberOfTracks = 0;
                TotalDuration = "00:00";
                return;
            }

            NumberOfTracks = (ulong)pvm.Tracks.Count;

            TimeSpan span = new TimeSpan();
            foreach (PlaylistItem i in pvm.Tracks)
            {
                TimeSpan p = getDuration(i.Duration);
                span = span.Add(p);
            }

            string spanmin = Math.Floor(span.TotalMinutes).ToString();
            string spansec = span.Seconds.ToString();

            if (spanmin.Length == 1)
                spanmin = "0" + spanmin;
            if (spansec.Length == 1)
                spansec = "0" + spansec;
            string new_duration = spanmin + ":" + spansec;

            TotalDuration = new_duration;

            span = new TimeSpan();
            for (int i = pvm.CurrentPlayPosition; i < pvm.Tracks.Count; i++)
            {
                TimeSpan p = getDuration(pvm.Tracks[i].Duration);
                span = span.Add(p);
            }

            double secs = span.TotalSeconds;
            if (progress >= 0)
                secs -= progress;

            if (secs < 0)
                secs = 0;

            spanmin = Math.Floor(secs/(double)60).ToString();
            spansec = Math.Floor(secs % (double)60).ToString();

            if (spanmin.Length == 1)
                spanmin = "0" + spanmin;
            if (spansec.Length == 1)
                spansec = "0" + spansec;

            new_duration = spanmin + ":" + spansec;

            Remaining = new_duration;
        }

        private ulong _numberOfTracks = 0;
        public ulong NumberOfTracks
        {
            get { return _numberOfTracks; }
            set
            {
                _numberOfTracks = value;
                OnPropertyChanged("NumberOfTracks");
            }
        }

        private string _totalDuration = "00:00";
        public string TotalDuration
        {
            get
            {
                return _totalDuration;
            }
            set
            {
                _totalDuration = value;
                OnPropertyChanged("TotalDuration");
            }
        }

        private string _remaining = "00:00";
        public string Remaining
        {
            get { return _remaining; }
            set
            {
                _remaining = value;
                OnPropertyChanged("Remaining");
            }
        }

        public void Set(PlaylistViewModel PVM)
        {
            if (pvm != null)
                pvm.PropertyChanged -= PVM_PropertyChanged;

            PVM.PropertyChanged += PVM_PropertyChanged;

            pvm = PVM;
        }

        private PlaylistViewModel pvm = null;
        private void PVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Tracks")
            {
                Update();
            }
        }

        TimeSpan getDuration(string durstr)
        {
            TimeSpan me = new TimeSpan();

            int idx = durstr.IndexOf(":");
            if (idx < 1 || idx == durstr.Length - 1)
                return me;

            string mins = durstr.Substring(0, idx);
            string secs = durstr.Substring(idx + 1);

            int mints = Convert.ToInt32(mins);
            int sects = Convert.ToInt32(secs);

            return new TimeSpan(0, mints, sects);
        }
    }
}
