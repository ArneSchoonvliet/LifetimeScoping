using System.Data.Entity;

namespace Data
{
    public class MyAwesomeContext : DbContext
    {
        public MyAwesomeContext()
        {

        }
        public DbSet<Client> Client { get; set; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public class Client
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
    }
}
