using DtronixPdf.Actions;
using DtronixPdf.Dispatcher;
using PDFiumCore;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace DtronixPdf
{
    public class PdfPage : IAsyncDisposable
    {
        private readonly ThreadDispatcher _dispatcher;
        private readonly FpdfDocumentT _documentInstance;
        private readonly FpdfPageT _pageInstance;
        private bool _isDisposed = false;

        public SizeF Size { get; private set; }

        public int InitialIndex { get; private set; }

        private PdfPage(ThreadDispatcher dispatcher, FpdfDocumentT documentInstance, FpdfPageT pageInstance)
        {
            _dispatcher = dispatcher;
            _documentInstance = documentInstance;
            _pageInstance = pageInstance;
        }

        internal static async Task<PdfPage> Create(
            ThreadDispatcher dispatcher,
            FpdfDocumentT documentInstance,
            int pageIndex)
        {
            var loadPageResult = await dispatcher.QueueWithResult(() =>
                fpdfview.FPDF_LoadPage(documentInstance, pageIndex));
            if (loadPageResult == null)
                throw new Exception($"Failed to open page for page index {pageIndex}.");

            var page = new PdfPage(dispatcher, documentInstance, loadPageResult)
            {
                InitialIndex = pageIndex
            };

            var getPageSizeResult = await dispatcher.QueueWithResult(() =>
            {
                var size = new FS_SIZEF_();
                var result = fpdfview.FPDF_GetPageSizeByIndexF(documentInstance, pageIndex, size);

                return result == 0 ? null : size;
            });

            if (getPageSizeResult == null)
                throw new Exception($"Could not retrieve page size for page index {pageIndex}.");

            page.Size = new SizeF(getPageSizeResult.Width, getPageSizeResult.Height);

            return page;
        }

        public async Task<PdfBitmap> Render(
            RenderFlags flags,
            float scale,
            DispatcherPriority priority)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PdfPage));

            return await _dispatcher.QueueWithResult(
                new RenderPageAction(
                    _dispatcher,
                    _pageInstance,
                    scale,
                    Size,
                    flags),
                priority);
        }


        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            await _dispatcher.QueueForCompletion(() =>
            {
                fpdfview.FPDF_ClosePage(_pageInstance);
            });
        }
    }
}