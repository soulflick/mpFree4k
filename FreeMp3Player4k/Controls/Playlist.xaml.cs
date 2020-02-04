using MpFree4k.Classes;
using MpFree4k.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MpFree4k.Controls
{

    /// <summary>
    /// Interaktionslogik für Playlist.xaml
    /// </summary>
    public partial class Playlist : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { return; };

        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        List<PlaylistItem> dragItems = new List<PlaylistItem>();
        private PlaylistViewModel vm = null;
        public Playlist()
        {
            vm = new PlaylistViewModel();
            this.DataContext = vm;
            vm.PropertyChanged += Playlist_PropertyChanged;
            MainWindow._singleton.Player.PropertyChanged += Player_PropertyChanged;
            MainWindow._singleton.Player.ValueChanged += Player_ValueChanged;
            this.Loaded += Playlist_Loaded;
            SizeChanged += Playlist_SizeChanged;
            InitializeComponent();
        }

        private void Player_ValueChanged(object sender, ValueChangedEvent e)
        {
            if (e.Key == "PlayTimeUpdate")
            {
                double progress = Convert.ToDouble(e.Value);
                vm.StatusVM.Update(progress);
            }
        }

        private void Playlist_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            setWidth();
        }

        private void Playlist_Loaded(object sender, RoutedEventArgs e)
        {
            setWidth();
        }

        void setWidth()
        {
            AvailableWidth = PlaylistView.ActualWidth;
            Decorator border = VisualTreeHelper.GetChild(this.PlaylistView, 0) as Decorator;
            if (border != null)
            {
                ScrollViewer scroller = border.Child as ScrollViewer;
                if (scroller != null)
                {
                    Visibility scrollbarVisibility = scroller.ComputedVerticalScrollBarVisibility;
                    double substract = SystemParameters.VerticalScrollBarWidth;
                    double newwid = 0;
                    if (scrollbarVisibility != Visibility.Collapsed)
                        newwid = scroller.ActualWidth - substract - 4;
                    else
                        newwid = scroller.ActualWidth - 4;

                    if (AvailableWidth != newwid)
                        AvailableWidth = newwid;
                }
            }
        }

        private double _availableWidth = 0;
        public double AvailableWidth
        {
            get { return _availableWidth; }
            set
            {
                _availableWidth = value;
                OnPropertyChanged("AvailableWidth");
            }
        }

        private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PlaylistViewModel VM = this.DataContext as PlaylistViewModel;
            if (VM.CurrentSong == null)
                return;

            PlaylistItem p = VM.CurrentSong;
            PlaylistView.ScrollIntoView(p);

            if (e.PropertyName == "Play")
                VM.StatusVM.Update();
        }

        private void Playlist_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Tracks")
            {
                PlaylistView.ItemsSource = null;
                PlaylistView.ItemsSource = vm.Tracks;
            }
            else if (e.PropertyName == "Play")
            {
                PlaylistViewModel VM = this.DataContext as PlaylistViewModel;
                if (VM.CurrentSong == null)
                    return;

                PlaylistItem p = VM.CurrentSong;
                PlaylistView.ScrollIntoView(p);
            }
        }

        private void PlaylistView_DragOver(object sender, DragEventArgs e)
        {
            if (!(e.OriginalSource is FrameworkElement))
                return;

            FrameworkElement element = (FrameworkElement)e.OriginalSource;
            if (element == null)
                return;

            ListViewItem lvi = (ListViewItem)PlaylistView.ItemContainerGenerator.ContainerFromItem(element.DataContext);
            if (lvi != null) // && !dragItems.Contains(lvi.DataContext) && dragItems.Any())
                (lvi.DataContext as PlaylistItem).DragOver = true;
        }

        private void PlaylistView_DragEnter(object sender, DragEventArgs e)
        {
        }

        private void PlaylistView_DragLeave(object sender, DragEventArgs e)
        {
            if (!(e.OriginalSource is FrameworkElement))
                return;

            FrameworkElement element = (FrameworkElement)e.OriginalSource;
            if (element == null)
                return;

            ListViewItem lvi = (ListViewItem)PlaylistView.ItemContainerGenerator.ContainerFromItem(element.DataContext);
            if (lvi != null)
                (lvi.DataContext as PlaylistItem).DragOver = false;
        }

        bool drag_invalidate = false;
        private void PlaylistView_Drop(object sender, DragEventArgs e)
        {
            if (!(e.OriginalSource is FrameworkElement))
                return;

            FrameworkElement element = (FrameworkElement)e.OriginalSource;
            if (element == null)
                return;

            ListViewItem lvi = null;
            int pos = vm.Tracks.Count;

            lvi = (ListViewItem)PlaylistView.ItemContainerGenerator.ContainerFromItem(element.DataContext);
            if (lvi != null)
                pos = (lvi.DataContext as PlaylistItem).Position - 1;

            if (e.Data.GetDataPresent("MediaLibraryItemData"))
            {
                //listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
                IList data = e.Data.GetData("MediaLibraryItemData") as IList;
                if (data == null || data.Count == 0)
                    return;

                List<PlaylistItem> infoItms = (from object item in data select (PlaylistItem)item).ToList();
                dragItems.Clear();
                //dragItems.AddRange(infoItms);

                vm.Add(infoItms, pos);

            }
            if (e.Data.GetDataPresent("PlaylistItemData"))
            {
                if (drag_invalidate)
                    return;

                //listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
                IList data = e.Data.GetData("PlaylistItemData") as IList;
                if (data == null || data.Count == 0)
                    return;

                List<PlaylistItem> infoItms = (from object item in data select (PlaylistItem)item).ToList();

                if (dragItems.Any(x => x._position == pos + 1))
                {
                    vm.UnDrag();
                    return;
                }

                vm.Move(infoItms, pos);
                vm.UpdatePlayPosition();
                dragItems.Clear();
                //dragItems.AddRange(infoItms);

            }

            vm.StatusVM.Update();
        }

        private bool mousedown = false;
        private void PlaylistView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            mousedown = true;
            mousepos = e.MouseDevice.GetPosition(this);

            var item = ItemsControl.ContainerFromElement(PlaylistView, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                if (!MainWindow.ctrl_down)
                {
                    if (item.IsSelected)
                        e.Handled = true;
                    else
                    {
                        PlaylistView.SelectedItems.Clear();
                        item.IsSelected = true;
                    }
                }
                else
                {
                    if (item.IsSelected)
                    {
                        if (PlaylistView.SelectedItems.Contains(item))
                            PlaylistView.SelectedItems.Remove(item);
                        else
                            PlaylistView.SelectedItems.Add(item);
                    }
                    //item.IsSelected = !item.IsSelected;
                }

            }
        }

        private void PlaylistView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (PlaylistView.SelectedItems == null ||
                    PlaylistView.SelectedItems.Count == 0)
                    return;

                int[] remove_indices = new int[PlaylistView.SelectedItems.Count];
                int idx = 0;
                foreach (PlaylistItem infoItm in PlaylistView.SelectedItems)
                {
                    remove_indices[idx] = infoItm._position - 1;
                    idx++;
                }
                vm.Remove(remove_indices.OrderBy(x => x).ToArray());

                vm.StatusVM.Update();
            }
        }

        private void PlaylistView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dragItems.Clear();

            foreach (PlaylistItem pli in PlaylistView.SelectedItems)
            {
                dragItems.Add(pli);
            }
        }

        private void TrackClick(object sender, MouseButtonEventArgs e)
        {
            PlaylistItem pi = (sender as ListViewItem).DataContext as PlaylistItem;
            vm.CurrentPlayPosition = pi._position - 1;
            vm.Invoke(PlayState.PlayFromStart);

            dragItems.Clear();

            e.Handled = true;
        }

        Point mousepos = new Point(0, 0);
        private void PlaylistView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            drag_invalidate = true;
            mousedown = false;
            mousepos = new Point(0, 0);
        }

        private void PlaylistView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (mousepos.X != 0 && mousepos.Y != 0 && mousedown && e.LeftButton == MouseButtonState.Pressed && e.OriginalSource is TextBlock)
            {
                //Point curpos = e.MouseDevice.GetPosition(this);
                //double x = Math.Abs(mousepos.X - curpos.X);
                //double y = Math.Abs(mousepos.Y - curpos.Y);
                //if (x < 3 || y < 3)
                //    return;

                drag_invalidate = false;

                //Point curpos = e.MouseDevice.GetPosition(this);

                //double x = Math.Abs(mousepos.X - curpos.X);
                //double y = Math.Abs(mousepos.Y - curpos.Y);

                //if (x < 4 || y < 4)
                //    return;

                //mousepos.X = double.NegativeInfinity;
                //mousepos.Y = double.PositiveInfinity;

                DragDrop.DoDragDrop(PlaylistView, new DataObject("PlaylistItemData", dragItems), DragDropEffects.Move);
            }
        }

        private void PlaylistView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            setWidth();
        }
    }
}
