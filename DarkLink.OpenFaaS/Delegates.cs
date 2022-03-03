using System;
using Microsoft.AspNetCore.Http;

namespace DarkLink.OpenFaaS;

public delegate Task FunctionContextDelegate(HttpContext context);

public delegate Task<object?> FunctionObjectAsyncDelegate<in TInput>(HttpContext context, TInput? input);

public delegate Task<string> FunctionStringAsyncDelegate(HttpContext context, string input);

public delegate object? FunctionObjectDelegate<in TInput>(HttpContext context, TInput? input);

public delegate string FunctionStringDelegate(HttpContext context, string input);
