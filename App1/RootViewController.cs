using System;
using System.Drawing;
using System.Collections.Generic;
using Foundation;
using UIKit;
using MobileCoreServices;
using static App1.Utility;
using System.IO;
using AudioToolbox;

namespace App1
{
	public partial class RootViewController : UIViewController
	{
		static bool UserInterfaceIdiomIsPhone
		{
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		DataSource dataSource;

		public RootViewController(IntPtr handle) : base(handle)
		{
			
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
			while (MessageQueue.Count > 0)
			{
				switch (MessageQueue.Dequeue())
				{
					case MSMessageType.Noop:
						
						break;
					case MSMessageType.ShowCouldntImportDialog:
						MsgBox("ファイルを追加できません", "既にプレイリストに同名のファイルがあり、これ以上代替の名前をつけることができません。読み込みを中断します。");
						break;
					case MSMessageType.ShowImportedDialog:
						MsgBox("ファイルを追加しました", $"SMF は正常にプレイリストに追加されました。");
						break;
				}
			}
			var files = Directory.GetFiles(IOHelper.GetFullPath("Music"));
			for (int i = 0; i < files.Length; i++)
				files[i] = Path.GetFileName(files[i]);
            FileList.Source = dataSource = new DataSource(files);
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

		

	}



	public class DataSource : UITableViewSource
	{

		// there is NO database or storage of Tasks in this example, just an in-memory List<>
		readonly List<string> items = new List<string>();


		string cellIdentifier = "taskcell"; // set in the Storyboard

		public DataSource(string[] i)
		{
			items.AddRange(i);
			
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

		public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			MsgBox("押された", "この後MIDI再生される👍👍👍");
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
				Question("本当に削除しますか？", $"削除された項目は復元できません。それでも'{items[indexPath.Row]}'を削除しますか？", (act) =>
				{
					IOHelper.Delete($"Music/{items[indexPath.Row]}");
					items.RemoveAt(indexPath.Row);
					tableView.DeleteRows(new[] { indexPath }, UITableViewRowAnimation.Fade);
				}, (act) =>
				{
					tableView.Editing = false;
				});
				
			}
			else if (editingStyle == UITableViewCellEditingStyle.Insert)
			{
				// Create a new instance of the appropriate class, insert it into the array, and add a new row to the table view.
			}
		}


	}
}