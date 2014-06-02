using System.Drawing;
using System.IO;
using ApprovalTests;
using ApprovalTests.Reporters;
using GhostscriptSharp;
using GhostscriptSharp.Settings;
using NUnit.Framework;

namespace GhostscriptSharpTests
{
    [TestFixture]
    [UseReporter(typeof(FileLauncherReporter) /*, typeof(ClipboardReporter), typeof(WinMergeReporter)*/)] //always false because Tiff created on dates are different 
    public class GhostscriptSharpTiffTests
    {
        const string TEST_FILE_LOCATION = "test.pdf";

        [Test, RequiresSTAAttribute]
        public void ConvertToTif()
        {
            var destination = new FileInfo(Path.GetTempPath() + "test_ConvertToTif.tif");
            GhostscriptWrapper.GenerateOutput(
                inputPath: TEST_FILE_LOCATION, outputPath: destination.FullName, 
                settings: new GhostscriptSettings
                              {
                                  Device = GhostscriptDevices.tiffg4,
                                  Resolution = new Size(400, 400),
                                  Page = GhostscriptPages.All,
                                  Size = new GhostscriptPageSize { Native = GhostscriptPageSizes.a4 },
                                         
                              } );
            Approvals.VerifyFile(destination.FullName);
        }
    }
}