﻿using Reko.Chromely.BrowserHost.Functions;
using System;
using System.Collections.Generic;
using System.Text;
using Xilium.CefGlue;

namespace Reko.Chromely.BrowserHost
{
	public class RekoBrowserGlobals
	{
		/// <summary>
		/// Create and store a function
		/// <paramref name="functionName" /> in the object <paramref name="global" />
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="functionName"></param>
		/// <param name="global"></param>
		private void RegisterFunction<T>(string functionName, CefV8Value global) where T : CefV8Handler, new() {
			var fn = CefV8Value.CreateFunction(functionName, new T());
			global.SetValue(functionName, fn);
		}

		/// <summary>
		/// Register custom variables and functions
		/// in the global context
		/// </summary>
		/// <param name="context"></param>
		public void RegisterGlobals(CefV8Context context) {
			context.Acquire(() => {
				var glbl = context.GetGlobal();
				RegisterFunction<ExecuteJavascript>("ExecuteJavascript", glbl);
			});
		}
	}
}
