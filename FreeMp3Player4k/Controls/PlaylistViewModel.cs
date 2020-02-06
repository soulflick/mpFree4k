using MpFree4k.Classes;
using MpFree4k.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MpFree4k.ViewModels
{
    public enum PlayState
    {
        Play,
        PlayFromStart
    }

    public class PlaylistViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { return; };

        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        public StatusViewModel StatusVM { get; set; }
        public PlaylistViewModel()
        {
            StatusVM = new StatusViewModel();
            StatusVM.Set(this);
        }

        private List<PlaylistItem> _tracks = new List<PlaylistItem>();
        public List<PlaylistItem> Tracks
        {
            get { return _tracks; }
            set
            {
                _tracks = value;
                OnPropertyChanged("Tracks");
                StatusVM.Update();
            }
        }

        public void UnDrag()
        {
            Tracks.ForEach(x => x.DragOver = false);
            OnPropertyChanged("Tracks");
        }

        public void Move(List<PlaylistItem> itms, int pos)
        {
            int min_idx = itms.Min(x => x._position - 1);
            foreach (PlaylistItem pi in itms)
                Tracks.Remove(pi);
            Add(itms, pos);
            enumerate(min_idx);
        }

        public void Move(List<PlaylistItem> itms, PlaylistItem itm)
        {
            foreach (PlaylistItem pi in itms)
                Tracks.Remove(pi);

            int dragpos = itm.Position;
            int updatepos = dragpos;
            updatepos = Math.Min(updatepos, itms.Min(i => i.Position)) - 1;
            updatepos = Math.Max(0, updatepos);

            enumerate(updatepos);
            dragpos = itm.Position;
            Add(itms, dragpos);
            enumerate(updatepos);
        }

        public void Add(List<PlaylistItem> items, int position = int.MaxValue)
        {
            if (position == int.MaxValue || position > Tracks.Count)
                position = (int)Tracks.Count;

            int startpos = position;

            foreach (PlaylistItem item in items)
            {
                PlaylistItem new_item = new PlaylistItem()
                {
                    Album = item.Album,
                    Title = item.Title,
                    Duration = item.Duration,
                    Artists = item.Artists,
                    Path = item.Path,
                    Year = item.Year,
                    Track = item.Track,
                    uniqueID = item.uniqueID,
                    IsPlaying = item.IsPlaying
                };
                Tracks.Insert((int)position, new_item);
                position++;
            }

            UnDrag();
            enumerate(startpos);

            OnPropertyChanged("Tracks");
        }

        public void Remove(int[] idxs)
        {
            if (idxs.Length == 0)
                return;

            int start = idxs[0];

            for (int i = idxs.Length - 1; i >= 0; i--)
            {
                int idx_p = idxs[i];
                if (Tracks.Count <= idx_p)
                    continue;

                Tracks.RemoveAt((int)idx_p);
            }

            enumerate(start);

            OnPropertyChanged("Tracks");
        }

        public void enumerate(int start)
        {
            for (int i = start; i < Tracks.Count; i++)
            {
                Tracks[(int)i]._position = i + 1;
                Tracks[(int)i].TrackNumber = (i + 1).ToString();
            }

            if (CurrentSong != null)
            {
                PlaylistItem current = Tracks.FirstOrDefault(x => x.uniqueID == CurrentSong.uniqueID);
                if (current != null)
                    CurrentPlayPosition = Math.Max(0, current._position - 1);
            }
            
        }

        public RepeatMode RepeatMode = RepeatMode.GoThrough;

        private int _currentPlayPosition = 0;
        public int CurrentPlayPosition
        {
            get { return _currentPlayPosition; }
            set
            {
                _currentPlayPosition = value;
               
                for (int i = 0; i < Tracks.Count; i++)
                    Tracks[i].IsPlaying = false;

                if (value < 0 || value >= Tracks.Count)
                    return;

                Tracks[value].IsPlaying = true;
            }
        }

        public PlaylistItem CurrentSong = null;

        public PlaylistItem GetCurrent()
        {
            if (CurrentPlayPosition >= Tracks.Count)
                CurrentPlayPosition = Tracks.Count - 1;

            if (CurrentPlayPosition < 0)
                CurrentPlayPosition = 0;

            if (Tracks.Count == 0)
                return null;

            CurrentSong = Tracks[CurrentPlayPosition];
            return CurrentSong;
        }

        public void UpdatePlayPosition()
        {

            return;
            if (CurrentSong != null)
                CurrentPlayPosition = CurrentSong._position - 1;

        }

        public PlaylistItem GetShuffle()
        {
            Random rnd = new Random();
            int idx = rnd.Next(0, Tracks.Count);
            CurrentPlayPosition = idx;
            return GetCurrent();
        }

        public PlaylistItem GetNext()
        {
            if (RepeatMode == RepeatMode.GoThrough)
            {
                if (CurrentPlayPosition >= Tracks.Count - 1)
                {
                    CurrentPlayPosition = Tracks.Count - 1;
                    return null;
                }
                else
                {
                    CurrentPlayPosition++;
                }
            }
            else if (RepeatMode == RepeatMode.Loop)
            {
                if (CurrentPlayPosition >= Tracks.Count -1)
                {
                    CurrentPlayPosition = 0;
                }
                else
                {
                    CurrentPlayPosition++;
                }
            }
            else if (RepeatMode == RepeatMode.Once)
            {
                return null;
                if (CurrentPlayPosition >= Tracks.Count - 1)
                {
                    CurrentPlayPosition = Tracks.Count - 1;
                    //return null;
                }
                else
                {
                    CurrentPlayPosition++;
                }
            }
            else if (RepeatMode == RepeatMode.RepeatOne)
            {
                if (CurrentPlayPosition >= Tracks.Count - 1)
                {
                    CurrentPlayPosition = Tracks.Count - 1;
                    //return null;
                }
                else
                {
                    CurrentPlayPosition++;
                }
            } else if (RepeatMode == RepeatMode.Shuffle)
            {
                return GetShuffle();
            }

            return GetCurrent();
        }

        public PlaylistItem GetPrevious()
        {
            if (RepeatMode == RepeatMode.GoThrough)
            {
                if (CurrentPlayPosition <= 0)
                {
                    CurrentPlayPosition = 0;
                }
                else
                {
                    CurrentPlayPosition--;
                }
            }
            else if (RepeatMode == RepeatMode.Loop)
            {
                if (CurrentPlayPosition <= 0)
                {
                    CurrentPlayPosition = 0;
                }
                else
                {
                    CurrentPlayPosition--;
                }
            }
            else if (RepeatMode == RepeatMode.Once)
            {
                if (CurrentPlayPosition <= 0)
                {
                    CurrentPlayPosition = 0;
                }
                else
                {
                    CurrentPlayPosition--;
                }
            }
            else if (RepeatMode == RepeatMode.RepeatOne)
            {
                if (CurrentPlayPosition <= 0)
                {
                    CurrentPlayPosition = 0;
                }
                else
                {
                    CurrentPlayPosition--;
                }
            }
            else if (RepeatMode == RepeatMode.Shuffle)
            {
                return GetShuffle();
            }

            return GetCurrent();
        }

        public void Invoke(PlayState state)
        {
            if (state == PlayState.Play)
                OnPropertyChanged("Play");
            else if (state == PlayState.PlayFromStart)
                OnPropertyChanged("PlayFromStart");
        }
    }
}
