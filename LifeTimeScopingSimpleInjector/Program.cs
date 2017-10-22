using LifeTimeScopingSimpleInjector;

namespace lifetimescoping
{
    class Program
    {
        static void Main(string[] args)
        {
            var application = new ApplicationManager();
            application.Run().GetAwaiter().GetResult();
        }
    }
}
