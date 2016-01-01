using System;
using System.Drawing;

using CoreFoundation;
using UIKit;
using Foundation;

namespace App1
{
	
	public partial class DebugViewController : UIViewController
	{

		public DebugViewController(IntPtr handle) : base(handle)
		{

		}

		UITextView textview;

		public override void DidReceiveMemoryWarning()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();

			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			this.View = textview = new UITextView();


			// Perform any additional setup after loading the view
		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);
			textview.Text = Logger.DumpedLogList;
		}

	}
}