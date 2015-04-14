﻿using Autofac;
using MonoDevelop.VersionControl.TFS.Core;
using MonoDevelop.VersionControl.TFS.Core.Services;
using MonoDevelop.VersionControl.TFS.Core.Structure;
using MonoDevelop.VersionControl.TFS.MonoDevelopWrappers;
using MonoDevelop.VersionControl.TFS.MonoDevelopWrappers.Implementation;
using MonoDevelop.VersionControl.TFS.VersionControl;
using MonoDevelop.VersionControl.TFS.VersionControl.Structure;

namespace MonoDevelop.VersionControl.TFS.Infrastructure
{
    static class DependencyInjection
    {
        public static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<Workspace>().As<IWorkspace>();
            builder.RegisterType<SoapInvoker>().As<ISoapInvoker>();
            Container = builder.Build();
        }

        public static IContainer Container { get; private set; }

        public static IWorkspace GetWorkspace(WorkspaceData workspaceData, ProjectCollection collection)
        {
            return Container.Resolve<IWorkspace>(new TypedParameter(typeof(WorkspaceData), workspaceData),
                                                 new TypedParameter(typeof(ProjectCollection), collection));
        }

        public static ISoapInvoker GetSoapInvoker(TFSService service)
        {
            return Container.Resolve<ISoapInvoker>(new TypedParameter(typeof(TFSService), service));
        }

    }


    public class ServiceBuilder : ContainerBuilder
    {
        public ServiceBuilder()
        {
            this.RegisterType<ProjectService>().As<IProjectService>().SingleInstance();
            this.RegisterType<LoggingService>().As<ILoggingService>().SingleInstance();
            this.RegisterType<ProgressService>().As<IProgressService>().SingleInstance();
        }
    }
}