﻿using MpFree4k.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MpFree4k.Classes
{
    public static class UserConfig
    {
        private static bool _showFullAlbum = true;
        public static bool ShowFullAlbum { get
            {
                return _showFullAlbum;
            }
            set
            {
                _showFullAlbum = value;
            }
        }

        private static bool _autoSavePlaylist = true;
        public static bool AutoSavePlaylist
        {
            get
            {
                return _autoSavePlaylist;
            }
            set
            {
                _autoSavePlaylist = value;
            }
        }

        private static bool _rememberSelectedAlbum = true;
        public static bool RememberSelectedAlbums
        {
            get { return _rememberSelectedAlbum; }
            set
            {
                _rememberSelectedAlbum = value;
            }

        }
        public static SkinColors Skin = SkinColors.Black_Smooth;
        public static FontSize FontSize = FontSize.Normal;
        public static ControlSize ControlSize = ControlSize.Bigger;
        public static ArtistViewType ArtistViewType = ArtistViewType.Plain;
        public static AlbumViewType AlbumViewType = AlbumViewType.Detail;
        public static TrackViewType TrackViewType = TrackViewType.List;
        public static int NumberRecentAlbums = 15;
        public static int NumberRecentTracks = 800;

    }
}
