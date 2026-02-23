using System.Collections.Generic;
using Avalonia.Controls;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricApp.Views;

public class BatchSearchWindow : Window
{
    private readonly BatchSearchViewModel _viewModel;

    public BatchSearchWindow(
        Dictionary<string, ResultVo<SaveVo>> resDict,
        List<InputSongId> inputSongIds,
        string inputText,
        SettingBean settingBean,
        SearchService searchService,
        StorageService storageService,
        IWindowProvider windowProvider)
    {
        Title = "Batch Search";
        Width = 1100;
        Height = 720;

        _viewModel = new BatchSearchViewModel(
            resDict,
            inputSongIds,
            inputText,
            settingBean,
            searchService,
            storageService,
            windowProvider);
        DataContext = _viewModel;
        Content = new BatchSearchView();
        Icon = Constants.GetIcon("search-result");
    }

    public void UpdateResults(Dictionary<string, ResultVo<SaveVo>> resDict, List<InputSongId> inputSongIds, string? inputText = null)
    {
        _viewModel.LoadResults(resDict, inputSongIds, inputText);
    }
}
