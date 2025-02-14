﻿#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using HyPlayer.Classes;
using Microsoft.Toolkit.Uwp.UI.Behaviors;
using Microsoft.Xaml.Interactivity;
using NeteaseCloudMusicApi;
using HyPlayer.Controls;
using Windows.UI.Xaml.Shapes;
using HyPlayer.HyPlayControl;

#endregion

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages;

/// <summary>
///     可用于自身或导航至 Frame 内部的空白页。
/// </summary>
public sealed partial class Me : Page, IDisposable
{
    private readonly ObservableCollection<SimpleListItem> likedPlayList = new();
    private readonly ObservableCollection<SimpleListItem> myPlayList = new();
    private string uid = "";

    public Me()
    {
        InitializeComponent();
    }

    public void Dispose()
    {
        ImageRect.ImageSource = null;
        myPlayList.Clear();
        likedPlayList.Clear();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        Dispose();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter != null)
        {
            uid = (string)e.Parameter;
            ButtonLogout.Visibility = Visibility.Collapsed;
        }
        else
        {
            uid = Common.LoginedUser.id;
        }

        LoadInfo();
        LoadPlayList();
    }

    public async void LoadPlayList()
    {
        List<SimpleListItem> mySongs = new();
        List<SimpleListItem> likedSongs = new();

        try
        {
            var json = await Common.ncapi.RequestAsync(CloudMusicApiProviders.UserPlaylist,
                new Dictionary<string, object> { ["uid"] = uid, ["limit"] = 999 });


            var myListIdx = 0;
            var subListIdx = 0;
            foreach (var PlaylistItemJson in json["playlist"].ToArray())
            {
                var ncp = NCPlayList.CreateFromJson(PlaylistItemJson);
                if (ncp.creater.id != uid)
                    //GridContainerSub.Children.Add(new PlaylistItem(ncp));
                    likedSongs.Add(
                        new SimpleListItem
                        {
                            CoverUri = ncp.cover + "?param=" + StaticSource.PICSIZE_SIMPLE_LINER_LIST_ITEM,
                            LineOne = ncp.creater.name,
                            LineThree = null,
                            LineTwo = null,
                            Order = myListIdx++,
                            ResourceId = "pl" + ncp.plid,
                            Title = ncp.name,
                            CanPlay = true
                        }
                    );
                else
                    mySongs.Add(
                        new SimpleListItem
                        {
                            CoverUri = ncp.cover + "?param=" + StaticSource.PICSIZE_SIMPLE_LINER_LIST_ITEM,
                            LineOne = ncp.creater.name,
                            LineThree = null,
                            LineTwo = null,
                            Order = subListIdx++,
                            ResourceId = "pl" + ncp.plid,
                            Title = ncp.name,
                            CanPlay = true
                        }
                    );
            }

            MySongListBox.ItemsSource = mySongs;
            LikedSongList.ItemsSource = likedSongs;
        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
        }
    }

    public async void LoadInfo()
    {
        if (uid == Common.LoginedUser?.id)
        {
            TextBoxUserName.Text = Common.LoginedUser.name;
            TextBoxSignature.Text = Common.LoginedUser.signature;
            ImageRect.ImageSource = Common.Setting.noImage
                ? null
                : new BitmapImage(new Uri(Common.LoginedUser.avatar, UriKind.RelativeOrAbsolute));
        }
        else
        {
            try
            {
                var json = await Common.ncapi.RequestAsync(CloudMusicApiProviders.UserDetail,
                    new Dictionary<string, object> { ["uid"] = uid });

                TextBoxUserName.Text = json["profile"]["nickname"].ToString();
                TextBoxSignature.Text = json["profile"]["signature"].ToString();
                ImageRect.ImageSource = Common.Setting.noImage
                    ? null
                    : new BitmapImage(new Uri(json["profile"]["avatarUrl"].ToString()));
            }
            catch (Exception ex)
            {
                Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
            }
        }
        /*
        (bool isok, JObject json) = await Common.ncapi.RequestAsync(CloudMusicApiProviders.UserLevel);
        if (isok)
        {
            TextBlockLevel.Text = "LV. " + json["data"]["level"].ToString();
        }
        */
    }

    private async void Logout_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await Common.ncapi.RequestAsync(CloudMusicApiProviders.Logout);
            Common.Logined = false;
            Common.LoginedUser = new NCUser();
            ApplicationData.Current.LocalSettings.Values["cookie"] = "";
            Common.ncapi = new CloudMusicApi();
            Common.PageMain.MainFrame.Navigate(typeof(BlankPage));
            Common.PageMain.MainFrame.Navigate(typeof(BasePage));
            ((App)Application.Current).InitializeJumpList();
        }
        catch
        {
        }
    }

    private async void BtnPlayClick(object sender, RoutedEventArgs e)
    {
        HyPlayList.RemoveAllSong();
        await HyPlayList.AppendNcSource(((Button)sender).Tag.ToString());
        HyPlayList.SongAppendDone();
        if (((Button)sender).Tag.ToString().Substring(0, 2) == "pl" ||
            ((Button)sender).Tag.ToString().Substring(0, 2) == "al")
        {
            HyPlayList.PlaySourceId = ((Button)sender).Tag.ToString().Substring(2);
        }

        HyPlayList.NowPlaying = -1;
        HyPlayList.SongMoveNext();
    }

    private void SongListItemClicked(object sender, TappedRoutedEventArgs e)
    {
        Common.NavigatePageResource(((Grid)sender).Tag.ToString());
    }
}