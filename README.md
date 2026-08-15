<div align="center">

# Metalsharp.LiquidTemplates

[![NuGet](https://img.shields.io/nuget/v/Metalsharp.LiquidTemplates.svg?logo=nuget&logoColor=white&style=for-the-badge)](https://www.nuget.org/packages/Metalsharp.LiquidTemplates/)

A [Metalsharp](https://github.com/IanWold/Metalsharp) plugin that renders your site's HTML through [Liquid](https://shopify.github.io/liquid/) templates, using [Fluid](https://github.com/sebastienros/fluid).

</div>

## Getting Started

Metalsharp.LiquidTemplates targets .NET 10 and is available on [NuGet](https://www.nuget.org/packages/Metalsharp.LiquidTemplates/):

```plaintext
dotnet add package Metalsharp.LiquidTemplates
```

It's a plugin for [Metalsharp](https://www.nuget.org/packages/Metalsharp/), so you'll want that too if you don't have it already:

```plaintext
dotnet add package Metalsharp
```

### Project Structure

A typical project using this plugin looks something like:

```text
ProjectFolder
├── Site
│   └── hello-world.md
├── Templates
│   ├── layout.liquid
│   └── post.liquid
├── Static
│   └── style.css
└── README.md
```

`Site` holds the content Metalsharp will process, `Templates` holds the `.liquid` files this plugin renders, and `Static` holds anything copied straight through to the output. None of these names are required — pick whatever layout suits your project.

## Tutorial

This walks through building a small blog-style site: Markdown content, a shared page layout, and a template specifically for posts.

### Write your content

Metalsharp doesn't know about `template` or `title` on its own — those come from [`Frontmatter`](https://github.com/IanWold/Metalsharp), which reads YAML frontmatter off each file into its metadata. Metalsharp.LiquidTemplates then reads that same metadata back out when it renders.

`Site/hello-world.md`:

```markdown
---
title: Hello World
author: Ian
template: post
---

This is my first post using **Metalsharp.LiquidTemplates**.
```

### Write a layout

`layout.liquid` is special: if it exists in your templates directory, it's applied to *every* HTML output file, whether or not that file specifies a `template`. Use it for the markup every page shares — `<html>`, navigation, footer, and so on. `{{ content }}` is where the rest of the rendered page gets injected.

`Templates/layout.liquid`:

```liquid
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>{{ title }}</title>
</head>
<body>
  <header>My Site</header>
  <main>{{ content }}</main>
  <footer>&copy; 2026</footer>
</body>
</html>
```

Any metadata a file has — `title`, `author`, whatever your frontmatter defines — is available directly in Liquid, in every template, including the layout.

### Write a template

Templates other than `layout` are opt-in: a file only gets one applied if its metadata sets `template` to that template's name (the filename without its `.liquid` extension). Named templates run *before* the layout, so their output becomes the layout's `{{ content }}`.

`Templates/post.liquid`:

```liquid
<article>
  <h1>{{ title }}</h1>
  <p class="byline">By {{ author }}</p>
  {{ content }}
</article>
```

Here, `{{ content }}` is the file's own body — in this case, the HTML that `UseMarkdown()` already generated from `hello-world.md`.

### Use the Metalsharp.LiquidTemplates plugin

```c#
using Metalsharp;
using Metalsharp.LiquidTemplates;

new MetalsharpProject()
    .AddInput("Site")
    .UseFrontmatter()
    .UseMarkdown()
    .UseLiquidTemplates("Templates")
    .AddOutput("Static")
    .Build();
```

Order matters: `UseLiquidTemplates` only renders HTML files that already exist in the output, so it needs to run after whatever plugin produces them — here, `UseMarkdown()`. By default it also reads `Templates` straight off disk and adds those files to the project for you, so you don't need a separate `AddInput` call for them.

### The result

`UseMarkdown()` keeps each file's virtual path when it converts it, so `Site/hello-world.md` becomes an output file at `Site/hello-world.html` — that's the path `Build()` writes to disk under the project's output directory. Its rendered content is:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <title>Hello World</title>
</head>
<body>
    <header>My Site</header>
    <main><article>
    <h1>Hello World</h1>
    <p class="byline">By Ian</p>
    <p>This is my first post using <strong>Metalsharp.LiquidTemplates</strong>.</p>
</article>
</main>
    <footer>&copy; 2026</footer>
</body>
</html>
```

A page with no `template` set in its frontmatter still gets wrapped in `layout.liquid` — it just skips the `post` step, so `{{ content }}` in the layout is that page's raw HTML instead of an `<article>`.

## How It Works

`UseLiquidTemplates(templateDirectory, loadFromFilesystem = true)` takes two arguments:

- **`templateDirectory`** — where your `.liquid` files live. What this path means depends on `loadFromFilesystem`.
- **`loadFromFilesystem`** — `true` by default. The plugin adds every file under `templateDirectory` on disk into Metalsharp's input files for you. Set it to `false` if you've already added the template files yourself (for example, via `.AddInput(...)`, or from an embedded resource) — in that case, `templateDirectory` is the name of the virtual directory they're already in, rather than a path on disk.

On build, the plugin:

1. Parses every file it finds in the template directory as a Liquid template, keyed by name (without the `.liquid` extension).
2. For each `.html` output file, checks its metadata for a `template` key. If present and a template by that name exists, renders it — the file's current content is available in the template as `{{ content }}`, and the rest of the file's metadata is available by name (`{{ title }}`, `{{ author }}`, etc.).
3. Renders `layout` over the (possibly already-templated) result, the same way.

Because metadata access uses Fluid's unsafe member access strategy, any metadata value — including nested objects — is readable from your templates without extra configuration.

If your template directory has no `layout.liquid`, the plugin logs an error for every HTML file it processes, since it always tries to apply one. Give it at least a minimal `layout.liquid` to avoid the noise.

## Docs

See the [Metalsharp documentation](https://github.com/IanWold/Metalsharp/blob/master/Metalsharp.Documentation/README.md) for more on Metalsharp itself — plugins, the file pipeline, and writing your own plugins — and the [Fluid documentation](https://github.com/sebastienros/fluid) for the full Liquid syntax supported here.
