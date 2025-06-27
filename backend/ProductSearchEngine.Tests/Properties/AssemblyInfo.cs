using NUnit.Framework;

// Enable parallel execution at the assembly level, but only for fixtures (test classes)
// This allows different test classes to run in parallel, but tests within the same class run sequentially
[assembly: Parallelizable(ParallelScope.Fixtures)]

// Set a reasonable number of workers to avoid overwhelming the system
[assembly: LevelOfParallelism(2)]
