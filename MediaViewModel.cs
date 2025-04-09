using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.Media.Control;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace GameBarMediaWidget
{
    public class MediaViewModel : INotifyPropertyChanged
    {
        private string _trackTitle;
        public string TrackTitle
        {
            get => _trackTitle;
            set { _trackTitle = value; OnPropertyChanged(); }
        }

        private string _artistName;
        public string ArtistName
        {
            get => _artistName;
            set { _artistName = value; OnPropertyChanged(); }
        }

        private string _albumTitle;
        public string AlbumTitle
        {
            get => _albumTitle;
            set { _albumTitle = value; OnPropertyChanged(); }
        }

        private ImageSource _defaultAlbumArt = new BitmapImage(
                        new Uri("ms-appx:///Assets/Icons/AlbumArtPlaceholder.scale-150.png"));
        private ImageSource _albumArt;
        public ImageSource AlbumArt
        {
            get
            {
                if (_albumArt != null)
                    return _albumArt;
                else
                    return _defaultAlbumArt;
            }
            set
            {
                if (value != null)
                    _albumArt = value;
                else
                    _albumArt = _defaultAlbumArt;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected async void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            await Window.Current.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
            );
        }
    }
}
