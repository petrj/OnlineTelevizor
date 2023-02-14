using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FilterPage : ContentPage, IOnKeyDown
    {
        private FilterPageViewModel _viewModel;
        private KeyboardFocusableItemList _focusItems;
        protected ILoggingService _loggingService;


        public FilterPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, TVService service)
        {
            InitializeComponent();

            var dialogService = new DialogService(this);
            _loggingService = loggingService;

            BindingContext = _viewModel = new FilterPageViewModel(loggingService, config, dialogService, service);

            BuildFocusableItems();
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("Favourite", new List<View>() { FavouriteBoxView, FavouriteSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Type", new List<View>() { TypeBoxView, TypePicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Group", new List<View>() { GroupBoxView, GroupPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Name", new List<View>() { NameBoxView, ChannelNameEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Clear", new List<View>() { ClearButton }));

            _focusItems.OnItemFocusedEvent += FilterPage_OnItemFocusedEvent;
        }

        private void FocusOrUnfocusToolBar()
        {
            _viewModel.ToolBarFocused = !_viewModel.ToolBarFocused;

            if (_viewModel.ToolBarFocused)
            {
                _focusItems.DeFocusAll();
            } else
            {
                _focusItems.FocusItem(_focusItems.LastFocusedItemName);
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"FilterPage Page OnKeyDown {key}{(longPress ? " (long)" : "")}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Left:
                    FocusOrUnfocusToolBar();
                    break;

                case KeyboardNavigationActionEnum.Down:
                    if(_viewModel.ToolBarFocused)
                    {
                        FocusOrUnfocusToolBar();
                    } else
                    {
                        _focusItems.FocusNextItem();
                    }
                    break;

                case KeyboardNavigationActionEnum.Up:
                    if (_viewModel.ToolBarFocused)
                    {
                        FocusOrUnfocusToolBar();
                    }
                    else
                    {
                        _focusItems.FocusPreviousItem();
                    }
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:

                    if (_viewModel.ToolBarFocused)
                    {
                        _viewModel.RefreshCommand.Execute(null);
                    } else
                    switch (_focusItems.FocusedItemName)
                    {
                        case "Favourite":
                            FavouriteSwitch.IsToggled = !FavouriteSwitch.IsToggled;
                            break;

                        case "Name":
                            ChannelNameEntry.Focus();
                            break;

                        case "Type":
                            TypePicker.Focus();
                            break;

                        case "Group":
                            GroupPicker.Focus();
                            break;

                        case "Clear":
                            _viewModel.ClearFilterCommand.Execute(null);
                            break;
                    }

                    break;
            }
        }

        private void FilterPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            // scroll to element
            MainScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
        }

        protected override void OnAppearing()
        {
            _focusItems.DeFocusAll();

            base.OnAppearing();

            _viewModel.NotifyFontSizeChange();
            _viewModel.RefreshCommand.Execute(null);
        }
    }
}