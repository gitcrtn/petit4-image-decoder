using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace Petit4Send
{
    class Options
    {
        [Option('i', "inputdir", Required = true, HelpText = "Input directory path")]
        public string InputDir { get; set; }

        [Option('o', "outputdir", Required = true, HelpText = "Output directory path")]
        public string OutputDir { get; set; }
    }

    class ScreenshotProps
    {
      public uint code;
      public uint filesize;
      public uint fileCRC;
      public string filename;
      public byte totalimages;
      public byte imagecount;
      public byte xorpattern;
      public byte compresstype;
      public uint streamsize;
      public ushort width;
      public ushort height;
      public byte[] stream;

      public string id
      {
        get
        {
          return this.filename + " " + this.filesize.ToString() + "  " + this.fileCRC.ToString();
        }
      }
    }

    class ImageDecoder
    {
        private ushort[] crctable;
        private List<List<ScreenshotProps>> fileinformation;
        private string outputdir;

        public ImageDecoder()
        {
            CalcCRCTable();
            this.fileinformation = new List<List<ScreenshotProps>>();
        }

        public void StartConvert(string[] filenames, string outputdir)
        {
            this.outputdir = outputdir;
            AddFiles(filenames);
            if (CheckConvertable())
                ConvertFiles();
            else
                Console.WriteLine("Error: Convertable file not found");
        }

        private bool CheckConvertable()
        {
            bool flag = false;
            for (int index = 0; index <= this.fileinformation.Count - 1; ++index)
            {
                List<ScreenshotProps> screenshotPropsList = this.fileinformation[index];
                if (screenshotPropsList.Count == (int)screenshotPropsList[0].totalimages)
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }

        private void PrintFileInfomations()
        {
            Console.WriteLine("Files:");
            Console.WriteLine(String.Format("  {0} files", this.fileinformation.Count.ToString()));

            for (int index = 0; index <= this.fileinformation.Count - 1; ++index)
            {
                List<ScreenshotProps> screenshotPropsList = this.fileinformation[index];
                Console.WriteLine(String.Format("  {0}:", (index + 1).ToString()));
                Console.WriteLine(String.Format("    {0}/{1}:",
                                                screenshotPropsList.Count.ToString(),
                                                screenshotPropsList[0].totalimages.ToString()));
                Console.WriteLine(String.Format("      Name: {0}", screenshotPropsList[0].filename.ToString()));
                Console.WriteLine(String.Format("      Size: {0}", screenshotPropsList[0].filesize.ToString()));
                Console.WriteLine(String.Format("      CRC:  {0}", screenshotPropsList[0].fileCRC.ToString("X4")));
            }
        }

        private void AddFiles(string[] filenames)
        {
            // this.Cursor = Cursors.WaitCursor;
            for (int index1 = 0; index1 <= filenames.Length - 1; ++index1)
            {
                try
                {
                    ScreenshotProps[] embeddedInformation = this.GetEmbeddedInformation(filenames[index1]);
                    for (int index2 = 0; index2 <= embeddedInformation.Length - 1; ++index2)
                    {
                        int index3 = -1;
                        for (int index4 = 0; index4 <= this.fileinformation.Count - 1; ++index4)
                        {
                            if (this.fileinformation[index4][0].id == embeddedInformation[index2].id)
                            {
                                index3 = index4;
                                break;
                            }
                        }
                        if (index3 >= 0)
                        {
                            for (int index4 = 0; index4 <= this.fileinformation[index3].Count - 1; ++index4)
                            {
                                if ((int) this.fileinformation[index3][index4].imagecount == (int) embeddedInformation[index2].imagecount)
                                throw new Exception("same file");
                            }
                            this.fileinformation[index3].Add(embeddedInformation[index2]);
                        }
                        else
                            this.fileinformation.Add(new List<ScreenshotProps>()
                            {
                                embeddedInformation[index2]
                            });
                    }
                }
                catch (Exception ex)
                {
                    if (((IEnumerable<string>) filenames).Count<string>() == 1)
                    {
                        // this.Cursor = Cursors.Default;
                        // int num = (int) MessageBox.Show(ex.ToString());
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            // this.UpdateListView();
            this.PrintFileInfomations();
            // this.Cursor = Cursors.Default;
        }

        private void ConvertFiles()
        {
            // this.Cursor = Cursors.WaitCursor;
            int num1 = 0;
            try
            {
                for (int index1 = 0; index1 <= this.fileinformation.Count - 1; ++index1)
                {
                    List<ScreenshotProps> screenshotPropsList = this.fileinformation[index1];
                    if (screenshotPropsList.Count == (int) screenshotPropsList[0].totalimages)
                    {
                        int num2 = 0;
                        string str = screenshotPropsList[0].filename.Substring(4);
                        string upperInvariant = screenshotPropsList[0].filename.Substring(0, 4).ToUpperInvariant();
                        string path = Path.Combine(this.outputdir, str);
                        List<byte> buf = new List<byte>();
                        for (int index2 = 0; index2 <= (int) screenshotPropsList[0].totalimages - 1; ++index2)
                        {
                            int index3 = 0;
                            while (index3 <= screenshotPropsList.Count - 1 && (int) screenshotPropsList[index3].imagecount != index2)
                                ++index3;
                            if (index3 >= screenshotPropsList.Count)
                                throw new Exception();
                            for (int index4 = 0; index4 < screenshotPropsList[index3].stream.Length; ++index4)
                                buf.Add((byte) ((uint) screenshotPropsList[index3].stream[index4] ^ (uint) screenshotPropsList[index3].xorpattern));
                        }
                        byte[] numArray = screenshotPropsList[0].compresstype != (byte) 1 ? buf.ToArray() : UnLZ(buf, 10, 5).ToArray();
                        long filesize = (long) screenshotPropsList[0].filesize;
                        if (filesize > (long) numArray.Length)
                        throw new Exception("stream length strange. file:" + screenshotPropsList[0].filename);
                        ushort num3 = ushort.MaxValue;
                        for (int index2 = 0; (long) index2 < filesize; ++index2)
                        num3 = (ushort) ((uint) this.crctable[((int) num3 ^ (int) numArray[index2]) & (int) byte.MaxValue] ^ (uint) num3 >> 8);
                        ushort num4 = (ushort) ((uint) num3 ^ (uint) ushort.MaxValue);
                        if ((int) screenshotPropsList[0].fileCRC != (int) num4)
                        throw new Exception("CRC mismatch. file:" + screenshotPropsList[0].filename);
                        if (upperInvariant == "GRP:")
                        {
                label_22:
                            FileStream fileStream;
                            try
                            {
                                fileStream = new FileStream(path + ".png", FileMode.CreateNew);
                            }
                            catch (IOException)
                            {
                                ++num2;
                                path = Path.Combine(this.outputdir, Path.GetFileNameWithoutExtension(str) + "(" + num2.ToString() + ")" + Path.GetExtension(str));
                                if (num2 >= 1000)
                                throw;
                                else
                                goto label_22;
                            }
                            int width = (int) numArray[0] + (int) numArray[1] * 256;
                            int height = (int) numArray[2] + (int) numArray[3] * 256;
                            using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                            {
                                for (int y = 0; y < height; ++y)
                                {
                                    for (int x = 0; x < width; ++x)
                                    {
                                        int index2 = (x + y * width) * 4 + 4;
                                        bitmap.SetPixel(x, y, Color.FromArgb((int) numArray[index2 + 3], (int) numArray[index2 + 2], (int) numArray[index2 + 1], (int) numArray[index2]));
                                    }
                                }
                                bitmap.Save((Stream) fileStream, ImageFormat.Png);
                            }
                            fileStream.Close();
                        }
                        else if (upperInvariant == "TXT:")
                        {
            label_39:
                            FileStream fileStream;
                            try
                            {
                                fileStream = new FileStream(path, FileMode.CreateNew);
                            }
                            catch (IOException)
                            {
                                ++num2;
                                path = Path.Combine(this.outputdir, Path.GetFileNameWithoutExtension(str) + "(" + num2.ToString() + ")" + Path.GetExtension(str));
                                if (num2 >= 1000)
                                throw;
                                else
                                goto label_39;
                            }
                            byte[] bytes = Encoding.UTF8.GetBytes(Encoding.Unicode.GetString(numArray, 0, (int) filesize));
                            using (fileStream)
                                fileStream.Write(bytes, 0, bytes.Length);
                        }
                        else
                        {
            label_48:
                            FileStream fileStream;
                            try
                            {
                                fileStream = new FileStream(path, FileMode.CreateNew);
                            }
                            catch (IOException)
                            {
                                ++num2;
                                path = Path.Combine(this.outputdir, Path.GetFileNameWithoutExtension(str) + "(" + num2.ToString() + ")" + Path.GetExtension(str));
                                if (num2 >= 1000)
                                throw;
                                else
                                goto label_48;
                            }
                            using (fileStream)
                                fileStream.Write(numArray, 0, (int) filesize);
                        }
                        ++num1;
                    }
                }
            }
            catch (Exception ex)
            {
                // this.Cursor = Cursors.Default;
                // int num2 = (int) MessageBox.Show(ex.ToString());
                Console.WriteLine(ex.ToString());
            }
            // this.Cursor = Cursors.Default;
            // int num5 = (int) MessageBox.Show(num1.ToString() + " images converted");
            Console.WriteLine(num1.ToString() + " images converted");
        }

        private static List<byte> LZ(List<byte> buf,
                                     int windowsize_bits,
                                     int maxmatchsize_bits,
                                     int mincompresssize)
        {
            if (maxmatchsize_bits < 1 || windowsize_bits < 1 || mincompresssize < 0)
                throw new Exception();
            if (maxmatchsize_bits > windowsize_bits)
                throw new Exception();
            int num1 = 1 << windowsize_bits;
            int num2 = 1 << maxmatchsize_bits;
            List<byte> byteList = new List<byte>();
            uint num3 = 0;
            int num4 = 0;
            int index1 = 0;
            while (index1 < buf.Count)
            {
                int num5 = -1;
                int num6 = -1;
                for (int index2 = 1; index2 <= num1 && index1 >= index2; ++index2)
                {
                    int num7 = 0;
                    while (index1 + num7 < buf.Count && (int) buf[index1 - index2 + num7] == (int) buf[index1 + num7])
                    {
                        ++num7;
                        if (num7 >= num2)
                        {
                            num5 = num7;
                            num6 = index2;
                            break;
                        }
                    }
                    if (num7 > num5)
                    {
                        num5 = num7;
                        num6 = index2;
                    }
                }
                if (num5 < mincompresssize)
                {
                    uint num7 = num3 | (uint) (0 << num4);
                    int num8 = num4 + 1;
                    num3 = num7 | (uint) buf[index1] << num8;
                    num4 = num8 + 8;
                    ++index1;
                }
                else
                {
                    uint num7 = num3 | (uint) (1 << num4);
                    int num8 = num4 + 1;
                    uint num9 = num7 | (uint) (num6 - 1 << num8);
                    int num10 = num8 + windowsize_bits;
                    num3 = num9 | (uint) (num5 - 1 << num10);
                    num4 = num10 + maxmatchsize_bits;
                    index1 += num5;
                }
                for (; num4 >= 8; num4 -= 8)
                {
                    byteList.Add((byte) (num3 & (uint) byte.MaxValue));
                    num3 >>= 8;
                }
            }
            for (; num4 > 0; num4 -= 8)
            {
                byteList.Add((byte) (num3 & (uint) byte.MaxValue));
                num3 >>= 8;
            }
            return byteList;
        }

        private static List<byte> UnLZ(List<byte> buf, int windowsize_bits, int maxmatchsize_bits)
        {
            int num1 = 1 << windowsize_bits;
            int num2 = 1 << maxmatchsize_bits;
            List<byte> byteList = new List<byte>();
            uint num3 = (uint) buf[0] | (uint) buf[1] << 8 | (uint) buf[2] << 16 | (uint) buf[3] << 24;
            int index1 = 3;
            int num4 = 0;
            while (index1 < buf.Count + 2)
            {
                int num5 = (int) (num3 >> num4) & 1;
                int num6 = num4 + 1;
                if (num5 == 0)
                {
                    byte num7 = (byte) (num3 >> num6 & (uint) byte.MaxValue);
                    num4 = num6 + 8;
                    byteList.Add(num7);
                }
                else
                {
                    int num7 = (int) ((long) (num3 >> num6) & (long) (num1 - 1)) + 1;
                    int num8 = num6 + windowsize_bits;
                    int num9 = (int) ((long) (num3 >> num8) & (long) (num2 - 1)) + 1;
                    num4 = num8 + maxmatchsize_bits;
                    int num10 = byteList.Count - num7;
                    for (int index2 = 0; index2 <= num9 - 1; ++index2)
                        byteList.Add(byteList[num10 + index2]);
                }
                while (num4 >= 8)
                {
                    ++index1;
                    num4 -= 8;
                    num3 >>= 8;
                    if (index1 < buf.Count)
                        num3 |= (uint) buf[index1] << 24;
                }
            }
            return byteList;
        }

        private ScreenshotProps[] GetEmbeddedInformation(string filename)
        {
            byte[] bytes = this.DecodeBitmap(filename);
            List<ScreenshotProps> screenshotPropsList = new List<ScreenshotProps>();
            int startIndex = 0;
            while (startIndex < bytes.Length - 4)
            {
                uint uint32 = BitConverter.ToUInt32(bytes, startIndex);
                int num = -1;
                if (uint32 == 1348096818U)
                num = 2;
                if (num < 0)
                {
                    ++startIndex;
                }
                else
                {
                    ScreenshotProps screenshotProps = new ScreenshotProps();
                    if (num == 2)
                    {
                        if (startIndex <= bytes.Length - 60)
                        {
                            screenshotProps.code = uint32;
                            screenshotProps.filesize = BitConverter.ToUInt32(bytes, startIndex + 4);
                            screenshotProps.fileCRC = BitConverter.ToUInt32(bytes, startIndex + 8);
                            int count = 0;
                            while (count < 36 && bytes[startIndex + count + 12] != (byte) 0)
                                ++count;
                            screenshotProps.filename = Encoding.ASCII.GetString(bytes, startIndex + 12, count);
                            screenshotProps.totalimages = bytes[startIndex + 48];
                            if (screenshotProps.totalimages == (byte) 0)
                                throw new Exception("invalid image count");
                            screenshotProps.imagecount = bytes[startIndex + 49];
                            if ((int) screenshotProps.imagecount >= (int) screenshotProps.totalimages)
                                throw new Exception("invalid image count");
                            screenshotProps.xorpattern = bytes[startIndex + 50];
                            screenshotProps.compresstype = bytes[startIndex + 51];
                            screenshotProps.streamsize = BitConverter.ToUInt32(bytes, startIndex + 52);
                            screenshotProps.width = BitConverter.ToUInt16(bytes, startIndex + 56);
                            screenshotProps.height = BitConverter.ToUInt16(bytes, startIndex + 58);
                            if ((long) (startIndex + 60) + (long) screenshotProps.streamsize > (long) bytes.Length)
                                throw new Exception("invalid stream size");
                            screenshotProps.stream = new byte[(int) screenshotProps.streamsize];
                            Array.Copy((Array) bytes, (long) (startIndex + 60), (Array) screenshotProps.stream, 0L, (long) screenshotProps.streamsize);
                            startIndex = startIndex + 60 + (int) screenshotProps.streamsize;
                        }
                        else
                        break;
                    }
                    screenshotPropsList.Add(screenshotProps);
                }
            }
            if (screenshotPropsList.Count == 0)
                throw new Exception("no embedded files.");
            return screenshotPropsList.ToArray();
        }

        private byte[] DecodeBitmap(string filename)
        {
            Bitmap bitmap = new Bitmap(filename);
            int width = bitmap.Width;
            int height = bitmap.Height;
            List<byte> byteList = new List<byte>();
            uint num1 = 0;
            uint num2 = 1;
            long num3 = 0;
            int num4 = 0;
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    int g = (int) bitmap.GetPixel(x, y).G;
                    uint num5 = g < 234 ? (g < 191 ? (g < 149 ? (g < 107 ? (g < 64 ? (g < 22 ? 0U : 1U) : 2U) : 3U) : 4U) : 5U) : 6U;
                    num1 += num5 * num2;
                    num2 *= 7U;
                    if (num2 >= 16807U)
                    {
                        num3 |= (long) (uint) (((int) num1 & 16383) << num4);
                        num2 = 1U;
                        num1 = 0U;
                        for (num4 += 14; num4 >= 8; num4 -= 8)
                        {
                            byteList.Add((byte) ((ulong) num3 & (ulong) byte.MaxValue));
                            num3 >>= 8;
                        }
                    }
                }
            }
            if (num4 > 0)
                byteList.Add((byte) ((ulong) num3 & (ulong) byte.MaxValue));
            return byteList.ToArray();
        }

        private void CalcCRCTable()
        {
            this.crctable = new ushort[256];
            for (ushort index1 = 0; index1 < (ushort)256; ++index1)
            {
                ushort num = index1;
                for (int index2 = 0; index2 < 8; ++index2)
                {
                    if (((int)num & 1) > 0)
                        num = (ushort)(51181UL ^ (ulong)((int)num >> 1));
                    else
                        num >>= 1;
                }
                this.crctable[(int)index1] = num;
            }
        }
    }

    class Petit4Send
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts => {
                    StartMain(opts);
                })
                .WithNotParsed(errors => {
                    Console.WriteLine("Command line arguments are wrong.");
                    Environment.Exit(1);
                });
        }

        static void StartMain(Options opts)
        {
            Console.WriteLine(String.Format("Input dir:  {0}", opts.InputDir));
            Console.WriteLine(String.Format("Output dir: {0}", opts.OutputDir));

            if (!Directory.Exists(opts.InputDir))
            {
                Console.WriteLine("Error: Input directory does not exist.");
                return;
            }

            if (!Directory.Exists(opts.OutputDir))
            {
                Console.WriteLine("Error: Output directory does not exist.");
                return;
            }

            var decoder = new ImageDecoder();
            var filenames = Directory.GetFiles(opts.InputDir, "*.jpg");
            decoder.StartConvert(filenames, opts.OutputDir);
        }
    }
}
