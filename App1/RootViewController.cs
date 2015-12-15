using System;
using System.Drawing;
using System.Collections.Generic;
using Foundation;
using UIKit;
using MobileCoreServices;

namespace App1
{
	public partial class RootViewController : UIViewController
	{
		static bool UserInterfaceIdiomIsPhone
		{
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		DataSource dataSource;

		Random rnd;

		public RootViewController(IntPtr handle) : base(handle)
		{
			rnd = new Random(33-4);
		}


		public override void DidReceiveMemoryWarning()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();

			// Release any cached data, images, etc that aren't in use.
		}

		#region View lifecycle

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			FileList.Source = dataSource = new DataSource();

		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);
		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);
		}

		public override void ViewWillDisappear(bool animated)
		{
			base.ViewWillDisappear(animated);
		}

		public override void ViewDidDisappear(bool animated)
		{
			base.ViewDidDisappear(animated);
		}

		#endregion

		partial void UIBarButtonItem789_Activated(UIBarButtonItem sender)
		{
			dataSource.Items.Insert(0, GetRandomText());
			
			using (var indexPath = NSIndexPath.FromRowSection(0, 0))
				FileList.InsertRows(new[] { indexPath }, UITableViewRowAnimation.Automatic);
		}

		string GetRandomText()
		{
			var a = rnd.Next(5);
			return new[] { "hage", "hige", "huge", "hege", "hoge" }[a];
		}

	}



	public class DataSource : UITableViewSource
	{

		// there is NO database or storage of Tasks in this example, just an in-memory List<>
		readonly List<string> items = new List<string>();

		public List<string> Items
		{
			get
			{
				return items;
			}
		}
		string cellIdentifier = "taskcell"; // set in the Storyboard

		public DataSource()
		{
			
		}
		public override nint RowsInSection(UITableView tableview, nint section)
		{
			return items.Count;
		}
		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			// in a Storyboard, Dequeue will ALWAYS return a cell,
			UITableViewCell cell = tableView.DequeueReusableCell(cellIdentifier);
			// now set the properties as normal
			cell.TextLabel.Text = items[indexPath.Row];
			cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
			return cell;
		}

		public override bool CanEditRow(UITableView tableView, NSIndexPath indexPath)
		{
			// Return false if you do not want the specified item to be editable.
			return true;
		}

		public override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
		{
			if (editingStyle == UITableViewCellEditingStyle.Delete)
			{
				// Delete the row from the data source.
				items.RemoveAt(indexPath.Row);
				tableView.DeleteRows(new[] { indexPath }, UITableViewRowAnimation.Fade);
			}
			else if (editingStyle == UITableViewCellEditingStyle.Insert)
			{
				// Create a new instance of the appropriate class, insert it into the array, and add a new row to the table view.
			}
		}

	}
}