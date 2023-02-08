using LoggerService;
using SledovaniTVAPI;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Threading;

namespace OnlineTelevizor.ViewModels
{
    public class FilterPageViewModel : BaseViewModel
    {
        private TVService _service;
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private GroupFilterItem _selectedGroupItem;
        private TypeFilterItem _selectedTypeItem;

        public ObservableCollection<GroupFilterItem> Groups { get; set; } = new ObservableCollection<GroupFilterItem>();
        public ObservableCollection<FilterItem> Types { get; set; } = new ObservableCollection<FilterItem>();

        public Command RefreshCommand { get; set; }
        public Command ClearFilterCommand { get; set; }

        public GroupFilterItem FirstGroup { get; private set; } = new GroupFilterItem() { Name = "*" };
        public TypeFilterItem FirstType { get; private set; } = new TypeFilterItem() { Name = "*" };

        private bool _toolBarFocused = false;

        public bool ToolBarFocused
        {
            get
            {
                return _toolBarFocused;
            }
            set
            {
                _toolBarFocused = value;
                OnPropertyChanged(nameof(RefreshIcon));
            }
        }

        public string RefreshIcon
        {
            get
            {
                if (ToolBarFocused)
                {
                    return "RefreshSelected.png";
                }
                else
                {
                    return "Refresh.png";
                }
            }
        }

        public string FontSizeForPicker
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string FontSizeForLabel
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public string FontSizeForEntry
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string ChannelNameFilter
        {
            get
            {
                return Config.ChannelFilterName;
            }
            set
            {
                Config.ChannelFilterName = value;
                OnPropertyChanged(nameof(ChannelNameFilter));
            }
        }

        public GroupFilterItem SelectedGroupItem
        {
            get
            {
                return _selectedGroupItem;
            }
            set
            {
                _selectedGroupItem = value;
                Config.ChannelFilterGroup = value == null ? "*" : value.Name;

                OnPropertyChanged(nameof(SelectedGroupItem));
            }
        }

        public TypeFilterItem SelectedTypeItem
        {
            get
            {
                return _selectedTypeItem;
            }
            set
            {
                _selectedTypeItem = value;
                Config.ChannelFilterType = value == null ? "*" : value.Name;
                OnPropertyChanged(nameof(SelectedTypeItem));
            }
        }

        public bool ShowOnlyFavouriteChannels
        {
            get
            {
                return Config.ShowOnlyFavouriteChannels;
            }
            set
            {
                Config.ShowOnlyFavouriteChannels = value;

                OnPropertyChanged(nameof(ShowOnlyFavouriteChannels));
            }
        }

        public FilterPageViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
           : base(loggingService, config, dialogService)
        {
            _service = service;
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;

            ClearFilterCommand = new Command(async () => await ClearFilter());
            RefreshCommand = new Command(async () => await Refresh());
        }

        public void NotifyFontSizeChange()
        {
            OnPropertyChanged(nameof(FontSizeForEntry));
            OnPropertyChanged(nameof(FontSizeForLabel));
            OnPropertyChanged(nameof(FontSizeForPicker));
        }

        private async Task ClearFilter()
        {
            SelectedTypeItem = FirstType;
            SelectedGroupItem = FirstGroup;
            ChannelNameFilter = "";
            ShowOnlyFavouriteChannels = false;

            await Refresh();
        }

        private async Task Refresh()
        {
            await _semaphoreSlim.WaitAsync();

            IsBusy = true;

            // Clearing Pickers leads to clearing config value via SelectedTypeItem
            var selectedGroupConfig = Config.ChannelFilterGroup;
            var selectedTypeConfig = Config.ChannelFilterType;

            try
            {
                Groups.Clear();
                Types.Clear();

                FirstGroup.Count = 0;
                FirstType.Count = 0;

                var groupToItem = new Dictionary<string, GroupFilterItem>();
                var typeToItem = new Dictionary<string, TypeFilterItem>();

                var channels = await _service.GetChannels();

                foreach (var ch in channels)
                {
                    FirstGroup.Count++;
                    FirstType.Count++;

                    if (!groupToItem.ContainsKey(ch.Group))
                    {
                        var g = new GroupFilterItem()
                        {
                            Name = ch.Group,
                            Count = 1
                        };

                        groupToItem.Add(ch.Group,g);
                    } else
                    {
                        groupToItem[ch.Group].Count++;
                    }

                    if (!typeToItem.ContainsKey(ch.Type))
                    {
                        var tp = new TypeFilterItem()
                        {
                            Name = ch.Type,
                            Count = 1
                        };

                        typeToItem.Add(ch.Type, tp);
                    }
                    else
                    {
                        typeToItem[ch.Type].Count++;
                    }
                }

                Groups.Add(FirstGroup);
                foreach (var kvp in groupToItem)
                    Groups.Add(kvp.Value);

                // selecting available after create ALL Groups
                if (!string.IsNullOrEmpty(selectedGroupConfig))
                {
                    foreach (var grp in Groups)
                    {
                        if (grp.Name == selectedGroupConfig)
                        {
                            SelectedGroupItem = grp;
                            break;
                        }
                    }
                }

                Types.Add(FirstType);
                foreach (var kvp in typeToItem)
                    Types.Add(kvp.Value);

                if (!string.IsNullOrEmpty(selectedTypeConfig))
                {
                    foreach (var kvp in typeToItem)
                    {
                        if (kvp.Key == selectedTypeConfig)
                        {
                            SelectedTypeItem = kvp.Value;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while refreshing filter page data");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsBusy));

                if (SelectedTypeItem == null)
                    SelectedTypeItem = FirstType;

                if (SelectedGroupItem == null)
                    SelectedGroupItem = FirstGroup;

                _semaphoreSlim.Release();
            }
        }
    }
}
