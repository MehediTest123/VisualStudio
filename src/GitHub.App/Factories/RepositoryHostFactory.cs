﻿using System;
using System.ComponentModel.Composition;
using GitHub.Authentication;
using GitHub.Caches;
using GitHub.Models;
using GitHub.Primitives;
using GitHub.Services;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using GitHub.Api;
using IObservableKeychainAdapter = GitHub.Caches.IObservableKeychainAdapter;

namespace GitHub.Factories
{
    [Export(typeof(IRepositoryHostFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RepositoryHostFactory : IRepositoryHostFactory
    {
        readonly IApiClientFactory apiClientFactory;
        readonly IHostCacheFactory hostCacheFactory;
        readonly ILoginManager loginManager;
        readonly IObservableKeychainAdapter keychain;
        readonly IAvatarProvider avatarProvider;
        readonly CompositeDisposable hosts = new CompositeDisposable();
        readonly IUsageTracker usage;

        [ImportingConstructor]
        public RepositoryHostFactory(
            IApiClientFactory apiClientFactory,
            IHostCacheFactory hostCacheFactory,
            ILoginManager loginManager,
            IObservableKeychainAdapter keychain,
            IAvatarProvider avatarProvider,
            IUsageTracker usage)
        {
            this.apiClientFactory = apiClientFactory;
            this.hostCacheFactory = hostCacheFactory;
            this.loginManager = loginManager;
            this.keychain = keychain;
            this.avatarProvider = avatarProvider;
            this.usage = usage;
        }

        public async Task<IRepositoryHost> Create(HostAddress hostAddress)
        {
            var apiClient = await apiClientFactory.Create(hostAddress);
            var hostCache = await hostCacheFactory.Create(hostAddress);
            var modelService = new ModelService(apiClient, hostCache, avatarProvider);
            var host = new RepositoryHost(apiClient, modelService, loginManager, keychain, usage);
            hosts.Add(host);
            return host;
        }

        public void Remove(IRepositoryHost host)
        {
            hosts.Remove(host);
        }

        bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                disposed = true;
                hosts.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
