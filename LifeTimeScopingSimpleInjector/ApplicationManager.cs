using Data;
using MediatR;
using MediatR.Pipeline;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LifeTimeScopingSimpleInjector
{
    internal class ApplicationManager
    {
        public async Task Run()
        {
            var container = new Container();
            container.Options.DefaultScopedLifestyle = new ThreadScopedLifestyle();
            var assemblies = GetAssemblies().ToArray();
            container.RegisterSingleton<IMediator, Mediator>();
            container.Register(typeof(IRequestHandler<,>), assemblies);
            container.Register(typeof(IAsyncRequestHandler<,>), assemblies);

            container.RegisterSingleton(new SingleInstanceFactory(container.GetInstance));
            container.RegisterSingleton(new MultiInstanceFactory(container.GetAllInstances));

            container.RegisterCollection(typeof(IPipelineBehavior<,>), Enumerable.Empty<Type>());
            container.RegisterCollection(typeof(IRequestPreProcessor<>), Enumerable.Empty<Type>());
            container.RegisterCollection(typeof(IRequestPostProcessor<,>), Enumerable.Empty<Type>());

            container.RegisterDecorator(
               typeof(IAsyncRequestHandler<,>),
               typeof(LifetimeScopeDecorator<,>));

            container.Register<MyAwesomeContext>(Lifestyle.Scoped);

            container.Verify();

            var mediator = container.GetInstance<IMediator>();
            var res = await mediator.Send(new AddClient());
            Console.WriteLine(res);
        }

        private IEnumerable<Assembly> GetAssemblies()
        {
            yield return typeof(IMediator).GetTypeInfo().Assembly;
            yield return typeof(AddClient).GetTypeInfo().Assembly;
        }
    }

    internal class LifetimeScopeDecorator<TRequest, TResponse> :
    IAsyncRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        private readonly Func<IAsyncRequestHandler<TRequest, TResponse>> _decorateeFactory;
        private readonly Container _container;

        public LifetimeScopeDecorator(
            Func<IAsyncRequestHandler<TRequest, TResponse>> decorateeFactory,
            Container container)
        {
            _decorateeFactory = decorateeFactory;
            _container = container;
        }

        public async Task<TResponse> Handle(TRequest message)
        {
            using (ThreadScopedLifestyle.BeginScope(_container))
            {
                var result = await _decorateeFactory.Invoke().Handle(message);
                return result;
            }
        }
    }

    internal class AddClient : IRequest<int>
    {

    }
    internal class ClientHandler : IAsyncRequestHandler<AddClient, int>
    {
        private readonly MyAwesomeContext _ctx;
        public ClientHandler(MyAwesomeContext ctx)
        {
            _ctx = ctx;
        }
        public async Task<int> Handle(AddClient message)
        {
            var c = new Client { FirstName = "Arne" };
            _ctx.Client.Add(c);
            await _ctx.SaveChangesAsync();
            return c.Id;
        }
    }

    

}
