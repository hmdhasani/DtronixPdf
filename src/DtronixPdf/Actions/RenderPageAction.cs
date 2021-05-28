using DtronixPdf.Dispatcher;
using DtronixPdf.Renderer.Dispatcher;
using PDFiumCore;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DtronixPdf.Actions
{
    internal class RenderPageAction : ThreadMessagePumpAction<PdfBitmap>
    {
        public readonly FpdfPageT _pageInstance;
        private readonly float _scale;
        private readonly SizeF _size;
        private readonly RenderFlags _flags;
        private readonly ThreadDispatcher _dispatcher;
        private FpdfBitmapT _bitmap;

        public RenderPageAction(ThreadDispatcher dispatcher,
            FpdfPageT pageInstance,
            float scale,
            SizeF size,
            RenderFlags flags)
        {
            _pageInstance = pageInstance;
            _scale = scale;
            _size = size;
            _flags = flags;
            _dispatcher = dispatcher;
        }

        protected override PdfBitmap OnExecute()
        {
            try
            {
                var width = (int)Math.Round(_size.Width * _scale);
                var height = (int)Math.Round(_size.Height * _scale);

                _bitmap = fpdfview.FPDFBitmapCreateEx(
                    width,
                    height,
                    (int)FPDFBitmapFormat.BGRA,
                    IntPtr.Zero,
                    0);

                if (_bitmap == null)
                    throw new Exception("failed to create a bitmap object");

                fpdfview.FPDFBitmapFillRect(_bitmap, 0, 0, width, height, (uint)Color.White.ToArgb());

                // |          | a b 0 |
                // | matrix = | c d 0 |
                // |          | e f 1 |
                using var matrix = new FS_MATRIX_();
                using var clipping = new FS_RECTF_();

                matrix.A = _scale;
                matrix.B = 0;
                matrix.C = 0;
                matrix.D = _scale;
                matrix.E = 0;
                matrix.F = 0;

                clipping.Left = 0;
                clipping.Right = width;
                clipping.Bottom = 0;
                clipping.Top = height;

                fpdfview.FPDF_RenderPageBitmapWithMatrix(_bitmap, _pageInstance, matrix, clipping, (int)_flags);

                return new PdfBitmap(
                    _bitmap,
                    width,
                    height,
                    _dispatcher,
                    PixelFormat.Format32bppArgb);
            }
            catch (OperationCanceledException)
            {
                fpdfview.FPDFBitmapDestroy(_bitmap);
                throw;
            }
            catch (Exception ex)
            {
                fpdfview.FPDFBitmapDestroy(_bitmap);
                throw new Exception("Error rendering page. Check inner exception.", ex);
            }
        }
    }
}