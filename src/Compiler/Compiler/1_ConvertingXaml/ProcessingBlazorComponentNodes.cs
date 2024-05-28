

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpenSilver.Compiler.Blazor
{
    internal static class ProcessingBlazorComponentNodes
    {
        //------------------------------------------------------------
        // This class will process the "RazorComponent" nodes
        //------------------------------------------------------------

        private static string pattern = @"(<[^>]*:RazorComponent\b[^>]*>)(.*?)(</[^>]*:RazorComponent>)";
        public static string Process(string xaml, 
            string outputFile,
            string fileNameWithPathRelativeToProjectRoot,
            string assemblyNameWithoutExtension, 
            out string[] generatedRazorFiles,
            out string[] generatedCSFiles)
        {
            StringBuilder updatedXAML = new StringBuilder();
            List<string> listGeneratedRazorFiles = new List<string>();
            List<string> listGeneratedCSFiles = new List<string>();

            var matches = Regex.Matches(xaml, pattern, RegexOptions.Singleline);

            // Find comments
            string commentPattern = @"<!--.*?-->";
            MatchCollection commentMatches = Regex.Matches(xaml, commentPattern, RegexOptions.Singleline);

            string outputDirectory = Path.GetDirectoryName(outputFile);
            string defaultNameSpaceValue = assemblyNameWithoutExtension + ".Blazor";
            defaultNameSpaceValue = char.ToUpper(defaultNameSpaceValue[0]) + defaultNameSpaceValue.Substring(1);

            var savedValues = new List<(string Content, int OpenTagStartPos, int OpenTagEndPos, string ModifiedOpenTag, int CloseTagStartPos, int CloseTagEndPos, string ModifiedCloseTag, string BindingProperties)>();

            int lastPos = 0;
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    bool insideComment = false;
                    foreach (Match comment in commentMatches)
                    {
                        if (match.Index >= comment.Index && match.Index + match.Length <= comment.Index + comment.Length)
                        {
                            insideComment = true;
                            break;
                        }
                    }
                    if (insideComment)
                        continue;

                    // Extract the full content of the RazorComponent, including nested content
                    string razorContent = RemoveXamlComments(RemoveRazorComments(match.Groups[2].Value.Trim()));
                    int tagOpenStartPos = match.Groups[1].Index;
                    int tagOpenEndPos = tagOpenStartPos + match.Groups[1].Length;
                    string openingTag = match.Groups[1].Value.Trim();

                    int tagCloseStartPos = match.Groups[3].Index;
                    int tagCloseEndPos = tagCloseStartPos + match.Groups[3].Length;

                    string namespaceValue = ExtractNamespace(razorContent);
                    string newAttributes = "";
                    string modifiedOpenTag = "";
                    string razorContentFileName = $"Inline_Razor_In_{Path.GetFileNameWithoutExtension(fileNameWithPathRelativeToProjectRoot)}_{savedValues.Count + 1}";
                    string newRazorComponentName = $"RazorComponent_{Path.GetFileNameWithoutExtension(fileNameWithPathRelativeToProjectRoot)}_{savedValues.Count + 1}";

                    // Set namespace of the razor content
                    if (namespaceValue == "")
                    {
                        namespaceValue = defaultNameSpaceValue;
                        razorContent = $"@namespace {namespaceValue}\r\n" + razorContent;
                    }

                    razorContent = TransformDataBinding(razorContent, newRazorComponentName, $"{namespaceValue}.{razorContentFileName}", out string bindingValuesInXAML, out string bindingProperties);

                    // Set ComponentType
                    newAttributes = $" ComponentType=\"local_blazor:{razorContentFileName}\" xmlns:local_blazor=\"clr-namespace:{namespaceValue}\"" + bindingValuesInXAML;
                    modifiedOpenTag = UpdateAttributesOnOpeningTag(openingTag, "local_blazor:" + newRazorComponentName, newAttributes);

                    string modifiedCloseTag = $"</local_blazor:{newRazorComponentName}>";
                    savedValues.Add((razorContent, tagOpenStartPos, tagOpenEndPos, modifiedOpenTag, tagCloseStartPos, tagCloseEndPos, modifiedCloseTag, bindingProperties));

                    string razorOutputFilePath = Path.Combine(outputDirectory, razorContentFileName + ".razor");
                    using (StreamWriter outRazorFile = new StreamWriter(razorOutputFilePath))
                    {
                        outRazorFile.Write(razorContent);
                    }

                    string csOutputFilePath = Path.Combine(outputDirectory, newRazorComponentName + ".cs");
                    using (StreamWriter outCSFile = new StreamWriter(csOutputFilePath))
                    {
                        string csContent = @$"
using global::OpenSilver.Compatibility.Blazor;
using global::System;
using global::System.Collections.Generic;
using global::System.Linq;
using global::System.Threading.Tasks;
using global::Microsoft.AspNetCore.Components;
{getUsingLines(razorContent)}
namespace {namespaceValue}
{{
    public class {newRazorComponentName} : RazorComponent
    {{
{bindingProperties}
    }}
}}
";
                        outCSFile.Write(csContent);
                        outCSFile.WriteLine("//" + modifiedOpenTag);
                    }

                    listGeneratedRazorFiles.Add(razorOutputFilePath);
                    listGeneratedCSFiles.Add(csOutputFilePath);

                    updatedXAML.Append(xaml.Substring(lastPos, tagOpenStartPos - lastPos));
                    updatedXAML.Append($"{modifiedOpenTag}{ReplaceWithSpacesExceptNewLines(match.Groups[2].Value)}{modifiedCloseTag}");

                    lastPos = tagCloseEndPos + 1;
                }
            }
            updatedXAML.Append(xaml.Substring(lastPos, xaml.Length - lastPos));

            generatedRazorFiles = listGeneratedRazorFiles.ToArray();
            generatedCSFiles = listGeneratedCSFiles.ToArray();

            return updatedXAML.ToString();
        }

        private static string TransformDataBinding(string content, string componentName, string componentType, out string bindingValuesInXAML, out string bindingPropertiesCode)
        {
            Dictionary<int, string> properties = new Dictionary<int, string>();

            bindingPropertiesCode = "";
            bindingValuesInXAML = "";

            // Regex to find all bindings
            string pattern = @"\""\{Binding.*?\}\""";
            Regex regex = new Regex(pattern);
            var matches = regex.Matches(content);

            if (matches.Count == 0)
                return content;

            // Replace bindings and prepare new properties
            for(int i = matches.Count - 1; i>= 0; i--)
            {
                Match match = matches[i];
                ExtractBindingFields(match.Groups[0].Value, out string bindingName, out string typeName);

                // Replace the binding in the content
                string generatedBindingName = $"Generated_DataBinding_{ReplaceInvalidCharactersForNaming(bindingName)}{i}";
                // Replace the binding in the content
                content = content.Substring(0, match.Index) + $"\"@{generatedBindingName}\"" + content.Substring(match.Index + match.Length);

                properties[properties.Count] = $"    public {typeName} {generatedBindingName} {{ get; set; }}";

                string generatedPropertyName = $"{componentName}_Binding_Value{properties.Count}";
                string propertyType = typeName == "dynamic" ? "object" : typeName;

                bindingValuesInXAML += $" {generatedPropertyName}=\"{{Binding {bindingName}}}\"";
                bindingPropertiesCode += $@"
        public static readonly System.Windows.DependencyProperty {generatedPropertyName}Property =
            System.Windows.DependencyProperty.Register(nameof({generatedPropertyName}), typeof({propertyType}), typeof(RazorComponent),
                new System.Windows.PropertyMetadata(null, new System.Windows.PropertyChangedCallback(On{generatedPropertyName}Changed)));

        public {propertyType} {generatedPropertyName}
        {{
            get {{ return ({propertyType})GetValue({generatedPropertyName}Property); }}
            set {{ SetValue({generatedPropertyName}Property, value); }}
        }}

        private static void On{generatedPropertyName}Changed(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {{
            OpenSilver.Compatibility.Blazor.RazorComponent control = d as OpenSilver.Compatibility.Blazor.RazorComponent;
            if (control != null)
            {{
                if (control.Rendered)
                {{
                    if (control.Instance is {componentType})
                    {{
                        (({componentType})control.Instance).{generatedBindingName} = ({propertyType})e.NewValue;
                        (({componentType})control.Instance).Refresh_razor_component();
                    }}
                }}
                else
                {{
                    System.Windows.RoutedEventHandler handler = (_, _) =>
                    {{
                        if (control.Instance is {componentType})
                        {{
                            (({componentType})control.Instance).{generatedBindingName} = ({propertyType})e.NewValue;
                            (({componentType})control.Instance).Refresh_razor_component();
                        }}
                    }};

                    control.OnComponentRendered -= handler;
                    control.OnComponentRendered += handler;
                }}
            }}
        }}
";
            }

            // Find the location to insert new properties in the @code block
            int codeIndex = content.IndexOf("@code {");

            string refreshFunction = "    public void Refresh_razor_component()\n    {\n        this.StateHasChanged();\n    }";
            if (codeIndex > -1)
            {
                codeIndex += "@code {".Length;
                string newProperties = string.Join("\n", properties.Values);
                content = content.Insert(codeIndex, "\n" + newProperties + "\n" + refreshFunction + "\n");
            }
            else
            {
                string newProperties = string.Join("\n", properties.Values);
                content = content + "\n@code {\n" + newProperties + "\n" + refreshFunction + "\n}\n";
            }

            return content;
        }

        /*
         * Extract binding fields.
         * Input: "{Binding Employees1, Type=IQueryable<Showcase.Employee1>}"
         * Output: bindingName="Employees1", dataType="IQueryable<Showcase.Employee1>"
         */
        static bool ExtractBindingFields(string input, out string bindingName, out string dataType)
        {
            input = input.Replace(", ", ",").Replace("Type =", "Type=");
            int typeIndex = input.IndexOf(",Type=");
            if (typeIndex != -1)
            {
                bindingName = input.Substring(9, typeIndex - 9).Trim();
                typeIndex += ",Type=".Length;
                dataType = input.Substring(typeIndex, input.Length - typeIndex - 2).Trim();
            }
            else
            {
                bindingName = input.Substring(9, input.Length - 11).Trim();
                dataType = "dynamic";
            }
            return true;
        }

        static string ReplaceInvalidCharactersForNaming(string bindingPath)
        {
            // Pattern to match '.', '[', or ']'
            string pattern = @"[\.\[\]]";
            // Replacement string
            string replacement = "_";

            // Replace matches of the pattern with the replacement string
            return Regex.Replace(bindingPath, pattern, replacement);
        }

        static string ReplaceWithSpacesExceptNewLines(string input)
        {
            StringBuilder sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (c == '\r' || c == '\n')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }

        private static string RemoveRazorComments(string content)
        {
            string commentPattern = @"@\*.*?\*@";
            return Regex.Replace(content, commentPattern, "", RegexOptions.Singleline);
        }

        private static string RemoveXamlComments(string xamlContent)
        {
            // Regex to match XML comments
            string commentPattern = @"<!--.*?-->";
            return Regex.Replace(xamlContent, commentPattern, "", RegexOptions.Singleline);
        }

        private static string ExtractNamespace(string content)
        {
            string pattern = @"@namespace\s+([^\s]+)";
            Match match = Regex.Match(content, pattern);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        // 
        private static string UpdateAttributesOnOpeningTag(string openingTag, string updatedComponentName, string newAttributes)
        {
            // Check if attributes already exist to avoid duplication
            if (openingTag.Contains("ComponentType"))
            {
                throw new Exception("ComponentType should not be declared in XAML.");
            }

            int pos = openingTag.IndexOf(" ");
            if (pos > 0)
            {
                openingTag = "<" + updatedComponentName + openingTag.Substring(pos);
            }

            openingTag = openingTag.TrimEnd('>') + newAttributes + ">";
            return openingTag;
        }

        private static string getUsingLines(string input)
        {
            StringBuilder sb = new StringBuilder();
            // Split the input into lines and filter lines starting with "@using"
            var usingLines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Where(line => line.TrimStart().StartsWith("@using"))
                                  .ToList();

            // Display the lines that start with "@using"
            foreach (var line in usingLines)
            {
                sb.AppendLine(line.TrimStart('@') + ";");
            }
            return sb.ToString();
        }
    }
}
