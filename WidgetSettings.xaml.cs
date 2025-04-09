using Microsoft.Gaming.XboxGameBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace GameBarMediaWidget
{
    public sealed partial class WidgetSettings : Page
    {
        private XboxGameBarWidget _settingWidget = null;

        public WidgetSettings()
        {
            this.InitializeComponent();
            this.Unloaded += OnPageUnloaded;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _settingWidget = e.Parameter as XboxGameBarWidget;
            if (_settingWidget != null)
            {
                await _settingWidget.CenterWindowAsync();
            }
            playbackControlToggle.IsOn = MainPage.IsPlaybackControlVisible;
            albumArtToggle.IsOn = MainPage.IsAlbumArtVisible;
        }

        private void PlaybackControlToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                MainPage.OnPlaybackControlVisibilityToggled.Invoke(toggleSwitch.IsOn);
            }
        }

        private void AlbumArtToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                MainPage.OnAlbumArtVisibilityToggled.Invoke(toggleSwitch.IsOn);
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            _settingWidget = null;
        }
    }
}
