# Integration Tests

Each scenario is stored under the [`TestData`](./TestData) directory as its own subdirectory.

Within it, there are two additional subdirectories: `Actual` and `Expected`. Everything in the `Actual` subdirectory is copied to a temporary directory for the tool to work from -- all files and subdirectories are recursively copied. After the tool is invoked, the test case can make assertions on the resulting files against those in the `Expected` directory for equality.

All the test files have had their extensions changed since having .CSPROJ files and .PROPS files could affect the solution. When these files are copied in preparation for the tool, their extensions are renamed to their intended use. `ProjectA.xml` becomes `ProjectA.csproj`, for example.
