using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;

namespace FAS_BGConverter
{
	class Program
	{
		private static void Main(string[] args)
		{
			Console.WriteLine("=====================================================");
			Console.WriteLine("FAS-BG cyan tiff file to the original size at 520 ppi");
			Console.WriteLine("Copyright 2019-2020 Genta Ito");
			Console.WriteLine("Version 3.0");
			Console.WriteLine("=====================================================");
			Console.WriteLine("");

			char[] bars = { '／', '―', '＼', '―' };

			// ドラッグアンドドロップされたファイルのファイルパスを取得
			// 先頭に格納される実行ファイル名を除く
			string[] filePath = Environment.GetCommandLineArgs();
			int startIndex = 0;;
			for (int i = 0; i < filePath.Length; i++)
			{
				int len = filePath[i].Length;
				if (filePath[i].Substring(len - 3, 3) != "exe")
				{
					startIndex = i;
					break;
				}
			}
			int numImg = filePath.Length - startIndex;
			string[] imageFilePath = new string[numImg];
			Array.Copy(filePath, startIndex, imageFilePath, 0, numImg);

			int imageCount = 0;

			// ファイルをひとつずつ処理する
			for (int i = 0; i < imageFilePath.Length; i++)
			{
				// 新しいファイル名の作成
				Console.WriteLine("Processing " + imageFilePath[i]);
				string dirName = Path.GetDirectoryName(imageFilePath[i]);
				string convertedTiffName = Path.GetFileNameWithoutExtension(imageFilePath[i]) + "_520ppi.tif";
				string convertedTiffPath = dirName + "\\" + convertedTiffName;

				// 元tiffファイルの読み込み
				using (Tiff ori = Tiff.Open(imageFilePath[i], "r"))
				{
					if (ori == null)
					{
						Console.WriteLine("Could not open incoming images");
						return;
					}

					int numTag = ori.NumberOfDirectories();
					FieldValue[] imageWidth = ori.GetField(TiffTag.IMAGEWIDTH);
					FieldValue[] imageLength = ori.GetField(TiffTag.IMAGELENGTH);
					FieldValue[] bitsPerSample = ori.GetField(TiffTag.BITSPERSAMPLE);
					FieldValue[] compression = ori.GetField(TiffTag.COMPRESSION);
					FieldValue[] photometric = ori.GetField(TiffTag.PHOTOMETRIC);
					FieldValue[] stripOffsets = ori.GetField(TiffTag.STRIPOFFSETS);
					FieldValue[] samplesPerPixel = ori.GetField(TiffTag.SAMPLESPERPIXEL);
					FieldValue[] rowsPerStrip = ori.GetField(TiffTag.ROWSPERSTRIP);
					FieldValue[] stripByteCounts = ori.GetField(TiffTag.STRIPBYTECOUNTS);
					FieldValue[] xResolution = ori.GetField(TiffTag.XRESOLUTION);
					FieldValue[] yResolution = ori.GetField(TiffTag.YRESOLUTION);
					FieldValue[] planarConfig = ori.GetField(TiffTag.PLANARCONFIG);
					FieldValue[] resolutionUnit = ori.GetField(TiffTag.RESOLUTIONUNIT);

					int scanlineSize = ori.ScanlineSize();
					int newScanlineSize = scanlineSize / 3;
					// buffer = [行][各行のscanline]
					byte[][] buffer = new byte[imageLength[0].ToInt()][];

					Console.WriteLine("Started reading image ...");
					for (int j = 0; j < imageLength[0].ToInt(); j++)
					{
						buffer[j] = new byte[scanlineSize];
						ori.ReadScanline(buffer[j], j);
					}
					Console.WriteLine("Finished reading image");

					using (Tiff output = Tiff.Open(convertedTiffPath, "w"))
					{
						output.SetField(TiffTag.IMAGEWIDTH, imageWidth[0]);
						output.SetField(TiffTag.IMAGELENGTH, imageLength[0]);
						output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample[0]);
						output.SetField(TiffTag.COMPRESSION, compression[0]);
						output.SetField(TiffTag.ROWSPERSTRIP, rowsPerStrip[0]);
						output.SetField(TiffTag.PLANARCONFIG, planarConfig[0]);
						output.SetField(TiffTag.RESOLUTIONUNIT, resolutionUnit[0]);

						// 解像度の変更
						output.SetField(TiffTag.XRESOLUTION, 520);
						output.SetField(TiffTag.YRESOLUTION, 520);

						// グレイスケールに変更
						output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
						output.SetField(TiffTag.SAMPLESPERPIXEL, 1);

						byte[][] newBuffer = new byte[imageLength[0].ToInt()][];
						Console.WriteLine("Started generating image ...");
						for (int j = 0; j < imageLength[0].ToInt(); j++)
						{
							newBuffer[j] = new byte[newScanlineSize];
							for (int k = 0; k < newScanlineSize; k++)
							{
								newBuffer[j][k] = buffer[j][3 * k];
							}
							output.WriteScanline(newBuffer[j], j);
						}
						Console.WriteLine("Finished generating image");
					}
				}

				Console.WriteLine("Saved " + convertedTiffPath);
				Console.WriteLine();

				imageCount++;
			}

			// コンソールウィンドウを維持
			Console.WriteLine();
			Console.WriteLine("Finished processing " + imageCount.ToString() + " files");
			Console.WriteLine("Hit any key to quit...");
			Console.ReadKey();
		}
	}
}
