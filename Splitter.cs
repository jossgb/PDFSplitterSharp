using System;
using CommandLine;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace PDFSplitter
{
    class Splitter
    {
        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "Set input file")]
            public string InputPath { get; set; }

            [Option('o', "output", Required = true, HelpText = "Set output file")]
            public string OutputPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (!System.IO.File.Exists(o.InputPath))
                       {

                           System.Console.Out.WriteLine("Failed to locate input file!");
                           System.Environment.Exit(-1);
                       }
                       else if (System.IO.File.Exists(o.OutputPath))
                       {
                           System.Console.Out.WriteLine("Output file exists!");
                           System.Environment.Exit(-2);
                       }
                       else
                       {
                           PerformSplit(o.InputPath, o.OutputPath);
                       }
                   });
        }

        static void PerformSplit(string inputFilePath, string outputFilePath)
        {
            var reader = new PdfReader(inputFilePath);
            var writer = new PdfWriter(outputFilePath);
            PdfDocument pdfDoc = new PdfDocument(reader);
            PdfDocument outputDoc = new PdfDocument(writer);
            //Get the first page, rotate it, split it down the middle then replace page 1 with the right half and add the left half to the last page of the doc
            System.Console.Out.WriteLine("Copying pages from {0} to {1}", inputFilePath, outputFilePath);

            System.Console.Out.WriteLine("Copying pages 1 to {0}", pdfDoc.GetNumberOfPages());
            pdfDoc.CopyPagesTo(1, pdfDoc.GetNumberOfPages(), outputDoc);  //duplicated pages
            System.Console.Out.WriteLine("Adding additional copy of first page to end of document");
            outputDoc.AddPage(outputDoc.GetPage(1)); //Add a copy of the first page to the end of the document
            System.Console.Out.WriteLine("Applying crop box adjustments");

            //Get crop box of page one. The left half is the back page, the right half the first
            var cropBox = outputDoc.GetPage(1).GetCropBox();

            if (outputDoc.GetPage(1).GetRotation() == 0 || outputDoc.GetPage(1).GetRotation() == 180)
            {
                cropBox.SetWidth(cropBox.GetWidth() * 0.5f);
                outputDoc.GetPage(1).SetCropBox(cropBox);
                cropBox.SetX(cropBox.GetX() + cropBox.GetWidth());
                outputDoc.GetLastPage().SetCropBox(cropBox);
            }
            else
            {
                cropBox.SetHeight(cropBox.GetHeight() * 0.5f);
                outputDoc.GetPage(1).SetCropBox(cropBox);
                cropBox.SetY(cropBox.GetY() + cropBox.GetHeight());
                outputDoc.GetLastPage().SetCropBox(cropBox);
            }
            outputDoc.Close();
            pdfDoc.Close();
            System.Console.Out.WriteLine("Saved document to {0}", outputFilePath);


        }
    }
}
