using LibVLCSharp.Shared;
using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace OnlineTelevizor.ViewModels
{
    public class RenderersViewModel : BaseViewModel
    {
        private LibVLC _libVLC;
        private RendererDiscoverer _rendererDiscoverer;
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public ObservableCollection<RendererItem> Renderers { get; set; } = new ObservableCollection<RendererItem>();

        public Command RefreshCommand { get; set; }

        public RendererItem _selectedItem;

        public RenderersViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
          : base(loggingService, config, dialogService)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;

            // load native libvlc libraries
            Core.Initialize();

            // create core libvlc object
            _libVLC = new LibVLC();

            // create a renderer discoverer
            _rendererDiscoverer = new RendererDiscoverer(_libVLC);

            // register callback when a new renderer is found
            _rendererDiscoverer.ItemAdded += RendererDiscoverer_ItemAdded;

            // start discovery on the local network
            _rendererDiscoverer.Start();
        }

        public RendererItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;

                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        private void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            if (!Renderers.Contains(e.RendererItem))
            {
                Renderers.Add(e.RendererItem);
            }
        }

        public LibVLC LibVLC
        {
            get
            {
                return _libVLC;
            }
        }

        public string FontSizeForRendererItem
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public string FontSizeForText
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string FontSizeForNote
        {
            get
            {
                return GetScaledSize(11).ToString();
            }
        }

        public async Task SelectPreviousItem()
        {
            await _semaphoreSlim.WaitAsync();

            await Task.Run(
                () =>
                {
                    try
                    {
                        if (Renderers.Count == 0)
                        return;

                        if (SelectedItem == null)
                        {
                            SelectedItem = Renderers[Renderers.Count - 1];
                        }
                        else
                        {
                            bool next = false;

                            for (var i = Renderers.Count - 1; i >= 0; i--)
                            {
                                var ch = Renderers[i];

                                if (next)
                                {
                                    SelectedItem = ch;
                                    break;
                                }
                                else
                                {
                                    if (ch == SelectedItem)
                                    {
                                        next = true;

                                        if (i == 0)
                                        {
                                            SelectedItem = Renderers[Renderers.Count - 1];
                                        }
                                    }
                                }
                            }
                        }

                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    };
                });
        }

        public async Task SelectNextItem()
        {
            await _semaphoreSlim.WaitAsync();

            await Task.Run(
                () =>
                {
                    try
                    {
                        if (Renderers.Count == 0)
                            return;

                        if (SelectedItem == null)
                        {
                            SelectedItem = Renderers[0];
                        }
                        else
                        {
                            bool next = false;

                            for (var i = 0; i < Renderers.Count; i++)
                            {
                                var ri = Renderers[i];

                                if (next)
                                {
                                    SelectedItem = ri;
                                    break;
                                }
                                else
                                {
                                    if (ri == SelectedItem)
                                    {
                                        next = true;

                                        if (i == Renderers.Count - 1)
                                        {
                                            SelectedItem = Renderers[0];
                                        }
                                    }
                                }
                            }
                        }

                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    };
                });
        }
    }
}
