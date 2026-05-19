namespace EatTogether.API.Models.Infra
{
	public class ImageUrlResolver
	{
		private readonly string _adminBaseUrl;
		private readonly bool _useLocalFiles;
		private readonly string _baseFolder;
		public ImageUrlResolver(IConfiguration config)
		{
			var staticRoot = config["StaticFilesRoot"];
			_adminBaseUrl = config["AdminBaseUrl"] ?? "";
			_useLocalFiles = !string.IsNullOrEmpty(staticRoot);
			_baseFolder = _useLocalFiles
				? staticRoot!
				: Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
		}

		public string Resolve(string? dbFileName, string itemName, string subFolder)
		{
			string fileName;

			if (!string.IsNullOrEmpty(dbFileName))
			{
				fileName = dbFileName;
			}
			else
			{
				var safeName = itemName ?? "";
				foreach(var c in Path.GetInvalidFileNameChars())
				{
					safeName = safeName.Replace(c, '_');
				}

				if (_useLocalFiles)
				{
					var folderPath = Path.Combine(_baseFolder, subFolder);
					var match = Directory.Exists(folderPath)
						? Directory.EnumerateFiles(folderPath, $"{safeName}.*").FirstOrDefault()
						: null;
					return match != null
						? $"/images/{subFolder}/{Path.GetFileName(match)}"
						: "";
				}

				fileName = $"{safeName}.jpg";
			}

			return _useLocalFiles
				? $"/images/{subFolder}/{fileName}"
				: $"{_adminBaseUrl}/images/{subFolder}/{fileName}";

			
		}
	}
}
