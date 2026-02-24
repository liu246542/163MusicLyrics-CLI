using System.Collections.Generic;
using Avalonia.Controls;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricApp.Views;

public class BlurSearchWindow : Window
{
    private BlurSearchViewModel? _viewModel;

    public BlurSearchWindow(List<SearchResultVo> searchResList)
    {
        Title = "搜索结果";
        Width = 1400;
        Height = 700;
        Icon = Constants.GetIcon("search-result");

        UpdateResults(searchResList);
    }

    public void UpdateResults(List<SearchResultVo> searchResList)
    {
        _viewModel = new BlurSearchViewModel(searchResList);
        var view = new BlurSearchView(_viewModel)
        {
            DataContext = _viewModel
        };

        DataContext = _viewModel;
        Content = view;
        _viewModel.LoadTypeAResults();
    }
}
