namespace PpmDecoderSharp.Test;

public class ParseHeaderTest
{
    public sealed record HeaderRecord(int Type, int Width, int Height, int Max, string? Comment = null)
    {
        internal static HeaderRecord Create(PpmHeader ppm) =>
            new((int)ppm.Format, ppm.Width, ppm.Height, ppm.MaxLevel, ppm.Comment);
    }

    [Theory]
    [InlineData("P6\n12 34\n255 ", 6, 12, 34, 255, null)]
    [InlineData("P2  \n 2\t 3 \t4\n ", 2, 2, 3, 4, null)]
    [InlineData("P3\n# comment\n12 34\n65535\r", 3, 12, 34, 65535, "comment")]
    [InlineData("P5\n# com ent\n12 34\n65535\r\n", 5, 12, 34, 65535, "com ent")]
    [InlineData("P6 23 45 67\n", 6, 23, 45, 67, null)]
    [InlineData("P6 # comment 23 45 67\t", 6, 23, 45, 67, "comment")]
    [InlineData("P1 2 3 ", 1, 2, 3, 1, null)]
    [InlineData("P4 # comment 2 3 ", 4, 2, 3, 1, "comment")]
    public void ParseHeaderText_Success(string headerText, int type, int width, int height, int maxLevel, string? comment)
    {
        var expected = new HeaderRecord(type, width, height, maxLevel, comment);

        var header = PpmHeader.ParseHeaderText(headerText);
        Assert.NotNull(header);

        var actual = HeaderRecord.Create(header);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("P6\n2 3\n4")]
    [InlineData("P6\n2 3\n")]
    [InlineData("P6\n2 ")]
    [InlineData("P6\n")]
    [InlineData("P")]
    [InlineData("")]
    [InlineData("P6\n2\n3\n")]
    [InlineData("P6\n\n2\n3\n")]
    [InlineData("P\n2 3\n4")]
    public void ParseHeaderText_Failed(string headerText)
    {
        var header = PpmHeader.ParseHeaderText(headerText);
        header.Should().BeNull();
    }

}
