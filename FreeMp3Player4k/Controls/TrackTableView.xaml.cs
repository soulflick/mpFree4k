using Classes;
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
    /// Interaktionslogik für TrackTable.xaml
    /// </summary>
    public partial class TrackTableView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { return; };
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        bool loaded = false;

        List<PlaylistItem> dragItems = new List<PlaylistItem>();
        public Thickness TableMargin { get; set; } = new Thickness(30);

        public TrackTableView()
        {
            this.DataContext = new TrackTableViewModel();
            Loaded += TrackTableView_Loaded;
            InitializeComponent();
        }

        public void SetMediaLibrary(Layers.MediaLibrary lib)
        {
            (this.DataContext as TrackTableViewModel).MediaLibrary = lib;
        }

        public void UpdateMargín(FontSize size)
        {
            TableMargin = new Thickness(2 * (int)size);
            OnPropertyChanged("TableMargin");
        }

        private void TrackTableView_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded && !Config.MediaHasChanged)
                return;

            TableMargin = new Thickness(3);

            MainWindow._singleton.Library.Current.PropertyChanged -= Current_PropertyChanged;
            MainWindow._singleton.Library.Current.PropertyChanged += Current_PropertyChanged;

            loaded = true;
            Config.MediaHasChanged = false;

            //TrackTable.ItemsSource = MainWindow._singleton.Library.Current.Files;
        }

        private void Current_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        private void TrackTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dragItems.Clear();

            foreach (FileViewInfo infoItm in TrackTable.SelectedItems)
            {
                if (!infoItm.IsVisible)
                    continue;

                PlaylistItem plitm = new PlaylistItem();
                PlaylistHelpers.CreateFromMediaItem(plitm, infoItm);
                dragItems.Add(plitm);
            }
        }

        private bool mousedown = false;
        private void TrackTable_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            mousedown = true;
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
            if (mousepos.X != 0 && mousepos.Y != 0 && mousedown && e.LeftButton == MouseButtonState.Pressed && e.OriginalSource is TextBlock)
            {
                Point curpos = e.MouseDevice.GetPosition(this);
                double x = Math.Abs(mousepos.X - curpos.X);
                double y = Math.Abs(mousepos.Y - curpos.Y);
                if (x < Config.drag_pixel || y < Config.drag_pixel)
                    return;

                DragDrop.DoDragDrop(TrackTable, new DataObject("MediaLibraryItemData", dragItems), DragDropEffects.Move);
            }
        }

        private void TrackTable_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playSelected();
        }

        private void playSelected()
        {
            PlaylistViewModel VM = (MainWindow._singleton).Playlist.DataContext as PlaylistViewModel;
            int playpos = VM.CurrentPlayPosition;
            int currentplaypos = -1;
            bool first = true;

            foreach (var f_Sel in TrackTable.SelectedItems)
            {
                PlaylistItem p_i = new PlaylistItem();
                PlaylistHelpers.CreateFromMediaItem(p_i, (FileViewInfo)f_Sel);

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
                
                if (first)
                {
                    currentplaypos = playpos;
                    VM.CurrentPlayPosition = currentplaypos;
                    (MainWindow._singleton.Playlist.DataContext as PlaylistViewModel).Invoke(PlayState.Play);
                }

                first = false;
            }

            VM.enumerate(currentplaypos);

        }

        private void _This_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow._singleton.Library.Current.QueryMe(ViewMode.Table);
        }

        Point mousepos = new Point(0, 0);
        private void TrackTable_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            mousedown = false;
            mousepos = new Point(0,0);
        }

        private void mnuCtxEdit_Click(object sender, RoutedEventArgs e)
        {
            if (TrackTable.SelectedItems == null ||
                TrackTable.SelectedItems.Count == 0)
                return;

            FileViewInfo fInfo = TrackTable.SelectedItems[0] as FileViewInfo;
            TagView view = new TagView();
            view.CurrentTag = fInfo;
            view.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            view.ShowDialog();
        }

        private void mnuCtxPlay_Click(object sender, RoutedEventArgs e)
        {
            playSelected();
        }

        private void mnuCtxAdd_Click(object sender, RoutedEventArgs e)
        {
            if (TrackTable.SelectedItem == null)
                return;

            foreach (var f_Sel in TrackTable.SelectedItems)
            {
                PlaylistItem p_i = new PlaylistItem();
                PlaylistHelpers.CreateFromMediaItem(p_i, (FileViewInfo)f_Sel);
                PlaylistViewModel VM = (MainWindow._singleton).Playlist.DataContext as PlaylistViewModel;

                VM.Add(new List<PlaylistItem>() { p_i });
            }
        }

        private void mnuCtxInsert_Click(object sender, RoutedEventArgs e)
        {
            if (TrackTable.SelectedItem == null)
                return;

            PlaylistViewModel VM = (MainWindow._singleton).Playlist.DataContext as PlaylistViewModel;
            int idx = VM.CurrentPlayPosition;

            foreach (var f_Sel in TrackTable.SelectedItems)
            {
                idx++;
                PlaylistItem p_i = new PlaylistItem();
                PlaylistHelpers.CreateFromMediaItem(p_i, (FileViewInfo)f_Sel);
               
                VM.Add(new List<PlaylistItem>() { p_i }, idx);
            }
        }
    }
}
