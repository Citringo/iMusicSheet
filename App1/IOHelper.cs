using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Foundation;

namespace App1
{
	class IOHelper
	{
		public static void CreateDirectory(string path)
		{
			var directoryname = GetFullPath(path);
			Directory.CreateDirectory(directoryname);
		}

		public static void CopyFile(string basepath, string targetpath)
		{
			var path1 = GetFullPath(basepath);
			var path2 = GetFullPath(targetpath);

			File.Copy(path1, path2);

		}

		public static void Delete(string path)
		{
			File.Delete(GetFullPath(path));
		}

		public static string GetFullPath(string path)
		{
			var documents =
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			return Path.Combine(documents, path);
		}

		public static string GetFullBundlePath(string path)
		{
			var documents =
				Path.Combine(NSBundle.MainBundle.BundlePath);
			return Path.Combine(documents, path);
		}

		public static bool Exists(string path)
		{
			return File.Exists(GetFullPath(path));
		}

		/// <summary>
		/// アプリ連携によって保存されたファイルをプレイリストに読み込みます。
		/// </summary>
		/// <param name="smfname">対象のファイル。</param>
		/// <exception cref="ArgumentException">ファイルが重複している。</exception>
		public static void StoreSmf(string smfname)
		{
			string fileName = "";
			for (int i = 0; i < 1000; i++)
			{
				if (i == 0)
				{
					fileName = smfname;
				}
				else
				{
					fileName = $"{Path.GetFileNameWithoutExtension(smfname)}({i}).{Path.GetExtension(smfname)}";
				}

				// 重複しなくなったら抜ける
				if (!Exists("Music/" + fileName))
					break;
			}

			if (Exists(fileName))
			{
				throw new ArgumentException("ファイル名が重複しています。");
			}


			CopyFile($"Inbox/{smfname}", $"Music/{fileName}");
			foreach (string s in Directory.GetFiles(GetFullPath("Inbox")))
				File.Delete(s);
        }


	}
}
