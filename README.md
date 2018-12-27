DotNetStackPrinter is an app that prints the stack traces of a target .NET process.

E.g. you run `DotNetStackPrinter.exe [pid]` and you will get output that looks like:

```
[18][Internal Frame, 'U-->M']
	[15]System.Threading.ThreadPoolWorkQueue.Dispatch (source line information unavailable)
		System.Threading.ThreadPoolWorkQueue.Dispatch (source line information unavailable)
		...snip...
		System.Linq.Parallel.ForAllOperator`1+ForAllEnumerator`1<System.Int32,System.Int32>.MoveNext (source line information unavailable)
		[8]UserQuery.Foo (C:\Users\rlee\AppData\Local\Temp\LINQPad5\_jwdbjrbm\query_ygxjlx.cs:41)
			UserQuery.Foo (C:\Users\rlee\AppData\Local\Temp\LINQPad5\_jwdbjrbm\query_ygxjlx.cs:41)
			UserQuery.Bar (C:\Users\rlee\AppData\Local\Temp\LINQPad5\_jwdbjrbm\query_ygxjlx.cs:45)
		[7]UserQuery.Foo (C:\Users\rlee\AppData\Local\Temp\LINQPad5\_jwdbjrbm\query_ygxjlx.cs:39)
			UserQuery.Foo (C:\Users\rlee\AppData\Local\Temp\LINQPad5\_jwdbjrbm\query_ygxjlx.cs:39)
		[1]LINQPad.ExecutionModel.Server.StartExecutionTrackingBackstop (source line information unavailable)
			LINQPad.ExecutionModel.Server.StartExecutionTrackingBackstop (source line information unavailable)
			System.Threading.WaitHandle.WaitOne (source line information unavailable)
	...snip...
[1]ProcessServer.Program.Main (source line information unavailable)
	ProcessServer.Program.Main (source line information unavailable)
	...snip..
	LINQPad.ExecutionModel.ProcessServer.Run (source line information unavailable)
	System.Threading.WaitHandle.WaitOne (source line information unavailable)
```

The numbers indicate how many threads are in each prefix of the stack trace.

The code is basically a thin wrapper around MDbg: [documentation](https://docs.microsoft.com/en-us/dotnet/framework/tools/mdbg-exe) and 
[download](https://www.microsoft.com/en-us/download/details.aspx?id=2282).
