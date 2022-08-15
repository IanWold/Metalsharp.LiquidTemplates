using Fluid;

namespace Metalsharp.LiquidTemplates;

public static class MetalsharpFileExtensions
{
	public static TemplateContext GetTemplateContext(this MetalsharpFile file)
	{
		var context = new TemplateContext(
			file.Metadata,
			new TemplateOptions()
			{
				MemberAccessStrategy = new UnsafeMemberAccessStrategy()
			}
		);

		context.SetValue("content", file.Text);

		return context;
	}
}
