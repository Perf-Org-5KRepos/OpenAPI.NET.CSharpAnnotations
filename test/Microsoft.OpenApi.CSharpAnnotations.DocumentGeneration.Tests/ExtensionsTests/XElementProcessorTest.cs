﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.ReferenceRegistries;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.ExtensionsTests
{
    [Collection("DefaultSettings")]
    public class XElementProcessorTest
    {
        private const string InputDirectory = "ExtensionsTests/Input";
        private readonly ITestOutputHelper _output;
        private readonly TypeFetcher typeFetcher = new TypeFetcher(
            new List<string>() { Path.Combine(
                InputDirectory,
                "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll") });
        private readonly SchemaReferenceRegistry schemaReferenceRegistry = new SchemaReferenceRegistry(
            new SchemaGenerationSettings(new DefaultPropertyNameResolver()));

        public XElementProcessorTest(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> GetTestCasesForXElementProcessorExampleShouldSucceed()
        {
            yield return new object[]
            {
                "Empty example element",
                XElement.Parse("<parent><example></example></parent>"),
                new Dictionary<string, OpenApiExample>()
            };

            yield return new object[]
            {
                "Example with url",
                XElement.Parse(
                    "<parent><example><summary>Test Example</summary><url>https://localhost/test.json</url></example></parent>"),
                new Dictionary<string, OpenApiExample>()
                {
                    {
                        "example1",
                        new OpenApiExample
                        {
                            ExternalValue = "https://localhost/test.json",
                            Summary = "Test Example"
                        }
                    }
                }
            };

            yield return new object[]
            {
                "Example element with cref",
                XElement.Parse(@"<parent><example name=""BodyExample"">"
                + @"<value><see cref=""F:Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.Examples.SampleObject1Example""/></value></example></parent>"),
                new Dictionary<string, OpenApiExample>()
                {
                    {
                        "BodyExample",
                        new OpenApiExample {
                            Value = new OpenApiStringReader().ReadFragment<IOpenApiAny>(
                            ExpectedExamples.SampleObject1Example,
                            OpenApiSpecVersion.OpenApi3_0,
                            out OpenApiDiagnostic _)}
                    }
                }
            };

            yield return new object[]
            {
                "Multiple Example elements",
                XElement.Parse(@"<parent><example><value>"
                + @"<see cref=""F:Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.Examples.SampleObject1Example""/>"
                + @"</value></example>"
                + @"<example><value>Test example 2</value></example></parent>"),
                new Dictionary<string, OpenApiExample>()
                {
                    {
                        "example1",
                        new OpenApiExample {
                            Value = new OpenApiStringReader().ReadFragment<IOpenApiAny>(
                            ExpectedExamples.SampleObject1Example,
                            OpenApiSpecVersion.OpenApi3_0,
                            out OpenApiDiagnostic _)}
                    },
                    {
                        "example2",
                        new OpenApiExample{Value=new OpenApiString("Test example 2")}
                    }
                }
           };

            yield return new object[]
            {
                "Example element with inline value",
                XElement.Parse(@"<parent><example><value>Test Example</value></example></parent>"),
                new Dictionary<string, OpenApiExample>()
                {
                    {
                        "example1",
                        new OpenApiExample {
                            Value = new OpenApiString("Test Example")}
                    }
                }
            };
        }

        public static IEnumerable<object[]> GetTestCasesForXElementProcessorExampleShouldFail()
        {
            yield return new object[]
            {
                "Example element contain both value and url.",
                XElement.Parse("<parent><example><value></value><url></url></example></parent>"),
                SpecificationGenerationMessages.ProvideEitherValueOrUrlTag
            };

            yield return new object[]
            {
                "Example value with no cref and value.",
                XElement.Parse(
                    @"<parent><example><summary>Test Example</summary><value></value></example></parent>"),
                SpecificationGenerationMessages.ProvideValueForExample
            };

            yield return new object[]
            {
                "Example element with cref containing type that doesn't exists in provided assembly.",
                XElement.Parse(@"<parent><example><value><see cref=""F:Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration."
                + @"Tests.DoesnotExists.DoesnotExists""/></value></example></parent>"),
                "Type \"Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.DoesnotExists\" could not be found."
                + " Ensure that it exists in one of the following assemblies: "
                + "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll"
            };

            yield return new object[]
            {
                "Example element with cref containing filed that doesn't exists in provided type.",
                XElement.Parse(@"<parent><example><value><see cref=""F:Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration."
                + @"Tests.Contracts.Examples.DoesNotExists""/></value></example></parent>"),
                "Field \"DoesNotExists\" could not be found for type: "
                + "\"Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.Examples\"."
            };
        }

        public static IEnumerable<object[]> GetTestCasesForXElementProcessorHeaderShouldFail()
        {
            yield return new object[]
            {
                "Header element with no name attribute.",
                XElement.Parse("<parent><header></header></parent>"),
                string.Format(SpecificationGenerationMessages.MissingNameAttribute, "header")
            };
        }

        public static IEnumerable<object[]> GetTestCasesForXElementProcessorHeaderShouldSucceed()
        {
            yield return new object[]
            {
                "Header with no description element",
                XElement.Parse(
                    @"<parent><header name=""header1"" cref=""T:System.String""></header></parent>"),
                new Dictionary<string, OpenApiHeader>()
                {
                    {
                        "header1",
                        new OpenApiHeader {
                            Schema =  new OpenApiSchema
                            {
                                Type = "string"
                            }
                        }
                    }
                }
            };

            yield return new object[]
            {
                "Header with description element",
                XElement.Parse(
                    @"<parent><header name=""header1"" cref=""T:System.String""><description> Test header </description></header></parent>"),
                new Dictionary<string, OpenApiHeader>()
                {
                    {
                        "header1",
                        new OpenApiHeader {
                            Schema =  new OpenApiSchema
                            {
                                Type = "string"
                            },
                            Description = "Test header"
                        }
                    }
                }
            };

            yield return new object[]
            {
                "Multiple header elements",
                XElement.Parse(
                    @"<parent><header name=""header1"" cref=""T:System.String""><description> Test header </description></header>"
                    + @"<header name=""header2"" cref=""T:System.String""></header></parent>"),
                new Dictionary<string, OpenApiHeader>()
                {
                    {
                        "header1",
                        new OpenApiHeader {
                            Schema =  new OpenApiSchema
                            {
                                Type = "string"
                            },
                            Description = "Test header"
                        }
                    },
                    {
                        "header2",
                        new OpenApiHeader {
                            Schema =  new OpenApiSchema
                            {
                                Type = "string"
                            }
                        }
                    }
                }
           };
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForXElementProcessorExampleShouldFail))]
        public void XElementProcessorExampleShouldFail(
            string testCaseName,
            XElement xElement,
            string expectedExceptionMessage)
        {
            _output.WriteLine(testCaseName);

            Action action = () => XElementProcessor.GetOpenApiExamples(xElement, typeFetcher);
            action.Should().Throw<Exception>(expectedExceptionMessage);
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForXElementProcessorExampleShouldSucceed))]
        public void XElementProcessorExampleShouldSucceed(
            string testCaseName,
            XElement xElement,
            Dictionary<string, OpenApiExample> expectedOpenApiExamples)
        {
            _output.WriteLine(testCaseName);

            var openApiExamples = XElementProcessor.GetOpenApiExamples(xElement,typeFetcher);
            openApiExamples.Should().BeEquivalentTo(expectedOpenApiExamples);
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForXElementProcessorHeaderShouldFail))]
        public void XElementProcessorHeaderShouldFail(
            string testCaseName,
            XElement xElement,
            string expectedExceptionMessage)
        {
            _output.WriteLine(testCaseName);

            Action action = () => XElementProcessor.GetOpenApiHeaders(xElement, typeFetcher, schemaReferenceRegistry);
            action.Should().Throw<Exception>(expectedExceptionMessage);
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForXElementProcessorHeaderShouldSucceed))]
        public void XElementProcessorHeaderShouldSucceed(
            string testCaseName,
            XElement xElement,
            Dictionary<string,OpenApiHeader> expectedOpenApiHeaders)
        {
            _output.WriteLine(testCaseName);

            var openApiHeaders = XElementProcessor.GetOpenApiHeaders(
                xElement,
                typeFetcher,
                schemaReferenceRegistry);

            openApiHeaders.Should().BeEquivalentTo(expectedOpenApiHeaders);
        }
    }
}