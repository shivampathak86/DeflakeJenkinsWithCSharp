using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ReadTestResult
{
    public class Program
    {
        static string  outputParam = string.Empty;
        public static int Main(string[] args)
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

            if (args.Where(a => a.StartsWith("/path")).FirstOrDefault() == null)
            {
                Console.WriteLine("/path parameter is required");
                return 1;
            }

            string filePath = args.Where(a => a.StartsWith("/path")).FirstOrDefault();
            var file = ResolveFilePath(filePath);

            if (file.StartsWith("Error: "))
            {
                Console.WriteLine(file);
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

            if (args.Where(a => a.StartsWith("/output")).FirstOrDefault() != null)
            {
                outputParam = ResolveOutputFileName(args.Where(a => a.StartsWith("/output")).FirstOrDefault());
                if (outputParam.StartsWith("Error: "))
                {
                    Console.WriteLine(outputParam);
                    return 1;
                }
            }

            Task task;
            if (file.EndsWith(".trx"))
                task = ReadTRXFile(file);
            else
                task = ReadXMLFile(file);

            task.Wait();

            return 0;
        }
        private static async Task ReadTRXFile(string file)
        {
            XDocument trxDoc = null;
            if (file.StartsWith("http"))
            {
                var stream = await HttpAdapter.GetAsync(file);
                trxDoc = XDocument.Load(stream);
            }
            else
            {
                trxDoc = XDocument.Load(file);
            }

            XNamespace defaultNs = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

            List<string> testIDList = new List<string>();
            string text = string.Empty;
            string xmltext = string.Empty;

            try
            {

                var UnitTestResultNode = (from data in trxDoc.Descendants(defaultNs + "Results")
                                          select data).Descendants(defaultNs + "UnitTestResult").ToList();

                foreach (var node in UnitTestResultNode)
                {
                    if (node.Attribute("outcome").Value == "Failed")
                        testIDList.Add(node.Attribute("testId").Value);
                }

                var UnitTestNode = (from data in trxDoc.Descendants(defaultNs + "TestDefinitions")
                                    select data).Descendants(defaultNs + "UnitTest").ToList();

                foreach (var testID in testIDList)
                {
                    foreach (var node in UnitTestNode)
                    {
                        if (testID == node.Attribute("id").Value)
                        {
                            var list = node.Descendants(defaultNs + "TestMethod").FirstOrDefault();
                            string name = list.Attribute("name").Value;
                            string className = list.Attribute("className").Value;

                            if (name.Contains("("))
                                name = name.Split('(')[0];

                            if (!xmltext.Contains("FullyQualifiedName = " + className + "." + name))
                                xmltext = xmltext + "FullyQualifiedName = " + className + "." + name + "|";

                            break;
                        }
                    }
                }

                string path = string.Empty;
                if (string.IsNullOrWhiteSpace(outputParam))
                    path = Environment.CurrentDirectory + @"\ReRunTestResults.txt";
                else
                    path = outputParam;
                File.WriteAllText(path, xmltext.Trim('|'));
            }

            catch (Exception e)
            {
                Console.WriteLine(Environment.CurrentDirectory);
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static async Task ReadXMLFile(string file)
        {
            try
            {
                XDocument doc = null;
                if (file.StartsWith("http"))
                {
                    var stream = await HttpAdapter.GetAsync(file);
                    doc = XDocument.Load(stream);
                }
                else
                {
                    doc = XDocument.Load(file);
                }

                // Reading XUnit Test Result File
                var xmlnode = doc.Root.Element("assembly").Element("collection");

                var xmltext = string.Empty;
                foreach (var n in xmlnode.Elements("test"))
                {
                    if (n.Attribute("result")?.Value == "Fail")
                    {
                        var name = n.Attribute("name")?.Value;
                        var className = n.Attribute("type")?.Value;

                        if (name.Contains("("))
                        {
                            name = name.Split('(')[0];
                        }

                        if (!xmltext.Contains("FullyQualifiedName = " + className + "." + name))
                        {
                            xmltext += "FullyQualifiedName = " + className + "." + name + "|";
                        }
                    }
                }

                var path = string.Empty;
                if (string.IsNullOrWhiteSpace(outputParam))
                {
                    path = Path.Combine(Environment.CurrentDirectory, "ReRunTestResults.txt");
                }
                else
                {
                    path = outputParam;
                }

                File.WriteAllText(path, xmltext.Trim('|'));
            }
            catch (Exception e)
            {
                Console.WriteLine(Environment.CurrentDirectory);
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static string ResolveFilePath(string path)
        {
            string paths = string.Empty;

            var argument = path.Substring(6, path.Length - 6);

            bool isWebPath = argument.StartsWith("http");
            bool isFile = File.Exists(argument);

            if (isWebPath || isFile)
                paths = argument;

            if (!isFile && !isWebPath)
                return string.Format("Error: {0} is not a valid file path", path);

            return paths;
        }
        private static string ResolveOutputFileName(string outputParam)
        {
            var splitOutput = outputParam.Substring(8, outputParam.Length - 8);

            if (splitOutput.Length == 1
                || !outputParam.EndsWith(".txt"))
                return "Error: /output parameter is in the incorrect format. Expected /output:<file name | directory and file name>. Execute /help for more information";

            return splitOutput;
        }
        private static void DispalyHelp()
        {
            Console.WriteLine(
            @"
            PARAMETERS:

            /path - REQUIRED PARAMETER - Parameter that determines the path of Test Result File.
	               This parameter will accept one of the following:
		            - file(s) name: looks for trx/xml Test Result files in the current directory. File extension is required 
			            example: testResults1.trx
		            - file(s) path: full path to trx/xml files. File extension is required 
			            example: c:\TestResults\testResults1.trx
                    - file(s) web path: full web path to trx/xml Test Result files. File extension is required.
                        example: http://localhost:8080/job/TestSuite/lastSuccessfulBuild/artifact/Test.Test/TestResults/TestResults.xml

            /output - OPTIONAL PARAMETER - The name/path of the output txt file. When not provided file will be saved in current direactory with name ReRunTestResults.txt
                This parameter will accept one of the following:
	            - name: Saves the file in the current directory. File extension is required.
		            example: /output:ReRunTestResults.txt
	            - path and name: Saves the file in specified directory. File extension is required.
		            example: /output:c:\TestResults\ReRunTestResults.txt
            ");
        }
    }
}
