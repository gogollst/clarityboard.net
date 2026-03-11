using System.Reflection;

namespace ClarityBoard.Infrastructure.Tests.Services.Documents;

public class PdfPageRasterizerTests
{
    [Fact]
    public void EncodeRawBgraToBmp_ReturnsBmpHeaderAndExpectedSize()
    {
        var method = typeof(ClarityBoard.Infrastructure.Services.Documents.PdfPageRasterizer)
            .GetMethod("EncodeRawBgraToBmp", BindingFlags.NonPublic | BindingFlags.Static)!;

        var bgra = new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255,
        };

        var result = (byte[])method.Invoke(null, new object?[] { bgra, 2, 2 })!;

        Assert.Equal((byte)'B', result[0]);
        Assert.Equal((byte)'M', result[1]);
        Assert.Equal(54 + bgra.Length, result.Length);
    }
}