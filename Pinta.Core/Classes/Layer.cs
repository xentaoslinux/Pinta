// 
// Layer.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Cairo;

namespace Pinta.Core
{
	public class Layer
	{
		public ImageSurface Surface { get; set; }
		public double Opacity { get; set; }
		public bool Hidden { get; set; }
		public string Name { get; set; }
		public bool Tiled { get; set; }
		public PointD Offset { get; set; }
		
		public Layer () : this (null)
		{
		}
		
		public Layer (ImageSurface surface) : this (surface, false, 1f, "")
		{
		}
		
		public Layer (ImageSurface surface, bool hidden, double opacity, string name)
		{
			Surface = surface;
			Hidden = hidden;
			Opacity = opacity;
			Name = name;
			Offset = new PointD (0, 0);
		}	
		
		public void Clear ()
		{
			using (Context g = new Context (Surface)) {
				g.Operator = Operator.Clear;
				g.Paint ();
			}
		}
		
		public void FlipHorizontal ()
		{
			Layer dest = PintaCore.Layers.CreateLayer ();
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Matrix = new Matrix (-1, 0, 0, 1, Surface.Width, 0);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public void FlipVertical ()
		{
			Layer dest = PintaCore.Layers.CreateLayer ();
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Matrix = new Matrix (1, 0, 0, -1, 0, Surface.Height);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public void Rotate180 ()
		{
			Layer dest = PintaCore.Layers.CreateLayer ();
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Matrix = new Matrix (-1, 0, 0, -1, Surface.Width, Surface.Height);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public void Rotate90CW ()
		{
			double w = PintaCore.Workspace.ImageSize.X;
			double h = PintaCore.Workspace.ImageSize.Y;
			
			Layer dest = PintaCore.Layers.CreateLayer ("", (int)h, (int)w);
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Translate (h / 2, w / 2);
				g.Rotate (Math.PI / 2);
				g.Translate (-w / 2, -h / 2);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public void Rotate90CCW ()
		{
			double w = PintaCore.Workspace.ImageSize.X;
			double h = PintaCore.Workspace.ImageSize.Y;
			
			Layer dest = PintaCore.Layers.CreateLayer ("", (int)h, (int)w);
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Translate (h / 2, w / 2);
				g.Rotate (Math.PI / -2);
				g.Translate (-w / 2, -h / 2);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public unsafe void Sepia ()
		{
			Desaturate ();
			
			UnaryPixelOp op = new UnaryPixelOps.Level(
				ColorBgra.Black, 
				ColorBgra.White,
				new float[] { 1.2f, 1.0f, 0.8f },
				ColorBgra.Black,
				ColorBgra.White);

			ImageSurface dest = Surface.Clone ();

			ColorBgra* dstPtr = (ColorBgra*)dest.DataPtr;
			int len = Surface.Data.Length / 4;
			
			op.Apply (dstPtr, len);

			using (Context g = new Context (Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}

			(dest as IDisposable).Dispose ();
		}
		
		public unsafe void Invert ()
		{
			ImageSurface dest = Surface.Clone ();

			ColorBgra* dstPtr = (ColorBgra*)dest.DataPtr;
			int len = Surface.Data.Length / 4;
			
			for (int i = 0; i < len; i++) {
				if (dstPtr->A != 0)
				*dstPtr = (ColorBgra.FromBgra((byte)(255 - dstPtr->B), (byte)(255 - dstPtr->G), (byte)(255 - dstPtr->R), dstPtr->A));
				dstPtr++;
			}

			using (Context g = new Context (Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}

			(dest as IDisposable).Dispose ();
		}
		
		public unsafe void Desaturate ()
		{
			ImageSurface dest = Surface.Clone ();

			ColorBgra* dstPtr = (ColorBgra*)dest.DataPtr;
			int len = Surface.Data.Length / 4;
			
			for (int i = 0; i < len; i++) {
				byte ib = dstPtr->GetIntensityByte();

				dstPtr->R = ib;
				dstPtr->G = ib;
				dstPtr->B = ib;
				dstPtr++;
			}

			using (Context g = new Context (Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}
			
			(dest as IDisposable).Dispose ();
		}
	}
}