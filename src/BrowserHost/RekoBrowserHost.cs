﻿#region
// Copyright 2020 the Reko contributors.

// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the
// 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
#endregion

using Caliburn.Light;
using Chromely.CefGlue.Browser;
using Chromely.Core.Configuration;
using Chromely.Core.Defaults;
using System;
using System.IO;
using System.Reflection;
using Xilium.CefGlue;
using Xilium.CefGlue.Wrapper;

namespace Reko.Chromely.BrowserHost
{
    public class RekoBrowserHost
	{
		private const string WINDOW_TITLE = "Reko Decompiler";

		private static IChromelyConfiguration CreateConfiguration() {
			return new DefaultConfiguration();
		}

		private static CefSettings CreateSettings() {
			return new CefSettings() {
				MultiThreadedMessageLoop = false,
				NoSandbox = true
			};
        }

		private static CefMessageRouterBrowserSide CreateRouter() {
			var routerConfig = new CefMessageRouterConfig();
			return new CefMessageRouterBrowserSide(routerConfig);
		}
		
		private static CefBrowserSettings CreateBrowserSettings() {
			return new CefBrowserSettings();
		}

		/// <summary>
		/// Creates the window construction parameters
		/// <see cref="CefWindowInfo"/> describes how we want the window to be created
		/// </summary>
		/// <returns></returns>
		private static CefWindowInfo CreateWindowInfo() {
			var windowInfo = CefWindowInfo.Create();
			windowInfo.SetAsPopup(IntPtr.Zero, WINDOW_TITLE);
			return windowInfo;
		}

        private CefGlueBrowser CreateBrowserObject() {
			var router = CreateRouter();          

			var container = new SimpleContainer();
			var taskRunner = new DefaultCommandTaskRunner(container);

			var browserSettings = CreateBrowserSettings();
			return new RekoGlueBrowser(this, container, config, taskRunner, router, browserSettings);
		}

		private readonly IChromelyConfiguration config;
		private readonly RekoBrowserApp app;

		private RekoGlueBrowser? browser;
		private CefBrowserHost? host;
		private bool running = false;

		private readonly string initialUrl;

		public RekoBrowserHost() {
			this.config = CreateConfiguration();
			this.app = new RekoBrowserApp(config);

			// Obtain the html url relative to the executing assembly
			var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
			initialUrl = Path.Combine(baseDirectory!, "app", "index.html");
		}

		/// <summary>
		/// Create the browser and start the blocking message loop
		/// </summary>
		public int Run(string[] args) {
            CefRuntime.Load();
            var mainArgs = new CefMainArgs(args);
            
            // fork. -1 indicates a browser process has been created
            int exitCode = CefRuntime.ExecuteProcess(mainArgs, app, IntPtr.Zero);
            if (exitCode != -1) {
				/**
				 * if we get here, we're not running on the browser process.
				 * since the initialization code must be ran only on the main browser process
				 * we bail out
				 **/
				return exitCode;
			}

            var settings = CreateSettings();
            CefRuntime.Initialize(mainArgs, settings, app, IntPtr.Zero);
            CefRuntime.RegisterSchemeHandlerFactory("reko", "", new RekoSchemeHandlerFactory());

            this.browser = (RekoGlueBrowser)CreateBrowserObject();
			browser.StartUrl = initialUrl;

			browser.Created += Browser_Created;
			browser.BeforeClose += Browser_BeforeClose;

			var window = CreateWindowInfo();
			browser.Create(window);

			running = true;
			while (running) {
				CefRuntime.DoMessageLoopWork();
			}

			host!.CloseBrowser(false);
			CefRuntime.Shutdown();
            return 0;
		}

		private void Browser_BeforeClose(object? sender, global::Chromely.CefGlue.Browser.EventParams.BeforeCloseEventArgs e) {
			this.running = false;
		}

		/// <summary>
		/// Saves the host locally as soon as the browser has been created
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Browser_Created(object? sender, EventArgs e) {
			var browser = sender as CefGlueBrowser;
			host = browser!.CefBrowser.GetHost();
        }
	}
}
