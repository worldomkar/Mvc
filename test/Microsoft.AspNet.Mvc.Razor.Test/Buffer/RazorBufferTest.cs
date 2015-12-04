﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNet.Html;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Buffer
{
    public class RazorBufferTest
    {
        [Fact]
        public void Append_AddsStringRazorValue()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");

            // Act
            buffer.Append("Hello world");

            // Assert
            var segment = Assert.Single(buffer.BufferSegments);
            Assert.Equal(1, buffer.CurrentCount);
            Assert.Equal("Hello world", segment.Data.Array[0].Value);
        }

        [Fact]
        public void Append_AddsHtmlContentRazorValue()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var content = new HtmlString("hello-world");

            // Act
            buffer.Append(content);

            // Assert
            var segment = Assert.Single(buffer.BufferSegments);
            Assert.Equal(1, buffer.CurrentCount);
            Assert.Same(content, segment.Data.Array[0].Value);
        }

        [Fact]
        public void AppendHtml_AddsHtmlStringValues()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var value = "Hello world";

            // Act
            buffer.AppendHtml(value);

            // Assert
            var segment = Assert.Single(buffer.BufferSegments);
            Assert.Equal(1, buffer.CurrentCount);
            var htmlString = Assert.IsType<HtmlString>(segment.Data.Array[0].Value);
            Assert.Equal("Hello world", htmlString.ToString());
        }

        [Fact]
        public void Append_CreatesNewSegments_WhenCurrentSegmentIsFull()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var expected = Enumerable.Range(0, TestRazorBufferScope.BufferSize).Select(i => i.ToString());

            // Act
            foreach (var item in expected)
            {
                buffer.Append(item);
            }
            buffer.Append("Hello");
            buffer.Append("world");

            // Assert
            Assert.Equal(2, buffer.CurrentCount);
            Assert.Collection(buffer.BufferSegments,
                segment => Assert.Equal(expected, segment.Data.Array.Select(v => v.Value)),
                segment =>
                {
                    var array = segment.Data.Array;
                    Assert.Equal("Hello", array[0].Value);
                    Assert.Equal("world", array[1].Value);
                });
        }

        [Fact]
        public void Append_CreatesNewSegments_WhenCurrentSegmentIsFull_ForBuffersWithNonZeroOffsets()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(3, 2), "some-name");

            // Act
            buffer.Append("1");
            buffer.Append("2");
            buffer.Append("3");
            buffer.Append("4");

            // Assert
            Assert.Equal(2, buffer.CurrentCount);
            Assert.Collection(buffer.BufferSegments,
                segment =>
                {
                    var array = segment.Data.Array;
                    Assert.Equal("1", array[3].Value);
                    Assert.Equal("2", array[4].Value);
                },
                segment =>
                {
                    var array = segment.Data.Array;
                    Assert.Equal("3", array[3].Value);
                    Assert.Equal("4", array[4].Value);
                });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(TestRazorBufferScope.BufferSize + 3)]
        public void Clear_ResetsBackingBufferAndIndex(int valuesToWrite)
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");

            // Act
            for (var i = 0; i < valuesToWrite; i++)
            {
                buffer.Append("Hello");
            }
            buffer.Clear();
            buffer.Append("world");

            // Assert
            var segment = Assert.Single(buffer.BufferSegments);
            Assert.Equal(1, buffer.CurrentCount);
            Assert.Equal("world", segment.Data.Array[0].Value);
        }

        [Fact]
        public void WriteTo_WritesSelf_WhenWriterIsHtmlTextWriter()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var htmlWriter = new Mock<HtmlTextWriter>();
            htmlWriter.Setup(w => w.Write(buffer)).Verifiable();

            // Act
            buffer.Append("Hello world");
            buffer.WriteTo(htmlWriter.Object, new HtmlTestEncoder());

            // Assert
            htmlWriter.Verify();
        }

        [Fact]
        public void WriteTo_WritesRazorValues_ToTextWriter()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var writer = new StringWriter();

            // Act
            buffer.Append("Hello");
            buffer.Append(new HtmlString(" world"));
            buffer.AppendHtml(" 123");
            buffer.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("Hello world 123", writer.ToString());
        }

        [Theory]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(23)]
        public void WriteTo_WritesRazorValuesFromAllBuffers(int valuesToWrite)
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(1, 5), "some-name");
            var writer = new StringWriter();
            var expected = string.Join("", Enumerable.Range(0, valuesToWrite).Select(_ => "abc"));

            // Act
            for (var i = 0; i < valuesToWrite; i++)
            {
                buffer.AppendHtml("abc");
            }
            buffer.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal(expected, writer.ToString());
        }
    }
}