using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PaperService> Services => Set<PaperService>();
    public DbSet<PaperRequest> Requests => Set<PaperRequest>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(user => user.GoogleSubject).IsUnique();
        modelBuilder.Entity<PendingRegistration>().HasIndex(item => item.Email).IsUnique();
        modelBuilder.Entity<PaperRequest>()
            .HasOne(request => request.Student)
            .WithMany(user => user.Requests)
            .HasForeignKey(request => request.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PaperRequest>()
            .HasOne(request => request.Service)
            .WithMany(service => service.Requests)
            .HasForeignKey(request => request.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
