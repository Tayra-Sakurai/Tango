// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later
using Maizuru.Converters;
using Maizuru.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Maizuru.Contexts
{
    public class MaizuruContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }

        public MaizuruContext(DbContextOptions<MaizuruContext> options)
            : base(options)
        {
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<float[]>()
                .HaveConversion<VectorConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(
                t =>
                {
                    t.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.Categories)
                    .OnDelete(DeleteBehavior.SetNull);
                });
            modelBuilder.Entity<Item>();
            modelBuilder.Entity<PaymentMethod>();
        }
    }
}
