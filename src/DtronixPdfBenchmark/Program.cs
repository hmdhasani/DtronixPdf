using DtronixPdf;
using DtronixPdf.Dispatcher;
using PDFiumCore;
using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DtronixPdfBenchmark
{
    class Program
    {
        static async Task Main()
        {
            await RenderViewport();
            Console.ReadLine();
        }

        static async Task RenderViewport()
        {
            var drawing = await PdfDocument.Load("TestPdf.pdf", null);
            await using var page = await drawing.GetPage(0);

            await using var result = await page.Render(RenderFlags.RenderAnnotations, scale: 2, DispatcherPriority.Normal);

            result.ToBitmap().Save($"test.jpg", ImageFormat.Jpeg);

            await drawing.DisposeAsync();
        }
    }
}