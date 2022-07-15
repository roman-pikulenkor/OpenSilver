
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
using System.Linq;
using System.Xml.Linq;

namespace DotNetForHtml5.Compiler
{
    internal static partial class GeneratingVBCode
    {
        private class GeneratorPass1 : IVBCodeGenerator
        {
            private readonly XamlReaderVB _reader;
            private readonly IMetadataVB _metadata;
            private readonly string _fileNameWithPathRelativeToProjectRoot;
            private readonly string _assemblyNameWithoutExtension;
            private readonly ReflectionOnSeparateAppDomainHandler _reflectionOnSeparateAppDomain;
            private readonly bool _isSLMigration;

            public GeneratorPass1(XDocument doc,
                string assemblyNameWithoutExtension,
                string fileNameWithPathRelativeToProjectRoot,
                ReflectionOnSeparateAppDomainHandler reflectionOnSeparateAppDomain,
                bool isSLMigration)
            {
                _reader = new XamlReaderVB(doc);
                _metadata = isSLMigration ? MetadataVB.Silverlight : MetadataVB.UWP;
                _assemblyNameWithoutExtension = assemblyNameWithoutExtension;
                _fileNameWithPathRelativeToProjectRoot = fileNameWithPathRelativeToProjectRoot;
                _isSLMigration = isSLMigration;
                _reflectionOnSeparateAppDomain = reflectionOnSeparateAppDomain;
            }

            public string Generate() => GenerateImpl();

            private string GenerateImpl()
            {
                // Get general information about the class:
                string className, namespaceStringIfAny, baseType;
                bool hasCodeBehind;
                GetClassInformationFromXaml(_reader.Document, _reflectionOnSeparateAppDomain,
                    out className, out namespaceStringIfAny, out baseType, out hasCodeBehind);

                List<string> resultingFieldsForNamedElements = new List<string>();
                List<string> resultingMethods = new List<string>();

                while (_reader.Read())
                {
                    if (_reader.NodeType != XamlNodeTypeVB.StartObject)
                        continue;

                    XElement element = _reader.ObjectData.Element;

                    // Get the namespace, local name, and optional assembly that correspond to the element
                    string namespaceName, localTypeName, assemblyNameIfAny;
                    GettingInformationAboutXamlTypesVB.GetClrNamespaceAndLocalName(element.Name, out namespaceName, out localTypeName, out assemblyNameIfAny);
                    string elementTypeInCSharp = _reflectionOnSeparateAppDomain.GetVbEquivalentOfXamlTypeAsString(
                        namespaceName, localTypeName, assemblyNameIfAny, true);

                    if (!hasCodeBehind)
                    {
                        // No code behind, no need to create fields for elementd with an x:Name
                        continue;
                    }

                    XAttribute xNameAttr = element.Attributes().FirstOrDefault(attr => IsAttributeTheXNameAttribute(attr));
                    if (xNameAttr != null && GetRootOfCurrentNamescopeForCompilation(element).Parent == null)
                    {
                        string name = xNameAttr.Value;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            string fieldModifier = "Private";
                            string fieldName = name;
                            resultingFieldsForNamedElements.Add(string.Format("    {0} {1} As {2}", fieldModifier, fieldName, elementTypeInCSharp));
                        }
                    }
                }

                if (hasCodeBehind)
                {
                    // Create the "IntializeComponent()" method:
                    string initializeComponentMethod = CreateInitializeComponentMethod(
                        $"Global.{_metadata.SystemWindowsNS}.Application",
                        string.Empty,
                        _assemblyNameWithoutExtension,
                        _fileNameWithPathRelativeToProjectRoot,
                        new List<string>());

                    // Wrap everything into a partial class:
                    string partialClass = GeneratePartialClass(initializeComponentMethod,
                                                               new ComponentConnectorBuilder().ToString(),
                                                               resultingFieldsForNamedElements,
                                                               className,
                                                               namespaceStringIfAny,
                                                               baseType,
#if BRIDGE
                                                           addApplicationEntryPoint: IsClassTheApplicationClass(baseType)
#else
                                                           addApplicationEntryPoint: false
#endif
);

                    string componentTypeFullName = GetFullTypeName(namespaceStringIfAny, className);

                    string factoryClass = GenerateFactoryClass(
                        componentTypeFullName,
                        GetUniqueName(_reader.Document.Root),
                        "Throw New Global.System.NotImplementedException()",
                        "Throw New Global.System.NotImplementedException()",
                        Enumerable.Empty<string>(),
                        $"Global.{_metadata.SystemWindowsNS}.UIElement",
                        _assemblyNameWithoutExtension,
                        _fileNameWithPathRelativeToProjectRoot,
                        baseType);

                    string finalCode = $@"
{factoryClass}
{partialClass}";

                    return finalCode;
                }
                else
                {
                    string finalCode = GenerateFactoryClass(
                        baseType,
                        GetUniqueName(_reader.Document.Root),
                        "Throw New Global.System.NotImplementedException()",
                        "Throw New Global.System.NotImplementedException()",
                        Enumerable.Empty<string>(),
                        $"Global.{_metadata.SystemWindowsNS}.UIElement",
                        _assemblyNameWithoutExtension,
                        _fileNameWithPathRelativeToProjectRoot,
                        baseType);

                    return finalCode;
                }
            }

            private static XElement GetRootOfCurrentNamescopeForCompilation(XElement element)
            {
                while (element.Parent != null)
                {
                    if (IsDataTemplate(element) || IsItemsPanelTemplate(element) || IsControlTemplate(element))
                    {
                        return element;
                    }
                    element = element.Parent;
                }
                return element;
            }


#if BRIDGE
            private bool IsClassTheApplicationClass(string className)
            {
                return className == $"Global.{_metadata.SystemWindowsNS}.Application";
            } 
#endif
        }
    }
}
