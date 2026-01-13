using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TallyJ4.Domain.Identity;
using TallyJ4.Domain.Entities;

namespace TallyJ4.EF.Context;

public partial class MainDbContext : IdentityDbContext<AppUser>
{
    public MainDbContext(DbContextOptions<MainDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ballot> Ballots { get; set; }

    public virtual DbSet<Election> Elections { get; set; }

    public virtual DbSet<ImportFile> ImportFiles { get; set; }

    public virtual DbSet<JoinElectionUser> JoinElectionUsers { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<OnlineVoter> OnlineVoters { get; set; }

    public virtual DbSet<OnlineVotingInfo> OnlineVotingInfos { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<Result> Results { get; set; }

    public virtual DbSet<ResultSummary> ResultSummaries { get; set; }

    public virtual DbSet<ResultTie> ResultTies { get; set; }

    public virtual DbSet<SmsLog> SmsLogs { get; set; }

    public virtual DbSet<Teller> Tellers { get; set; }

    public virtual DbSet<TwoFactorToken> TwoFactorTokens { get; set; }

    public virtual DbSet<Vote> Votes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ballot>(entity =>
        {
            entity.Property(e => e.BallotCode).HasComputedColumnSql("([ComputerCode]+CONVERT([varchar],[BallotNumAtComputer],(0)))", true);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Location).WithMany(p => p.Ballots)
                .HasPrincipalKey(p => p.LocationGuid)
                .HasForeignKey(d => d.LocationGuid)
                .HasConstraintName("FK_Ballot_Location1");
        });

        modelBuilder.Entity<Election>(entity =>
        {
            entity.Property(e => e.ElectionGuid).HasDefaultValueSql("(CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0)))");
            entity.Property(e => e.OnlineCloseIsEstimate).HasDefaultValue(true);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<ImportFile>(entity =>
        {
            entity.Property(e => e.FileSize).HasComputedColumnSql("(datalength([Contents]))", false);
            entity.Property(e => e.HasContent).HasComputedColumnSql("(CONVERT([bit],case when [Contents] IS NULL then (0) else (1) end,(0)))", false);

            entity.HasOne(d => d.Election).WithMany(p => p.ImportFiles)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .HasConstraintName("FK_ImportFile_Election");
        });

        modelBuilder.Entity<JoinElectionUser>(entity =>
        {
            entity.HasOne(d => d.Election).WithMany(p => p.JoinElectionUsers)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .HasConstraintName("FK_JoinElectionUser_Election");

            // entity.HasOne(d => d.User).WithMany(p => p.JoinElectionUsers)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_JoinElectionUser_Users");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.RowId).HasName("PK_VotingLocation");

            entity.HasOne(d => d.Election).WithMany(p => p.Locations)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .HasConstraintName("FK_Location_Election");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.Property(e => e.AsOf).HasDefaultValueSql("(getdate())");
        });


        modelBuilder.Entity<Message>(entity =>
        {
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Election).WithMany(p => p.Messages)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .HasConstraintName("FK_Message_Election");
        });

        modelBuilder.Entity<OnlineVoter>(entity =>
        {
            entity.Property(e => e.VoterIdType).HasDefaultValue("E");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasIndex(e => new { e.ElectionGuid, e.Email }, "IX_PersonEmail")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL AND [Email]<>'')");

            entity.HasIndex(e => new { e.ElectionGuid, e.Phone }, "IX_PersonPhone")
                .IsUnique()
                .HasFilter("([Phone] IS NOT NULL AND [Phone]<>'')");

            entity.Property(e => e.FullName).HasComputedColumnSql("((((([LastName]+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+', ')+coalesce([FirstName],''))+coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))", true);
            entity.Property(e => e.FullNameFl).HasComputedColumnSql("((((coalesce([FirstName]+' ','')+[LastName])+coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))", true);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.RowVersionInt).HasComputedColumnSql("(CONVERT([bigint],[_RowVersion],(0)))", false);

            entity.HasOne(d => d.Election).WithMany(p => p.People)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .HasConstraintName("FK_Person_Election");
        });


        modelBuilder.Entity<Result>(entity =>
        {
            entity.Property(e => e.Section).IsFixedLength();

            entity.HasOne(d => d.Election).WithMany(p => p.Results)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Result_Election");

            entity.HasOne(d => d.Person).WithMany(p => p.Results)
                .HasPrincipalKey(p => p.PersonGuid)
                .HasForeignKey(d => d.PersonGuid)
                .HasConstraintName("FK_Result_Person");
        });

        modelBuilder.Entity<ResultSummary>(entity =>
        {
            entity.Property(e => e.ResultType).IsFixedLength();

            entity.HasOne(d => d.Election).WithMany(p => p.ResultSummaries)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .HasConstraintName("FK_ResultSummary_Election1");
        });

        modelBuilder.Entity<ResultTie>(entity =>
        {
            entity.HasOne(d => d.Election).WithMany(p => p.ResultTies)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .HasConstraintName("FK_ResultTie_Election");
        });

        modelBuilder.Entity<Teller>(entity =>
        {
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Election).WithMany(p => p.Tellers)
                .HasPrincipalKey(p => p.ElectionGuid)
                .HasForeignKey(d => d.ElectionGuid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teller_Election");
        });

        modelBuilder.Entity<TwoFactorToken>(entity =>
        {
            entity.Property(e => e.TokenGuid).HasDefaultValueSql("(CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0)))");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne<AppUser>()
                .WithOne(u => u.TwoFactorToken)
                .HasForeignKey<TwoFactorToken>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Ballot).WithMany(p => p.Votes)
                .HasPrincipalKey(p => p.BallotGuid)
                .HasForeignKey(d => d.BallotGuid)
                .HasConstraintName("FK_Vote_Ballot");

            entity.HasOne(d => d.Person).WithMany(p => p.Votes)
                .HasPrincipalKey(p => p.PersonGuid)
                .HasForeignKey(d => d.PersonGuid)
                .HasConstraintName("FK_Vote_Person1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
