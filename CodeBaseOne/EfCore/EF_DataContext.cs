using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.EfCore
{
    /// <summary>
    /// DbContext acts as a bridge between the logic and the db
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class EF_DataContext : DbContext
    {
        /// <summary>
        /// moq woruld require a parameterless constructor
        /// N.B. - not currently mocking this - instead we use an in-memory db for tests
        /// </summary>
        public EF_DataContext() { }

        /// <summary>
        /// delegate the creation of the
        /// datacontext to the .Net dependency injection container.
        /// To allow this we need to expose a public constructor
        /// with a DbContextOptions parameter. This way we can pass configuration
        /// to the context.
        /// </summary>
        public EF_DataContext(DbContextOptions<EF_DataContext> options): base(options) { }

        /// <summary>
        /// We want the identifer columns to increment themselves.
        /// We override OnModelCreating as follows:
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // auto incrementing with PostgreSQL - and uuid generation when required
            modelBuilder.UseSerialColumns();
            // Add the Postgres Extension for UUID generation 
            modelBuilder.HasPostgresExtension("uuid-ossp");
        }

        public DbSet<Product> Products { get; set; }
        public virtual DbSet<User> Users { get; set; } // enables lazy loading
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; } // can be overridden - eg when mocking
    }
}
