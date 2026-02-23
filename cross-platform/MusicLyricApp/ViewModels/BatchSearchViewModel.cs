using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

public partial class BatchSearchViewModel : ViewModelBase
{
    private readonly SearchService _searchService;
    private readonly StorageService _storageService;
    private readonly SettingBean _settingBean;
    private readonly IWindowProvider _windowProvider;

    public SearchResultViewModel SearchResultViewModel { get; } = new();
    public SearchParamViewModel SearchParamViewModel { get; } = new();

    public ObservableCollection<BatchSearchItemViewModel> Items { get; } = new();

    [ObservableProperty] private string _batchInputText = "";
    [ObservableProperty] private string _lastSaveFolderPath = "";
    [ObservableProperty] private string _summary = "";
    [ObservableProperty] private string _statusMessage = "";

    public BatchSearchViewModel(
        Dictionary<string, ResultVo<SaveVo>> resDict,
        List<InputSongId> inputSongIds,
        string inputText,
        SettingBean settingBean,
        SearchService searchService,
        StorageService storageService,
        IWindowProvider windowProvider)
    {
        _storageService = storageService;
        _settingBean = settingBean;
        _searchService = searchService;
        _windowProvider = windowProvider;
        BatchInputText = inputText;
        LastSaveFolderPath = _settingBean.Config.LastSaveFolderPath;
        SearchParamViewModel.Bind(_settingBean.Param);

        LoadResults(resDict, inputSongIds);
    }

    public void LoadResults(Dictionary<string, ResultVo<SaveVo>> resDict, List<InputSongId> inputSongIds, string? inputText = null)
    {
        if (!string.IsNullOrWhiteSpace(inputText))
        {
            BatchInputText = inputText;
        }

        Items.Clear();

        var saveMap = new Dictionary<string, SaveVo>();
        RenderUtils.RenderSearchResult(resDict, saveMap);
        SearchResultViewModel.SaveVoMap = saveMap;

        foreach (var input in inputSongIds)
        {
            if (!resDict.TryGetValue(input.SongId, out var result))
            {
                continue;
            }

            if (result.IsSuccess())
            {
                var saveVo = saveMap[input.SongId];
                Items.Add(new BatchSearchItemViewModel
                {
                    SongId = input.SongId,
                    SongName = saveVo.SongVo.Name,
                    Singer = string.Join(_settingBean.Config.SingerSeparator, saveVo.SongVo.Singer),
                    Album = saveVo.SongVo.Album,
                    Status = "Success",
                    Error = "",
                    Progress = 100
                });
            }
            else
            {
                Items.Add(new BatchSearchItemViewModel
                {
                    SongId = input.SongId,
                    SongName = "",
                    Singer = "",
                    Album = "",
                    Status = "Failed",
                    Error = result.ErrorMsg,
                    Progress = 0
                });
            }
        }

        var total = resDict.Count;
        var success = resDict.Values.Count(v => v.IsSuccess());
        var failed = total - success;
        Summary = $"Total {total} | Success {success} | Failed {failed}";
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task ExecuteSaveAllAsync()
    {
        try
        {
            var message = await _storageService.SaveResult(SearchResultViewModel, _settingBean, _windowProvider);
            StatusMessage = message;
            LastSaveFolderPath = _settingBean.Config.LastSaveFolderPath;
        }
        catch (System.Exception ex)
        {
            await DialogHelper.ShowMessage(ex);
        }
    }

    [RelayCommand]
    private async Task ExecuteSelectFolderAsync()
    {
        try
        {
            var folders = await _windowProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select folder",
                AllowMultiple = false
            });

            if (folders.Count == 0)
            {
                return;
            }

            var folder = folders[0];
            var localPath = folder.Path?.LocalPath ?? folder.Path?.ToString();
            if (string.IsNullOrWhiteSpace(localPath))
            {
                return;
            }

            BatchInputText = localPath;
            await RunBatchSearchAsync(localPath);
        }
        catch (System.Exception ex)
        {
            await DialogHelper.ShowMessage(ex);
        }
    }

    [RelayCommand]
    private void ExecuteSelectAll()
    {
        foreach (var item in Items)
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ExecuteClearSelected()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    private void ExecuteDeleteSelected()
    {
        var toRemove = Items.Where(item => item.IsSelected).ToList();
        if (toRemove.Count == 0)
        {
            return;
        }

        foreach (var item in toRemove)
        {
            Items.Remove(item);
            SearchResultViewModel.SaveVoMap.Remove(item.SongId);
        }

        var total = Items.Count;
        var success = Items.Count(item => item.Status == "Success" || item.Status == "Saved");
        var failed = total - success;
        Summary = $"Total {total} | Success {success} | Failed {failed}";
    }

    [RelayCommand]
    private async Task ExecuteStartSelectedAsync()
    {
        var selected = Items.Where(item => item.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "No items selected.";
            return;
        }

        var saveMap = new Dictionary<string, SaveVo>();
        foreach (var item in selected)
        {
            if (SearchResultViewModel.SaveVoMap.TryGetValue(item.SongId, out var saveVo))
            {
                saveMap[item.SongId] = saveVo;
            }
        }

        if (saveMap.Count == 0)
        {
            StatusMessage = "Selected items have no successful results.";
            return;
        }

        try
        {
            var temp = new SearchResultViewModel { SaveVoMap = saveMap };
            var message = await _storageService.SaveResult(temp, _settingBean, _windowProvider);
            StatusMessage = message;
            LastSaveFolderPath = _settingBean.Config.LastSaveFolderPath;

            foreach (var item in selected)
            {
                if (saveMap.ContainsKey(item.SongId))
                {
                    item.Status = "Saved";
                    item.Progress = 100;
                }
            }
        }
        catch (System.Exception ex)
        {
            await DialogHelper.ShowMessage(ex);
        }
    }

    private async Task RunBatchSearchAsync(string inputText)
    {
        try
        {
            SearchParamViewModel.SearchText = inputText;
            _searchService.InitSongIds(SearchParamViewModel, _settingBean);

            var result = await Task.Run(() =>
                _searchService.SearchSongs(SearchParamViewModel.SongIds, _settingBean));

            LoadResults(result, SearchParamViewModel.SongIds);
        }
        catch (System.Exception ex)
        {
            await DialogHelper.ShowMessage(ex);
        }
    }
}
