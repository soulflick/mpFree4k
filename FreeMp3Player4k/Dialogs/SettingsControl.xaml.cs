﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Xml;
using System.Xml.Linq;
using MpFree4k.Classes;
using MpFree4k.Enums;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MpFree4k.Dialogs
{

    /// <summary>
    /// Interaktionslogik für LibrarySelector.xaml
    /// </summary>
    public partial class SettingControl : Window, INotifyPropertyChanged
    {
        static string settingsFile = "Settings.xml";

        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { return; };

        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        public SettingControl()
        {
            this.DataContext = this;

            this.Loaded += SettingControl_Loaded;
            InitializeComponent();


            comboSkins.SelectionChanged += ComboBoxSkin_SelectionChanged;
            comboSizes.SelectionChanged += ComboSizes_SelectionChanged;

            autoSavePlaylist.IsChecked = UserConfig.AutoSavePlaylist;
            showAlbumArtists.IsChecked = UserConfig.ShowFullAlbum;
            rememberSelected.IsChecked = UserConfig.RememberSelectedAlbums;

            readConfig();
        }

        private void SettingControl_Loaded(object sender, RoutedEventArgs e)
        {
            comboSizes.SelectedIndex = (int)UserConfig.FontSize;
            comboSkins.SelectedIndex = (int)UserConfig.Skin;

            bool found = false;
            for (int i = 0; i < comboControlSize.Items.Count; i++)
            {
                if (((int)(comboControlSize.Items[i] as ComboBoxItem).Tag) == (int)UserConfig.ControlSize)
                {
                    comboControlSize.SelectedIndex = i;
                    found = true;
                    break;
                }
            }
            if (!found)
                comboControlSize.SelectedIndex = 1;
        }

        private bool clicked = false;
        private void ComboSizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!clicked)
                return;

            ComboBoxItem ci = (sender as ComboBox).SelectedItem as ComboBoxItem;
            if (ci == null)
                return;

            FontSize size = (FontSize)ci.Tag;
            UserConfig.FontSize = size;
            if (MainWindow._singleton.TableView.ArtistView != null)
                SkinAdaptor.ApplyFontSize(MainWindow._singleton, size);

            MainWindow._singleton.Player.NotifyPropertyChanged("ButtonSize");

        }

        public bool HasConfig = false;

        void readConfig()
        {
            if (!File.Exists(settingsFile))
                return;

            HasConfig = true;
            XDocument doc = XDocument.Load(settingsFile);

            var keys = doc.Descendants("setting");
            foreach (var key in keys)
            {
                if (key.Attribute("name") == null ||
                   key.Attribute("value") == null)
                    continue;

                string key_str = key.Attribute("name").Value.ToLower();
                string key_val = key.Attribute("value").Value.ToLower();

                if (key_str.ToLower() == "skin")
                {
                    if (key_val == "gray") UserConfig.Skin = SkinColors.Gray;
                    else if (key_val == "dark") UserConfig.Skin = SkinColors.Dark;
                    else if (key_val == "blue") UserConfig.Skin = SkinColors.Blue;
                    else if (key_val == "black_smooth") UserConfig.Skin = SkinColors.Black_Smooth;
                    else UserConfig.Skin = SkinColors.White;
                }

                else if (key_str.ToLower() == "fontsize")
                {
                    int a = 0;
                    if (int.TryParse(key_val, out a))
                    {
                        if (Enum.IsDefined(typeof(FontSize), a))
                        {
                            UserConfig.FontSize = (FontSize)a;
                        }
                    }
                }

                else if (key_str.ToLower() == "controlsize")
                {
                    int a = 0;
                    if (int.TryParse(key_val, out a))
                    {
                        if (Enum.IsDefined(typeof(ControlSize), a))
                        {
                            UserConfig.ControlSize = (ControlSize)a;
                        }
                    }
                }

                else if (key_str == "artistviewtype")
                {
                    int a = 0;
                    if (int.TryParse(key_val, out a))
                    {
                        if (Enum.IsDefined(typeof(ArtistViewType), a))
                        {
                            UserConfig.ArtistViewType = (ArtistViewType)a;
                        }
                    }
                }

                else if (key_str == "albumviewtype")
                {
                    int a = 0;
                    if (int.TryParse(key_val, out a))
                    {
                        if (Enum.IsDefined(typeof(AlbumViewType), a))
                        {
                            UserConfig.AlbumViewType = (AlbumViewType)a;
                        }
                    }
                }

                else if (key_str == "trackviewtype")
                {
                    int a = 0;
                    if (int.TryParse(key_val, out a))
                    {
                        if (Enum.IsDefined(typeof(TrackViewType), a))
                        {
                            UserConfig.TrackViewType = (TrackViewType)a;
                        }
                    }
                }

                else if (key_str == "autosaveplaylist")
                {
                    bool _true = true;
                    if (bool.TryParse(key_val, out _true))
                        UserConfig.AutoSavePlaylist = _true;
                }

                else if (key_str == "showfullalbum")
                {
                    bool _true = false;
                    if (bool.TryParse(key_val, out _true))
                        UserConfig.ShowFullAlbum = _true;
                }

                else if (key_str == "rememberalbums")
                {
                    bool _true = false;
                    if (bool.TryParse(key_val, out _true))
                        UserConfig.RememberSelectedAlbums = _true;
                }

                else if (key_str == "numbertracks")
                {
                    int a = 0;
                    if (int.TryParse(key_val, out a))
                    {
                        UserConfig.NumberRecentTracks = a;
                        tbNumTracks.Text = a.ToString();
                    }
                }

                else if (key_str == "numberalbums")
                {
                    int a = 0;
                    if (int.TryParse(key_val, out a))
                    {
                        UserConfig.NumberRecentAlbums = a;
                        tbNumAlbums.Text = a.ToString();
                    }
                }

            }

        }

        string sanitize(string str)
        {
            char[] _c = new char[] { '/', '\\', '"', '\t', '\n', '\r' };
            foreach (char c in _c)
                str = str.Replace(c, '_');
            return str;
        }



        private void ComboBoxSkin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!clicked)
                return;

            ComboBoxItem ci = (sender as ComboBox).SelectedItem as ComboBoxItem;
            if (ci == null)
                return;

            SkinColors selected_Color = (SkinColors)ci.Tag;

            if (MainWindow._singleton.TableView.ArtistView != null)
                SkinAdaptor.ApplySkin(MainWindow._singleton, selected_Color, UserConfig.FontSize);

            UserConfig.Skin = selected_Color;
        }

        private void _This_Closing(object sender, CancelEventArgs e)
        {
            UserConfig.ShowFullAlbum = showAlbumArtists.IsChecked == true;
            UserConfig.AutoSavePlaylist = autoSavePlaylist.IsChecked == true;
            UserConfig.RememberSelectedAlbums = rememberSelected.IsChecked == true;
            WriteUserConfig();
        }

        public static void WriteUserConfig()
        {
            string doc = "<xml>\n\t<settings>\n";
            doc += "\t\t<setting name=\"skin\" value=\"" + UserConfig.Skin.ToString() + "\"/>\n";
            doc += "\t\t<setting name=\"fontsize\" value=\"" + (int)UserConfig.FontSize + "\"/>\n";
            doc += "\t\t<setting name=\"controlsize\" value=\"" + (int)UserConfig.ControlSize + "\"/>\n";
            doc += "\t\t<setting name=\"artistviewtype\" value=\"" + (int)UserConfig.ArtistViewType + "\"/>\n";
            doc += "\t\t<setting name=\"albumviewtype\" value=\"" + (int)UserConfig.AlbumViewType + "\"/>\n";
            doc += "\t\t<setting name=\"trackviewtype\" value=\"" + (int)UserConfig.TrackViewType + "\"/>\n";
            doc += "\t\t<setting name=\"autosaveplaylist\" value=\"" + (bool)UserConfig.AutoSavePlaylist + "\"/>\n";
            doc += "\t\t<setting name=\"showfullalbum\" value=\"" + (bool)UserConfig.ShowFullAlbum + "\"/>\n";
            doc += "\t\t<setting name=\"rememberalbums\" value=\"" + (bool)UserConfig.RememberSelectedAlbums + "\"/>\n";
            doc += "\t\t<setting name=\"numbertracks\" value=\"" + UserConfig.NumberRecentTracks + "\"/>\n";
            doc += "\t\t<setting name=\"numberalbums\" value=\"" + UserConfig.NumberRecentAlbums + "\"/>\n";
            doc += "\t</settings>\n</xml>";

            File.WriteAllText(settingsFile, doc);
        }

        private void comboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            clicked = true;
        }

        private void comboControlSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem itm = (sender as ComboBox).SelectedItem as ComboBoxItem;
            ControlSize csize = (ControlSize)itm.Tag;
            UserConfig.ControlSize = csize;
            MainWindow._singleton.Player.NotifyPropertyChanged("ButtonSize");
        }

        private void tbNumAlbums_TextChanged(object sender, TextChangedEventArgs e)
        {
            int d = 0;
            if (int.TryParse((sender as TextBox).Text, out d))
            {
                if (d > 0 && d <= 640)
                    UserConfig.NumberRecentAlbums = d;
            }
        }

        private void tbNumTracks_TextChanged(object sender, TextChangedEventArgs e)
        {
            int d = 0;
            if (int.TryParse((sender as TextBox).Text, out d))
            {
                if (d > 0 && d <= 16000)
                    UserConfig.NumberRecentTracks = d;
            }
        }
    }
}
