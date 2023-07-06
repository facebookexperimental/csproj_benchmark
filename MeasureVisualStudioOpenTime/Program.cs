/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using EnvDTE;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace MeasureVisualStudioOpenTime
{
    internal class Program
    {
        static string SolutionTemplate = @"""
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.33214.272
MinimumVisualStudioVersion = 10.0.40219.1
_PROJECT_LIST_
Global
  GlobalSection(SolutionConfigurationPlatforms) = preSolution
    Debug|Any CPU = Debug|Any CPU
    Release|Any CPU = Release|Any CPU
  EndGlobalSection
  GlobalSection(ProjectConfigurationPlatforms) = postSolution
_CONFIG_LIST_
  EndGlobalSection
  GlobalSection(SolutionProperties) = preSolution
    HideSolutionNode = FALSE
  EndGlobalSection
  GlobalSection(ExtensibilityGlobals) = postSolution
    SolutionGuid = {5C5E2D55-4C25-42EC-A600-F1BD1E3C3028}
  EndGlobalSection
EndGlobal
""";

        // Requires references to:
        // C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\PublicAssemblies\Microsoft.VisualStudio.Interop.dll
        // C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\PublicAssemblies\envdte.dll
        [SupportedOSPlatform("windows")]
        static TimeSpan OpenAndTimeSolution(string solutionPath)
        {
            DateTime then = DateTime.Now;
            Type? dteType = Type.GetTypeFromProgID("VisualStudio.DTE.17.0");
            if (dteType == null)
            {
                throw new ApplicationException("Unable to load the Visual Studio interop type");
            }
            DTE? dte = (DTE?)Activator.CreateInstance(dteType);
            if (dte == null)
            {
                throw new ApplicationException("Unable to instantiate a DTE object");
            }
            dte.MainWindow.Visible = true;
            dte.ExecuteCommand("File.OpenProject", solutionPath);
            TimeSpan delta = DateTime.Now.Subtract(then);
            dte.Quit();
            return delta;
        }

        static string GenerateSolution(string templateProjectDirectory, string generatedProjectRoot, int projectCount, bool unityType)
        {
            MD5 md5 = MD5.Create();
            StringBuilder projectListBuilder = new StringBuilder();
            StringBuilder configurationListBuilder = new StringBuilder();

            if (!Directory.Exists(generatedProjectRoot))
            {
                Directory.CreateDirectory(generatedProjectRoot);
            }

            for (int i = 1; i <= projectCount; i++)
            {
                string projectName = "ScaleTest" + i.ToString().PadLeft(3, '0');
                string className = "Class" + i.ToString().PadLeft(3, '0');
                string projectId = new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(projectName))).ToString().ToUpper();

                projectListBuilder.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{projectName}\\{projectName}.csproj\", \"{{{projectId}}}\"");
                projectListBuilder.AppendLine("EndProject");
                configurationListBuilder.AppendLine($"    {{{projectId}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                configurationListBuilder.AppendLine($"    {{{projectId}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                configurationListBuilder.AppendLine($"    {{{projectId}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                configurationListBuilder.AppendLine($"    {{{projectId}}}.Release|Any CPU.Build.0 = Release|Any CPU");

                string generatedProjectDirectory = Path.Combine(generatedProjectRoot, projectName);
                if (!Directory.Exists(generatedProjectDirectory))
                {
                    Directory.CreateDirectory(generatedProjectDirectory);
                }

                string generatedPropertiesDirectory = Path.Combine(generatedProjectDirectory, "Properties");
                if (!Directory.Exists(generatedPropertiesDirectory))
                {
                    Directory.CreateDirectory(generatedPropertiesDirectory);
                }

                string generatedProjectPath = Path.Combine(generatedProjectDirectory, projectName + ".csproj");
                string generatedCsPath = Path.Combine(generatedProjectDirectory, className + ".cs");
                string generatedAssemblyInfoPath = Path.Combine(generatedPropertiesDirectory, "AssemblyInfo.cs");

                File.Copy(Path.Combine(templateProjectDirectory, "ScaleTest001.csproj"), generatedProjectPath);
                File.Copy(Path.Combine(templateProjectDirectory, "Class001.cs"), generatedCsPath);
                File.Copy(Path.Combine(templateProjectDirectory, @"Properties\AssemblyInfo.cs"), generatedAssemblyInfoPath);

                string data = File.ReadAllText(generatedProjectPath);
                // TODO: Consider not hardcoding the project id and class name
                data = data.Replace("CC86F22C-2589-41C3-8069-D7F0046AAE9E", projectId);
                data = data.Replace("Class001", className);
                if (unityType)
                {
                    data = data.Replace("<OutputType>Library</OutputType>", "<OutputType>Library</OutputType>\n    <ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>");
                }
                File.WriteAllText(generatedProjectPath, data);

                data = File.ReadAllText(generatedCsPath);
                // TODO: Consider not hardcoding the class name
                data = data.Replace("Class001", className);
                File.WriteAllText(generatedCsPath, data);

                data = File.ReadAllText(generatedAssemblyInfoPath);
                // TODO: Consider not hardcoding the project id
                data = data.Replace("cc86f22c-2589-41c3-8069-d7f0046aae9e", projectId.ToLower());
                File.WriteAllText(generatedAssemblyInfoPath, data);
            }

            string solutionPath = Path.Combine(generatedProjectRoot, "ScaleTest.sln");
            string solutionData = SolutionTemplate.Replace("_PROJECT_LIST_", projectListBuilder.ToString());
            solutionData = solutionData.Replace("_CONFIG_LIST_", configurationListBuilder.ToString());
            File.WriteAllText(solutionPath, solutionData);

            md5.Dispose();

            return solutionPath;
        }


        [SupportedOSPlatform("windows")]
        static TimeSpan GenerateScaleTest(string generatedProjectRoot, string templateCsproj, int projectCount, bool unityType)
        {
            if (Directory.Exists(generatedProjectRoot))
            {
                for (int i = 1; i <= 5; i++)
                {
                    try
                    {
                        Directory.Delete(generatedProjectRoot, true);
                        break;
                    }
                    catch
                    {
                        // It looks like sometimes VS still has files locked even though we've asked the IDE it exit
                        // Do a linear backoff retry
                        Console.WriteLine($"Could not delete {generatedProjectRoot} directory, sleeping for {i} seconds...");
                        System.Threading.Thread.Sleep(1000 * i);
                    }
                }
            }

            string templatePath = Path.GetDirectoryName(templateCsproj)!;

            Console.WriteLine($"Generating solution with {projectCount} project(s), unityType: {unityType}");
            string generatedSolutionPath = GenerateSolution(templatePath, generatedProjectRoot, projectCount, unityType);
            Console.WriteLine("Opening solution...");
            TimeSpan span = OpenAndTimeSolution(generatedSolutionPath);
            Console.WriteLine($"Solution opened in {span}");
            return span;
        }

        [SupportedOSPlatform("windows")]
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: MeasureVisualStudioOpenTime [template csproj] [iterations]");
                return;
            }

            int iterationCount = 0;
            if (!int.TryParse(args[1], out iterationCount))
            {
                Console.WriteLine($"Invalid argument: {args[1]} cannot be parsed as an integer");
                return;
            }

            string templateCsproj = args[0];
            if (!File.Exists(templateCsproj))
            {
                Console.WriteLine($"Invalid argument: File {args[0]} does not exist");
            }

            string tempPathRoot = Environment.ExpandEnvironmentVariables("%TEMP%");

            string generatedProjectRoot = Path.Combine(tempPathRoot, "generatedTest");
            
            string resultFilePath = Path.Combine(tempPathRoot, @"generatedTestResults.csv");
            File.WriteAllText(resultFilePath, $"Project Count,Using Unity,Time (s){Environment.NewLine}");

            for (int i = 0; i < iterationCount; i++)
            {
                int projectCount = 2 << i;
                TimeSpan span = GenerateScaleTest(generatedProjectRoot, templateCsproj, projectCount, false);
                File.AppendAllText(resultFilePath, $"{projectCount},false,{span.TotalSeconds}{Environment.NewLine}");
                span = GenerateScaleTest(generatedProjectRoot, templateCsproj, projectCount, true);
                File.AppendAllText(resultFilePath, $"{projectCount},true,{span.TotalSeconds}{Environment.NewLine}");
            }

            Console.WriteLine($"All results written to {resultFilePath}");
        }
    }
}
