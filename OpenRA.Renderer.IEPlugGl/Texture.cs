#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using OpenRA.FileFormats.Graphics;
using Tao.OpenGl;
using System.IO;
using System;

namespace OpenRA.Renderer.IEPlugGl
{
	public class Texture : ITexture
	{
		internal int texture;
		
		public Texture(GraphicsDevice dev)
		{
			Gl.glGenTextures(1, out texture);
			GraphicsDevice.CheckGlError();
		}
		
		public Texture(GraphicsDevice dev, Bitmap bitmap)
		{
			Gl.glGenTextures(1, out texture);
			GraphicsDevice.CheckGlError();
			SetData(bitmap);
		}

		void PrepareTexture()
		{
			GraphicsDevice.CheckGlError();
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
			GraphicsDevice.CheckGlError();
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
			GraphicsDevice.CheckGlError();
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
			GraphicsDevice.CheckGlError();
			
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_BASE_LEVEL, 0);
			GraphicsDevice.CheckGlError();
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_LEVEL, 0);
			GraphicsDevice.CheckGlError();
		}
		
		public void SetData(byte[] colors, int width, int height)
		{
			if (!IsPowerOf2(width) || !IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			unsafe
			{
				fixed (byte* ptr = &colors[0])
				{
					IntPtr intPtr = new IntPtr((void*)ptr);
					PrepareTexture();
					Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, width, height,
						0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, intPtr);
					GraphicsDevice.CheckGlError();
				}
			}			
		}

		// An array of RGBA
		public void SetData(uint[,] colors)
		{
			int width = colors.GetUpperBound(1) + 1;
			int height = colors.GetUpperBound(0) + 1;
			
			if (!IsPowerOf2(width) || !IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width,height));
			
			unsafe
			{
				fixed (uint* ptr = &colors[0,0])
				{
					IntPtr intPtr = new IntPtr((void *) ptr);
					PrepareTexture();
					Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, width, height,
						0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, intPtr);
					GraphicsDevice.CheckGlError();
				}
			}			
		}
		
		public void SetData(Bitmap bitmap)
		{
			if (!IsPowerOf2(bitmap.Width) || !IsPowerOf2(bitmap.Height))
			{
				//throw new InvalidOperationException( "non-power-of-2-texture" );
				bitmap = new Bitmap(bitmap, new Size(NextPowerOf2(bitmap.Width), NextPowerOf2(bitmap.Height)));
			}
			
			var bits = bitmap.LockBits(
				new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppArgb);
			
			PrepareTexture();
			Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, bits.Width, bits.Height,
				0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bits.Scan0);        // todo: weird strides
			GraphicsDevice.CheckGlError();
			bitmap.UnlockBits(bits);
		}

		bool IsPowerOf2(int v)
		{
			return (v & (v - 1)) == 0;
		}

		int NextPowerOf2(int v)
		{
			--v;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			++v;
			return v;
		}
	}
}
