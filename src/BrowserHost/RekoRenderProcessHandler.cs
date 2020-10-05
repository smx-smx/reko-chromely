﻿using Reko.Chromely.RekoHosting;
using Reko.Core.Configuration;
using Reko.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Xilium.CefGlue;

namespace Reko.Chromely.BrowserHost
{
	/// <summary>
	/// Custom RenderProcessHandler used to register global variables and functions
	/// </summary>
	public class RekoRenderProcessHandler : CefRenderProcessHandler
	{
        private readonly PendingPromisesRepository pendingPromises;
        private readonly ServiceContainer services;
        private Decompiler? decompiler;

        public RekoRenderProcessHandler()
        {
            this.services = new ServiceContainer();
            this.pendingPromises = new PendingPromisesRepository();
        }

		/// <summary>
		/// Register globals once the context is ready
		/// </summary>
		/// <param name="browser"></param>
		/// <param name="frame"></param>
		/// <param name="context"></param>
		protected override void OnContextCreated(CefBrowser browser, CefFrame frame, CefV8Context context)
        {
            CreateRekoInstance(context);

            new RekoBrowserGlobals(pendingPromises, this.decompiler!, context).RegisterGlobals();
        }

        private void CreateRekoInstance(CefV8Context context)
        {
            var fsSvc = new FileSystemServiceImpl();
            var listener = new ListenerService(context);
            var diagSvc = new DiagnosticsService(listener, context);
            var dfSvc = new DecompiledFileService(fsSvc);
            services.AddService(typeof(IFileSystemService), fsSvc);
            services.AddService(typeof(DecompilerEventListener), listener);
            var configSvc = RekoConfigurationService.Load(services, "reko/reko.config");
            services.AddService(typeof(IConfigurationService), configSvc);
            services.AddService(typeof(IDiagnosticsService), diagSvc);
            services.AddService(typeof(IDecompiledFileService), dfSvc);
            services.AddService(typeof(ITypeLibraryLoaderService), new TypeLibraryLoaderServiceImpl(services));
            var loader = new Reko.Loading.Loader(services);
            this.decompiler = new Reko.Decompiler(loader, services);
        }

        protected override bool OnProcessMessageReceived(CefBrowser browser, CefFrame frame, CefProcessId sourceProcess, CefProcessMessage message)
        {
            if (message.Name == "openFileReply")
            {
                var promiseId = message.Arguments.GetInt(0);
                var filePath = message.Arguments.GetString(1);

                var promise = pendingPromises.RemovePromise(promiseId);
                if (filePath is null)
                {
                    promise.Reject(null!);
                }
                else
                {
                    promise.Resolve(filePath);
                }
                return true;
            }
            return false;
        }
    }
}