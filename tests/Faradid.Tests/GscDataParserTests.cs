using System.IO;
using System.Text;
using System.Threading.Tasks;
using Faradid.Engine;
using Xunit;

namespace Faradid.Tests;

public class GscDataParserTests
{
    [Fact]
    public async Task ParseCsvAsync_ValidCsv_ReturnsPopulatedSummary()
    {
        // Arrange
        var parser = new GscDataParser();
        var csvContent = @"Date,Query,Page,Country,Device,Clicks,Impressions,CTR,Position
2026-11-01,راهکار الکترونیک سهند,https://irres.ir/,IRN,MOBILE,15,120,12.5%,1.2
2026-11-01,نرم افزار پسماند,https://irres.ir/solutions/waste,DEU,DESKTOP,8,95,8.4%,3.5
2026-11-02,کنترل تردد سازمانی,https://irres.ir/solutions/access,IRN,MOBILE,12,110,10.9%,2.1";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await parser.ParseCsvAsync(stream, "آزمون دوره");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("آزمون دوره", result.PeriodName);
        Assert.Equal(35, result.TotalClicks);
        Assert.Equal(325, result.TotalImpressions);
        Assert.True(result.AverageCtr > 0.10);
        Assert.Equal(2, result.CountryBreakdown.Count);
        Assert.Equal(2, result.DeviceBreakdown.Count);
    }

    [Fact]
    public void CreatePresetData_IrresDomain_ReturnsPopulatedMetricsAndGermanVpnSplit()
    {
        // Arrange
        var parser = new GscDataParser();

        // Act
        var result = parser.CreatePresetData("irres.ir", "دوره آزمایشی", 28);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalClicks > 100);
        Assert.True(result.TotalImpressions > 2000);
        Assert.NotEmpty(result.TopQueries);
        Assert.NotEmpty(result.TopPages);
        Assert.Contains(result.CountryBreakdown, c => c.Key == "DEU");
        Assert.Contains(result.CountryBreakdown, c => c.Key == "IRN");
    }
}
