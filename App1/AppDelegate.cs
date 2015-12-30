using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Foundation;
using UIKit;
using static App1.Utility;
using static App1.IOHelper;
namespace App1
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations
		public override UIWindow Window
		{
			get;
			set;
		}
		// This method is invoked when the application is about to move from active to inactive state.
		// OpenGL applications should use this method to pause.
		public override void OnResignActivation (UIApplication application)
		{
			
		}
		// This method should be used to release shared resources and it should store the application state.
		// If your application supports background exection this method is called instead of WillTerminate
		// when the user quits.
		public override void DidEnterBackground (UIApplication application)
		{
		}
		// This method is called as part of the transiton from background to active state.
		public override void WillEnterForeground (UIApplication application)
		{
			var vc = this.Window.RootViewController.PresentedViewController as RootViewController;
			if (vc != null)
			{
				while (MessageQueue.Count > 0)
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
						case MSMessageType.UpdateFilelist:
								var fils = Directory.GetFiles(GetFullPath("Music"));
								for (int i = 0; i < fils.Length; i++)
									fils[i] = Path.GetFileName(fils[i]);
								vc.dataSource.UpdateDatas(fils);
								vc.FileList.ReloadData();
							break;
					}
			}
		}
		// This method is called when the application is about to terminate. Save data, if needed.
		public override void WillTerminate (UIApplication application)
		{
			
		}

		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
			if (!Directory.Exists(GetFullPath("Music")))
			{
				CreateDirectory("Music");
				Logger.Info("Created music folder!");
				File.Copy(GetFullBundlePath("Hello, music sheet.mid"), GetFullPath("Music/Hello, music sheet.mid"));
				Logger.Info("Installed a midi file named 'Hello, music sheet'.");
			}

#if DEBUG
			if (!File.Exists(GetFullPath("Music/Hello, music sheet.mid")))
			{
				File.Copy(GetFullBundlePath("Hello, music sheet.mid"), GetFullPath("Music/Hello, music sheet.mid"));
				Logger.Info("To debug, I add 'Hello, music sheet.mid' to playlist!");
			}
#endif

			/*if (launchOptions != null)
			{
				NSObject urlObject;
				if (launchOptions.TryGetValue(UIApplication.LaunchOptionsUrlKey, out urlObject))
				{
					var url = urlObject as NSUrl;
					LocateSmf(url);
				}
			}
			else
			{
				Logger.Info("App began running without launchOption. This means no file-copy.");
			}*/

			return true;
		}

		private void LocateSmf(NSUrl url)
		{
			Logger.Info("Will locate smf.");
			try
			{
				StoreSmf(url.LastPathComponent);
				MessageQueue.Enqueue(MSMessageType.ShowImportedDialog);
				MessageQueue.Enqueue(MSMessageType.UpdateFilelist);
				Logger.Info("Successfully located smf!");
			}
			catch (ArgumentException)
			{
				MessageQueue.Enqueue(MSMessageType.ShowCouldntImportDialog);
				Logger.Warning("Couldn't locate smf...");
			}
		}

		public override bool HandleOpenURL(UIApplication application, NSUrl url)
		{
			LocateSmf(url);
			
			return true;
		}


	}
}
