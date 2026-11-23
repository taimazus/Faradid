using System;
using System.IO;
using System.Threading.Tasks;
using Faradid.Engine;
using Xunit;

namespace Faradid.Tests;

public class LocalSourceAuditorTests
{
    [Fact]
    public async Task AuditPathAsync_SimulatedDirectory_EvaluatesRulesAndScores()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "FaradidAuditTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var webConfig = @"<configuration>
  <system.webServer>
    <httpErrors errorMode=""Custom"" />
    <rewrite>
      <rules>
        <rule name=""Redirect to HTTPS"" stopProcessing=""true"">
          <match url=""(.*)"" />
        </rule>
      </rules>
    </rewrite>
    <staticContent>
      <clientCache cacheControlMode=""UseMaxAge"" cacheControlMaxAge=""30.00:00:00"" />
    </staticContent>
  </system.webServer>
</configuration>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "web.config"), webConfig);

            var robots = "User-agent: *\nDisallow: /admin\nSitemap: https://test.ir/sitemap.xml";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "robots.txt"), robots);

            var view = @"<!DOCTYPE html>
<html>
<head>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <link rel=""canonical"" href=""https://test.ir/"" />
  <meta property=""og:title"" content=""تست"" />
  <script type=""application/ld+json"">{}</script>
</head>
<body></body>
</html>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Index.cshtml"), view);

            var auditor = new LocalSourceAuditor();

            // Act
            var result = await auditor.AuditPathAsync(tempDir, Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TechnicalHealthScore >= 80);
            Assert.True(result.PassedChecksCount >= 5);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
