using LibVLCSharp.Shared;
using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;

namespace OnlineTelevizor.ViewModels
{
    public class RenderersViewModel : BaseViewModel
    {
        private LibVLC _libVLC;
        private RendererDiscoverer _rendererDiscoverer;

        public ObservableCollection<RendererItem> Renderers { get; set; } = new ObservableCollection<RendererItem>();

        public Command RefreshCommand { get; set; }


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

    }
}
