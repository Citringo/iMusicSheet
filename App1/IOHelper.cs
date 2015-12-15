using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace App1
{
	class IOHelper
	{
		public static void CreateDirectory(string path)
		{
			// Sample code from the article
			var documents =
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var directoryname = Path.Combine(documents, n);
			Directory.CreateDirectory(directoryname);

		}
	}
}
