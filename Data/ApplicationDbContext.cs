using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintMessage> ComplaintMessages => Set<ComplaintMessage>();
    public DbSet<ComplaintAttachment> ComplaintAttachments => Set<ComplaintAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Business>(e =>
        {
            e.HasIndex(b => b.OwnerUserId);
            e.HasOne(b => b.Owner)
                .WithMany()
                .HasForeignKey(b => b.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Complaint>(e =>
        {
            e.HasOne(c => c.Consumer)
                .WithMany()
                .HasForeignKey(c => c.ConsumerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Business)
                .WithMany(b => b.Complaints)
                .HasForeignKey(c => c.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Category)
                .WithMany(c => c.Complaints)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.AssignedAdvocate)
                .WithMany()
                .HasForeignKey(c => c.AssignedAdvocateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ComplaintMessage>(e =>
        {
            e.HasOne(m => m.Complaint)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Author)
                .WithMany()
                .HasForeignKey(m => m.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ComplaintAttachment>(e =>
        {
            e.HasOne(a => a.Complaint)
                .WithMany(c => c.Attachments)
                .HasForeignKey(a => a.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.UploadedBy)
                .WithMany()
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.Complaint)
                .WithMany(c => c.Notifications)
                .HasForeignKey(n => n.ComplaintId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
