using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;

namespace OnlineTelevizor.Droid
{
    [Activity(Name= "net.petrjanousek.OnlineTelevizor.SplashActivity", Label = "Online Televizor", Theme = "@style/SplashTheme", Icon = "@drawable/icon", Banner = "@drawable/banner", MainLauncher = true, NoHistory = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { Intent.CategoryLauncher })]
    public class SplashActivity : AppCompatActivity
    {
        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnCreate(savedInstanceState, persistentState);
        }

        protected override void OnResume()
        {
            base.OnResume();
            new Task(() => { StartMainActivity(); }).Start();
        }

        // Prevent the back button from canceling the startup process
        public override void OnBackPressed() { }

        // Simulates background work that happens behind the splash screen
        private async void StartMainActivity ()
        {
            StartActivity(new Intent(Application.Context, typeof (MainActivity)));
        }
    }
}