
/*===================================================================================
* 
*   Copyright (c) Userware (OpenSilver.net, CSHTML5.com)
*      
*   This file is part of both the OpenSilver Compiler (https://opensilver.net), which
*   is licensed under the MIT license (https://opensource.org/licenses/MIT), and the
*   CSHTML5 Compiler (http://cshtml5.com), which is dual-licensed (MIT + commercial).
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;

namespace OpenSilver.Compiler.Blazor
{
    public class BlazorComponentProcessor : Task
    {
        private List<ITaskItem> _listRemovedFiles = new List<ITaskItem>();
        private List<string> _listGeneratedRazorFiles = new List<string>();
        private List<string> _listGeneratedCSFilesForBlazor = new List<string>();
        private List<string> _listGeneratedXamlForBlazor = new List<string>();
        
        private readonly Stopwatch _watch;

        [ThreadStatic]
        private static MD5 _hash;

        private static MD5 Hash => _hash ??= MD5.Create();

        [Required]
        public string Language { get; set; }

        [Required]
        public string AssemblyName { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public bool IsSecondPass { get; set; }

        [Required]
        public bool VerifyHash { get; set; }

        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        [Output]
        public ITaskItem[] GeneratedCSFiles { get; set; }
        [Output]
        public ITaskItem[] GeneratedXAMLFiles { get; set; }

        [Output]
        public ITaskItem[] RemovedFiles { get; set; }

        [Output]
        public ITaskItem[] GeneratedRazorFiles { get; set; }

        public BlazorComponentProcessor()
        {
            _watch = new Stopwatch();
        }

        public override bool Execute()
        {
            _listGeneratedRazorFiles.Clear();
            _listGeneratedCSFilesForBlazor.Clear();
            _listGeneratedXamlForBlazor.Clear();

            if (!string.Equals(Language, "c#", StringComparison.OrdinalIgnoreCase))
            {
                Log.LogError($"'{Language}' is not a supported language (C#).");
                return false;
            }

            _watch.Start();

            string operationName = $"OpenSilver: BlazorPreprocessor (pass {(IsSecondPass ? "2" : "1")})";
            Log.LogMessage($"{operationName} starting...");

            if (SourceFiles.Length > 0)
            {
                foreach (ITaskItem item in SourceFiles)
                {
                    if (!IsXamlFile(item))
                    {
                        continue;
                    }

                    try
                    {
                        string outputFilePath = GetOutputFile(item);
                        string sourceFilePath = item.GetMetadata("FullPath");
                        string fileIdentity = GetFileIdentity(item);
                        string xaml = ReadFileContent(sourceFilePath);

                        if (!VerifyHash || IsFileOutdated(xaml, outputFilePath))
                        {
                            TimeSpan start = _watch.Elapsed;

                            string[] razorFiles;
                            string[] csFilesForRazor;
                            bool bUpdated;

                            // Process the "RazorComponent" nodes
                            xaml = ProcessingBlazorComponentNodes.Process(xaml, outputFilePath, fileIdentity, AssemblyName,
                                out razorFiles, out csFilesForRazor, out bUpdated);

                            if (!bUpdated)
                            {
                                continue;
                            }

                            _listRemovedFiles.Add(item);
                            _listGeneratedXamlForBlazor.Add(outputFilePath);

                            _listGeneratedRazorFiles.AddRange(razorFiles);
                            _listGeneratedCSFilesForBlazor.AddRange(csFilesForRazor);

                            string generatedCode = xaml + Environment.NewLine + CreateCSHeaderContainingHash(xaml);

                            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                            using (var sw = new StreamWriter(outputFilePath))
                            {
                                sw.Write(generatedCode);
                            }

                            Log.LogMessage($"  {fileIdentity} -> {outputFilePath} ({(_watch.Elapsed - start).TotalMilliseconds} ms).");
                        }
                        else
                        {
                            Log.LogMessage($"  '{outputFilePath}' is up to date.");
                        }
                    }
                    catch (Exception ex)
                    {
                        string sourceFile = item.GetMetadata("FullPath");
                        Log.LogErrorFromException(ex, true, true, sourceFile);
                    }
                }
            }

            RemovedFiles = _listRemovedFiles.ToArray();
            GeneratedCSFiles = _listGeneratedCSFilesForBlazor.Select(s => new TaskItem(s)).ToArray();
            GeneratedXAMLFiles = _listGeneratedXamlForBlazor.Select(s => new TaskItem(s)).ToArray();
            GeneratedRazorFiles = _listGeneratedRazorFiles.Select(s => new TaskItem(s)).ToArray();

            if (Log.HasLoggedErrors)
            {
                Log.LogMessage(MessageImportance.High, $"{operationName} failed after {_watch.ElapsedMilliseconds} ms.");
                Log.LogMessage(MessageImportance.High, "Note: the XAML editor sometimes raises errors that are misleading. To see only real non-misleading errors, make sure to close all the XAML editor windows/tabs before compiling.");
            }
            else
            {
                Log.LogMessage($"{operationName} finished after {_watch.ElapsedMilliseconds} ms.");
            }

            return true;
        }

        // From XamlPreprocessor
        private const string XamlHash = "XamlHash";
        private const string XamlHashStart = "<" + XamlHash + ">";
        private const string XamlHashEnd = "</" + XamlHash + ">";

        private string GetOutputFile(ITaskItem item) => Path.Combine(IntermediateOutputPath, GetFileName(item));

        private string GetFileName(ITaskItem item)
        {
            string fileIdentity = GetFileIdentity(item);

            return $"{Path.GetFileNameWithoutExtension(fileIdentity)}.g.xaml";
        }

        private static string ComputeHash(string str)
        {
            var hash = Hash.ComputeHash(Encoding.UTF8.GetBytes(str));

            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        private static bool IsFileOutdated(string xaml, string outputFile)
        {
            // Check if the output file exists:
            if (!File.Exists(outputFile))
            {
                return true;
            }

            // Read the header of the output file (the first line of the file), which contains the hash of the previous XAML that it was compiled from:
            string fileFooter = "";
            using (var reader = new StreamReader(outputFile))
            {
                while (!reader.EndOfStream)
                {
                    fileFooter = reader.ReadLine();
                }
            }

            int x1 = fileFooter.IndexOf(XamlHashStart);
            int x2 = fileFooter.IndexOf(XamlHashEnd);

            if (x1 > 0 && x2 > 0 && x1 < x2)
            {
                string previousXamlHash = fileFooter.Substring(x1 + XamlHashStart.Length, (x2 - (x1 + XamlHashStart.Length)));
                string xamlHash = ComputeHash(xaml);

                return previousXamlHash != xamlHash;
            }

            return true;
        }

        private static string GetFileIdentity(ITaskItem item)
        {
            string identity = item.GetMetadata("Link");
            if (string.IsNullOrEmpty(identity))
            {
                identity = item.GetMetadata("Identity");
            }
            return identity;
        }

        private static string ReadFileContent(string filePath)
        {
            using (var sr = new StreamReader(filePath))
            {
                return sr.ReadToEnd();
            }
        }

        private static bool IsXamlFile(ITaskItem item) =>
            string.Equals(item.GetMetadata("Extension"), ".xaml", StringComparison.OrdinalIgnoreCase);

        private static string CreateCSHeaderContainingHash(string xaml)
        {
            string fileHash = ComputeHash(xaml);
            return $"<!--<OpenSilver><{XamlHash}>{fileHash}</{XamlHash}><CompilationDate>{DateTime.Now.ToString(CultureInfo.InvariantCulture)}</CompilationDate></OpenSilver>-->";
        }
    }
}
