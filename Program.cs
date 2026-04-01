using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace DecodeSave;

public class Program
{
	public static int Main()
	{
		Dictionary<string, Dictionary<string, string>> sheets = [];
		Dictionary<string, string> metadata = [];
		List<string> contents = [];
		string cwd = Path.GetDirectoryName(AppContext.BaseDirectory) ?? "/";
		var targetDir = TryCreateDir(Path.Combine(cwd, "text-asset"));
		var outputDir = TryCreateDir(Path.Combine(cwd, "output"));

		FileInfo[] fileInfos = targetDir.GetFiles();
		if (fileInfos.Length == 0)
		{
			Console.WriteLine($"No files to decrypt. Place *.bytes files in '{targetDir.FullName}' directory and restart.");
			return 1;
		}

		string commonFileNameStart = fileInfos[0].Name;
		var tmpDir = TryCreateDir(Path.Combine(cwd, "tmp"));
		foreach (var (index, fileInfo) in fileInfos.Index())
		{
			commonFileNameStart = string.Concat(commonFileNameStart
				.Zip(fileInfo.Name)
				.TakeWhile(tuple => tuple.First == tuple.Second)
				.Select(tuple => tuple.First));
			using var sr = new StreamReader(fileInfo.FullName);
			Console.Write($"Decoding {fileInfo.Name} ...");

			string content = Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(sr.ReadToEnd())));
			contents.Add(content);

			string outputFilename = fileInfo.Name.Replace(fileInfo.Extension, ".txt");
			using var outputSr = new StreamWriter(Path.Combine(tmpDir.FullName, outputFilename));
			outputSr.Write(content);

			Console.WriteLine("done!");
		}

		foreach (var (content, fileInfo) in contents.Zip(fileInfos))
		{
			string sheet = fileInfo.Name
				.Replace(fileInfo.Extension, "")[commonFileNameStart.Length..];
			using var xmlReader = XmlReader.Create(new StringReader(content));
			while (xmlReader.ReadToFollowing("entry"))
			{
				xmlReader.MoveToFirstAttribute();
				string value = xmlReader.Value;
				xmlReader.MoveToElement();
				string text = xmlReader.ReadElementContentAsString().Trim();
				text = UnescapeXml(text);
				if (!sheets.ContainsKey(sheet))
				{
					sheets[sheet] = [];
				}

				sheets[sheet][value] = text;
			}
		}

		string saveDirName = commonFileNameStart;
		if (string.IsNullOrEmpty(saveDirName))
		{
			saveDirName = "output";
		}

		if (saveDirName.EndsWith('_'))
		{
			saveDirName = saveDirName[0..^1];
		}

		var saveDir = TryCreateDir(Path.Combine(outputDir.FullName, saveDirName));
		var sheetDirPath = Path.Combine(saveDir.FullName, "sheet");

		Console.WriteLine($"Moving file to {saveDir.FullName}");
		if (Directory.Exists(sheetDirPath))
		{
			var sheetDir = TryCreateDir(sheetDirPath);
			foreach (var file in tmpDir.GetFiles())
			{
				file.CopyTo(Path.Combine(sheetDir.FullName, file.Name), true);
			}
			tmpDir.Delete(true);
		}
		else
		{
			tmpDir.MoveTo(sheetDirPath);
		}

		metadata["Language"] = saveDirName == "output" ? "" : saveDirName;

		var entryDir = TryCreateDir(Path.Combine(saveDir.FullName, "entry"));
		File.WriteAllBytes(Path.Combine(entryDir.FullName, "entry.json"), JsonSerializer.SerializeToUtf8Bytes(sheets, new JsonSerializerOptions { WriteIndented = true }));
		File.WriteAllBytes(Path.Combine(entryDir.FullName, "metadata.json"), JsonSerializer.SerializeToUtf8Bytes(metadata, new JsonSerializerOptions { WriteIndented = true }));
		File.Copy(Path.Combine(entryDir.FullName, "metadata.json"), Path.Combine(sheetDirPath, "metadata.json"));

		Console.WriteLine($"All files are processed succesfully! The decoded results are produced in '{saveDir.FullName}'.");
		return 0;
	}

#pragma warning disable SYSLIB0022 // Type or member is obsolete
	static byte[] Decrypt(byte[] encryptedBytes)
	{
		using RijndaelManaged rijndaelManaged = new RijndaelManaged
		{
			Key = Encoding.UTF8.GetBytes("UKu52ePUBwetZ9wNX88o54dnfKRu0T1l"),
			Mode = CipherMode.ECB,
			Padding = PaddingMode.PKCS7
		};

		return rijndaelManaged.CreateDecryptor().TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
	}
#pragma warning restore SYSLIB0022

	static string UnescapeXml(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		return s.Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&gt;", ">")
			.Replace("&lt;", "<")
			.Replace("&amp;", "&");
	}

	static DirectoryInfo TryCreateDir(string path)
	{
		DirectoryInfo dir = new DirectoryInfo(path);
		if (!dir.Exists)
		{
			dir.Create();
		}

		return dir;
	}
}