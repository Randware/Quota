using Microsoft.EntityFrameworkCore;

namespace Database.Model;
public class QuotaContext : DbContext
{
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<Quotee> Quotees { get; set; }
    public DbSet<QuoteQuotee> QuoteQuotees { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Guild> Guilds { get; set; }
    public DbSet<GuildConfig> GuildConfigs { get; set; }
    public DbSet<AllowedChannel> AllowedChannels { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Session> Sessions { get; set; }

    /// <summary>
    /// Create a new DbContext with the provided options
    /// </summary>
    public QuotaContext(DbContextOptions<QuotaContext> options) : base(options) { }

    /// <summary>
    /// Create a new DbContext with default options 
    /// </summary>
    public QuotaContext() : base(new DbContextOptionsBuilder<QuotaContext>()
            .UseSqlite("Data Source=database.db")
            .Options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Quote
        modelBuilder.Entity<Quote>(b =>
        {
            b.HasKey(q => q.ID);
            b.Property(q => q.MessageID).IsRequired();
            b.Property(q => q.Content).IsRequired();
            b.Property(q => q.Upvotes).IsRequired();
            b.Property(q => q.Downvotes).IsRequired();
            b.Property(q => q.CreatedAt);

            b.HasOne(q => q.Guild)
             .WithMany(g => g.Quotes)
             .HasForeignKey(q => q.GuildID)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(q => q.SubmittedBy)
             .WithMany(u => u.QuotesSubmitted)
             .HasForeignKey(q => q.SubmittedByID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Many-to-Many: Quote <-> Quotee
        modelBuilder.Entity<QuoteQuotee>(b =>
        {
            b.HasKey(qq => new { qq.QuoteID, qq.QuoteeID });

            b.HasOne(qq => qq.Quote)
             .WithMany(q => q.QuoteQuotees)
             .HasForeignKey(qq => qq.QuoteID);

            b.HasOne(qq => qq.Quotee)
             .WithMany(qe => qe.QuoteQuotees)
             .HasForeignKey(qq => qq.QuoteeID);
        });

        // Quotee
        modelBuilder.Entity<Quotee>(b =>
        {
            b.HasKey(qe => qe.ID);
            b.Property(qe => qe.Name).IsRequired();

            b.HasOne(qe => qe.User)
             .WithMany(u => u.QuoteeProfiles)
             .HasForeignKey(qe => qe.UserID)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // User
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.ID);
            b.Property(u => u.DiscordID).IsRequired();
        });

        // Guild
        modelBuilder.Entity<Guild>(b =>
        {
            b.HasKey(g => g.ID);
            b.Property(g => g.DiscordID).IsRequired();

            b.HasOne(g => g.Config)
             .WithOne(cfg => cfg.Guild)
             .HasForeignKey<Guild>(g => g.ConfigID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // GuildConfig
        modelBuilder.Entity<GuildConfig>(b =>
        {
            b.HasKey(cfg => cfg.ID);
            b.Property(cfg => cfg.UpvoteEmoji).IsRequired();
            b.Property(cfg => cfg.DownvoteEmoji).IsRequired();
            b.Property(cfg => cfg.LockAllowedChannels).IsRequired();
            b.Property(cfg => cfg.Comments).IsRequired();
        });

        // AllowedChannel
        modelBuilder.Entity<AllowedChannel>(b =>
        {
            b.HasKey(ac => new { ac.GuildConfigID, ac.Channel });
            b.Property(ac => ac.Channel).IsRequired();

            b.HasOne(ac => ac.GuildConfig)
             .WithMany(cfg => cfg.Guild.AllowedChannels)
             .HasForeignKey(ac => ac.GuildConfigID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Permission
        modelBuilder.Entity<Permission>(b =>
        {
            b.HasKey(p => new { p.GuildConfigID, p.Role, p.PermissionType });
            b.Property(p => p.Role).IsRequired();
            b.Property(p => p.PermissionType).HasConversion<string>();

            b.HasOne(p => p.GuildConfig)
             .WithMany(cfg => cfg.Guild.Permissions)
             .HasForeignKey(p => p.GuildConfigID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // DiscordToken
        modelBuilder.Entity<DiscordToken>(b =>
        {
            b.HasKey(dt => dt.ID);
            b.Property(dt => dt.DiscordID).IsRequired();
            b.Property(dt => dt.AccessToken).IsRequired();
            b.Property(dt => dt.RefreshToken).IsRequired();
            b.Property(dt => dt.ExpiresAt).IsRequired();
            b.Property(dt => dt.CreatedAt).IsRequired();
            b.HasIndex(dt => dt.DiscordID).IsUnique();
        });

        // Session
        modelBuilder.Entity<Session>(b =>
        {
            b.HasKey(s => s.ID);
            b.Property(s => s.RefreshToken).IsRequired();
            b.Property(s => s.Revoked).IsRequired();
            b.Property(s => s.ExpiresAt).IsRequired();
            b.Property(s => s.CreatedAt).IsRequired();

            b.HasOne(s => s.Token)
                       .WithMany(dt => dt.Sessions)
                       .HasForeignKey(s => s.TokenID)
                       .IsRequired()
                       .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
