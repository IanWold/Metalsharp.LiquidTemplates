using Xunit;

namespace Metalsharp.LiquidTemplates.Tests;

public class LiquidTemplatesTests
{
	record Author(string Name);

	static MetalsharpFile TemplateFile(string name, string liquid) =>
		new(liquid, $"templates/{name}.liquid");

	static MetalsharpFile OutputFile(string path, string html, Dictionary<string, object>? metadata = null) =>
		metadata is null
			? new MetalsharpFile(html, path)
			: new MetalsharpFile(html, path, metadata);

	static void WithTempDirectory(Action<string> action)
	{
		var directory = Directory.CreateTempSubdirectory();

		try
		{
			action(directory.FullName);
		}
		finally
		{
			directory.Delete(recursive: true);
		}
	}

	[Fact]
	public void UseLiquidTemplates_ReturnsSameProjectForChaining()
	{
		var project = new MetalsharpProject();

		var result = project.UseLiquidTemplates("templates", false);

		Assert.Same(project, result);
	}

	[Fact]
	public void UseLiquidTemplates_RendersOutputFiles_UsingProvidedTemplateDirectory()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("layout", "<html>{{ content }}</html>"));
		project.AddOutput(OutputFile("index.html", "<p>Body</p>"));

		project.UseLiquidTemplates("templates", false);

		Assert.Equal("<html><p>Body</p></html>", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void UseLiquidTemplates_DefaultsLoadFromFilesystemToTrue() =>
		WithTempDirectory(directory =>
		{
			File.WriteAllText(Path.Combine(directory, "layout.liquid"), "<html>{{ content }}</html>");

			var project = new MetalsharpProject();
			project.AddOutput(OutputFile("index.html", "<p>Body</p>"));

			project.UseLiquidTemplates(directory);

			Assert.Equal("<html><p>Body</p></html>", project.OutputFiles.Single().Text);
		});

	[Fact]
	public void Execute_RendersNamedTemplateThenLayout_WhenTemplateMetadataIsSet()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("layout", "<html>{{ content }}</html>"));
		project.AddInput(TemplateFile("post", "<article>{{ content }}</article>"));
		project.AddOutput(OutputFile("post.html", "<p>Body</p>", new() { ["template"] = "post" }));

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Equal("<html><article><p>Body</p></article></html>", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void Execute_RendersLayoutOnly_WhenNoTemplateMetadataIsSet()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("layout", "<html>{{ content }}</html>"));
		project.AddOutput(OutputFile("index.html", "<p>Body</p>"));

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Equal("<html><p>Body</p></html>", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void Execute_SkipsNamedTemplateStep_WhenTemplateMetadataValueIsNotAString()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("layout", "<html>{{ content }}</html>"));
		project.AddOutput(OutputFile("index.html", "<p>Body</p>", new() { ["template"] = 42 }));

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Equal("<html><p>Body</p></html>", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void Execute_IgnoresNonHtmlOutputFiles()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("layout", "<html>{{ content }}</html>"));
		project.AddOutput(OutputFile("style.css", "body { color: red; }"));

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Equal("body { color: red; }", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void Execute_DoesNotMatchTemplateName_WhenMetadataIncludesFileExtension()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("post", "<article>{{ content }}</article>"));
		project.AddOutput(OutputFile("index.html", "<p>Body</p>", new() { ["template"] = "post.liquid" }));

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Equal("<p>Body</p>", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void Execute_LogsError_WhenNamedTemplateIsNotFound()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("layout", "{{ content }}"));
		project.AddOutput(OutputFile("index.html", "<p>Body</p>", new() { ["template"] = "missing" }));

		var errors = new List<string>();
		project.OnAnyLog += (_, args) =>
		{
			if (args.Level == LogLevel.Error)
			{
				errors.Add(args.Message);
			}
		};

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Contains(errors, message => message.Contains("missing"));
	}

	[Fact]
	public void Execute_LeavesOutputUnchanged_WhenNamedTemplateIsNotFound()
	{
		var project = new MetalsharpProject();
		project.AddOutput(OutputFile("index.html", "<p>Body</p>", new() { ["template"] = "missing" }));

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Equal("<p>Body</p>", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void Execute_LogsError_WhenNoLayoutTemplateIsLoaded()
	{
		var project = new MetalsharpProject();
		project.AddOutput(OutputFile("index.html", "<p>Body</p>"));

		var errors = new List<string>();
		project.OnAnyLog += (_, args) =>
		{
			if (args.Level == LogLevel.Error)
			{
				errors.Add(args.Message);
			}
		};

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Contains(errors, message => message.Contains("layout"));
	}

	[Fact]
	public void Execute_ExposesFileMetadataAsLiquidVariables()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("layout", "<title>{{ title }}</title>{{ content }}"));
		project.AddOutput(OutputFile("index.html", "<p>Body</p>", new() { ["title"] = "Hello World" }));

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Equal("<title>Hello World</title><p>Body</p>", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void Execute_AllowsUnsafeAccessToNestedMetadataObjectProperties()
	{
		var project = new MetalsharpProject();
		project.AddInput(TemplateFile("layout", "{{ author.Name }}: {{ content }}"));
		project.AddOutput(OutputFile("index.html", "<p>Body</p>", new() { ["author"] = new Author("Ian") }));

		new LiquidTemplates("templates", false).Execute(project);

		Assert.Equal("Ian: <p>Body</p>", project.OutputFiles.Single().Text);
	}

	[Fact]
	public void Execute_LoadsTemplatesFromDisk_WhenLoadFromFilesystemIsTrue() =>
		WithTempDirectory(directory =>
		{
			File.WriteAllText(Path.Combine(directory, "layout.liquid"), "<html>{{ content }}</html>");

			var project = new MetalsharpProject();
			project.AddOutput(OutputFile("index.html", "<p>Body</p>"));

			new LiquidTemplates(directory).Execute(project);

			Assert.Equal("<html><p>Body</p></html>", project.OutputFiles.Single().Text);
		});

	[Fact]
	public void Execute_DoesNotAccessDisk_WhenLoadFromFilesystemIsFalse()
	{
		var project = new MetalsharpProject();
		project.AddOutput(OutputFile("index.html", "<p>Body</p>"));

		var exception = Record.Exception(() =>
			new LiquidTemplates("/this/path/does/not/exist", false).Execute(project));

		Assert.Null(exception);
	}
}
