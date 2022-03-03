using System;
using Microsoft.AspNetCore.Http;

namespace DarkLink.OpenFaaS;

public delegate Task Delegates(HttpContext context);

public delegate Task<object?> FunctionObjectDelegate<in TInput>(HttpContext context, TInput? input);

public delegate Task<string> FunctionStringDelegate(HttpContext context, string input);
