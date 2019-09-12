using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAS_BGConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("=====================================");
			Console.WriteLine("FAS-BG cyan tiff file to the original size at 520 ppi");
			Console.WriteLine("Copyright 2019 Genta Ito");
			Console.WriteLine("Version 2.1");
			Console.WriteLine("=====================================");
			Console.WriteLine("");

			FileStream myFS;

			// ドラッグアンドドロップされたファイルのファイルパスを取得
			// 先頭に格納される実行ファイル名を除く
			string[] filePath = Environment.GetCommandLineArgs();
			int startIndex = 0;
			int numImg = 0;
			for (int i = 0; i < filePath.Length; i++)
			{
				int len = filePath[i].Length;
				if (filePath[i].Substring(len - 3, 3) != "exe")
				{
					startIndex = i;
					break;
				}
			}
			numImg = filePath.Length - startIndex;
			string[] imageFilePath = new string[numImg];
			Array.Copy(filePath, startIndex, imageFilePath, 0, numImg);

			var imageCount = 0;

			// ファイルをひとつずつ処理する
			for (int i = 0; i < imageFilePath.Length; i++)
			{
				int imageWidth;
				int imageLength;

				System.IO.FileInfo info = new System.IO.FileInfo(imageFilePath[i]);
				Console.WriteLine("Processing " + imageFilePath[i]);

				string fileName = Path.GetFileName(imageFilePath[i]);
				string dirName = Path.GetDirectoryName(imageFilePath[i]);
				string originalTiffName = Path.GetFileNameWithoutExtension(imageFilePath[i]) + ".tif";
				string convertedTiffName = Path.GetFileNameWithoutExtension(imageFilePath[i]) + "_520ppi.tif";
				string convertedTiffPath = dirName + "\\" + convertedTiffName;

				// white/cyanによって分岐
				Console.WriteLine("White/Cyan? [w/c]: ");
				var wc = Console.ReadLine();
				if (wc == "w")
				{
					imageWidth = 3204;
					imageLength = 2102;
				}
				else if (wc == "c")
				{
					imageWidth = 3160;
					imageLength = 2074;
				}
				else
				{
					Console.WriteLine("ERROR: Invalid input");
					continue;
				}
				
				// イメージファイルを変数に格納する
				// バイナリデータ格納用変数
				byte[] raw = new byte[imageWidth * imageLength];
				using (BinaryReader br = new BinaryReader(File.Open(imageFilePath[i], FileMode.Open)))
				{
					// ヘッダー部分をスキップする
					br.BaseStream.Seek(0x00000008, SeekOrigin.Begin);

					for (int j = 0; j < raw.Length; j++)
					{
						raw[j] = br.ReadByte();
						br.ReadByte();
						br.ReadByte();
					}
				}

				using (myFS = new FileStream(convertedTiffPath, FileMode.OpenOrCreate, FileAccess.Write))
				{
					using (BinaryWriter bw = new BinaryWriter(myFS))
					{
						myFS.Seek(0x00000000, SeekOrigin.Begin);

						//TIFFファイルの共通ヘッダー
						//タグの数は13個
						byte[] data = { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08, 0x00, 0x0D };
						bw.Write(data);

						//1個めのタグ
						//ImageWidthタグ= 3160 or 3204 (w)
						if (wc == "w")
						{
							byte[] tagImageWidth = { 0x01, 0x00,
									   0x00, 0x03,
									   0x00, 0x00, 0x00, 0x01,
									   0x0C, 0x84,
									   0x00, 0x00 };
							bw.Write(tagImageWidth);
						}
						else
						{
							byte[] tagImageWidth = { 0x01, 0x00,
									   0x00, 0x03,
									   0x00, 0x00, 0x00, 0x01,
									   0x0C, 0x58,
									   0x00, 0x00 };
							bw.Write(tagImageWidth);
						}

						//2個めのタグ
						//ImageHeightタグ =2074 or 2102 (w)
						if (wc == "w")
						{
							byte[] tagImageHeight = { 0x01, 0x01,
											0x00, 0x03,
											0x00, 0x00, 0x00, 0x01,
											0x08, 0x36,
											0x00, 0x00 };
							bw.Write(tagImageHeight);
						}
						else
						{
							byte[] tagImageHeight = { 0x01, 0x01,
											0x00, 0x03,
											0x00, 0x00, 0x00, 0x01,
											0x08, 0x1A,
											0x00, 0x00 };
							bw.Write(tagImageHeight);
						}

						//3個めのタグ
						//BitsPerSampleタグ=offset
						byte[] tagBitsPerSample = { 0x01, 0x02,
										  0x00, 0x03,
										  0x00, 0x00, 0x00, 0x01,
										  0x00, 0x08, 0x00, 0x00 };
						bw.Write(tagBitsPerSample);

						//4個めのタグ
						//Compressionタグ=非圧縮
						byte[] tagCompression = { 0x01, 0x03,
										0x00, 0x03,
										0x00, 0x00, 0x00, 0x01,
										0x00, 0x01,
										0x00, 0x00 };
						bw.Write(tagCompression);

						//5個めのタグ
						//PhotometricInterpretationタグ=白コードモノクロ
						byte[] tagPhotometricInterpretation = { 0x01, 0x06,
												0x00, 0x03,
												0x00, 0x00, 0x00, 0x01,
												0x00, 0x01,
												0x00, 0x00 };
							bw.Write(tagPhotometricInterpretation);
						
						//6個めのタグ
						//StripOffsets=0x000000BA
						byte[] tagStripOffsets = { 0x01, 0x11,
										 0x00, 0x04,
										 0x00, 0x00, 0x00, 0x01,
										 0x00, 0x00, 0x00, 0xBA };
						bw.Write(tagStripOffsets);

						//7個めのタグ
						//SamplesPerPixel=1
						byte[] tagSamplesPerPixel = {0x01, 0x15,
										0x00, 0x03,
										0x00, 0x00, 0x00, 0x01,
										0x00, 0x01,
										0x00, 0x00 };
						bw.Write(tagSamplesPerPixel);

                        //8個めのタグ
                        //RowsPerStrip=2074 or 2102 (w)
                        if (wc == "w")
						{
							byte[] tagRowsPerStrip = { 0x01, 0x16,
										 0x00, 0x03,
										 0x00, 0x00, 0x00, 0x01,
										 0x08, 0x36,
										 0x00, 0x00 };
							bw.Write(tagRowsPerStrip);
						}
						else
						{
							byte[] tagRowsPerStrip = { 0x01, 0x16,
										 0x00, 0x03,
										 0x00, 0x00, 0x00, 0x01,
										 0x08, 0x1A,
										 0x00, 0x00 };
							bw.Write(tagRowsPerStrip);
						}

						//9個めのタグ
						//StripByteCounts=6553840 or 6734808 (w)
						if (wc == "w")
						{
							byte[] tagStripByteCounts = { 0x01, 0x17,
											0x00, 0x04,
											0x00, 0x00, 0x00, 0x01,
											0x00, 0x64, 0x00, 0xF0 };
							bw.Write(tagStripByteCounts);
						}
						else
						{
							byte[] tagStripByteCounts = { 0x01, 0x17,
											0x00, 0x04,
											0x00, 0x00, 0x00, 0x01,
											0x00, 0x6B, 0xB4, 0x1C };
							bw.Write(tagStripByteCounts);
						}
						//10個めのタグ
						//XResolution = offset
						byte[] tagXResolution = { 0x01, 0x1A,
										0x00, 0x05,
										0x00, 0x00, 0x00, 0x01,
										0x00, 0x00, 0x00, 0xAA };
						bw.Write(tagXResolution);

						//11個めのタグ
						//YResolution = offset
						byte[] tagYResolution = { 0x01, 0x1B,
										0x00, 0x05,
										0x00, 0x00, 0x00, 0x01,
										0x00, 0x00, 0x00, 0xB2 };
						bw.Write(tagYResolution);

						//12個めのタグ
						//PlanarConfiguration=pixel優先モード
						byte[] tagPlanarConfiguration = {0x01, 0x1C,
											0x00, 0x03,
											0x00, 0x00, 0x00, 0x01,
											0x00, 0x01,
											0x00, 0x00 };
						bw.Write(tagPlanarConfiguration);

						//13個めのタグ
						//ResolutionUnit=インチ
						byte[] tagResolutionUnit = { 0x01, 0x28,
										   0x00, 0x03,
										   0x00, 0x00, 0x00, 0x01,
										   0x00, 0x02,
										   0x00, 0x00 };
						bw.Write(tagResolutionUnit);

						//NextIFDOffset
						byte[] NextIFDOffset = { 0x00, 0x00, 0x00, 0x00 };
						bw.Write(NextIFDOffset);

						//XResolution
						if (wc == "w")
						{
							//分子=323600*2.6 pixel
							byte[] XResolutionNumerator = { 0x00, 0x0C, 0xD6, 0x90 };
							bw.Write(XResolutionNumerator);
							//分母=1640 inch
							byte[] XResolutionDenominator;
							XResolutionDenominator = new byte[] { 0x00, 0x00, 0x06, 0x68 };
							bw.Write(XResolutionDenominator);
						}
						else
						{
							//分子=328000*2.6 pixel
							byte[] XResolutionNumerator = { 0x00, 0x0D, 0x03, 0x40 };
							bw.Write(XResolutionNumerator);
							//分母=1640 inch
							byte[] XResolutionDenominator;
							XResolutionDenominator = new byte[] { 0x00, 0x00, 0x06, 0x68 };
							bw.Write(XResolutionDenominator);
						}

						//YResolution
						if (wc == "w")
						{
							//分子=212400*2.6 pixel
							byte[] YResolutionNumerator = { 0x00, 0x08, 0x6D, 0x30 };
							bw.Write(YResolutionNumerator);
							//分母=1076 inch
							byte[] YResolutionDenominator;
							YResolutionDenominator = new byte[] { 0x00, 0x00, 0x04, 0x34 };
							bw.Write(YResolutionDenominator);
						}
						else
						{
							//分子=215200*2.6 pixel
							byte[] YResolutionNumerator = { 0x00, 0x08, 0x89, 0xA0 };
							bw.Write(YResolutionNumerator);
							//分母=1076 inch
							byte[] YResolutionDenominator;
							YResolutionDenominator = new byte[] { 0x00, 0x00, 0x04, 0x34 };
							bw.Write(YResolutionDenominator);
						}

						myFS.Seek(0x000000C0, SeekOrigin.Begin);
						bw.Write(raw);
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
