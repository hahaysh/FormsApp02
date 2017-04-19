using Android.App;
using Android.Widget;
using Android.OS;

namespace XamarinAndroid01
{
    [Activity(Label = "XamarinAndroid01", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 0;
        Button btnClickme;
        Button btnCallme;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            btnClickme = FindViewById<Button>(Resource.Id.button1);
            btnClickme.Click += BtnClickme_Click;
            btnCallme = FindViewById<Button>(Resource.Id.button2);
            btnCallme.Click += BtnCallme_Click; ;

            // Set our view from the "main" layout resource
            // SetContentView (Resource.Layout.Main);
        }

        private void BtnCallme_Click(object sender, System.EventArgs e)
        {
            
            //throw new System.NotImplementedException();
        }

        private void BtnClickme_Click(object sender, System.EventArgs e)
        {
            //throw new System.NotImplementedException();
            btnClickme.Text = string.Format("{0} Clicked", count++);
        }
    }
}

