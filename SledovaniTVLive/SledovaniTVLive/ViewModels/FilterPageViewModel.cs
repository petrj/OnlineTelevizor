using Android.App;
using Android.Content;
using LoggerService;
using SledovaniTVAPI;
using SledovaniTVLive.Models;
using SledovaniTVLive.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;

namespace SledovaniTVLive.ViewModels
{
    public class FilterPageViewModel : BaseViewModel
    {
        private TVService _service;
        private ISledovaniTVConfiguration _config;
        private Dictionary<string, GroupFilterItem> _groupToItem = new Dictionary<string, GroupFilterItem>();
        private Dictionary<string, FilterItem> _typeToItem = new Dictionary<string, FilterItem>();

        private string _channelNameFilter { get; set; }

        public ObservableCollection<FilterItem> Groups { get; set; } = new ObservableCollection<FilterItem>();
        public ObservableCollection<FilterItem> Types { get; set; } = new ObservableCollection<FilterItem>();

        public Command RefreshCommand { get; set; }
        public Command ClearFilterCommand { get; set; }

        public FilterItem SelectedGroupItem { get; set; }
        public FilterItem SelectedTypeItem { get; set; }

        public FilterItem FirstGroup { get; private set; } = new FilterItem() { Name = "*" };
        public FilterItem FirstType { get; private set; } = new FilterItem() { Name = "*" };

        public string ChannelNameFilter
        {
            get
            {
                return _channelNameFilter;
            }
            set
            {
                _channelNameFilter = value;
                OnPropertyChanged(nameof(ChannelNameFilter));
            }
        }

        public FilterPageViewModel(ILoggingService loggingService, ISledovaniTVConfiguration config, IDialogService dialogService, Context context, TVService service)
           : base(loggingService, config, dialogService, context)
        {
            _service = service;
            _loggingService = loggingService;
            _dialogService = dialogService;
            _context = context;
            _config = config;

            ClearFilterCommand = new Command(async () => await ClearFilter());
            RefreshCommand = new Command(async () => await Refresh());
            // SomeCommand = new Command(async () => await Task.Run(delegate { }));            
        }

        private async Task ClearFilter()
        {
            SelectedTypeItem = FirstType;
            SelectedGroupItem = FirstGroup;            
            _config.ChannelGroup = "*";
            _config.ChannelType = "*";
            ChannelNameFilter = "";

            await Refresh();
        }

        private async Task Refresh()
        {
            IsBusy = true;
          
            SelectedTypeItem = FirstType;
            SelectedGroupItem = FirstGroup;
            
            try
            {
                Groups.Clear();
                Types.Clear();

                FirstGroup.Count = 0;
                FirstType.Count = 0;

                _groupToItem.Clear();
                _typeToItem.Clear();

                Groups.Add(FirstGroup);
                Types.Add(FirstType);

                var channels = await _service.GetChannels();

                foreach (var ch in channels)
                {
                    FirstGroup.Count++;
                    FirstType.Count++;

                    if (!_groupToItem.ContainsKey(ch.Group))
                    {              
                        var g = new GroupFilterItem()
                        {
                            Name = ch.Group,
                            Count = 1
                        };

                        Groups.Add(g);

                        if ((!String.IsNullOrEmpty(_config.ChannelGroup)) && (ch.Group == _config.ChannelGroup))
                        {
                            SelectedGroupItem = g;
                        }

                        _groupToItem.Add(ch.Group,g);
                    } else
                    {
                        _groupToItem[ch.Group].Count++;
                    }

                    if (!_typeToItem.ContainsKey(ch.Type))
                    {
                        var tp = new FilterItem()
                        {
                            Name = ch.Type,
                            Count = 1
                        };

                        Types.Add(tp);

                        if ((!String.IsNullOrEmpty(_config.ChannelType)) && (ch.Type == _config.ChannelType))
                        {
                            SelectedTypeItem = tp;
                        }

                        _typeToItem.Add(ch.Type, tp);
                    }
                    else
                    {
                        _typeToItem[ch.Type].Count++;                        
                    }
                }
            } finally
            {   
                IsBusy = false;             
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(SelectedGroupItem));
                OnPropertyChanged(nameof(SelectedTypeItem));                
            }            
        }
    }
}
