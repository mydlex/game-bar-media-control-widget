using Microsoft.Gaming.XboxGameBar;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace GameBarMediaWidget
{
    public sealed partial class MainPage : Page
    {
        public MediaViewModel ViewModel { get; set; }
        private XboxGameBarWidget _widget = null;
        private GlobalSystemMediaTransportControlsSession _currentSession = null;
        private GlobalSystemMediaTransportControlsSessionManager _manager = null;
        public static bool IsPlaybackControlVisible = true;
        public static bool IsAlbumArtVisible = true;
        public static Action<bool> OnPlaybackControlVisibilityToggled;
        public static Action<bool> OnAlbumArtVisibilityToggled;

        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = new MediaViewModel();
            this.DataContext = ViewModel; // Link XAML bindings to this ViewModel
            this.Loaded += OnMainPageLoaded;
            this.Unloaded += OnPageUnloaded;

            // Make album art square
            InfoStack.SizeChanged += async (s, args) =>
            {
                double stackHeight = InfoStack.ActualHeight;
                AlbumArtImage.Height = stackHeight;
                AlbumArtImage.Width = stackHeight;

                _widget.MinWindowSize = new Size(260, stackHeight + (16 * 2));

                await Task.Yield();
                Size newSize = new Size(this.ActualWidth, stackHeight + (16 * 2));
                await _widget.TryResizeWindowAsync(newSize);
            };

            OnPlaybackControlVisibilityToggled += TogglePlaybackControlVisibility;
            OnAlbumArtVisibilityToggled += ToggleAlbumArtVisibility;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _widget = e.Parameter as XboxGameBarWidget;

            if (_widget != null)
            {
                _widget.RequestedOpacityChanged += Widget_RequestedOpacityChanged;
                BackgroundLayer.Opacity = _widget.RequestedOpacity;

                _widget.SettingsClicked += Widget_SettingsClicked;

                if (ApplicationData.Current.LocalSettings.Values.TryGetValue("PlaybackControlVisibility", out object controlVisibilityPref) && controlVisibilityPref is bool controlVisibility)
                {
                    SetPlaybackControlVisibility(controlVisibility);
                }

                if (ApplicationData.Current.LocalSettings.Values.TryGetValue("AlbumArtVisibility", out object albumArtVisibilityPref) && albumArtVisibilityPref is bool albumArtVisibility)
                {
                    SetAlbumArtVisibility(albumArtVisibility);
                }
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            if (_widget != null)
            {
                _widget.RequestedOpacityChanged -= Widget_RequestedOpacityChanged;
            }

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChangedAsync;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }

            if (_manager != null)
                _manager.CurrentSessionChanged -= OnSessionChangedAsync;

            OnPlaybackControlVisibilityToggled -= TogglePlaybackControlVisibility;
            OnAlbumArtVisibilityToggled -= ToggleAlbumArtVisibility;
        }

        private async void Widget_RequestedOpacityChanged(XboxGameBarWidget sender, object args)
        {
            await BackgroundLayer.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                BackgroundLayer.Opacity = sender.RequestedOpacity;
            });
        }

        private async void Widget_SettingsClicked(XboxGameBarWidget sender, object args)
        {
            // if necessary pre-configure any required data needed by the settings widget prior to activation
            // ...

            await sender.ActivateSettingsAsync();
        }

        private async void OnMainPageLoaded(object sender, RoutedEventArgs e)
        {
            await InitializeSMTCAsync();
        }

        private async Task InitializeSMTCAsync()
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_manager != null)
            {
                _manager.CurrentSessionChanged += OnSessionChangedAsync;
            }

            await SetupSessionAsync(_manager);

        }

        private async void OnSessionChangedAsync(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            await SetupSessionAsync(sender);
        }

        private async Task SetupSessionAsync(GlobalSystemMediaTransportControlsSessionManager manager)
        {
            var newSession = manager.GetCurrentSession();
            if (newSession != null)
            {
                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChangedAsync;
                    _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                }
                _currentSession = newSession;
                // Subscribe to events
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChangedAsync;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;

                // Optionally fetch initial data
                await SetMediaPropertiesAsync(_currentSession);
                var playback = _currentSession.GetPlaybackInfo();
                UpdatePlayPauseIcon(playback.PlaybackStatus);
            }
            else
            {
                await RootGrid.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    // Reset UI
                    ViewModel.AlbumArt = null;
                    ViewModel.TrackTitle = "";
                    ViewModel.ArtistName = "";
                    ViewModel.AlbumTitle = "";
                });
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;

            if (_currentSession?.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                _currentSession?.TryPauseAsync();
            }
            else
            {
                _currentSession?.TryPlayAsync();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;

            _currentSession?.TrySkipNextAsync();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;

            _currentSession?.TrySkipPreviousAsync();
        }

        private async void OnMediaPropertiesChangedAsync(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await SetMediaPropertiesAsync(sender);
        }

        private async Task SetMediaPropertiesAsync(GlobalSystemMediaTransportControlsSession sender)
        {
            var props = await sender.TryGetMediaPropertiesAsync();

            if (props != null)
            {
                string title = props.Title;
                string artist = props.Artist;
                string albumTitle = string.IsNullOrEmpty(props.AlbumTitle) ? string.Empty : $"• {props.AlbumTitle}";
                var thumbnailRef = props.Thumbnail;

                await RootGrid.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (thumbnailRef != null)
                    {
                        var bitmapImage = new BitmapImage();
                        using (var stream = await thumbnailRef.OpenReadAsync())
                        {
                            await bitmapImage.SetSourceAsync(stream);
                        }

                        ViewModel.AlbumArt = bitmapImage;
                    }
                    else
                    {
                        ViewModel.AlbumArt = null;
                    }
                    ViewModel.TrackTitle = title;
                    ViewModel.ArtistName = artist;
                    ViewModel.AlbumTitle = albumTitle;
                });
            }
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            var playback = sender.GetPlaybackInfo();

            UpdatePlayPauseIcon(playback.PlaybackStatus);
        }

        private async void UpdatePlayPauseIcon(GlobalSystemMediaTransportControlsSessionPlaybackStatus status)
        {
            await RootGrid.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PlayPauseIcon.Source = new BitmapImage(
                        new Uri($"ms-appx:///Assets/Buttons/{(status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing ? "pause" : "play")}.png")
                    );
                });
        }

        public async void TogglePlaybackControlVisibility(bool isVisible)
        {
            await RootGrid.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SetPlaybackControlVisibility(isVisible);
            });
            ApplicationData.Current.LocalSettings.Values["PlaybackControlVisibility"] = isVisible;
        }

        private void SetPlaybackControlVisibility(bool isVisible)
        {
            PlaybackControlStack.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            IsPlaybackControlVisible = isVisible;
        }

        public async void ToggleAlbumArtVisibility(bool isVisible)
        {
            await RootGrid.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SetAlbumArtVisibility(isVisible);
            });
            ApplicationData.Current.LocalSettings.Values["AlbumArtVisibility"] = isVisible;
        }

        private void SetAlbumArtVisibility(bool isVisible)
        {
            AlbumArtImage.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            IsAlbumArtVisible = isVisible;
        }
    }
}
