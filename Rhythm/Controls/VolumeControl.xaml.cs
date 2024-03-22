using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Rhythm.Views;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Controls;
public sealed partial class VolumeControl : UserControl
{
    private bool _isMuted = false;
    private double _volume = 100;


    public VolumeControl()
    {
        this.InitializeComponent();
    }

    private void VolumeMuteButton_Click(object sender, RoutedEventArgs e)
    {
        _isMuted = !_isMuted;
        VolumeMuteIcon.Glyph = GetVolumeGlyph();
        if (_isMuted)
        {
            RhythmMediaPlayer.mediaPlayer.Volume = 0;
        }
        else
        {
            RhythmMediaPlayer.mediaPlayer.Volume = _volume / 100;
        }
    }

    private string GetVolumeGlyph()
    {
        if (_isMuted) return "\ue74f";
        if (_volume < 25) return "\ue992";
        if (_volume < 50) return "\ue993";
        if (_volume < 75) return "\ue994";
        return "\ue995";
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        _volume = e.NewValue;
        if (!_isMuted)
        {
            RhythmMediaPlayer.mediaPlayer.Volume = _volume / 100;
        }
        VolumeMuteIcon.Glyph = GetVolumeGlyph();
    }

    private void UserControl_Loading(FrameworkElement sender, object args)
    {
        _volume = RhythmMediaPlayer.mediaPlayer.Volume * 100;
        VolumeSlider.Value = _volume;
    }
}
