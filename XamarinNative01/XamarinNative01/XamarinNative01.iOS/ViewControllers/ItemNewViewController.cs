using System;
using XamarinNative01.Helpers;
using XamarinNative01.Model;
using XamarinNative01.ViewModel;
using UIKit;

namespace XamarinNative01.iOS
{
	public partial class ItemNewViewController : UIViewController
    {
        public Item Item { get; set; }
        public ItemsViewModel ViewModel { get; set; }

		public ItemNewViewController(IntPtr handle) : base(handle)
		{

        }

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

            // Perform any additional setup after loading the view, typically from a nib.

            btnSaveItem.TouchUpInside += async (sender, e) =>
			{
				var _item = new Item();
				_item.Text = txtTitle.Text;
				_item.Description = txtDesc.Text;

                await ViewModel.AddItem(_item);
                NavigationController.PopViewController(true);
			};
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}

