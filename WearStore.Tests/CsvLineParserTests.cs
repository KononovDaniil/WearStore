using WearStore.Services;

namespace WearStore.Tests;

public class CsvLineParserTests
{
    [Fact]
    public void Parse_SimpleLine_ReturnsTwelveFields()
    {
        var line = "3001,Футболка белая,Летняя футболка,990,RUB,Одежда,25,/images/tshirt.jpg,true,M,белый,хлопок";

        var fields = CsvLineParser.Parse(line);

        Assert.Equal(12, fields.Count);
        Assert.Equal("3001", fields[0]);
    }

    [Fact]
    public void Parse_EmptyFields_AreEmptyStrings()
    {
        var line = "3002,Джинсы синие,,3490,RUB,Одежда,10,,true,46,синий,деним";

        var fields = CsvLineParser.Parse(line);

        Assert.Equal(12, fields.Count);
        Assert.Equal(string.Empty, fields[2]);
        Assert.Equal(string.Empty, fields[7]);
    }

    [Fact]
    public void Parse_CommaInsideQuotes_DoesNotSplitField()
    {
        var line = "3003,\"Футболка, оверсайз\",Тест,1990,RUB,Одежда,5,/img.jpg,true,L,чёрный,хлопок";

        var fields = CsvLineParser.Parse(line);

        Assert.Equal(12, fields.Count);
        Assert.Equal("Футболка, оверсайз", fields[1]);
    }

    [Fact]
    public void Parse_EscapedQuotes_ReturnsSingleQuotes()
    {
        var line = "3004,\"Футболка \"\"Special\"\"\",Описание,1500,RUB,Одежда,5,/img.jpg,true,M,красный,хлопок";

        var fields = CsvLineParser.Parse(line);

        Assert.Equal(12, fields.Count);
        Assert.Equal("Футболка \"Special\"", fields[1]);
    }

    [Fact]
    public void Parse_TrimsSpacesAroundFields()
    {
        var fields = CsvLineParser.Parse(" a , b ,c");

        Assert.Equal(new[] { "a", "b", "c" }, fields);
    }
}