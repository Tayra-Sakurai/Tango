// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using Maizuru.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace Maizuru.Factories
{
    public class FakeMaizuruContextFactory : IDesignTimeDbContextFactory<MaizuruContext>
    {
        public MaizuruContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<MaizuruContext> optionsBuilder = new();
            optionsBuilder.UseSqlite("Data Source=Database.db");

            return new(optionsBuilder.Options);
        }
    }
}
