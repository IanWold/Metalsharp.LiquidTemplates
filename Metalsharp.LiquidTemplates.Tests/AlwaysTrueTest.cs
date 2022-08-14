using Xunit;

namespace Metalsharp.LiquidTemplates.Tests;

public class AlwaysTrueTest
{
	[Fact]
	public void AlwaysTrue() =>
		Assert.True(true);
}
