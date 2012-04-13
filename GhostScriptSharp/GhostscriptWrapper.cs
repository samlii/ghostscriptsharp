using System;
using System.Runtime.InteropServices;

namespace GhostscriptSharp
{
    /// <summary>
    /// Wraps the Ghostscript API with a C# interface
    /// </summary>
    public class GhostscriptWrapper
    {
        const string ghostScriptDll = "gsdll32.dll";

        #region Hooks into Ghostscript DLL
        [DllImport(ghostScriptDll, EntryPoint = "gsapi_new_instance")]
        private static extern int CreateAPIInstance(out IntPtr pinstance, IntPtr caller_handle);

        [DllImport(ghostScriptDll, EntryPoint = "gsapi_init_with_args")]
        private static extern int InitAPI(IntPtr instance, int argc, string[] argv);

        [DllImport(ghostScriptDll, EntryPoint = "gsapi_exit")]
        private static extern int ExitAPI(IntPtr instance);

        [DllImport(ghostScriptDll, EntryPoint = "gsapi_delete_instance")]
        private static extern void DeleteAPIInstance(IntPtr instance);
        #endregion

        #region Globals

        private static readonly string[] ARGS = new string[] {
                                                                 // Keep gs from writing information to standard output
                                                                 "-q",                     
                                                                 "-dQUIET",
               
                                                                 "-dPARANOIDSAFER",       // Run this command in safe mode
                                                                 "-dBATCH",               // Keep gs from going into interactive mode
                                                                 "-dNOPAUSE",             // Do not prompt and pause for each page
                                                                 "-dNOPROMPT",            // Disable prompts for user interaction           
                                                                 "-dMaxBitmap=500000000", // Set high for better performance
                                                                 "-dNumRenderingThreads=4", // Multi-core, come-on!
                
                                                                 // Configure the output anti-aliasing, resolution, etc
                                                                 "-dAlignToPixels=0",
                                                                 "-dGridFitTT=0",
                                                                 "-dTextAlphaBits=4",
                                                                 "-dGraphicsAlphaBits=4"
                                                             };
        #endregion

        /// <summary>
        /// Generates a thumbnail jpg for the pdf at the input path and saves it 
        /// at the output path
        /// </summary>
        public static void GeneratePageThumb(string inputPath, string outputPath, int page, int width, int height)
        {
            GeneratePageThumbs(inputPath, outputPath, page, page, width, height);
        }

        /// <summary>
        /// Generates a collection of thumbnail jpgs for the pdf at the input path 
        /// starting with firstPage and ending with lastPage.
        /// Put "%d" somewhere in the output path to have each of the pages numbered
        /// </summary>
        public static void GeneratePageThumbs(string inputPath, string outputPath, int firstPage, int lastPage, int width, int height)
        {
            CallAPI(GetArgs(inputPath, outputPath, firstPage, lastPage, width, height));
        }

        /// <summary>
        /// Rasterises a PDF into selected format
        /// </summary>
        /// <param name="inputPath">PDF file to convert</param>
        /// <param name="outputPath">Destination file</param>
        /// <param name="settings">Conversion settings</param>
        public static void GenerateOutput(string inputPath, string outputPath, GhostscriptSettings settings)
        {
            CallAPI(GetArgs(inputPath, outputPath, settings));
        }

        /// <summary>
        /// Calls the Ghostscript API with a collection of arguments to be passed to it
        /// </summary>
        private static void CallAPI(string[] args)
        {
            // Get a pointer to an instance of the Ghostscript API and run the API with the current arguments
            IntPtr gsInstancePtr;
            lock (resourceLock)
            {
                CreateAPIInstance(out gsInstancePtr, IntPtr.Zero);
                try
                {
                    int result = InitAPI(gsInstancePtr, args.Length, args);

                    if (result < 0)
                    {
                        throw new ExternalException("Ghostscript conversion error", result);
                    }
                }
                finally
                {
                    Cleanup(gsInstancePtr);
                }
            }
        }

        /// <summary>
        /// GS can only support a single instance, so we need to bottleneck any multi-threaded systems.
        /// </summary>
        private static object resourceLock = new object();

        /// <summary>
        /// Frees up the memory used for the API arguments and clears the Ghostscript API instance
        /// </summary>
        private static void Cleanup(IntPtr gsInstancePtr)
        {
            ExitAPI(gsInstancePtr);
            DeleteAPIInstance(gsInstancePtr);
        }

        /// <summary>
        /// Returns an array of arguments to be sent to the Ghostscript API
        /// </summary>
        /// <param name="inputPath">Path to the source file</param>
        /// <param name="outputPath">Path to the output file</param>
        /// <param name="firstPage">The page of the file to start on</param>
        /// <param name="lastPage">The page of the file to end on</param>
        private static string[] GetArgs(string inputPath,
                                        string outputPath,
                                        int firstPage,
                                        int lastPage,
                                        int width,
                                        int height)
        {
            // To maintain backwards compatibility, this method uses previous hardcoded values.

            GhostscriptSettings s = new GhostscriptSettings();
            s.Device = Settings.GhostscriptDevices.jpeg;
            s.Page.Start = firstPage;
            s.Page.End = lastPage;
            s.Resolution = new System.Drawing.Size(width, height);
			
            Settings.GhostscriptPageSize pageSize = new Settings.GhostscriptPageSize();
            pageSize.Native = GhostscriptSharp.Settings.GhostscriptPageSizes.a7;
            s.Size = pageSize;

            return GetArgs(inputPath, outputPath, s);
        }

        /// <summary>
        /// Returns an array of arguments to be sent to the Ghostscript API
        /// </summary>
        /// <param name="inputPath">Path to the source file</param>
        /// <param name="outputPath">Path to the output file</param>
        /// <param name="settings">API parameters</param>
        /// <returns>API arguments</returns>
        private static string[] GetArgs(string inputPath,
                                        string outputPath,
                                        GhostscriptSettings settings)
        {
            System.Collections.ArrayList args = new System.Collections.ArrayList(ARGS);

            if (settings.Device == Settings.GhostscriptDevices.UNDEFINED)
            {
                throw new ArgumentException("An output device must be defined for Ghostscript", "GhostscriptSettings.Device");
            }

            if (settings.Page.AllPages == false && (settings.Page.Start <= 0 && settings.Page.End < settings.Page.Start))
            {
                throw new ArgumentException("Pages to be printed must be defined.", "GhostscriptSettings.Pages");
            }

            if (settings.Resolution.IsEmpty)
            {
                throw new ArgumentException("An output resolution must be defined", "GhostscriptSettings.Resolution");
            }

            if (settings.Size.Native == Settings.GhostscriptPageSizes.UNDEFINED && settings.Size.Manual.IsEmpty)
            {
                throw new ArgumentException("Page size must be defined", "GhostscriptSettings.Size");
            }

            // Output device
            args.Add(String.Format("-sDEVICE={0}", settings.Device));

            // Pages to output
            if (settings.Page.AllPages)
            {
                args.Add("-dFirstPage=1");
            }
            else
            {
                args.Add(String.Format("-dFirstPage={0}", settings.Page.Start));
                if (settings.Page.End >= settings.Page.Start)
                {
                    args.Add(String.Format("-dLastPage={0}", settings.Page.End));
                }
            }

            // Page size
            if (settings.Size.Native == Settings.GhostscriptPageSizes.UNDEFINED)
            {
                args.Add(String.Format("-dDEVICEWIDTHPOINTS={0}", settings.Size.Manual.Width));
                args.Add(String.Format("-dDEVICEHEIGHTPOINTS={0}", settings.Size.Manual.Height));
            }
            else
            {
                args.Add(String.Format("-sPAPERSIZE={0}", settings.Size.Native.ToString()));
            }

            // Page resolution
            args.Add(String.Format("-dDEVICEXRESOLUTION={0}", settings.Resolution.Width));
            args.Add(String.Format("-dDEVICEYRESOLUTION={0}", settings.Resolution.Height));

            // Files
            args.Add(String.Format("-sOutputFile={0}", outputPath));
            args.Add(inputPath);

            return (string[])args.ToArray(typeof(string));

        }
    }
}