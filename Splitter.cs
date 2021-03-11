using System;
using System.IO;
using CommandLine;
using iText.Kernel.Pdf;

namespace PDFSplitter
{
    public static class Splitter
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    if (!File.Exists(o.InputPath))
                    {
                        Console.Out.WriteLine("Failed to locate input file!");
                        Environment.Exit(-1);
                    }
                    else if (File.Exists(o.OutputPath))
                    {
                        if (o.RemoveExistingFiles)
                        {
                            Console.Out.WriteLine("Removing existing file...");
                            File.Delete(o.OutputPath);
                            PerformSplit(o.InputPath, o.OutputPath);
                        }
                        else
                        {
                            Console.Out.WriteLine("Output file exists!");
                            Environment.Exit(-2);
                        }
                    }
                    else
                    {
                        PerformSplit(o.InputPath, o.OutputPath);
                    }
                });
        }

        /// <summary>
        /// Perform the split operation. Takes the first page of the document and copties to the end, then apply the a rotation transform to the 
        /// first and last pages if required before finally setting the crop box to hide the section we are not interested in
        /// </summary>
        /// <param name="inputFilePath">The path to the source PDF</param>
        /// <param name="outputFilePath">Where the output should be saved</param>
        private static void PerformSplit(string inputFilePath, string outputFilePath)
        {
            var reader = new PdfReader(inputFilePath);
            var writer = new PdfWriter(outputFilePath);

            PdfDocument pdfDoc = new(reader);
            PdfDocument outputDoc = new(writer);

            //Get the first page, rotate it, split it down the middle then replace page 1 with the right half and add the left half to the last page of the doc
            Console.Out.WriteLine("Copying pages from {0} to {1}", inputFilePath, outputFilePath);
            Console.Out.Write("        index -> 1 to {0}... ", pdfDoc.GetNumberOfPages());
            pdfDoc.CopyPagesTo(1, pdfDoc.GetNumberOfPages(), outputDoc);
            Console.Out.Write("Done. \n Adding additional copy of first page to end of document... ");
            pdfDoc.CopyPagesTo(1, 1, outputDoc); //Copy first page to end of document
            Console.Out.WriteLine("Done.");

            var firstPage = outputDoc.GetFirstPage();
            var lastPage = outputDoc.GetLastPage();

            Console.Out.Write("Applying crop box adjustments... ");

            //Get crop box of page one. The left half is the back page, the right half the first for a rotated page
            var cropBox = firstPage.GetCropBox();

            if (firstPage.GetRotation() == 0 || firstPage.GetRotation() == 180)
            {
                firstPage.SetRotation(firstPage.GetRotation() == 0 ? 270 : 90);
                cropBox.SetHeight(cropBox.GetHeight() * 0.5f);
                firstPage.SetCropBox(cropBox);

                cropBox.SetY(cropBox.GetHeight() + cropBox.GetY());
                lastPage.SetCropBox(cropBox);
                lastPage.SetRotation(lastPage.GetRotation() == 0 ? 270 : 90);
            }
            else //Split vertically
            {
                cropBox.SetHeight(cropBox.GetHeight() * 0.5f);
                firstPage.SetCropBox(cropBox);

                cropBox.SetY(cropBox.GetY() + cropBox.GetHeight());
                lastPage.SetCropBox(cropBox);
            }
            Console.Out.WriteLine("Done.");

            outputDoc.Close();
            pdfDoc.Close();
        }

        /// <summary>
        /// Options class for the command line arguments
        /// </summary>
        public class Options
        {
            public Options(string inputPath, string outputPath, bool removeExistingFiles)
            {
                InputPath = inputPath;
                OutputPath = outputPath;
                RemoveExistingFiles = removeExistingFiles;
            }

            public Options()
            {
            }

            [Option('i', "input", Required = true, HelpText = "Set input file")]
            public string InputPath { get; set; }

            [Option('o', "output", Required = true, HelpText = "Set output file")]
            public string OutputPath { get; set; }

            [Option('f', "force", Required = false, HelpText = "Remove existing output file", Default = false)]
            public bool RemoveExistingFiles { get; set; }
        }
    }
}