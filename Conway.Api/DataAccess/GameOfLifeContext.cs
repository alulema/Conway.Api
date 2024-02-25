using Conway.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Conway.Api.DataAccess;

public class GameOfLifeContext : DbContext
{
    public GameOfLifeContext(DbContextOptions<GameOfLifeContext> options) : base(options) { }

    public virtual DbSet<GameBoard> Boards { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameBoard>().Ignore(b => b.State);
    }

}