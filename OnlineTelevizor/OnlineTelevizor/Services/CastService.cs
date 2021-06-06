using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineTelevizor.Services
{    
    // https://code.videolan.org/mfkl/libvlcsharp-samples/-/blob/master/Chromecast/Chromecast/MainPage.xaml.cs    

    public class CastService
    {
        LibVLC _libVLC;
        RendererDiscoverer _rendererDiscoverer;
        readonly HashSet<RendererItem> _rendererItems = new HashSet<RendererItem>();
        IDialogService _dialogService;
        MediaPlayer _mediaPlayer;

        public CastService(IDialogService dialogService)
        {
            _dialogService = dialogService;
            DiscoverChromecasts();
        }

        private bool DiscoverChromecasts()
        {
            _rendererItems.Clear();

            // load native libvlc libraries
            Core.Initialize();

            // create core libvlc object
            _libVLC = new LibVLC();

            // create a renderer discoverer
            _rendererDiscoverer = new RendererDiscoverer(_libVLC);

            // register callback when a new renderer is found
            _rendererDiscoverer.ItemAdded += RendererDiscoverer_ItemAdded;            

            // start discovery on the local network
            return _rendererDiscoverer.Start();
        }

        /// <summary>
        /// Raised when a renderer has been discovered or has been removed
        /// </summary>
        private void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            // add newly found renderer item to local collection
            _rendererItems.Add(e.RendererItem);
        }

        public void StopCasting()
        {
            if (_mediaPlayer != null)
                _mediaPlayer.Stop();
        }

        /// <summary>
        /// This is the method that starts the playback on the chromecast
        /// </summary>
        public async Task StartCasting(string url)
        {
            // abort casting if no renderer items were found
            if (!_rendererItems.Any())
            {
                await _dialogService.Information("Nebylo nalezeno žádné zařízení");                
                return;
            }

            // create new media
            using (var media = new Media(_libVLC, new Uri(url)))
            {
                // create the mediaplayer
                _mediaPlayer = new MediaPlayer(_libVLC);

                var items = new List<string>();
                var itemNameToRenderer = new Dictionary<string, RendererItem>();
                foreach (var item in _rendererItems)
                {
                    var name = item.Name;

                    var count = 2;
                    while (itemNameToRenderer.ContainsKey(name))
                    {
                        name = $"{item.Name} ({count})";
                        count++;
                    }

                    items.Add(name);
                    itemNameToRenderer.Add(name, item);
                }

                var selectedRenderer = await _dialogService.Select(items, "Odeslat do", "Zpět");

                if (selectedRenderer == "Zpět")
                    return;

                _mediaPlayer.SetRenderer(itemNameToRenderer[selectedRenderer]);

                _mediaPlayer.Play(media);
            }
        }

        public bool IsCasting()
        {
            return _mediaPlayer != null && _mediaPlayer.IsPlaying;
        }
    }
}
