﻿using Classes;
using MpFree4k.Classes;
using MpFree4k.Enums;
using MpFree4k.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MpFree4k.Controls
{
    /// <summary>
    /// Interaktionslogik für Favourites.xaml
    /// </summary>
    public partial class Favourites : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { return; };
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }
        // FavouriteAlbums

        FavouritesViewModel VM = null;

        public Thickness TableMargin { get; set; } = new Thickness(30);

        public Favourites()
        {
            VM = new FavouritesViewModel();

            this.DataContext = VM;

            this.Loaded += Favourites_Loaded;

            InitializeComponent();

        }

        bool loaded = false;
        private void Favourites_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                TableMargin = new Thickness(3);
                OnPropertyChanged("TableMargin");
            }

            if (MainWindow._singleton.MainViews.SelectedIndex == (int)TabOrder.Favourites)
                VM.Load();
        }

        public void Reload()
        {
            VM.Load();
        }

        public void UpdateMargín(FontSize size)
        {
            TableMargin = new Thickness(2 * (int)size);
            OnPropertyChanged("TableMargin");
        }

        List<PlaylistItem> dragItems_albums = new List<PlaylistItem>();
        List<PlaylistItem> dragItems_tracks = new List<PlaylistItem>();
        private void TrackTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dragItems_tracks.Clear();

            foreach (FileViewInfo infoItm in TrackTable.SelectedItems)
            {
                if (!infoItm.IsVisible)
                    continue;

                PlaylistItem plitm = new PlaylistItem();
                PlaylistHelpers.CreateFromMediaItem(plitm, infoItm);
                dragItems_tracks.Add(plitm);
            }
        }

        private bool mousedown_tracks = false;
        private bool mousedown_albums = false;
        private void TrackTable_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            mousedown_tracks = true;
            mousepos = e.MouseDevice.GetPosition(this);
            var item = ItemsControl.ContainerFromElement(TrackTable, e.OriginalSource as DependencyObject) as DataGridRow;

            if (item != null)
            {
                if (!MainWindow.ctrl_down)
                {
                    if (item.IsSelected)
                        e.Handled = true;
                    else
                    {
                        TrackTable.SelectedItems.Clear();
                        item.IsSelected = true;
                    }
                }
                else
                {
                    if (item.IsSelected)
                    {
                        if (TrackTable.SelectedItems.Contains(item))
                            TrackTable.SelectedItems.Remove(item);
                        else
                            TrackTable.SelectedItems.Add(item);
                    }
                    //item.IsSelected = !item.IsSelected;
                }

            }
        }

        private void TrackTable_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (mousepos.X != 0 && mousepos.Y != 0 && mousedown_tracks && e.LeftButton == MouseButtonState.Pressed && e.OriginalSource is TextBlock)
            {
                Point curpos = e.MouseDevice.GetPosition(this);
                double x = Math.Abs(mousepos.X - curpos.X);
                double y = Math.Abs(mousepos.Y - curpos.Y);
                if (x < Config.drag_pixel || y < Config.drag_pixel)
                    return;

                DragDrop.DoDragDrop(TrackTable, new DataObject("MediaLibraryItemData", dragItems_tracks), DragDropEffects.Move);
            }
        }

        private void TrackTable_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileViewInfo f_Sel = TrackTable.SelectedItem as FileViewInfo;
            PlaylistItem p_i = new PlaylistItem();
            PlaylistHelpers.CreateFromMediaItem(p_i, f_Sel);
            PlaylistViewModel VM = (MainWindow._singleton).Playlist.DataContext as PlaylistViewModel;

            int playpos = VM.CurrentPlayPosition;

            if (VM.Tracks.Count > 0)
            {
                if (VM.Tracks.Count <= playpos)
                    playpos = VM.Tracks.Count;
                else
                    playpos++;
            }
            else
                playpos = 0;

            VM.Add(new List<PlaylistItem>() { p_i }, playpos);
            PlaylistItem cloned = VM.Tracks[playpos];
            VM.enumerate(playpos);
            VM.CurrentPlayPosition = cloned._position - 1;
            (MainWindow._singleton.Playlist.DataContext as PlaylistViewModel).Invoke(PlayState.Play);

        }

        Point mousepos = new Point(0, 0);
        private void TrackTable_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            mousedown_tracks = false;
            mousepos = new Point(0, 0);
        }

        private void ListAlbums_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (mousepos.X != 0 && mousepos.Y != 0 && mousedown_albums && e.LeftButton == MouseButtonState.Pressed && e.OriginalSource is TextBlock)
            {
                if (!dragItems_albums.Any())
                    return;

                Point curpos = e.MouseDevice.GetPosition(this);
                double x = Math.Abs(mousepos.X - curpos.X);
                double y = Math.Abs(mousepos.Y - curpos.Y);
                if (x < Config.drag_pixel || y < Config.drag_pixel)
                    return;

                DragDrop.DoDragDrop(ListAlbums, new DataObject("MediaLibraryItemData", dragItems_albums), DragDropEffects.Move);
            }
        }

        private void ListAlbums_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            mousedown_albums = true;
            mousepos = e.MouseDevice.GetPosition(this);
            dragItems_albums.Clear();

            SimpleAlbumItem a = (sender as ListView).SelectedItem as SimpleAlbumItem;
            if (a == null || a.Tracks == null || !a.Tracks.Any())
                return;

            foreach (string track in a.Tracks)
            {
                FileViewInfo fi = new FileViewInfo(track);
                PlaylistItem plitm = new PlaylistItem();
                PlaylistHelpers.CreateFromMediaItem(plitm, fi);
                dragItems_albums.Add(plitm);
            }

            a.IsSelected = true;

        }

        private void ListAlbums_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            mousedown_albums = false;
            mousepos = new Point(0, 0);
        }

        private void ListAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //dragItems_albums.Clear();

            //SimpleAlbumItem a = (sender as ListView).SelectedItem as SimpleAlbumItem;
            //if (a == null || a.Tracks == null || !a.Tracks.Any())
            //    return;

            //foreach (string track in a.Tracks)
            //{
            //    FileViewInfo fi = new FileViewInfo(track);
            //    PlaylistItem plitm = new PlaylistItem();
            //    PlaylistHelpers.CreateFromMediaItem(plitm, fi);
            //    dragItems_albums.Add(plitm);
            //}

            e.Handled = true;
        }

        private void ListAlbums_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            List<SimpleAlbumItem> ai = new List<SimpleAlbumItem>();
            foreach (var i in (sender as ListView).SelectedItems)
                ai.Add(i as SimpleAlbumItem);

            FavouritesViewModel VM = (this.DataContext as FavouritesViewModel);
            VM.Remove(ai);
            VM.LoadAlbums();
        }
    }
}
