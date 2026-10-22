using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CorvusIAM.Data
{
    public class SqliteDbContext : ApplicationDbContext
    {
        public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SQLite has no native rowversion type. Keep the field as a normal
            // concurrency token so inserts do not fail with a NOT NULL value.
            modelBuilder.Entity<CorvusIAM.Entities.ChamadoSuporte>()
                .Property(c => c.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedNever();
        }
    }

    public class SqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
    {
        public SqliteDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
            optionsBuilder.UseSqlite("Data Source=corvusiam.db");
            return new SqliteDbContext(optionsBuilder.Options);
        }
    }
}
