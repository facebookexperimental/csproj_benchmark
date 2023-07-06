# csproj_benchmark

This is a tool to measure Visual Studio startup performance.

It allows benchmark Visual Studio's performance when opening solutions with a large number of projects.

Compares Unity-specific C# projects vs regular C# project files.

## Usage

Run the project specifying path to a template project and number of iterations.
Iterations are exponential, meaning 2^n projects will be generated for each iteration n.

For example:

```
cd MeasureVisualStudioOpenTime
dotnet run ..\ScaleTest\ScaleTest001.csproj 2
```

Results of the test pass will be written to "%TEMP%"\generatedTestResults.csv

## Example output (n=6, largest solution has 2^6 = 64 projects)

Project Count,Using Unity,Time (s)
2,false,3.0529294
2,true,3.4489026
4,false,3.1521585
4,true,3.7414049
8,false,3.1588828
8,true,4.2534398
16,false,3.2494854
16,true,5.104617
32,false,3.7950575
32,true,7.3427347
64,false,4.5223834
64,true,11.0249453

We notice a significant increase for Unity-style projects.

## Contributing

See the [CONTRIBUTING](CONTRIBUTING.md) file for how to help out.

## License

MIT License

Copyright (c) Meta Platforms, Inc. and affiliates.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
