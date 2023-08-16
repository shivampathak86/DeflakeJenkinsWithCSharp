using ReadTestResult;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TRXFileMerge
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0 || args.Contains("/h") || args.Contains("/help"))
            {
                DispalyHelp();
                return 1;
            }

            //if (args.Where(a => a.StartsWith("/auth")).FirstOrDefault() == null)
            //{
            //    Console.WriteLine("/auth parameter is required");
            //    return 1;
            //}

            if (args.Where(a => a.StartsWith("/trx")).FirstOrDefault() == null)
            {
                Console.WriteLine("/trx parameter is required");
                return 1;
            }

            string trxArg = args.Where(a => a.StartsWith("/trx")).FirstOrDefault();
            var trxFiles = ResolveTrxFilePaths(trxArg);
            if (trxFiles.Count == 0)
            {
                Console.WriteLine("No trx files found!");
                return 1;
            }

            if (trxFiles.Count == 1)
            {
                if (trxFiles[0].StartsWith("Error: "))
                {
                    Console.WriteLine(trxFiles[0]);
                    return 1;
                }
            }
            else
            {
                if (args.Where(a => a.StartsWith("/output")).FirstOrDefault() == null)
                {
                    Console.WriteLine("/output parameter is required");
                    return 1;
                }

                string outputParam = ResolveOutputFileName(args.Where(a => a.StartsWith("/output")).FirstOrDefault());
                if (outputParam.StartsWith("Error: "))
                {
                    Console.WriteLine(outputParam);
                    return 1;
                }

                // Jenkins Auth Token
                if (args.Where(a => a.StartsWith("/auth")).FirstOrDefault() != null)
                {
                    try
                    {
                        string auth = args.Where(a => a.StartsWith("/auth")).FirstOrDefault();
                        GlobalVariables.User = auth.Split(":")[1];
                        GlobalVariables.ApiToken = auth.Split(":")[2];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        return 1;
                    }
                }

                if (trxFiles.Contains(outputParam))
                    trxFiles.Remove(outputParam);

                try
                {
                    var combinedTestRun = await TestRunMerger.MergeTRXsAndSave(trxFiles, outputParam);
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                    Console.WriteLine("Error: " + ex.Message);
                    return 1;
                }
            }

            return 0;
        }
        private static string ResolveOutputFileName(string outputParam)
        {
            var splitOutput = outputParam.Split(new char[] { ':' });

            if (splitOutput.Length == 1
                || !outputParam.EndsWith(".trx"))
                return "Error: /output parameter is in the incorrect format. Expected /output:<file name | directory and file name>. Execute /help for more information";

            return outputParam.Substring(8, outputParam.Length - 8);
        }
        private static List<string> ResolveTrxFilePaths(string trxParams)
        {
            List<string> paths = new List<string>();

            var argument = trxParams.Substring(5, trxParams.Length - 5).Split(new char[] { ',' }).ToList();
            foreach (var args in argument)
            {
                bool isWebPath = args.StartsWith("http");
                bool isTrxFile = File.Exists(args) && args.EndsWith(".trx");

                if (isWebPath || isTrxFile)
                    paths.Add(args);

                if (!isTrxFile && !isWebPath)
                    return new List<string>
                    {
                        string.Format("Error: {0} is not a trx file valid path", args)
                    };
            }

            return paths;
        }
        private static void DispalyHelp()
        {
            Console.WriteLine(
            @"
            PARAMETERS:

            /trx - REQUIRED PARAMETER - Parameter that determines which two trx files will be merged. First file should contain the ReExecuted Test Results, and second file should contain the original Test execution results.
	               This parameter will accept one of the following:
		            - file(s) name: looks for trx files in the current directory. File extension is required.
			            example: /trx:testResults1.trx,testResults2.trx
		            - file(s) path: full path to trx files. File extension is required.
			            example: /trx:c:\TestResults\testResults1.trx,c:\TestResults\testResults2.trx
                    - file(s) web path: full web path to trx files. File extension is required.
                        example: /trx:http://localhost:8080/job/TestSuite/lastSuccessfulBuild/artifact/Test.Test/TestResults/TestResults.trx,c:\TestResults\testResults2.trx

            /output - REQUIRED PARAMETER - The name/path of the output trx file
                This parameter will accept one of the following:
	            - name: saves the file in the current directory. File extension is required.
		            example: /output:combinedTestResults.trx
	            - path and name: saves the file in specified directory. File extension is required.
		            example: /output:c:\TestResults\combinedTestResults.trx

            ");
        }
    }
}
