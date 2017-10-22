using Autofac;
using Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LifetimeScopingAutofac
{
    internal class ApplicationManager
    {
        public async Task Run()
        {
            var builder = new ContainerBuilder();

            // mediator itself
            builder
              .RegisterType<Mediator>()
              .As<IMediator>()
              .SingleInstance();

            // request handlers
            builder
              .Register<SingleInstanceFactory>(ctx => {
                  var c = ctx.Resolve<IComponentContext>();
                  return t => {object o; return c.TryResolve(t, out o) ? o : null; };
              })
              .InstancePerLifetimeScope();

            // notification handlers
            builder
              .Register<MultiInstanceFactory>(ctx => {
                  var c = ctx.Resolve<IComponentContext>();
                  return t => (IEnumerable<object>)c.Resolve(typeof(IEnumerable<>).MakeGenericType(t));
              })
              .InstancePerLifetimeScope();

            builder
            .RegisterType<MyAwesomeContext>()
            .AsSelf()
            .InstancePerLifetimeScope();

            // finally register our custom code (individually, or via assembly scanning)
            // - requests & handlers as transient, i.e. InstancePerDependency()
            // - pre/post-processors as scoped/per-request, i.e. InstancePerLifetimeScope()
            // - behaviors as transient, i.e. InstancePerDependency()
            //builder.RegisterAssemblyTypes(typeof(ClientHandler).GetTypeInfo().Assembly).Named(.AsImplementedInterfaces(); // via assembly scan
            builder.RegisterType<ClientHandler>().AsImplementedInterfaces();

            var container = builder.Build();

            var mediator = container.Resolve<IMediator>();

            using (var scope = container.BeginLifetimeScope())
            {
                var result = await mediator.Send(new AddClient());
                Console.WriteLine(result);
            }
        }
    }

    internal class AddClient : IRequest<int>
    {

    }
    internal class ClientHandler : IRequestHandler<AddClient, int>
    {
        private readonly MyAwesomeContext _ctx;
        public ClientHandler(MyAwesomeContext ctx)
        {
            _ctx = ctx;
        }
        public int Handle(AddClient message)
        {
            var c = new Client { FirstName = "Arne" };
            _ctx.Client.Add(c);
            _ctx.SaveChanges();
            return c.Id;
        }
    }
}
