[![.NET](https://github.com/BlazorOcticons/BlazorOcticons/actions/workflows/dotnet.yml/badge.svg)](https://github.com/BlazorOcticons/BlazorOcticons/actions/workflows/dotnet.yml)
![GitHub](https://img.shields.io/github/license/BlazorOcticons/BlazorOcticons)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/BlazorOcticons?logo=nuget)

# BlazorOcticons

**BlazorOcticons** is an easy-to-use GitHub Octicons built as customizable `.razor` components.

| NuGet Package           | Description |
|-------------------------|-------------|
| BlazorOcticons          | Main package which contains only `.razor` components |
| BlazorOcticonsGenerator | Helper package which contains Source Generator for Octicons |

## Installation

1. Install the BlazorOcticons NuGet package

``` 
dotnet add package BlazorOcticons
```

2. In the `_Imports.razor` file add `@using BlazorOcticons.Octicons;`

``` razor

@using System.Net.Http
@using System.Net.Http.Json
...
@using Microsoft.AspNetCore.Components
@using Microsoft.JSInterop
@using BlazorOcticons.Octicons

```

3. That's it! Now you can use the components in your project

## Usage

Inside your code, use any of GitHub Octicons as `.razor` components:

``` razor
<div class="p-3">
  ...
  <MarkGithub16 Color="#702AF7" Size="48"/>
  ...
</div>
```

## Contribute

All contributions are welcome! Feel free to raise any issues (bugs or feature requests), submit pull requests, etc.
