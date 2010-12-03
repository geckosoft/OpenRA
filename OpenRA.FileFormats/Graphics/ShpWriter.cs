using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenRA.FileFormats.Graphics
{
	public class ShpWriter
	{
		public ushort Width { get; protected set; }
		public ushort Height { get; protected set; }
		public List<ImageHeader> Headers { get; protected set; }
		public Format Format { get; protected set; }

		public ushort ImageCount
		{
			get { return (ushort) Headers.Count; }
		}

		public Size Size { get { return new Size(Width, Height); } }

		public ShpWriter(ushort width, ushort height, Format format)
		{
			Headers = new List<ImageHeader>();
			Width = width;
			Height = height;
			Format = format;
		}

		public static ShpWriter Convert(Format format, string palette,string[] sourceFiles)
		{
			switch (format)
			{
				case Format.Format00:
					return Format00.Convert(palette, sourceFiles, false);
				case Format.Format01:
					return Format01.Convert(palette, sourceFiles, false);
				default:
					throw new NotImplementedException("Format not implemented yet.");
			}
		}

		public void Save(string targetFile)
		{
			using (var f = File.OpenWrite(targetFile))
			{
				f.SetLength(0);
				using (var bw = new BinaryWriter(f))
				{
					bw.Write(ImageCount);
					bw.Write((ushort)0);
					bw.Write((ushort)0);
					bw.Write((ushort)Size.Width);
					bw.Write((ushort)Size.Height);
					bw.Write((uint)0);
					WriteHeaders(bw);
					WriteData(bw);
				}
			}
		}

		private void WriteData(BinaryWriter bw)
		{
			foreach (var h in Headers)
			{
				bw.Write((int)h.Image.Length);
				bw.Write(h.Image);
			}
		}

		void WriteHeaders(BinaryWriter bw)
		{
			int offset = (int) bw.BaseStream.Position + (8*(ImageCount + 2));

			foreach (var h in Headers)
			{
				var of = (((uint)Format << 24) | (offset));
				bw.Write((uint)of);

				bw.Write((ushort)0);
				bw.Write((ushort)0);

				offset += h.Image.Length + 4; // +4 == int (size field)
			}

			// end-of-file header
			bw.Write((uint)0);
			bw.Write((ushort)0);
			bw.Write((ushort)0);
			// all zeroes
			bw.Write((uint)0);
			bw.Write((ushort)0);
			bw.Write((ushort)0);
		}
	}
}
